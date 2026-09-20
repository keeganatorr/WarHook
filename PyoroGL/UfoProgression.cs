using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using Microsoft.Xna.Framework;

namespace MonogameTest
{
    enum UfoUpgradeEffect { Fire, Tractor, Engine, Hull, Capacity, Growth, StartingBonus, BeamWidth, Repair, SoldierValue,
        Core, TwinShot, TripleShot, PointDefense, Plasma, Focus, Matrix, Warp, Nanohull, AutoRepair,
        LongHaul, Interest, Exponential, Prestige, FlightClearance, Shield }
    enum UfoBranch { Core, Weapons, Beam, Ship, Hull, Yield, Hybrid }
    sealed record UfoPrerequisite(string Node, int Rank = 1);
    sealed record UfoUpgrade(string Id, string Title, string ShortName, UfoUpgradeEffect Effect,
        float Amount, int BaseCost, int MaxRank, UfoBranch Branch, Vector2 Position,
        UfoPrerequisite[] Requires, int RequiredCount = 0);

    sealed class UfoProgression
    {
        // Stable IDs preserve existing saves; coordinates and graph edges define
        // the web independently of array order. RequiredCount > 0 means any N.
        public static readonly UfoUpgrade[] Nodes = {
            new("fire", "RAPID FIRE", "RAPID FIRE", UfoUpgradeEffect.Fire, 0.2f, 3, 5, UfoBranch.Weapons, new Vector2(-76, -40), new UfoPrerequisite[] { new("core", 1) }),
            new("tractor", "TRACTOR DRIVE", "TRACTOR", UfoUpgradeEffect.Tractor, 0.2f, 3, 10, UfoBranch.Beam, new Vector2(76, -40), new UfoPrerequisite[] { new("core", 1) }),
            new("engine", "THRUSTERS", "THRUSTERS", UfoUpgradeEffect.Engine, 0.15f, 3, 5, UfoBranch.Ship, new Vector2(-76, 40), new UfoPrerequisite[] { new("core", 1) }),
            new("hull", "HULL PLATING", "HULL", UfoUpgradeEffect.Hull, 25f, 3, 5, UfoBranch.Hull, new Vector2(-76, 106), new UfoPrerequisite[] { new("core", 1) }),
            new("fire2", "PULSE ACCELERATOR", "PULSE", UfoUpgradeEffect.Fire, 0.15f, 10, 5, UfoBranch.Weapons, new Vector2(-155, -85), new UfoPrerequisite[] { new("fire", 2) }),
            new("capacity2", "DUAL ABDUCTION", "BEAM II", UfoUpgradeEffect.Capacity, 2f, 12, 1, UfoBranch.Beam, new Vector2(155, -105), new UfoPrerequisite[] { new("tractor", 2) }),
            new("engine2", "ION ENGINES", "ION", UfoUpgradeEffect.Engine, 0.15f, 12, 5, UfoBranch.Ship, new Vector2(-155, 40), new UfoPrerequisite[] { new("engine", 2) }),
            new("repair", "ENGINEER TOOLS", "REPAIR", UfoUpgradeEffect.Repair, 5f, 12, 5, UfoBranch.Hull, new Vector2(-155, 164), new UfoPrerequisite[] { new("hull", 2) }),
            new("growth", "SURVIVAL DIVIDEND", "SURVIVAL", UfoUpgradeEffect.Growth, 0.2f, 8, 5, UfoBranch.Yield, new Vector2(76, 40), new UfoPrerequisite[] { new("core", 1) }),
            new("capacity3", "TRIPLE ABDUCTION", "BEAM III", UfoUpgradeEffect.Capacity, 3f, 30, 1, UfoBranch.Beam, new Vector2(235, -105), new UfoPrerequisite[] { new("capacity2", 1) }),
            new("width", "WIDE APERTURE", "APERTURE", UfoUpgradeEffect.BeamWidth, 0.1f, 12, 5, UfoBranch.Beam, new Vector2(155, -35), new UfoPrerequisite[] { new("tractor", 2) }),
            new("hull2", "REINFORCED FRAME", "FRAME", UfoUpgradeEffect.Hull, 25f, 25, 5, UfoBranch.Hull, new Vector2(-235, 106), new UfoPrerequisite[] { new("hull", 4) }),
            new("fire3", "PARTICLE ARRAY", "ARRAY", UfoUpgradeEffect.TripleShot, 1f, 150, 1, UfoBranch.Weapons, new Vector2(-395, -40), new UfoPrerequisite[] { new("plasma", 5) }),
            new("capacity4", "QUAD ABDUCTION", "BEAM IV", UfoUpgradeEffect.Capacity, 4f, 65, 1, UfoBranch.Beam, new Vector2(315, -70), new UfoPrerequisite[] { new("capacity3", 1), new("focus", 3) }),
            new("growth2", "COMPOUND RETURNS", "COMPOUND", UfoUpgradeEffect.Growth, 0.2f, 20, 5, UfoBranch.Yield, new Vector2(155, 40), new UfoPrerequisite[] { new("growth", 3) }),
            new("start", "LAUNCH DIVIDEND", "LAUNCH", UfoUpgradeEffect.StartingBonus, 0.1f, 30, 5, UfoBranch.Yield, new Vector2(155, 106), new UfoPrerequisite[] { new("growth", 3) }),
            new("growth3", "LONG HAUL", "LONG HAUL", UfoUpgradeEffect.LongHaul, 0.15f, 55, 5, UfoBranch.Yield, new Vector2(235, 40), new UfoPrerequisite[] { new("growth2", 1) }),
            new("capacity5", "FLEET ABDUCTION", "FLEET", UfoUpgradeEffect.Capacity, 5f, 175, int.MaxValue, UfoBranch.Beam, new Vector2(395, -145), new UfoPrerequisite[] { new("matrix", 3) }),
            new("engine3", "WARP CORE", "WARP", UfoUpgradeEffect.Warp, 0.25f, 60, 3, UfoBranch.Ship, new Vector2(-235, 40), new UfoPrerequisite[] { new("engine2", 4) }),
            new("start2", "COLONY DIVIDEND", "COLONY", UfoUpgradeEffect.StartingBonus, 0.1f, 65, 5, UfoBranch.Yield, new Vector2(235, 106), new UfoPrerequisite[] { new("start", 1) }),
            new("core", "UFO CORE", "UFO CORE", UfoUpgradeEffect.Core, 0f, 0, 1, UfoBranch.Core, new Vector2(0, 0), new UfoPrerequisite[] {  }),
            new("twin", "TWIN CANNONS", "TWIN", UfoUpgradeEffect.TwinShot, 1f, 35, 1, UfoBranch.Weapons, new Vector2(-235, -85), new UfoPrerequisite[] { new("fire2", 3) }),
            new("point", "POINT DEFENCE", "DEFENCE", UfoUpgradeEffect.PointDefense, 3f, 25, 3, UfoBranch.Weapons, new Vector2(-155, -5), new UfoPrerequisite[] { new("fire", 4) }),
            new("plasma", "PLASMA CYCLER", "PLASMA", UfoUpgradeEffect.Plasma, 0.1f, 45, 5, UfoBranch.Weapons, new Vector2(-315, -40), new UfoPrerequisite[] { new("twin", 1), new("point", 1) }),
            new("focus", "BEAM FOCUS", "FOCUS", UfoUpgradeEffect.Focus, 0.25f, 25, 5, UfoBranch.Beam, new Vector2(235, -35), new UfoPrerequisite[] { new("width", 3) }),
            new("matrix", "BEAM MATRIX", "MATRIX", UfoUpgradeEffect.Matrix, 0.15f, 80, 3, UfoBranch.Beam, new Vector2(395, -70), new UfoPrerequisite[] { new("capacity4", 1) }),
            new("nano", "NANOHULL", "NANOHULL", UfoUpgradeEffect.Nanohull, 50f, 85, 3, UfoBranch.Hull, new Vector2(-315, 75), new UfoPrerequisite[] { new("hull2", 1), new("engine3", 1) }),
            new("auto", "AUTO REPAIR", "AUTO REPAIR", UfoUpgradeEffect.AutoRepair, 2f, 200, 1, UfoBranch.Hull, new Vector2(-395, 150), new UfoPrerequisite[] { new("nano", 3), new("repair", 5) }),
            new("interest", "INTEREST ENGINE", "INTEREST", UfoUpgradeEffect.Interest, 0.01f, 100, 5, UfoBranch.Yield, new Vector2(315, 106), new UfoPrerequisite[] { new("start2", 1) }),
            new("exponential", "EXPONENTIAL YIELD", "EXPONENT", UfoUpgradeEffect.Exponential, 0.05f, 160, 3, UfoBranch.Yield, new Vector2(395, 75), new UfoPrerequisite[] { new("growth3", 1), new("interest", 1) }),
            new("prestige", "PRESTIGE", "PRESTIGE", UfoUpgradeEffect.Prestige, 0f, 0, 1, UfoBranch.Hybrid, new Vector2(0, 225), new UfoPrerequisite[] { new("fire3", 1), new("capacity5", 1), new("auto", 1), new("exponential", 3) }, 3),
            new("clearance", "FLIGHT CLEARANCE", "CLEARANCE", UfoUpgradeEffect.FlightClearance, 14f, 8, 5, UfoBranch.Ship, new Vector2(-155, 85), new UfoPrerequisite[] { new("engine", 1) }),
            // A costly yield branch upgrade: each rank adds one more base
            // crew unit to the value of future soldiers, while staying
            // reachable from the early Survival Dividend route in existing saves.
            new("soldier-value", "SOLDIER VALUE", "SOLDIER PAY", UfoUpgradeEffect.SoldierValue, 1f, 100, 5, UfoBranch.Yield, new Vector2(76, 106), new UfoPrerequisite[] { new("growth", 1) }),
            new("shield", "SHIELD ARRAY", "SHIELD", UfoUpgradeEffect.Shield, .25f, 18, 5, UfoBranch.Hull, new Vector2(-235, 164), new UfoPrerequisite[] { new("hull", 2) }),
        };
        readonly string saveKey;
        readonly bool memoryOnly;
        readonly string path;
        Dictionary<string, int> ranks = new();
        string lastRound = "";
        public double Balance { get; private set; }
        public long PrestigeCount { get; private set; }
        public bool Exists { get; private set; }
        public int GameMode { get; private set; }
        public int Music { get; private set; } = 1;
        public string Error { get; private set; } = "";

