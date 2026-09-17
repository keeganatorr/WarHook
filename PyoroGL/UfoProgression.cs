using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;

namespace MonogameTest
{
    enum UfoUpgradeEffect { Fire, Tractor, Engine, Hull, Capacity, Growth, StartingBonus, BeamWidth, Repair }

    sealed record UfoUpgrade(string Id, string Title, string ShortName, string Description,
        UfoUpgradeEffect Effect, int BaseCost, int MaxRank, int Parent = -1, int RequiredRank = 1);

    sealed class UfoProgression
    {
        public static readonly UfoUpgrade[] Nodes = {
            new("fire", "RAPID FIRE", "FIRE", "25% MORE BASE FIRE RATE PER RANK", UfoUpgradeEffect.Fire, 3, 5),
            new("tractor", "TRACTOR DRIVE", "TRACTOR", "25% MORE BASE LIFT AND PULL PER RANK", UfoUpgradeEffect.Tractor, 3, 5),
            new("engine", "THRUSTERS", "ENGINES", "20% MORE BASE FLIGHT SPEED PER RANK", UfoUpgradeEffect.Engine, 3, 5),
            new("hull", "HULL PLATING", "HULL", "25 MORE MAX HULL HEALTH PER RANK", UfoUpgradeEffect.Hull, 3, 5),
            new("fire2", "PULSE ACCELERATOR", "PULSE", "25% MORE BASE FIRE RATE PER RANK", UfoUpgradeEffect.Fire, 12, 5, 0, 2),
            new("capacity2", "DUAL ABDUCTION", "BEAM 2", "CARRY TWO PEOPLE IN ONE BEAM", UfoUpgradeEffect.Capacity, 12, 1, 1, 2),
            new("engine2", "ION ENGINES", "ION", "20% MORE BASE FLIGHT SPEED PER RANK", UfoUpgradeEffect.Engine, 12, 5, 2, 2),
            new("repair", "ENGINEER TOOLS", "REPAIR", "ENGINEERS REPAIR 5 MORE HULL PER RANK", UfoUpgradeEffect.Repair, 12, 5, 3, 2),
            new("growth", "SURVIVAL DIVIDEND", "YIELD", "25% FASTER MULTIPLIER GROWTH PER RANK", UfoUpgradeEffect.Growth, 20, 5, 4),
            new("capacity3", "TRIPLE ABDUCTION", "BEAM 3", "CARRY THREE PEOPLE IN ONE BEAM", UfoUpgradeEffect.Capacity, 25, 1, 5),
            new("width", "WIDE APERTURE", "WIDTH", "10% WIDER TRACTOR CONE PER RANK", UfoUpgradeEffect.BeamWidth, 15, 5, 6),
            new("hull2", "REINFORCED FRAME", "ARMOR", "25 MORE MAX HULL HEALTH PER RANK", UfoUpgradeEffect.Hull, 20, 5, 7),
            new("fire3", "PARTICLE ARRAY", "ARRAY", "25% MORE BASE FIRE RATE PER RANK", UfoUpgradeEffect.Fire, 35, 5, 8),
            new("capacity4", "QUAD ABDUCTION", "BEAM 4", "CARRY FOUR PEOPLE IN ONE BEAM", UfoUpgradeEffect.Capacity, 45, 1, 9),
            new("growth2", "COMPOUND RETURNS", "YIELD II", "25% FASTER MULTIPLIER GROWTH PER RANK", UfoUpgradeEffect.Growth, 30, 5, 10),
            new("start", "LAUNCH DIVIDEND", "BONUS", "START MULTIPLIER 0.10X HIGHER PER RANK", UfoUpgradeEffect.StartingBonus, 35, 5, 11),
            new("growth3", "LONG HAUL", "YIELD III", "25% FASTER MULTIPLIER GROWTH PER RANK", UfoUpgradeEffect.Growth, 60, 5, 12),
            new("capacity5", "FLEET ABDUCTION", "BEAM 5", "CARRY FIVE PEOPLE IN ONE BEAM", UfoUpgradeEffect.Capacity, 80, 1, 13),
            new("engine3", "WARP THRUSTERS", "WARP", "20% MORE BASE FLIGHT SPEED PER RANK", UfoUpgradeEffect.Engine, 55, 5, 14),
            new("start2", "COLONY DIVIDEND", "BONUS II", "START MULTIPLIER 0.10X HIGHER PER RANK", UfoUpgradeEffect.StartingBonus, 65, 5, 15)
        };
        const string SaveKey = "warhook.ufo.progression.v1";
        readonly bool memoryOnly;
        readonly string path;
        Dictionary<string, int> ranks = new();
        string lastRound = "";
        public double Balance { get; private set; }
        public string Error { get; private set; } = "";

