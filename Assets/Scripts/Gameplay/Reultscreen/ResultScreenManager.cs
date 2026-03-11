using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class ResultScreenManager : MonoBehaviour
{
    [Header("Tower Title Images")]
    [Tooltip("Assign in order: Tower1, Tower2, Tower3, Tower4")]
    [SerializeField] private List<Sprite> towerTitleSprites = new List<Sprite>();
    [SerializeField] private Image towerTitleImage;

    [Header("Results Image")]
    [SerializeField] private Image resultsImage;

    [Header("Score Values")]
    [Tooltip("These are the NUMBER texts only — the ones showing 0")]
    [SerializeField] private TMP_Text correctAnswerValue;
    [SerializeField] private TMP_Text wrongAnswerValue;
    [SerializeField] private TMP_Text highScoreValue;
    [SerializeField] private TMP_Text totalScoreValue;

    [Header("Achievement")]
    [SerializeField] private GameObject achievementSection;
    [SerializeField] private TMP_Text achievementTitleText;
    [SerializeField] private TMP_Text achievementDescText;
    [SerializeField] private Image achievementIcon;

    [Header("Achievement Icons")]
    [SerializeField] private Sprite geniusIcon;
    [SerializeField] private Sprite conquerorIcon;
    [SerializeField] private Sprite challengerIcon;
    [SerializeField] private Sprite stepsIcon;

    [Header("Rewards Section")]
    [SerializeField] private GameObject rewardsSection;
    [SerializeField] private Transform rewardsContainer;
    [SerializeField] private List<GameObject> rewardItems = new List<GameObject>();

    [Header("Navigation")]
    [SerializeField] private Button tapToContinueButton;
    [SerializeField] private string nextSceneName = "MapScene";

    [Header("Settings")]
    [SerializeField] private float replenishThreshold = 0.85f;

    private const string HIGH_SCORE_PREFIX = "HighScore_";
    private const string FIRST_CLEAR_PREFIX = "FirstClear_";
    private const string ATTEMPT_PREFIX = "Attempts_";

    private int correct;
    private int wrong;
    private int total;
    private string stageID;
    private int towerIndex;
    private bool isFirstClear;
    private bool isFirstAttempt;

    private void Start()
    {
        // Load result data
        correct = ResultData.GetCorrect();
        wrong = ResultData.GetWrong();
        total = ResultData.GetTotal();
        stageID = ResultData.GetStageID();
        towerIndex = ResultData.GetTowerIndex();

        // First clear and attempt tracking
        string firstClearKey = FIRST_CLEAR_PREFIX + stageID;
        string attemptKey = ATTEMPT_PREFIX + stageID;
        isFirstClear = PlayerPrefs.GetInt(firstClearKey, 0) == 0;
        isFirstAttempt = PlayerPrefs.GetInt(attemptKey, 0) == 0;

        // Increment attempt counter
        int attempts = PlayerPrefs.GetInt(attemptKey, 0);
        PlayerPrefs.SetInt(attemptKey, attempts + 1);
        PlayerPrefs.Save();

        // Update high score
        UpdateHighScore();

        // Display everything
        DisplayTowerTitle();
        DisplayScores();
        DisplayAchievement();
        DisplayRewards();

        // Mark as cleared if passed
        if (isFirstClear && correct >= AchievementData.GetPassTarget(stageID))
        {
            PlayerPrefs.SetInt(firstClearKey, 1);
            PlayerPrefs.Save();
        }

        // Hook up button
        if (tapToContinueButton != null)
            tapToContinueButton.onClick.AddListener(OnTapToContinue);
    }

    private void DisplayTowerTitle()
    {
        if (towerTitleImage != null &&
            towerTitleSprites != null &&
            towerIndex < towerTitleSprites.Count &&
            towerTitleSprites[towerIndex] != null)
        {
            towerTitleImage.sprite = towerTitleSprites[towerIndex];
        }
        else
        {
            Debug.LogWarning("[ResultScreenManager] Tower title sprite not found " +
                "for index: " + towerIndex);
        }
    }

    private void DisplayScores()
    {
        // Only update the VALUE text — labels stay as designed in Unity
        if (correctAnswerValue != null)
            correctAnswerValue.text = correct.ToString();

        if (wrongAnswerValue != null)
            wrongAnswerValue.text = wrong.ToString();

        if (totalScoreValue != null)
            totalScoreValue.text = correct.ToString();

        // High score value — only show on rechallenge
        int highScore = GetHighScore();
        if (highScoreValue != null)
        {
            highScoreValue.gameObject.SetActive(!isFirstAttempt);
            highScoreValue.text = highScore.ToString();
        }
    }

    private void DisplayAchievement()
    {
        if (achievementSection == null) return;

        AchievementType achievement = AchievementEvaluator.Evaluate(
            correct, total, stageID);

        if (achievement == AchievementType.None)
        {
            achievementSection.SetActive(false);
            return;
        }

        achievementSection.SetActive(true);

        switch (achievement)
        {
            case AchievementType.GeniusOfTheTower:
                SetAchievement(
                    "Genius of the Tower",
                    "Perfect score! Absolutely brilliant!",
                    geniusIcon);
                break;

            case AchievementType.ConquerorOfTheTower:
                SetAchievement(
                    "Conqueror of the Tower",
                    "Cleared with an outstanding score!",
                    conquerorIcon);
                break;

            case AchievementType.ChallengerOfTheTower:
                SetAchievement(
                    "Challenger of the Tower",
                    "Cleared the tower! Keep pushing!",
                    challengerIcon);
                break;

            case AchievementType.StepsTowardsSuccess:
                SetAchievement(
                    "Steps Towards Success",
                    "Every step counts. Keep trying!",
                    stepsIcon);
                break;
        }

        Debug.Log("[ResultScreenManager] Achievement: " + achievement);
    }

    private void SetAchievement(string title, string desc, Sprite icon)
    {
        if (achievementTitleText != null) achievementTitleText.text = title;
        if (achievementDescText != null) achievementDescText.text = desc;
        if (achievementIcon != null && icon != null)
            achievementIcon.sprite = icon;
    }

    private void DisplayRewards()
    {
        if (rewardsSection == null) return;

        if (!isFirstClear)
        {
            rewardsSection.SetActive(false);
            return;
        }

        rewardsSection.SetActive(true);

        if (rewardsContainer != null)
        {
            foreach (GameObject reward in rewardItems)
            {
                if (reward != null)
                    Instantiate(reward, rewardsContainer).SetActive(true);
            }
        }

        // Replenish hint if 85%+
        float scorePercent = (float)correct / total;
        if (scorePercent >= replenishThreshold)
        {
            int currentHints = PlayerPrefs.GetInt("PlayerHints", 3);
            if (currentHints < 3)
            {
                PlayerPrefs.SetInt("PlayerHints", currentHints + 1);
                PlayerPrefs.Save();
                Debug.Log("[ResultScreenManager] Hint replenished!");
            }
        }
    }

    private void UpdateHighScore()
    {
        string key = HIGH_SCORE_PREFIX + stageID;
        int prevBest = PlayerPrefs.GetInt(key, 0);

        if (correct > prevBest)
        {
            PlayerPrefs.SetInt(key, correct);
            PlayerPrefs.Save();
            Debug.Log("[ResultScreenManager] New high score: " + correct);
        }
    }

    private int GetHighScore()
    {
        return PlayerPrefs.GetInt(HIGH_SCORE_PREFIX + stageID, 0);
    }

    private void OnTapToContinue()
    {
        if (SceneTransitionManager.Instance != null)
            SceneTransitionManager.Instance.NavigateTo(nextSceneName, false);
        else
            UnityEngine.SceneManagement.SceneManager.LoadScene(nextSceneName);
    }
}