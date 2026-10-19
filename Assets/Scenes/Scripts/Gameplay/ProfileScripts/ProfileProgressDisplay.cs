using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Gameplay.CutsceneManager;

public class ProfileProgressDisplay : MonoBehaviour
{
    // Player Name
    [Header("Player Name")]
    [SerializeField] private TMP_Text playerNameTXT;

    // Progress Percentage
    [Header("Progress Percentage")]
    [SerializeField] private TMP_Text percentageTXT;

    // Int Display Fields
    [Header("Int Display Fields")]
    [SerializeField] private TMP_Text towersCompletedTXT;
    [SerializeField] private TMP_Text badgesEarnedTXT;
    [SerializeField] private TMP_Text totalQuestionsAnsweredTXT;
    [SerializeField] private TMP_Text recipesUnlockedTXT;

    // Rank Display
    [Header("Rank Display")]
    [SerializeField] private TMP_Text titleTXT;
    [SerializeField] private Image rankImage;
    // FIX: 4 rank tiers now -> this array must have exactly 4 sprites assigned
    // in the Inspector, in this order:
    //   [0] Beginner   [1] Challenger   [2] Expert   [3] Genius
    // The old code used spriteIndex values of 0, 2, 4 for only 3 tiers, which
    // went out of bounds for a 3-element array and silently kept whatever
    // placeholder sprite was already on rankImage.
    [SerializeField] private Sprite[] rankSprites;

    // Config
    [Header("Config")]
    [Tooltip("Total badges possible -- 4 badges x 3 towers = 12")]
    [SerializeField] private int totalBadges = 12;

    [Tooltip("Legacy fallback totals. The registry is used when it has entries; " +
             "these are only read if the registry is empty.")]
    [SerializeField] private int totalTowers = 4;
    [SerializeField] private int totalRecipes = 6;

    // PlayerPrefs Keys
    private const string UNLOCKED_PREFIX = ProgressKeys.Unlocked;
    private const string FIRST_CLEAR_PREFIX = ProgressKeys.FirstClear;
    private const string HIGH_SCORE_PREFIX = ProgressKeys.HighScore;
    private const string BADGE_PREFIX = ProgressKeys.Badge;
    private const string TOTAL_QUESTIONS_KEY = ProgressKeys.TotalQuestionsAnswered;

    private static readonly string[] achievementNames =
    {
        "GeniusOfTheTower",
        "ConquerorOfTheTower",
        "ChallengerOfTheTower",
        "StepsTowardsSuccess"
    };

    [Header("Recipe Stage IDs (must match RecipeUnlockManager)")]
    [SerializeField] private string[] recipeStageIDs;

    // ------------------------------------------------------------------
    // The registry is the source of truth for how many towers and recipes
    // exist. The serialized counts above are only a fallback.
    //
    // NOTE: this script used to have a towerNames array of "Tower1",
    // "Tower2" ... while TowerUnlockManager writes "Unlocked_Tower_1",
    // "Unlocked_Tower_2" ... so the unlocked-tower count on the profile
    // screen could never go above one. The registry uses the same
    // identifiers the game writes, so the count is correct now.
    // ------------------------------------------------------------------

    private int EffectiveTotalTowers =>
        AdminGameConfig.Levels.Count > 0 ? AdminGameConfig.Levels.Count : totalTowers;

    private int EffectiveTotalRecipes =>
        AdminGameConfig.Recipes.Count > 0 ? AdminGameConfig.Recipes.Count : totalRecipes;

    private void OnEnable()
    {
        RefreshAll();
    }

    // Public Entry Point
    public void RefreshAll()
    {
        RefreshPlayerName();

        int towersCompleted = CountTowersCompleted();
        int badgesEarned = CountBadgesEarned();
        int totalQAnswered = GetTotalQuestionsAnswered();
        int recipesUnlocked = CountRecipesUnlocked();

        DisplayInts(towersCompleted, badgesEarned, totalQAnswered, recipesUnlocked);
        DisplayPercentage(towersCompleted, badgesEarned, recipesUnlocked);
        DisplayRank(badgesEarned, totalBadges);
    }

    // Updates only the name field — call this after the player changes their name
    public void RefreshPlayerName()
    {
        if (playerNameTXT == null) return;

        string name = PlayerNameManager.Instance != null
            ? PlayerNameManager.Instance.GetPlayerName()
            : PlayerPrefs.GetString("PlayerName", "Player");

        playerNameTXT.text = name;
    }