        public UfoProgression(bool memoryOnly = false, string savePath = null, int slot = 0)
        {
            this.memoryOnly = memoryOnly;
            if (slot < 0 || slot > 3) throw new ArgumentOutOfRangeException(nameof(slot));
            saveKey = slot == 0 ? "warhook.ufo.progression.v1" : $"warhook.ufo.save.{slot}.v1";
            path = savePath ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Warhook",
                slot == 0 ? "ufo-progression.json" : $"ufo-save-{slot}.json");
            if (memoryOnly) return;
            try
            {
#if WEB
                string saved = WebInterop.LoadSave(saveKey);
#else
                string saved = File.Exists(path) ? File.ReadAllText(path) : null;
#endif
                if (string.IsNullOrWhiteSpace(saved)) return;
                Exists = true;
                using var json = JsonDocument.Parse(saved);
                var root = json.RootElement;
                // Siege mode was removed; older saves are treated as the UFO mode.
                GameMode = 0;
                int version = root.TryGetProperty("version", out var versionElement) && versionElement.TryGetInt32(out int parsedVersion)
                    ? parsedVersion : 1;
                if (root.TryGetProperty("music", out var track) && track.TryGetInt32(out int music)) Music = Math.Clamp(music, 1, 5);
                if (root.TryGetProperty("prestiges", out var prestigeElement) && prestigeElement.TryGetInt64(out long savedPrestiges))
                    PrestigeCount = Math.Max(0, savedPrestiges);
                if (root.TryGetProperty("balance", out var balance) && balance.TryGetDouble(out double amount) && double.IsFinite(amount))
                    Balance = Math.Clamp(Math.Round(amount, 2), 0, 1_000_000_000);
                if (root.TryGetProperty("lastRound", out var last) && last.ValueKind == JsonValueKind.String) lastRound = last.GetString();
                if (root.TryGetProperty("ranks", out var levels) && levels.ValueKind == JsonValueKind.Object)
                {
                    // Convert the old one-time Mothership purchase into a
                    // retained prestige without removing the player's tree.
                    if (PrestigeCount == 0 && levels.TryGetProperty("mothership", out var oldMothership)
                        && oldMothership.TryGetInt32(out int oldPrestige) && oldPrestige > 0)
                        PrestigeCount = oldPrestige;
                    foreach (var node in Nodes)
                        if (levels.TryGetProperty(node.Id, out var level) && level.TryGetInt32(out int rank))
                        {
                            ranks[node.Id] = Math.Clamp(rank, 0, node.MaxRank);
                            // Old repeatable Array/Warp ranks become capstones.
                            // Refund removed ranks at their original purchase prices.
                            if (version < 2 && rank > node.MaxRank && (node.Id == "fire3" || node.Id == "engine3"))
                                for (int r = node.MaxRank + 1; r <= Math.Min(rank, 5); r++)
                                    Balance = Math.Min(1_000_000_000, Balance + (node.Id == "fire3" ? 35 : 55) * r * r);
                        }
                }
            }
            catch (Exception) { Error = "COULD NOT READ UPGRADE SAVE"; }
        }

