using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace MonogameTest
{
    // Keeps disk writes off the game thread and replaces complete save files atomically.
    sealed class HighScoreStore
    {
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
