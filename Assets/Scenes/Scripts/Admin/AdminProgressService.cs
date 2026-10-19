using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Every read and write of player progress goes through here.
///
/// Two reasons this exists rather than calling PlayerPrefs directly:
///   1. The key prefixes were duplicated across DevCheatTool, TowerUnlockManager,
///      ResultScreenManager, ProfileProgressDisplay and TowerDebugger, and had
///      already drifted apart. ProgressKeys is now the only place they live.
///   2. Resetting progress has to be surgical. PlayerPrefs.DeleteAll() would
///      also wipe the language choice, the audio volumes, the teacher's admin
///      configuration and the admin PIN, so this deletes only progress keys.
///
/// Note the identifier split that the game uses:
///   stageId   "Stage_1" - scores, badges, attempts, hints
///   towerName "Tower_1" - unlock time and unlocked flag
/// </summary>
public static class AdminProgressService
{
    // ------------------------------------------------------------------
    // Scores and progression
    // ------------------------------------------------------------------

    public static int GetHighScore(string stageId)
    {
        return PlayerPrefs.GetInt(ProgressKeys.HighScoreFor(stageId), 0);
    }

    public static void SetHighScore(string stageId, int value)
    {
        PlayerPrefs.SetInt(ProgressKeys.HighScoreFor(stageId), Mathf.Max(0, value));
        PlayerPrefs.Save();
    }

    public static bool IsFirstCleared(string stageId)
    {
        return PlayerPrefs.GetInt(ProgressKeys.FirstClearFor(stageId), 0) == 1;
    }

    public static void SetFirstCleared(string stageId, bool cleared)
    {
        PlayerPrefs.SetInt(ProgressKeys.FirstClearFor(stageId), cleared ? 1 : 0);
        PlayerPrefs.Save();
    }

    public static int GetAttempts(string stageId)
    {
        return PlayerPrefs.GetInt(ProgressKeys.AttemptsFor(stageId), 0);
    }

    public static void SetAttempts(string stageId, int value)
    {
        PlayerPrefs.SetInt(ProgressKeys.AttemptsFor(stageId), Mathf.Max(0, value));
        PlayerPrefs.Save();
    }

    // ------------------------------------------------------------------
    // Badges
    // ------------------------------------------------------------------

    public static bool HasBadge(string stageId, AchievementType type)
    {
        return PlayerPrefs.GetInt(ProgressKeys.BadgeFor(stageId, type.ToString()), 0) == 1;
    }

    public static void SetBadge(string stageId, AchievementType type, bool earned)
    {
        PlayerPrefs.SetInt(ProgressKeys.BadgeFor(stageId, type.ToString()), earned ? 1 : 0);
        PlayerPrefs.Save();
    }

    public static int GetBadgeCount(string stageId)
    {
        int count = 0;
        AchievementType[] types = AdminGameConfig.BadgeTypes;
        for (int i = 0; i < types.Length; i++)
        {
            if (HasBadge(stageId, types[i])) count++;
        }
        return count;
    }

    // ------------------------------------------------------------------
    // Rune keys
    // ------------------------------------------------------------------

    public static int GetRuneKeys()
    {
        return PlayerPrefs.GetInt(ProgressKeys.RuneKeys, 0);
    }

    public static void SetRuneKeys(int value)
    {
        PlayerPrefs.SetInt(ProgressKeys.RuneKeys, Mathf.Max(0, value));
        PlayerPrefs.Save();
    }

    // ------------------------------------------------------------------
    // Hints
    // ------------------------------------------------------------------

    public static int GetHints(string stageId)
    {
        return HintStorage.Get(stageId);
    }

    public static void SetHints(string stageId, int value)
    {
        HintStorage.Set(stageId, value);
    }

    // ------------------------------------------------------------------
    // Tower unlocks
    // ------------------------------------------------------------------

    public static bool IsTowerUnlocked(string towerName)
    {
        return PlayerPrefs.GetInt(ProgressKeys.UnlockedFor(towerName), 0) == 1;
    }