        public bool StartNew() => Commit(0, new Dictionary<string, int>(), "", 0, 1, 0);
        public bool SavePreferences(int mode, int music) => Commit(Balance, ranks, lastRound, 0, music);
        public bool Import(UfoProgression source) => Commit(source.Balance,
            new Dictionary<string, int>(source.ranks), source.lastRound, 0, source.Music, source.PrestigeCount);

        public static int Index(string id) => Array.FindIndex(Nodes, n => n.Id == id);
        public int Rank(string id) => Rank(Index(id));
        public int Rank(int index) => Nodes[index].Effect == UfoUpgradeEffect.Core ? 1
            : ranks.TryGetValue(Nodes[index].Id, out int rank) ? rank : 0;
        public float Bonus(UfoUpgradeEffect effect)
        {
            float sum = 0;
            for (int i = 0; i < Nodes.Length; i++) if (Nodes[i].Effect == effect) sum += Rank(i) * Nodes[i].Amount;
            return sum;
        }
        public int Capacity
        {
            get
            {
                int capacity = 1;
                for (int i = 0; i < Nodes.Length; i++)
                    if (Nodes[i].Effect == UfoUpgradeEffect.Capacity && Rank(i) > 0)
                        capacity = Math.Max(capacity, (int)Math.Min(int.MaxValue,
                            (long)Nodes[i].Amount + Rank(i) - 1));
                return capacity;
            }
        }
        public int Total(UfoUpgradeEffect effect)
        {
            int sum = 0;
            for (int i = 0; i < Nodes.Length; i++) if (Nodes[i].Effect == effect) sum += Rank(i);
            return sum;
        }
        public int Cost(int index)
        {
            if (Nodes[index].Effect == UfoUpgradeEffect.Prestige) return 0;
            long nextRank = (long)Rank(index) + 1;
            long baseCost = Nodes[index].BaseCost;
            if (baseCost == 0) return 0;
            if (nextRank > Math.Sqrt(int.MaxValue / (double)baseCost)) return int.MaxValue;
            return (int)(baseCost * nextRank * nextRank);
        }
        public bool Unlocked(int index)
        {
            if (Rank(index) > 0) return true; // Honor purchases from older graphs.
            var node = Nodes[index];
            int met = 0;
            foreach (var prerequisite in node.Requires) if (Rank(prerequisite.Node) >= prerequisite.Rank) met++;
            return met >= (node.RequiredCount > 0 ? node.RequiredCount : node.Requires.Length);
        }
        public bool CanBuy(int index) => Unlocked(index) && Rank(index) < Nodes[index].MaxRank
            && (Nodes[index].Effect == UfoUpgradeEffect.Prestige || Balance >= Cost(index));
        public bool Buy(int index)
        {
            if (!CanBuy(index)) return false;
            if (Nodes[index].Effect == UfoUpgradeEffect.Prestige) return Prestige();
            var nextRanks = new Dictionary<string, int>(ranks) { [Nodes[index].Id] = Rank(index) + 1 };
            return Commit(Balance - Cost(index), nextRanks, lastRound);
        }
        public bool Prestige()
        {
            int index = Index("prestige");
            if (!CanBuy(index)) return false;
            long nextCount = PrestigeCount < long.MaxValue ? PrestigeCount + 1 : PrestigeCount;
            return Commit(0, new Dictionary<string, int>(), "", null, Music, nextCount);
        }
        public bool Bank(string roundId, double reward)
        {
            if (lastRound == roundId) return true;
            return Commit(Math.Min(1_000_000_000, Balance + Math.Max(0, reward)), ranks, roundId);
        }
        bool Commit(double balance, Dictionary<string, int> levels, string roundId, int? mode = null, int? music = null,
            long? prestiges = null)
        {
            int nextMode = 0, nextMusic = Math.Clamp(music ?? Music, 1, 5);
            long nextPrestiges = Math.Max(0, prestiges ?? PrestigeCount);
            balance = Math.Round(balance, 2, MidpointRounding.AwayFromZero);
            try
            {
                if (!memoryOnly)
                {
                    using var stream = new MemoryStream();
                    using (var json = new Utf8JsonWriter(stream))
                    {
                        json.WriteStartObject(); json.WriteNumber("version", 3); json.WriteNumber("balance", balance);
                        json.WriteNumber("prestiges", nextPrestiges);
                        json.WriteNumber("mode", nextMode); json.WriteNumber("music", nextMusic);
                        json.WriteString("lastRound", roundId); json.WriteStartObject("ranks");
                        foreach (var rank in levels) json.WriteNumber(rank.Key, rank.Value);
                        json.WriteEndObject(); json.WriteEndObject();
                    }
                    string saved = Encoding.UTF8.GetString(stream.ToArray());
#if WEB
                    WebInterop.SaveSave(saveKey, saved);
#else
                    Directory.CreateDirectory(Path.GetDirectoryName(path));
                    File.WriteAllText(path + ".tmp", saved);
                    File.Move(path + ".tmp", path, true);
#endif
                }
                Balance = balance; PrestigeCount = nextPrestiges; ranks = levels; lastRound = roundId; Error = "";
                Exists = true; GameMode = nextMode; Music = nextMusic;
                return true;
            }
            catch (Exception) { Error = "SAVE FAILED - RETRY"; return false; }
        }
    }
}
