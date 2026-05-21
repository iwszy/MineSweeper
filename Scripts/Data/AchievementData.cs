using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace MineSweeper;

public class AchievementData
{
    [JsonInclude] public Dictionary<string, bool> Unlocked { get; private set; } = new();

    [JsonInclude] public Dictionary<string, int> ModeWins { get; private set; } = new();

    [JsonInclude] public int CumulativeLosses { get; set; }

    public void Load() {
        var loaded = DataStore.Load<AchievementData>("achievements.json");
        Unlocked = loaded.Unlocked;
        ModeWins = loaded.ModeWins;
        CumulativeLosses = loaded.CumulativeLosses;
    }

    public void Save() {
        DataStore.Save("achievements.json", this);
    }

    public bool IsUnlocked(string id) =>
        Unlocked.TryGetValue(id, out var unlocked) && unlocked;

    public List<AchievementDef.Achievement> CheckOnGameEnd(
        bool won, GameMode mode, double time, int flagCount,
        bool allMinesFlagged, bool flagsEverUsed,
        int maxChordRevealCount, int firstClickRevealCount,
        int maxConsecutiveReveals, BestTimes bestTimes) {
        var unlocked = new List<AchievementDef.Achievement>();

        if (!won) {
            CumulativeLosses++;
            if (CumulativeLosses >= 100) {
                TryUnlock("A10", unlocked);
            }
            Save();
            return unlocked;
        }

        // Won — check all win-related achievements

        // A01: First win
        TryUnlock("A01", unlocked);

        // A02: All mines correctly flagged
        if (allMinesFlagged) {
            TryUnlock("A02", unlocked);
        }

        // A03: No flags used
        if (!flagsEverUsed) {
            TryUnlock("A03", unlocked);
        }

        // A04: Chord revealed at least 5 cells
        if (maxChordRevealCount >= 5) {
            TryUnlock("A04", unlocked);
        }

        // A05: 入门 in under 30s
        if (mode.Name == "入门" && time < 30) {
            TryUnlock("A05", unlocked);
        }

        // A06: 进阶 with first click big reveal (>= 20 cells)
        if (mode.Name == "进阶" && firstClickRevealCount >= 20) {
            TryUnlock("A06", unlocked);
        }

        // A07: 大师 win
        if (mode.Name == "大师") {
            TryUnlock("A07", unlocked);
        }

        // A08: 超凡 win
        if (mode.Name == "超凡") {
            TryUnlock("A08", unlocked);
        }

        // A09: 100 consecutive reveals without flagging
        if (maxConsecutiveReveals >= 100) {
            TryUnlock("A09", unlocked);
        }

        // A11: 普通 under 120s
        if (mode.Name == "普通" && time < 120) {
            TryUnlock("A12", unlocked);
        }

        // A12: All 5 preset modes won
        TrackModeWin(mode.Name);
        if (ModeWins.Count >= 5) {
            TryUnlock("A12", unlocked);
        }

        // A13: 超凡 with <= 5 flags
        if (mode.Name == "超凡" && flagCount <= 5) {
            TryUnlock("A14", unlocked);
        }

        // A14: Time at least 10% faster than previous best
        var prevBest = bestTimes.Get(mode.Name);
        if (prevBest.HasValue && time < prevBest.Value * 0.9) {
            TryUnlock("A14", unlocked);
        }

        Save();
        return unlocked;
    }

    private void TrackModeWin(string modeName) {
        ModeWins.TryAdd(modeName, 0);
        ModeWins[modeName]++;
    }

    private void TryUnlock(string id, List<AchievementDef.Achievement> newlyUnlocked) {
        if (IsUnlocked(id)) {
            return;
        }
        Unlocked[id] = true;
        var def = AchievementDef.All.Find(a => a.Id == id);
        if (def != null) {
            newlyUnlocked.Add(def);
        }
    }
}