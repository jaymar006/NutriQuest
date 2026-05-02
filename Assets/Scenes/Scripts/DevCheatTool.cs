using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class DevCheatTool : MonoBehaviour
{
    [Header("Panel")]
    [SerializeField] private GameObject cheatPanel;
    [SerializeField] private Button toggleButton;
    [SerializeField] private Button closeButton;

    [Header("Rune Keys")]
    [SerializeField] private Button addRuneKeyButton;
    [SerializeField] private Button maxRuneKeysButton;
    [SerializeField] private Button zeroRuneKeysButton;

    [Header("Scores")]
    [SerializeField] private Button setScoreStage1Button;
    [SerializeField] private Button setScoreStage2Button;
    [SerializeField] private Button setScoreStage3Button;
    [SerializeField] private TMP_InputField scoreInputField;

    [Header("Stage Specific Badges")]
    [SerializeField] private Button unlockBadgesStage1Button;
    [SerializeField] private Button unlockBadgesStage2Button;
    [SerializeField] private Button unlockBadgesStage3Button;

    [Header("Tower Unlock")]
    [SerializeField] private Button unlockAllTowersButton;
    [SerializeField] private Button lockAllTowersButton;
    [SerializeField] private Button skipWaitTimerButton;

    [Header("Recipes")]
    [SerializeField] private Button unlockAllRecipesButton;
    [SerializeField] private Button lockAllRecipesButton;

    [Header("Badges")]
    [SerializeField] private Button unlockAllBadgesButton;
    [SerializeField] private Button clearAllBadgesButton;

    [Header("General")]
    [SerializeField] private Button resetEverythingButton;
    [SerializeField] private TMP_Text feedbackText;

    // FIX: Direct reference to ProfileProgressDisplay so every action refreshes the profile.
    //      Assign this in the Inspector if the profile panel is in the same scene.
    //      If it's in a different scene, the FindObjectOfType fallback handles it automatically.
    [Header("Profile (assign if in same scene)")]
    [SerializeField] private ProfileProgressDisplay profileDisplay;

    [Header("Settings")]
    [SerializeField] private bool enableInBuild = false;

    private const string HIGH_SCORE_PREFIX = "HighScore_";
    private const string FIRST_CLEAR_PREFIX = "FirstClear_";
    private const string BADGE_PREFIX = "Badge_";
    private const string UNLOCK_TIME_PREFIX = "UnlockTime_";
    private const string UNLOCKED_PREFIX = "Unlocked_";
    private const string ATTEMPT_PREFIX = "Attempts_";

    // Tower/badge stage IDs
    [Header("Stage IDs (towers + badges)")]
    [SerializeField] private string[] stageIDs = { "Stage_1", "Stage_2", "Stage_3", "Stage_4" };
    [SerializeField] private string[] towerNames = { "Tower_1", "Tower_2", "Tower_3", "Tower_4" };

    // FIX: Separate recipe stage IDs that match RecipeUnlockManager.requiredStageID exactly.
    //      These are often different from tower stageIDs (e.g. "Tower1_Stage1" vs "Stage_1").
    //      The old code was writing FIRST_CLEAR_Stage_1 but RecipeUnlockManager was reading
    //      FIRST_CLEAR_Tower1_Stage1, so unlock/lock had no effect on recipes.
    [Header("Recipe Stage IDs (must match RecipeUnlockManager requiredStageID values)")]
    [SerializeField] private string[] recipeStageIDs;

    private AchievementType[] allBadges = new AchievementType[]
    {
        AchievementType.GeniusOfTheTower,
        AchievementType.ConquerorOfTheTower,
        AchievementType.ChallengerOfTheTower,
        AchievementType.StepsTowardsSuccess
    };

    private void Awake()
    {
        if (!Debug.isDebugBuild && !enableInBuild)
        {
            gameObject.SetActive(false);
            return;
        }
    }

    private void Start()
    {
        if (cheatPanel != null)
            cheatPanel.SetActive(false);

        if (toggleButton != null)
            toggleButton.onClick.AddListener(() =>
                cheatPanel.SetActive(!cheatPanel.activeSelf));

        if (closeButton != null)
            closeButton.onClick.AddListener(() =>
                cheatPanel.SetActive(false));

        // Rune Keys
        if (addRuneKeyButton != null)
            addRuneKeyButton.onClick.AddListener(AddRuneKey);
        if (maxRuneKeysButton != null)
            maxRuneKeysButton.onClick.AddListener(MaxRuneKeys);
        if (zeroRuneKeysButton != null)
            zeroRuneKeysButton.onClick.AddListener(ZeroRuneKeys);

        // Scores
        if (setScoreStage1Button != null)
            setScoreStage1Button.onClick.AddListener(() => SetScore("Stage_1"));
        if (setScoreStage2Button != null)
            setScoreStage2Button.onClick.AddListener(() => SetScore("Stage_2"));
        if (setScoreStage3Button != null)
            setScoreStage3Button.onClick.AddListener(() => SetScore("Stage_3"));

        // Stage Specific Badges
        if (unlockBadgesStage1Button != null)
            unlockBadgesStage1Button.onClick.AddListener(() => UnlockBadgesForStage("Stage_1"));
        if (unlockBadgesStage2Button != null)
            unlockBadgesStage2Button.onClick.AddListener(() => UnlockBadgesForStage("Stage_2"));
        if (unlockBadgesStage3Button != null)
            unlockBadgesStage3Button.onClick.AddListener(() => UnlockBadgesForStage("Stage_3"));

        // Tower Unlock
        if (unlockAllTowersButton != null)
            unlockAllTowersButton.onClick.AddListener(UnlockAllTowers);
        if (lockAllTowersButton != null)
            lockAllTowersButton.onClick.AddListener(LockAllTowers);
        if (skipWaitTimerButton != null)
            skipWaitTimerButton.onClick.AddListener(SkipWaitTimers);

        // Recipes
        if (unlockAllRecipesButton != null)
            unlockAllRecipesButton.onClick.AddListener(UnlockAllRecipes);
        if (lockAllRecipesButton != null)
            lockAllRecipesButton.onClick.AddListener(LockAllRecipes);

        // Badges
        if (unlockAllBadgesButton != null)
            unlockAllBadgesButton.onClick.AddListener(UnlockAllBadges);
        if (clearAllBadgesButton != null)
            clearAllBadgesButton.onClick.AddListener(ClearAllBadges);

        // General
        if (resetEverythingButton != null)
            resetEverythingButton.onClick.AddListener(ResetEverything);
    }

    // -------------------------------------------------------------------------
    // FIX: Central profile refresh helper. Finds ProfileProgressDisplay in scene
    //      if not assigned in Inspector. Called at the end of every single action
    //      so the profile page is never stale after a cheat is applied.
    // -------------------------------------------------------------------------
    private void RefreshProfile()
    {
        // Use the assigned reference first, fall back to scene search
        ProfileProgressDisplay display = profileDisplay != null
            ? profileDisplay
            : FindFirstObjectByType<ProfileProgressDisplay>();

        if (display != null)
            display.RefreshAll();
    }

    // -------------------------------------------------------------------------
    // Rune Keys
    // -------------------------------------------------------------------------

    private void AddRuneKey()
    {
        if (RuneKeySystem.Instance != null)
            RuneKeySystem.Instance.AddKey(1);
        RefreshProfile();
        ShowFeedback("+1 Rune Key added!");
    }

    private void MaxRuneKeys()
    {
        if (RuneKeySystem.Instance != null)
            RuneKeySystem.Instance.AddKey(99);
        RefreshProfile();
        ShowFeedback("Rune Keys maxed!");
    }

    private void ZeroRuneKeys()
    {
        PlayerPrefs.SetInt("RuneKeys", 0);
        PlayerPrefs.Save();

        if (RuneKeySystem.Instance != null)
            RuneKeySystem.Instance.RefreshAllDisplays();

        RefreshProfile();
        ShowFeedback("Rune Keys set to 0!");
    }

    // -------------------------------------------------------------------------
    // Scores
    // -------------------------------------------------------------------------

    private void SetScore(string stageID)
    {
        if (scoreInputField == null)
        {
            ShowFeedback("Score input not assigned!");
            return;
        }

        string input = scoreInputField.text.Trim();

        if (string.IsNullOrEmpty(input))
        {
            ShowFeedback("Enter a score first!");
            return;
        }

        if (!int.TryParse(input, out int score))
        {
            ShowFeedback("Numbers only!");
            return;
        }

        bool isFirstAttempt = PlayerPrefs.GetInt(ATTEMPT_PREFIX + stageID, 0) == 0;

        PlayerPrefs.SetInt(HIGH_SCORE_PREFIX + stageID, score);
        PlayerPrefs.SetInt(ATTEMPT_PREFIX + stageID, 1);

        AchievementType earned = AchievementEvaluator.Evaluate(score, 10);
        if (earned != AchievementType.None)
        {
            string badgeKey = BADGE_PREFIX + stageID + "_" + earned.ToString();
            PlayerPrefs.SetInt(badgeKey, 1);
            Debug.Log("[DevCheatTool] Badge earned: " + earned.ToString());
        }

        PlayerPrefs.Save();

        if (TowerUnlockManager.Instance != null)
            TowerUnlockManager.Instance.OnLevelCleared(stageID);

        foreach (LevelInfoScreen screen in FindObjectsByType<LevelInfoScreen>(FindObjectsSortMode.None))
            screen.RefreshDisplay();

        RefreshProfile();
        ShowFeedback(stageID + " score set to " + score + "!");
    }

    // -------------------------------------------------------------------------
    // Badges
    // -------------------------------------------------------------------------

    private void UnlockBadgesForStage(string stageID)
    {
        foreach (AchievementType badge in allBadges)
        {
            string key = BADGE_PREFIX + stageID + "_" + badge.ToString();
            PlayerPrefs.SetInt(key, 1);
        }

        PlayerPrefs.SetInt(HIGH_SCORE_PREFIX + stageID, 10);
        PlayerPrefs.SetInt(ATTEMPT_PREFIX + stageID, 1);
        PlayerPrefs.SetInt(FIRST_CLEAR_PREFIX + stageID, 1);
        PlayerPrefs.Save();

        if (TowerUnlockManager.Instance != null)
            TowerUnlockManager.Instance.RefreshUnlockStates();
        if (RecipeUnlockManager.Instance != null)
            RecipeUnlockManager.Instance.RefreshUnlockStates();

        RefreshProfile();
        ShowFeedback("Badges unlocked for " + stageID + "!");
        Debug.Log("[DevCheatTool] Badges unlocked for: " + stageID);
    }

    private void UnlockAllBadges()
    {
        foreach (string stage in stageIDs)
        {
            foreach (AchievementType badge in allBadges)
            {
                string key = BADGE_PREFIX + stage + "_" + badge.ToString();
                PlayerPrefs.SetInt(key, 1);
            }
        }

        PlayerPrefs.Save();
        RefreshProfile();
        ShowFeedback("All badges unlocked!");
    }

    private void ClearAllBadges()
    {
        foreach (string stage in stageIDs)
        {
            foreach (AchievementType badge in allBadges)
            {
                string key = BADGE_PREFIX + stage + "_" + badge.ToString();
                PlayerPrefs.DeleteKey(key);
            }
        }

        PlayerPrefs.Save();
        RefreshProfile();
        ShowFeedback("All badges cleared!");
    }

    // -------------------------------------------------------------------------
    // Towers
    // -------------------------------------------------------------------------

    private void UnlockAllTowers()
    {
        foreach (string tower in towerNames)
        {
            PlayerPrefs.SetInt(UNLOCKED_PREFIX + tower, 1);
            PlayerPrefs.DeleteKey(UNLOCK_TIME_PREFIX + tower);
        }

        foreach (string stage in stageIDs)
        {
            PlayerPrefs.SetInt(HIGH_SCORE_PREFIX + stage, 10);
            PlayerPrefs.SetInt(ATTEMPT_PREFIX + stage, 1);
        }

        PlayerPrefs.Save();

        if (TowerUnlockManager.Instance != null)
            TowerUnlockManager.Instance.RefreshUnlockStates();

        RefreshProfile();
        ShowFeedback("All towers unlocked!");
    }

    private void LockAllTowers()
    {
        foreach (string tower in towerNames)
        {
            PlayerPrefs.DeleteKey(UNLOCKED_PREFIX + tower);
            PlayerPrefs.DeleteKey(UNLOCK_TIME_PREFIX + tower);
        }

        foreach (string stage in stageIDs)
        {
            PlayerPrefs.DeleteKey(HIGH_SCORE_PREFIX + stage);
            PlayerPrefs.DeleteKey(ATTEMPT_PREFIX + stage);
        }

        PlayerPrefs.Save();

        if (TowerUnlockManager.Instance != null)
            TowerUnlockManager.Instance.RefreshUnlockStates();

        RefreshProfile();
        ShowFeedback("All towers locked!");
    }

    private void SkipWaitTimers()
    {
        foreach (string tower in towerNames)
        {
            string key = UNLOCK_TIME_PREFIX + tower;
            if (!string.IsNullOrEmpty(PlayerPrefs.GetString(key, "")))
            {
                DateTime pastTime = DateTime.UtcNow.AddMinutes(-10);
                PlayerPrefs.SetString(key, pastTime.ToString());
            }
        }

        PlayerPrefs.Save();

        if (TowerUnlockManager.Instance != null)
            TowerUnlockManager.Instance.RefreshUnlockStates();

        RefreshProfile();
        ShowFeedback("Wait timers skipped!");
    }

    // -------------------------------------------------------------------------
    // Recipes
    // -------------------------------------------------------------------------

    private void UnlockAllRecipes()
    {
        // FIX: Use recipeStageIDs (not stageIDs) so the correct FIRST_CLEAR_ keys
        //      are written — the ones RecipeUnlockManager actually reads.
        if (recipeStageIDs != null)
        {
            foreach (string stage in recipeStageIDs)
            {
                if (string.IsNullOrEmpty(stage)) continue;
                PlayerPrefs.SetInt(FIRST_CLEAR_PREFIX + stage, 1);
                PlayerPrefs.SetInt(ATTEMPT_PREFIX + stage, 1);
            }
        }

        PlayerPrefs.Save();

        if (RecipeUnlockManager.Instance != null)
            RecipeUnlockManager.Instance.RefreshUnlockStates();

        RefreshProfile();
        ShowFeedback("All recipes unlocked!");
    }

    private void LockAllRecipes()
    {
        // FIX: Use recipeStageIDs for the same reason as above
        if (recipeStageIDs != null)
        {
            foreach (string stage in recipeStageIDs)
            {
                if (string.IsNullOrEmpty(stage)) continue;
                PlayerPrefs.DeleteKey(FIRST_CLEAR_PREFIX + stage);
            }
        }

        PlayerPrefs.Save();

        if (RecipeUnlockManager.Instance != null)
            RecipeUnlockManager.Instance.RefreshUnlockStates();

        RefreshProfile();
        ShowFeedback("All recipes locked!");
    }

    // -------------------------------------------------------------------------
    // Reset
    // -------------------------------------------------------------------------

    private void ResetEverything()
    {
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();

        if (TowerUnlockManager.Instance != null)
            TowerUnlockManager.Instance.RefreshUnlockStates();

        if (RecipeUnlockManager.Instance != null)
            RecipeUnlockManager.Instance.RefreshUnlockStates();

        if (RuneKeySystem.Instance != null)
            RuneKeySystem.Instance.RefreshAllDisplays();

        // FIX: Refresh profile display so it shows zeroed values immediately
        RefreshProfile();

        ShowFeedback("Everything reset!");
        Debug.Log("[DevCheatTool] Full reset done.");
    }

    // -------------------------------------------------------------------------
    // Feedback
    // -------------------------------------------------------------------------

    private void ShowFeedback(string message)
    {
        if (feedbackText == null) return;
        feedbackText.text = message;
        CancelInvoke(nameof(ClearFeedback));
        Invoke(nameof(ClearFeedback), 2f);
    }

    private void ClearFeedback()
    {
        if (feedbackText != null)
            feedbackText.text = "";
    }
}