    // Counters
    private int CountTowersCompleted()
    {
        int count = 0;

        if (PlayerPrefs.GetInt(HIGH_SCORE_PREFIX + "Stage_1", 0) > 0)
            count++;

        // Tiers 2 and 3 are counted from the registry so the identifiers match
        // the ones TowerUnlockManager actually writes.
        for (int i = 1; i < AdminGameConfig.Levels.Count - 1; i++)
        {
            if (PlayerPrefs.GetInt(ProgressKeys.UnlockedFor(AdminGameConfig.Levels[i].towerName), 0) == 1)
                count++;
        }

        int s1 = PlayerPrefs.GetInt(HIGH_SCORE_PREFIX + "Stage_1", 0);
        int s2 = PlayerPrefs.GetInt(HIGH_SCORE_PREFIX + "Stage_2", 0);
        int s3 = PlayerPrefs.GetInt(HIGH_SCORE_PREFIX + "Stage_3", 0);
        if ((s1 + s2 + s3) >= AdminGameConfig.DefaultTower4RequiredTotal)
            count++;

        return Mathf.Min(count, EffectiveTotalTowers);
    }

    private int CountBadgesEarned()
    {
        int count = 0;

        string[] badgeStageIDs = { "Stage_1", "Stage_2", "Stage_3" };

        foreach (string stageID in badgeStageIDs)
        {
            foreach (string achievement in achievementNames)
            {
                string key = BADGE_PREFIX + stageID + "_" + achievement;
                if (PlayerPrefs.GetInt(key, 0) == 1)
                    count++;
            }
        }

        return Mathf.Min(count, totalBadges);
    }

    private int GetTotalQuestionsAnswered()
    {
        return PlayerPrefs.GetInt(TOTAL_QUESTIONS_KEY, 0);
    }

    private int CountRecipesUnlocked()
    {
        if (recipeStageIDs == null) return 0;
        int count = 0;
        foreach (string stageID in recipeStageIDs)
        {
            if (string.IsNullOrEmpty(stageID) ||
                PlayerPrefs.GetInt(FIRST_CLEAR_PREFIX + stageID, 0) == 1)
                count++;
        }
        return Mathf.Min(count, EffectiveTotalRecipes);
    }

    // Display Methods
    private void DisplayInts(int towers, int badges, int questions, int recipes)
    {
        if (towersCompletedTXT != null) towersCompletedTXT.text = towers.ToString();
        if (badgesEarnedTXT != null) badgesEarnedTXT.text = badges.ToString();
        if (totalQuestionsAnsweredTXT != null) totalQuestionsAnsweredTXT.text = questions.ToString();
        if (recipesUnlockedTXT != null) recipesUnlockedTXT.text = recipes.ToString();
    }

    private void DisplayPercentage(int towers, int badges, int recipes)
    {
        if (percentageTXT == null) return;

        float towerRatio = EffectiveTotalTowers > 0 ? (float)towers / EffectiveTotalTowers : 0f;
        float badgeRatio = totalBadges > 0 ? (float)badges / totalBadges : 0f;
        float recipeRatio = EffectiveTotalRecipes > 0 ? (float)recipes / EffectiveTotalRecipes : 0f;

        float overall = (towerRatio + badgeRatio + recipeRatio) / 3f;
        int percent = Mathf.RoundToInt(overall * 100f);

        percentageTXT.text = percent + "%";
    }

    // FIX: 4-tier rank system, replacing the old 3-tier one that used
    // out-of-range sprite indices (0, 2, 4). Tiers now scale off
    // totalPossibleBadges instead of a hardcoded number, so this still
    // works correctly if totalBadges ever changes:
    //
    //   Beginner   -> 0 badges                          -> sprite index 0
    //   Challenger -> 1 badge up to < 50% of total       -> sprite index 1
    //   Expert     -> 50% of total up to < full          -> sprite index 2
    //   Genius     -> full totalPossibleBadges           -> sprite index 3
    //
    // rankSprites must have exactly 4 elements assigned in the Inspector,
    // in that order, or the corresponding sprite won't be found and the
    // placeholder will keep showing (same failure mode as before).
    private void DisplayRank(int badgesEarned, int totalPossibleBadges)
    {
        string title;
        int spriteIndex;

        int halfway = totalPossibleBadges / 2;

        if (badgesEarned == 0)
        {
            title = "Beginner";
            spriteIndex = 0;
        }
        else if (badgesEarned < halfway)
        {
            title = "Challenger";
            spriteIndex = 1;
        }
        else if (badgesEarned < totalPossibleBadges)
        {
            title = "Expert";
            spriteIndex = 2;
        }
        else
        {
            title = "Genius";
            spriteIndex = 3;
        }

        if (titleTXT != null)
            titleTXT.text = title;

        if (rankImage != null && rankSprites != null &&
            spriteIndex >= 0 && spriteIndex < rankSprites.Length && rankSprites[spriteIndex] != null)
        {
            rankImage.sprite = rankSprites[spriteIndex];
        }
        else if (rankImage != null)
        {
            // FIX: warn instead of silently keeping the placeholder, so a
            // missing/misassigned sprite is obvious in the console instead
            // of just quietly not updating.
            Debug.LogWarning("[ProfileProgressDisplay] rankSprites is missing an entry for '" + title +
                              "' (index " + spriteIndex + "). Assign 4 sprites in the Inspector: " +
                              "Beginner, Challenger, Expert, Genius, in that order.");
        }
    }

    // Button Callback
    public void OnRefreshButtonClicked()
    {
        RefreshAll();
    }
}