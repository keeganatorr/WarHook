using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;

namespace MonogameTest
{
    // Optional online leaderboards backed by a Supabase project (plain REST
    // calls to the auto-generated PostgREST API — no SDK dependency, works on
    // desktop and in the browser).
    //
    // Config is a supabase.json {"Url": ..., "AnonKey": ...} embedded into the
    // assembly at build time by setup-leaderboards.sh (git-ignored, so no
    // plaintext key file ships in release outputs). Desktop also accepts a
    // supabase.json next to the executable, and WARHOOK_SUPABASE_URL /
    // WARHOOK_SUPABASE_ANON_KEY override everything. The anon key is public
    // by design; the server-side RPC validates submissions and rate-limits
    // them (see supabase/schema.sql).
    //
    // Every failure is non-fatal: the game falls back to local scores.
    static class OnlineScores
    {
        public enum Status { NotFetched, Loading, Loaded, Failed }

        public sealed record Entry(string Initials, int Score, bool IsMine);

        // Immutable snapshot; workers replace the array slot, the UI reads it.
        public sealed record Snapshot(Status Status, Entry[] Entries, DateTime FetchedAt);

        sealed record Config(string Url, string AnonKey);

        static HttpClient http;
        static readonly Snapshot[] snapshots = new Snapshot[2]; // [0]=Game A, [1]=Game B
        static string playerId;

        public static bool Enabled { get; private set; }

        public static void Initialize(string persistentPlayerId)
        {
            playerId = persistentPlayerId;
            try
            {
                // Release builds embed supabase.json. The desktop sidecar is
                // deliberately a fallback so local development can change
                // endpoints without rebuilding; environment variables win over
                // both sources and are useful for smoke tests/CI.
                string json = TryReadConfigText();
                var config = string.IsNullOrWhiteSpace(json)
                    ? null
                    : JsonSerializer.Deserialize<Config>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                string url = FirstNonBlank(Environment.GetEnvironmentVariable("WARHOOK_SUPABASE_URL"), config?.Url);
                string key = FirstNonBlank(Environment.GetEnvironmentVariable("WARHOOK_SUPABASE_ANON_KEY"), config?.AnonKey);

                if (!Uri.TryCreate(url?.TrimEnd('/'), UriKind.Absolute, out var baseUri) ||
                    !string.Equals(baseUri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase) ||
                    string.IsNullOrWhiteSpace(baseUri.Host) || string.IsNullOrWhiteSpace(key))
                    return;

                http = new HttpClient { BaseAddress = new Uri(baseUri + "/"), Timeout = TimeSpan.FromSeconds(8) };
                http.DefaultRequestHeaders.Add("apikey", key);
                http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", key);
                http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                Enabled = true;
            }
            catch (Exception e)
            {
                Console.Error.WriteLine("Online leaderboards disabled: " + e.Message);
                Enabled = false;
            }
        }

        static string FirstNonBlank(string preferred, string fallback) =>
            string.IsNullOrWhiteSpace(preferred) ? fallback : preferred.Trim();

        static string TryReadConfigText()
        {
            string embedded = TryReadEmbeddedConfig();
            if (!string.IsNullOrWhiteSpace(embedded) || GameAssets.IsWeb)
                return embedded;

            // Dev convenience: plain file next to the desktop executable, no
            // rebuild. This is never used by the browser build.
            string path = Path.Combine(AppContext.BaseDirectory, "supabase.json");
            return File.Exists(path) ? File.ReadAllText(path) : null;
        }

        // supabase.json embedded into the entry assembly at build time.
        static string TryReadEmbeddedConfig()
        {
            try
            {
                var assembly = System.Reflection.Assembly.GetEntryAssembly();
                if (assembly == null) return null;
                foreach (string name in assembly.GetManifestResourceNames())
                {
                    if (!name.EndsWith("supabase.json", StringComparison.OrdinalIgnoreCase)) continue;
                    using (var stream = assembly.GetManifestResourceStream(name))
                    using (var reader = new StreamReader(stream))
                        return reader.ReadToEnd();
                }
            }
            catch (Exception)
            {
                // Missing/unreadable resource = feature disabled.
            }
            return null;
        }

