using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
#if WEB
using System.Runtime.InteropServices.JavaScript;
#endif

namespace MonogameTest
{
    // Reads and writes the complete save document through a SaveBackend.
    // Desktop writes go to a worker thread and replace save files atomically;
    // the web backend (browser localStorage) must run on the main thread.
    sealed class HighScoreStore
    {
        // Optional hook: notified with (gameB, initials, score) whenever a
        // score is saved. Game1 wires it to the online leaderboard service.
        public static Action<bool, string, int> OnlineSubmitHook;

        interface SaveBackend
        {
            bool SyncWrites { get; }
            string Read();
            void Write(string json);
        }

        sealed class FileSaveBackend : SaveBackend
        {
            readonly string path;
            public FileSaveBackend(string path) { this.path = path; }
            public bool SyncWrites => false;
            public string Read()
            {
                return !File.Exists(path) ? null : File.ReadAllText(path);
            }
            public void Write(string json)
            {
                string temporary = path + ".tmp";
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                File.WriteAllText(temporary, json);
                File.Move(temporary, path, true);
            }
        }

#if WEB
        sealed class WebSaveBackend : SaveBackend
        {
            const string Key = "warhook.ufo.highscores";
            public bool SyncWrites => true;
            public string Read() => WebInterop.LoadSave(Key);
            public void Write(string json) => WebInterop.SaveSave(Key, json);
        }
#endif

        public sealed record Entry(string Id, string Initials, int Score);
        readonly List<Entry>[] tables = { new List<Entry>(), new List<Entry>() };
        readonly SaveBackend backend;
        readonly object gate = new object();
        readonly int[] scores = { 10000, 10000 };
        public string PlayerId { get; private set; } = Guid.NewGuid().ToString("N");
        Task writer = Task.CompletedTask;
        bool dirty;

        public HighScoreStore(string path)
        {
#if WEB
            backend = OperatingSystem.IsBrowser() ? new WebSaveBackend() : new FileSaveBackend(path);
#else
            backend = new FileSaveBackend(path);
#endif
            try
            {
                string saved = backend.Read();
                if (saved == null) return;
                using var json = JsonDocument.Parse(saved);
                if (json.RootElement.ValueKind != JsonValueKind.Object) return;
                if (json.RootElement.TryGetProperty("PlayerId", out var playerId) &&
                    playerId.ValueKind == JsonValueKind.String &&
                    Guid.TryParseExact(playerId.GetString(), "N", out _))
                    PlayerId = playerId.GetString().ToLowerInvariant();
                ReadScore(json.RootElement, "GameA", 0);
                ReadScore(json.RootElement, "GameB", 1);
                ReadTable(json.RootElement, "ScoresA", 0);
                ReadTable(json.RootElement, "ScoresB", 1);
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException || e is JsonException)
            {
                Console.Error.WriteLine("Could not load high scores: " + e.Message);
            }
#if WEB
            catch (Exception e) when (backend is WebSaveBackend)
            {
                // Don't let a storage failure break the game.
                Console.Error.WriteLine("Could not load high scores: " + e.Message);
            }
#endif
        }

        void ReadScore(JsonElement root, string name, int mode)
        {
            if (root.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.Number &&
                value.TryGetInt32(out int score) && score >= 0)
                scores[mode] = Math.Max(scores[mode], score);
        }

        void ReadTable(JsonElement root, string name, int mode)
        {
            if (!root.TryGetProperty(name, out var rows))
            {
                // Migrate an existing personal best without inventing a default entry.
                string legacy = mode == 0 ? "GameA" : "GameB";
                if (root.TryGetProperty(legacy, out var best) && best.ValueKind == JsonValueKind.Number && best.TryGetInt32(out int value) && value > 10000)
                    tables[mode].Add(new Entry(Guid.NewGuid().ToString("N"), "OLD", value));
                return;
            }
            if (rows.ValueKind != JsonValueKind.Array) return;
            foreach (var row in rows.EnumerateArray())
            {
                if (row.ValueKind != JsonValueKind.Object ||
                    !row.TryGetProperty("Score", out var score) || score.ValueKind != JsonValueKind.Number ||
                    !score.TryGetInt32(out int value) || value < 0) continue;
                string initials = row.TryGetProperty("Initials", out var text) && text.ValueKind == JsonValueKind.String
                    ? text.GetString() : "AAA";
                string id = row.TryGetProperty("Id", out var savedId) && savedId.ValueKind == JsonValueKind.String
                    ? savedId.GetString() : Guid.NewGuid().ToString("N");
                tables[mode].Add(new Entry(id, NormalizeInitials(initials), value));
                scores[mode] = Math.Max(scores[mode], value);
            }
            tables[mode] = tables[mode].OrderByDescending(e => e.Score).Take(10).ToList();
        }

