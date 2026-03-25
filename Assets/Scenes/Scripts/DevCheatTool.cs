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

    [Header("Settings")]
    [SerializeField] private bool enableInBuild = false;

    private const string HIGH_SCORE_PREFIX = "HighScore_";
    private const string FIRST_CLEAR_PREFIX = "FirstClear_";
    private const string BADGE_PREFIX = "Badge_";
    private const string UNLOCK_TIME_PREFIX = "UnlockTime_";
    private const string UNLOCKED_PREFIX = "Unlocked_";

    private string[] stageIDs = { "Stage_1", "Stage_2", "Stage_3", "Stage_4" };
    private string[] towerNames = { "Tower1", "Tower2", "Tower3", "Tower4" };

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

    private void AddRuneKey()
    {
        if (RuneKeySystem.Instance != null)
            RuneKeySystem.Instance.AddKey(1);
        ShowFeedback("+1 Rune Key added!");
    }

    private void MaxRuneKeys()
    {
        if (RuneKeySystem.Instance != null)
            RuneKeySystem.Instance.AddKey(99);
        ShowFeedback("Rune Keys maxed!");
    }

    private void ZeroRuneKeys()
    {
        PlayerPrefs.SetInt("RuneKeys", 0);
        PlayerPrefs.Save();
        ShowFeedback("Rune Keys set to 0!");
    }

    private void SetScore(string stageID)
    {
        if (scoreInputField == null) return;

        if (int.TryParse(scoreInputField.text, out int score))
        {
            PlayerPrefs.SetInt(HIGH_SCORE_PREFIX + stageID, score);
            PlayerPrefs.Save();

            if (TowerUnlockManager.Instance != null)
                TowerUnlockManager.Instance.RefreshUnlockStates();

            ShowFeedback(stageID + " score set to " + score + "!");
        }
        else
        {
            ShowFeedback("Invalid score input!");
        }
    }

    private void UnlockAllTowers()
    {
        foreach (string tower in towerNames)
        {
            PlayerPrefs.SetInt(UNLOCKED_PREFIX + tower, 1);
            PlayerPrefs.DeleteKey(UNLOCK_TIME_PREFIX + tower);
        }

        // Set max scores for all stages
        foreach (string stage in stageIDs)
            PlayerPrefs.SetInt(HIGH_SCORE_PREFIX + stage, 10);

        PlayerPrefs.Save();

        if (TowerUnlockManager.Instance != null)
            TowerUnlockManager.Instance.RefreshUnlockStates();

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
            PlayerPrefs.DeleteKey(HIGH_SCORE_PREFIX + stage);

        PlayerPrefs.Save();

        if (TowerUnlockManager.Instance != null)
            TowerUnlockManager.Instance.RefreshUnlockStates();

        ShowFeedback("All towers locked!");
    }

    private void SkipWaitTimers()
    {
        foreach (string tower in towerNames)
        {
            string key = UNLOCK_TIME_PREFIX + tower;
            if (!string.IsNullOrEmpty(PlayerPrefs.GetString(key, "")))
            {
                // Set time to 10 minutes ago so timer expires immediately
                DateTime pastTime = DateTime.UtcNow.AddMinutes(-10);
                PlayerPrefs.SetString(key, pastTime.ToString());
            }
        }

        PlayerPrefs.Save();

        if (TowerUnlockManager.Instance != null)
            TowerUnlockManager.Instance.RefreshUnlockStates();

        ShowFeedback("Wait timers skipped!");
    }

    private void UnlockAllRecipes()
    {
        foreach (string stage in stageIDs)
        {
            PlayerPrefs.SetInt(FIRST_CLEAR_PREFIX + stage, 1);
        }

        PlayerPrefs.Save();

        if (RecipeUnlockManager.Instance != null)
            RecipeUnlockManager.Instance.RefreshUnlockStates();

        ShowFeedback("All recipes unlocked!");
    }

    private void LockAllRecipes()
    {
        foreach (string stage in stageIDs)
            PlayerPrefs.DeleteKey(FIRST_CLEAR_PREFIX + stage);

        PlayerPrefs.Save();

        if (RecipeUnlockManager.Instance != null)
            RecipeUnlockManager.Instance.RefreshUnlockStates();

        ShowFeedback("All recipes locked!");
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
        ShowFeedback("All badges cleared!");
    }

    private void ResetEverything()
    {
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();

        if (HintSystem.Instance != null)
            HintSystem.Instance.ResetHints();

        if (TowerUnlockManager.Instance != null)
            TowerUnlockManager.Instance.RefreshUnlockStates();

        if (RecipeUnlockManager.Instance != null)
            RecipeUnlockManager.Instance.RefreshUnlockStates();

        ShowFeedback("Everything reset!");
        Debug.Log("[DevCheatTool] Full reset done.");
    }

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