        public UfoProgression(bool memoryOnly = false, string savePath = null)
        {
            this.memoryOnly = memoryOnly;
            path = savePath ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Warhook", "ufo-progression.json");
            if (memoryOnly) return;
            try
            {
#if WEB
                string saved = WebInterop.LoadSave(SaveKey);
#else
                string saved = File.Exists(path) ? File.ReadAllText(path) : null;
#endif
                if (string.IsNullOrWhiteSpace(saved)) return;
                using var json = JsonDocument.Parse(saved);
                var root = json.RootElement;
                if (root.TryGetProperty("balance", out var balance) && balance.TryGetDouble(out double amount) && double.IsFinite(amount))
                    Balance = Math.Clamp(Math.Round(amount, 2), 0, 1_000_000_000);
                if (root.TryGetProperty("lastRound", out var last) && last.ValueKind == JsonValueKind.String) lastRound = last.GetString();
                if (root.TryGetProperty("ranks", out var levels) && levels.ValueKind == JsonValueKind.Object)
                    foreach (var node in Nodes)
                        if (levels.TryGetProperty(node.Id, out var level) && level.TryGetInt32(out int rank))
                            ranks[node.Id] = Math.Clamp(rank, 0, node.MaxRank);
            }
            catch (Exception) { Error = "COULD NOT READ UPGRADE SAVE"; }
        }

        public int Rank(int index) => ranks.TryGetValue(Nodes[index].Id, out int rank) ? rank : 0;
        public int Total(UfoUpgradeEffect effect)
        {
            int sum = 0;
            for (int i = 0; i < Nodes.Length; i++) if (Nodes[i].Effect == effect) sum += Rank(i);
            return sum;
        }
        public int Cost(int index) => Nodes[index].BaseCost * (Rank(index) + 1) * (Rank(index) + 1);
        public bool Unlocked(int index) => Nodes[index].Parent < 0 || Rank(Nodes[index].Parent) >= Nodes[index].RequiredRank;
        public bool CanBuy(int index) => Unlocked(index) && Rank(index) < Nodes[index].MaxRank && Balance >= Cost(index);
        public bool Buy(int index)
        {
            if (!CanBuy(index)) return false;
            var nextRanks = new Dictionary<string, int>(ranks) { [Nodes[index].Id] = Rank(index) + 1 };
            return Commit(Balance - Cost(index), nextRanks, lastRound);
        }
        public bool Bank(string roundId, double reward)
        {
            if (lastRound == roundId) return true;
            return Commit(Math.Min(1_000_000_000, Balance + Math.Max(0, reward)), ranks, roundId);
        }
        bool Commit(double balance, Dictionary<string, int> levels, string roundId)
        {
            balance = Math.Round(balance, 2, MidpointRounding.AwayFromZero);
            try
            {
                if (!memoryOnly)
                {
                    using var stream = new MemoryStream();
                    using (var json = new Utf8JsonWriter(stream))
                    {
                        json.WriteStartObject(); json.WriteNumber("version", 1); json.WriteNumber("balance", balance);
                        json.WriteString("lastRound", roundId); json.WriteStartObject("ranks");
                        foreach (var rank in levels) json.WriteNumber(rank.Key, rank.Value);
                        json.WriteEndObject(); json.WriteEndObject();
                    }
                    string saved = Encoding.UTF8.GetString(stream.ToArray());
#if WEB
                    WebInterop.SaveSave(SaveKey, saved);
#else
                    Directory.CreateDirectory(Path.GetDirectoryName(path));
                    File.WriteAllText(path + ".tmp", saved);
                    File.Move(path + ".tmp", path, true);
#endif
                }
                Balance = balance; ranks = levels; lastRound = roundId; Error = "";
                return true;
            }
            catch (Exception) { Error = "SAVE FAILED - RETRY"; return false; }
        }
    }
}