        static string NormalizeInitials(string text) =>
            new string((text ?? "").ToUpperInvariant().Where(c => c >= 'A' && c <= 'Z' || c >= '0' && c <= '9')
                .Take(3).ToArray()).PadRight(3, 'A');

        public Entry[] Entries(bool gameB)
        {
            lock (gate) return tables[gameB ? 1 : 0].ToArray();
        }

        public bool Qualifies(bool gameB, int score)
        {
            lock (gate)
            {
                var table = tables[gameB ? 1 : 0];
                return score >= 0 && (table.Count < 10 || score > table[table.Count - 1].Score);
            }
        }

        public string Submit(bool gameB, string initials, int score)
        {
            lock (gate)
            {
                if (!Qualifies(gameB, score)) return null;
                int mode = gameB ? 1 : 0;
                var entry = new Entry(Guid.NewGuid().ToString("N"), NormalizeInitials(initials), score);
                tables[mode].Add(entry);
                tables[mode] = tables[mode].OrderByDescending(e => e.Score).Take(10).ToList();
                scores[mode] = Math.Max(scores[mode], score);
                dirty = true;
                ScheduleWrite();
                try { OnlineSubmitHook?.Invoke(gameB, entry.Initials, score); }
                catch { /* Online submission must never block saving. */ }
                return entry.Id;
            }
        }

        static void WriteTable(Utf8JsonWriter json, string name, Entry[] entries)
        {
            json.WriteStartArray(name);
            foreach (var entry in entries)
            {
                json.WriteStartObject();
                json.WriteString("Id", entry.Id);
                json.WriteString("Initials", entry.Initials);
                json.WriteNumber("Score", entry.Score);
                json.WriteEndObject();
            }
            json.WriteEndArray();
        }

        public int Get(bool gameB)
        {
            lock (gate) return scores[gameB ? 1 : 0];
        }

        public int Record(bool gameB, int score)
        {
            lock (gate)
            {
                int mode = gameB ? 1 : 0;
                if (score <= scores[mode]) return scores[mode];
                scores[mode] = score;
                dirty = true;
                ScheduleWrite();
                return scores[mode];
            }
        }

        void ScheduleWrite()
        {
            if (backend.SyncWrites)
            {
                // Browser: localStorage only works on the main thread, which is
                // where Submit/Record run. Monitor is re-entrant so WritePending
                // can take the same lock.
                if (dirty) WritePending();
                return;
            }
            if (writer.IsCompleted) writer = Task.Run(WritePending);
        }

        void WritePending()
        {
            while (true)
            {
                int gameA, gameB;
                Entry[] tableA, tableB;
                string playerId;
                lock (gate)
                {
                    if (!dirty)
                    {
                        // Mark idle while holding the same lock used by Record.
                        writer = Task.CompletedTask;
                        return;
                    }
                    gameA = scores[0];
                    gameB = scores[1];
                    playerId = PlayerId;
                    tableA = tables[0].ToArray();
                    tableB = tables[1].ToArray();
                    dirty = false;
                }
                try
                {
                    backend.Write(SerializeSave(playerId, gameA, gameB, tableA, tableB));
                }
                catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
                {
                    Console.Error.WriteLine("Could not save high scores: " + e.Message);
                }
                catch (Exception e) when (backend.SyncWrites)
                {
                    // Web storage failures (JSException etc.) must not kill the game.
                    Console.Error.WriteLine("Could not save high scores: " + e.Message);
                }
            }
        }

        static string SerializeSave(string playerId, int gameA, int gameB, Entry[] tableA, Entry[] tableB)
        {
            using var stream = new MemoryStream();
            using (var json = new Utf8JsonWriter(stream))
            {
                json.WriteStartObject();
                json.WriteString("PlayerId", playerId);
                json.WriteNumber("GameA", gameA);
                json.WriteNumber("GameB", gameB);
                WriteTable(json, "ScoresA", tableA);
                WriteTable(json, "ScoresB", tableB);
                json.WriteEndObject();
                json.Flush();
            }
            return Encoding.UTF8.GetString(stream.ToArray());
        }

        public void Flush()
        {
            Task pending;
            lock (gate) pending = writer;
            pending.GetAwaiter().GetResult();
        }
    }

#if WEB
    // JSImport targets registered by index.html (wwwroot).
    static partial class WebInterop
    {
        [JSImport("globalThis.warhookInterop.loadSave")]
        internal static partial string LoadSave(string key);

        [JSImport("globalThis.warhookInterop.saveSave")]
        internal static partial void SaveSave(string key, string json);
    }
#endif
}