        static string ModeName(bool gameB) => gameB ? "game_b" : "game_a";

        // Fire-and-forget submission; HighScoreStore notifies us on saves.
        public static void QueueSubmit(bool gameB, string initials, int score)
        {
            if (!Enabled) return;
            string mode = ModeName(gameB);
            _ = Task.Run(async () =>
            {
                try
                {
                    using var content = new StringContent(
                        JsonSerializer.Serialize(new Dictionary<string, object>
                        {
                            ["p_mode"] = mode,
                            ["p_initials"] = initials,
                            ["p_score"] = score,
                            ["p_player_id"] = playerId
                        }),
                        Encoding.UTF8, "application/json");
                    using var response = await http.PostAsync("rest/v1/rpc/submit_score", content);
                    if (!response.IsSuccessStatusCode)
                        Console.Error.WriteLine("Online score submit failed: HTTP " + (int)response.StatusCode);
                    else
                    {
                        int index = gameB ? 1 : 0;
                        var current = snapshots[index];
                        if (current == null)
                            snapshots[index] = null;
                        else if (current.Status == Status.Loaded)
                            snapshots[index] = current with { FetchedAt = DateTime.MinValue };
                    }
                }
                catch (Exception e)
                {
                    Console.Error.WriteLine("Online score submit failed: " + e.Message);
                }
            });
        }

        // Kick off a leaderboard fetch on a worker; the UI polls Snapshot().
        public static void BeginFetch(bool gameB)
        {
            if (!Enabled) return;
            int index = gameB ? 1 : 0;
            var current = snapshots[index];
            if (current != null && current.Status == Status.Loading) return;

            snapshots[index] = new Snapshot(Status.Loading,
                current?.Entries ?? Array.Empty<Entry>(), DateTime.UtcNow);
            string mode = ModeName(gameB);
            _ = Task.Run(async () =>
            {
                Snapshot result;
                try
                {
                    using var response = await http.GetAsync(
                        "rest/v1/rpc/top_scores?p_mode=" + Uri.EscapeDataString(mode) +
                        "&p_player_id=" + Uri.EscapeDataString(playerId ?? ""));
                    response.EnsureSuccessStatusCode();
                    var json = await response.Content.ReadAsStringAsync();
                    var rows = JsonSerializer.Deserialize<Row[]>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                        ?? Array.Empty<Row>();
                    var entries = new Entry[rows.Length];
                    for (int i = 0; i < rows.Length; i++)
                        entries[i] = new Entry(rows[i].Initials, rows[i].Score, rows[i].IsMine);
                    result = new Snapshot(Status.Loaded, entries, DateTime.UtcNow);
                }
                catch (Exception)
                {
                    result = new Snapshot(Status.Failed, Array.Empty<Entry>(), DateTime.UtcNow);
                }
                snapshots[index] = result;
            });
        }

        sealed record Row(string Initials, int Score,
            [property: JsonPropertyName("is_mine")] bool IsMine);

        public static Snapshot GetSnapshot(bool gameB) => snapshots[gameB ? 1 : 0];

        // A loaded board goes stale after this long; a failed one waits out the
        // shorter cooldown. The score screen polls every frame, so without the
        // failure cooldown a dead connection would refetch 60 times a second.
        const double StaleMinutes = 5;
        const double RetryFailedSeconds = 10;

        // True when the visible snapshot is missing, stale, or the last fetch
        // failed long enough ago to be worth retrying.
        public static bool ShouldFetch(bool gameB)
        {
            if (!Enabled) return false;
            var snapshot = GetSnapshot(gameB);
            if (snapshot == null) return true;
            var age = DateTime.UtcNow - snapshot.FetchedAt;
            if (snapshot.Status == Status.Failed) return age.TotalSeconds >= RetryFailedSeconds;
            return snapshot.Status == Status.Loaded && age.TotalMinutes >= StaleMinutes;
        }
    }
}
