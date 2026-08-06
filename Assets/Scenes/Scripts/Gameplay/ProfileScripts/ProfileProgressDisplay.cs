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
    [Tooltip("Total number of towers in the game")]
    [SerializeField] private int totalTowers = 4;

    [Tooltip("Total number of recipes in the game")]
    [SerializeField] private int totalRecipes = 6;

    [Tooltip("Total badges possible -- 4 badges x 3 towers = 12")]
    [SerializeField] private int totalBadges = 12;

    [Tooltip("Stage IDs to read high scores from")]
    [SerializeField] private string[] stageIDs = { "Stage_1", "Stage_2", "Stage_3", "Stage_4" };

    [Tooltip("Questions for Stage 1 to 3")]
    [SerializeField] private int questionsPerStage = 10;

    [Tooltip("Questions for Stage 4 (combines Tower 1-3)")]
    [SerializeField] private int questionsStageFour = 30;

    // PlayerPrefs Keys
    private const string UNLOCKED_PREFIX = "Unlocked_";
    private const string FIRST_CLEAR_PREFIX = "FirstClear_";
    private const string HIGH_SCORE_PREFIX = "HighScore_";
    private const string BADGE_PREFIX = "Badge_";
    private const string TOTAL_QUESTIONS_KEY = "TotalQuestionsAnswered";

    private static readonly string[] achievementNames =
    {
        "GeniusOfTheTower",
        "ConquerorOfTheTower",
        "ChallengerOfTheTower",
        "StepsTowardsSuccess"
    };

    [Header("Tower Names (must match TowerUnlockManager)")]
    [SerializeField] private string[] towerNames = { "Tower1", "Tower2", "Tower3", "Tower4" };

    [Header("Recipe Stage IDs (must match RecipeUnlockManager)")]
    [SerializeField] private string[] recipeStageIDs;

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

        for (int i = 1; i < towerNames.Length - 1; i++)
        {
            if (PlayerPrefs.GetInt(UNLOCKED_PREFIX + towerNames[i], 0) == 1)
                count++;
        }

        int s1 = PlayerPrefs.GetInt(HIGH_SCORE_PREFIX + "Stage_1", 0);
        int s2 = PlayerPrefs.GetInt(HIGH_SCORE_PREFIX + "Stage_2", 0);
        int s3 = PlayerPrefs.GetInt(HIGH_SCORE_PREFIX + "Stage_3", 0);
        if ((s1 + s2 + s3) >= 26)
            count++;

        return Mathf.Min(count, totalTowers);
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
        return Mathf.Min(count, totalRecipes);
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

        float towerRatio = totalTowers > 0 ? (float)towers / totalTowers : 0f;
        float badgeRatio = totalBadges > 0 ? (float)badges / totalBadges : 0f;
        float recipeRatio = totalRecipes > 0 ? (float)recipes / totalRecipes : 0f;

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