    public static void SetTowerUnlocked(string towerName, bool unlocked)
    {
        PlayerPrefs.SetInt(ProgressKeys.UnlockedFor(towerName), unlocked ? 1 : 0);
        PlayerPrefs.Save();
    }

    public static string GetUnlockTime(string towerName)
    {
        return PlayerPrefs.GetString(ProgressKeys.UnlockTimeFor(towerName), "");
    }

    public static void SetUnlockTime(string towerName, DateTime utc)
    {
        PlayerPrefs.SetString(ProgressKeys.UnlockTimeFor(towerName), utc.ToString("o"));
        PlayerPrefs.Save();
    }

    public static void ClearUnlockTime(string towerName)
    {
        PlayerPrefs.DeleteKey(ProgressKeys.UnlockTimeFor(towerName));
    }

    // ------------------------------------------------------------------
    // Bulk operations
    // ------------------------------------------------------------------

    /// <summary>
    /// Clears one stage: score, first clear, attempts, badges, hints and the
    /// claimed-rewards flag.
    /// </summary>
    public static void ResetStage(string stageId)
    {
        PlayerPrefs.DeleteKey(ProgressKeys.HighScoreFor(stageId));
        PlayerPrefs.DeleteKey(ProgressKeys.FirstClearFor(stageId));
        PlayerPrefs.DeleteKey(ProgressKeys.AttemptsFor(stageId));
        PlayerPrefs.DeleteKey(ProgressKeys.RewardsClaimedFor(stageId));
        PlayerPrefs.DeleteKey(ProgressKeys.HintsFor(stageId));

        AchievementType[] types = AdminGameConfig.BadgeTypes;
        for (int i = 0; i < types.Length; i++)
        {
            PlayerPrefs.DeleteKey(ProgressKeys.BadgeFor(stageId, types[i].ToString()));
        }
    }

    /// <summary>
    /// Clears every progress key the game writes, but leaves the language
    /// choice, the audio volumes, the admin configuration and the admin PIN
    /// alone.
    /// </summary>
    public static void ResetAllProgress()
    {
        IReadOnlyList<AdminGameConfig.LevelEntry> levels = AdminGameConfig.Levels;
        for (int i = 0; i < levels.Count; i++)
        {
            AdminGameConfig.LevelEntry level = levels[i];
            ResetStage(level.stageId);
            PlayerPrefs.DeleteKey(ProgressKeys.UnlockTimeFor(level.towerName));
            PlayerPrefs.DeleteKey(ProgressKeys.UnlockedFor(level.towerName));
        }

        PlayerPrefs.DeleteKey(ProgressKeys.RuneKeys);
        PlayerPrefs.DeleteKey(ProgressKeys.RuneLastRegen);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// A snapshot of every progress value, used by the progress tab to render
    /// the current state.
    /// </summary>
    public class StageSnapshot
    {
        public string stageId;
        public string towerName;
        public int highScore;
        public bool firstCleared;
        public int attempts;
        public int hints;
        public bool unlocked;
        public bool[] badges;

        public int BadgeCount
        {
            get
            {
                if (badges == null) return 0;
                int n = 0;
                for (int i = 0; i < badges.Length; i++) if (badges[i]) n++;
                return n;
            }
        }
    }

    public static StageSnapshot Snapshot(string stageId)
    {
        AdminGameConfig.LevelEntry level = AdminGameConfig.LevelForStage(stageId);
        StageSnapshot snap = new StageSnapshot
        {
            stageId = stageId,
            towerName = level != null ? level.towerName : "Tower",
            highScore = GetHighScore(stageId),
            firstCleared = IsFirstCleared(stageId),
            attempts = GetAttempts(stageId),
            hints = GetHints(stageId),
            unlocked = level != null && IsTowerUnlocked(level.towerName),
        };

        AchievementType[] types = AdminGameConfig.BadgeTypes;
        snap.badges = new bool[types.Length];
        for (int i = 0; i < types.Length; i++)
        {
            snap.badges[i] = HasBadge(stageId, types[i]);
        }
        return snap;
    }
}
