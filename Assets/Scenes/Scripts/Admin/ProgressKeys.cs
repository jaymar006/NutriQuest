using UnityEngine;

/// <summary>
/// Single source of truth for every PlayerPrefs key the game writes.
///
/// These prefixes were previously duplicated across DevCheatTool, TowerUnlockManager,
/// ResultScreenManager, ProfileProgressDisplay, TowerDebugger, HintStorage, RuneKeySystem
/// and LocalizationManager. The duplication is what allowed stage-id strings to drift
/// (e.g. "FirstClear_Stage_1" written by one system while another read
/// "FirstClear_Tower1_Stage1"), silently breaking unlocks.
///
/// Every new code should build keys through this class. Note that a TOWER and a STAGE are
/// different concepts: a tower has a name ("Tower_1"), a stage has an id ("Stage_1"), and a
/// recipe may key off a compound id ("Tower1_Stage1"). Do not assume one from the other -
/// copy the literal from the Inspector field of the system that reads it.
/// </summary>
public static class ProgressKeys
{
    // ----- Score and progression -----
    public const string HighScore = "HighScore_";
    public const string FirstClear = "FirstClear_";
    public const string Badge = "Badge_";
    public const string Attempts = "Attempts_";
    public const string RewardsClaimed = "RewardsClaimed_";

    // ----- Unlocks and timers -----
    public const string UnlockTime = "UnlockTime_";
    public const string Unlocked = "Unlocked_";
    public const string CutsceneSeen = "CutsceneSeen_";

    // ----- Hints -----
    public const string Hints = "PlayerHints_";
    public const string HintsMax = "PlayerHintsMax";

    // ----- Rune keys -----
    public const string RuneKeys = "RuneKeys";
    public const string RuneLastRegen = "RuneLastRegen";

    // ----- Profile -----
    public const string PlayerName = "PlayerName";

    // ----- Language -----
    public const string SelectedLanguage = "SelectedLanguage";
    public const string HasChosenLanguage = "HasChosenLanguage";
    public const string LanguageSelected = "LanguageSelected";

    // ----- Audio -----
    public const string MusicVolume = "MusicVolume";
    public const string SfxVolume = "SFXVolume";

    // ----- Last completed result (written by ResultData) -----
    public const string ResultCorrect = "Result_Correct";
    public const string ResultWrong = "Result_Wrong";
    public const string ResultTotal = "Result_Total";
    public const string ResultStageId = "Result_StageID";
    public const string ResultTowerIndex = "Result_TowerIndex";

    // ----- Lifetime counters -----
    public const string TotalQuestionsAnswered = "TotalQuestionsAnswered";

    // ------------------------------------------------------------------
    // Composed keys
    // ------------------------------------------------------------------

    public static string HighScoreFor(string stageId) => HighScore + NullSafe(stageId);
    public static string FirstClearFor(string stageId) => FirstClear + NullSafe(stageId);
    public static string AttemptsFor(string stageId) => Attempts + NullSafe(stageId);
    public static string BadgeFor(string stageId, string badgeType) =>
        Badge + NullSafe(stageId) + "_" + NullSafe(badgeType);
    public static string RewardsClaimedFor(string stageId) => RewardsClaimed + NullSafe(stageId);
    public static string UnlockTimeFor(string towerName) => UnlockTime + NullSafe(towerName);
    public static string UnlockedFor(string towerName) => Unlocked + NullSafe(towerName);
    public static string CutsceneSeenFor(string sceneName) => CutsceneSeen + NullSafe(sceneName);
    public static string HintsFor(string stageId) => Hints + NullSafe(stageId);

    private static string NullSafe(string value) => string.IsNullOrEmpty(value) ? "Default" : value;
}
