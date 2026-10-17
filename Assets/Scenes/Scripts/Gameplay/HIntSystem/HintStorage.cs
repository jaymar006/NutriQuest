using UnityEngine;

// =============================================================================
// HintStorage  NutriQuest
//
// Saves hints PER TOWER in PlayerPrefs. Static, so nothing to place in a scene,
// and usable from any scene (HintSystem in the quiz scene, ResultScreenManager
// in the result scene).
//
// A tower is identified by its Stage ID (the same ID ScoreManager uses for
// progress, e.g. "Stage_1"), so each tower keeps its own hint count.
//
// PlayerPrefs keys written:
//   PlayerHints_<stageID>   hints left for that tower
//   PlayerHintsMax          the max hints per tower (written by HintSystem so the
//                           result scene knows the cap without a second setting)
//
// The old single global key "PlayerHints" is no longer used.
// =============================================================================
public static class HintStorage
{
    private const string HINTS_PREFIX = "PlayerHints_";
    private const string MAX_KEY = "PlayerHintsMax";
    public const int DEFAULT_MAX_HINTS = 3;

    /// <summary>Max hints per tower. Set by HintSystem from its Inspector value.</summary>
    public static int MaxHints => Mathf.Max(0, PlayerPrefs.GetInt(MAX_KEY, DEFAULT_MAX_HINTS));

    public static void SetMaxHints(int max)
    {
        max = Mathf.Max(0, max);
        if (PlayerPrefs.GetInt(MAX_KEY, -1) == max) return; // avoid needless disk writes
        PlayerPrefs.SetInt(MAX_KEY, max);
        PlayerPrefs.Save();
    }

    /// <summary>Hints left for this tower. A tower never played starts with the max.</summary>
    public static int Get(string towerKey)
    {
        return Mathf.Clamp(PlayerPrefs.GetInt(Key(towerKey), MaxHints), 0, MaxHints);
    }

    public static void Set(string towerKey, int value)
    {
        PlayerPrefs.SetInt(Key(towerKey), Mathf.Clamp(value, 0, MaxHints));
        PlayerPrefs.Save();
    }

    /// <summary>Adds hints to this tower, never above the max. Returns true if any were added.</summary>
    public static bool Add(string towerKey, int amount)
    {
        int before = Get(towerKey);
        Set(towerKey, before + amount);
        return Get(towerKey) > before;
    }

    private static string Key(string towerKey)
    {
        return HINTS_PREFIX + (string.IsNullOrEmpty(towerKey) ? "Default" : towerKey);
    }
}
