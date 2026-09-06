using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace MonogameTest
{
    // Keeps disk writes off the game thread and replaces complete save files atomically.
    sealed class HighScoreStore
    {
        public sealed record Entry(string Id, string Initials, int Score);
        readonly List<Entry>[] tables = { new List<Entry>(), new List<Entry>() };
        readonly string path;
        readonly object gate = new object();
        readonly int[] scores = { 10000, 10000 };
        Task writer = Task.CompletedTask;
        bool dirty;

        public HighScoreStore(string path)
        {
            this.path = path;
            try
            {
                if (!File.Exists(path)) return;
                using var json = JsonDocument.Parse(File.ReadAllText(path));
                if (json.RootElement.ValueKind != JsonValueKind.Object) return;
                ReadScore(json.RootElement, "GameA", 0);
                ReadScore(json.RootElement, "GameB", 1);
                ReadTable(json.RootElement, "ScoresA", 0);
                ReadTable(json.RootElement, "ScoresB", 1);
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException || e is JsonException)
            {
                Console.Error.WriteLine("Could not load high scores: " + e.Message);
            }
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
                if (writer.IsCompleted) writer = Task.Run(WritePending);
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
                if (writer.IsCompleted) writer = Task.Run(WritePending);
                return scores[mode];
            }
        }

        void WritePending()
        {
            while (true)
            {
                int gameA, gameB;
                Entry[] tableA, tableB;
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
                    tableA = tables[0].ToArray();
                    tableB = tables[1].ToArray();
                    dirty = false;
                }
                string temporary = path + ".tmp";
                try
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(path));
                    using (var stream = File.Create(temporary))
                    {
                        using (var json = new Utf8JsonWriter(stream))
                        {
                            json.WriteStartObject();
                            json.WriteNumber("GameA", gameA);
                            json.WriteNumber("GameB", gameB);
                            WriteTable(json, "ScoresA", tableA);
                            WriteTable(json, "ScoresB", tableB);
                            json.WriteEndObject();
                            json.Flush();
                        }
                        stream.Flush(true);
                    }
                    File.Move(temporary, path, true);
                }
                catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
                {
                    Console.Error.WriteLine("Could not save high scores: " + e.Message);
                }
            }
        }

        public void Flush()
        {
            Task pending;
            lock (gate) pending = writer;
            pending.GetAwaiter().GetResult();
        }
    }
}
