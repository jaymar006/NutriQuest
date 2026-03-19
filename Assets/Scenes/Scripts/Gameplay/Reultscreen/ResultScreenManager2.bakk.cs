using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class ResultScreenManager2 : MonoBehaviour
{
    [Header("Fade Settings")]
    [SerializeField] private Image fadeImage;
    [SerializeField] private float fadeDuration = 0.8f;

    [Header("Tower Title Objects")]
    [Tooltip("Assign in order: Tower1, Tower2, Tower3, Tower4")]
    [SerializeField] private List<GameObject> towerTitleObjects = new List<GameObject>();

    [Header("Results Image")]
    [SerializeField] private Image resultsImage;

    [Header("Score Values")]
    [SerializeField] private TMP_Text correctAnswerValue;
    [SerializeField] private TMP_Text wrongAnswerValue;
    [SerializeField] private TMP_Text highScoreValue;
    [SerializeField] private TMP_Text totalScoreValue;

    [Header("Achievement Buttons")]
    [Tooltip("Assign in order: Genius, Conqueror, Challenger, Steps")]
    [SerializeField] private List<Button> achievementButtons = new List<Button>();
    [Tooltip("Assign matching modals in same order: Genius, Conqueror, Challenger, Steps")]
    [SerializeField] private List<ModalWindowScript> achievementModals = new List<ModalWindowScript>();

    [Header("Rewards Section")]
    [SerializeField] private GameObject rewardsSection;
    [SerializeField] private Transform rewardsContainer;

    [System.Serializable]
    public class RewardItem
    {
        public string rewardName;
        public Button rewardButton;
        public ModalWindowScript rewardModal;
    }

    [SerializeField] private List<RewardItem> rewardItems = new List<RewardItem>();

    [Header("Navigation")]
    [SerializeField] private Button tapToContinueButton;
    [SerializeField] private string nextSceneName = "MapScene";

    [Header("Settings")]
    [SerializeField] private float replenishThreshold = 0.85f;

    private const string HIGH_SCORE_PREFIX = "HighScore_";
    private const string FIRST_CLEAR_PREFIX = "FirstClear_";
    private const string ATTEMPT_PREFIX = "Attempts_";
    private const string BADGE_PREFIX = "Badge_";

    private int correct;
    private int wrong;
    private int total;
    private string stageID;
    private int towerIndex;
    private bool isFirstClear;
    private bool isFirstAttempt;
    private bool isTransitioning = false;

    private AchievementType earnedThisRun = AchievementType.None;

    private void Start()
    {
        if (tapToContinueButton != null)
            tapToContinueButton.interactable = false;

        correct = ResultData.GetCorrect();
        wrong = ResultData.GetWrong();
        total = ResultData.GetTotal();
        stageID = ResultData.GetStageID();
        towerIndex = ResultData.GetTowerIndex();

        Debug.Log("[ResultScreenManager] correct: " + correct);
        Debug.Log("[ResultScreenManager] wrong: " + wrong);
        Debug.Log("[ResultScreenManager] total: " + total);
        Debug.Log("[ResultScreenManager] stageID: " + stageID);
        Debug.Log("[ResultScreenManager] percent: " + ((float)correct / total));

        string firstClearKey = FIRST_CLEAR_PREFIX + stageID;
        string attemptKey = ATTEMPT_PREFIX + stageID;
        isFirstClear = PlayerPrefs.GetInt(firstClearKey, 0) == 0;
        isFirstAttempt = PlayerPrefs.GetInt(attemptKey, 0) == 0;

        Debug.Log("[ResultScreenManager] isFirstClear: " + isFirstClear);
        Debug.Log("[ResultScreenManager] isFirstAttempt: " + isFirstAttempt);

        int attempts = PlayerPrefs.GetInt(attemptKey, 0);
        PlayerPrefs.SetInt(attemptKey, attempts + 1);
        PlayerPrefs.Save();

        UpdateHighScore();

        earnedThisRun = AchievementEvaluator.Evaluate(correct, total, isFirstAttempt);
        Debug.Log("[ResultScreenManager] earnedThisRun: " + earnedThisRun);

        SaveBadgeIfEarned(earnedThisRun);

        if (isFirstClear && correct >= AchievementData.GetPassTarget(stageID))
        {
            PlayerPrefs.SetInt(firstClearKey, 1);
            PlayerPrefs.Save();
        }

        DisplayTowerTitle();
        DisplayScores();
        DisplayAchievements();
        DisplayRewards();

        StartCoroutine(FadeInSequence());
    }

    private void SaveBadgeIfEarned(AchievementType achievement)
    {
        if (achievement == AchievementType.None) return;

        string key = BADGE_PREFIX + stageID + "_" + achievement.ToString();
        PlayerPrefs.SetInt(key, 1);
        PlayerPrefs.Save();

        Debug.Log("[ResultScreenManager] Badge saved: " + key);
    }

    private bool IsBadgeEverEarned(AchievementType achievement)
    {
        string key = BADGE_PREFIX + stageID + "_" + achievement.ToString();
        return PlayerPrefs.GetInt(key, 0) == 1;
    }

    private void SetupFadeImage()
    {
        if (fadeImage == null) return;

        RectTransform rt = fadeImage.GetComponent<RectTransform>();
        if (rt != null)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        Canvas canvas = fadeImage.GetComponentInParent<Canvas>();
        if (canvas != null)
        {
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 999;
        }

        fadeImage.color = Color.black;
    }

    private IEnumerator FadeIn()
    {
        if (fadeImage == null) yield break;

        SetupFadeImage();
        fadeImage.gameObject.SetActive(true);
        fadeImage.canvasRenderer.SetAlpha(1f);

        float time = 0f;
        while (time < fadeDuration)
        {
            time += Time.deltaTime;
            fadeImage.canvasRenderer.SetAlpha(Mathf.Lerp(1f, 0f, time / fadeDuration));
            yield return null;
        }

        fadeImage.canvasRenderer.SetAlpha(0f);
        fadeImage.gameObject.SetActive(false);
    }

    private IEnumerator FadeOut()
    {
        if (fadeImage == null) yield break;

        SetupFadeImage();
        fadeImage.gameObject.SetActive(true);
        fadeImage.canvasRenderer.SetAlpha(0f);

        float time = 0f;
        while (time < fadeDuration)
        {
            time += Time.deltaTime;
            fadeImage.canvasRenderer.SetAlpha(Mathf.Lerp(0f, 1f, time / fadeDuration));
            yield return null;
        }

        fadeImage.canvasRenderer.SetAlpha(1f);
    }

    private IEnumerator FadeInSequence()
    {
        yield return StartCoroutine(FadeIn());

        if (tapToContinueButton != null)
        {
            tapToContinueButton.interactable = true;
            tapToContinueButton.onClick.AddListener(OnTapToContinue);
        }
    }

    private IEnumerator FadeOutSequence()
    {
        isTransitioning = true;

        if (tapToContinueButton != null)
            tapToContinueButton.interactable = false;

        yield return StartCoroutine(FadeOut());

        Debug.Log("[ResultScreenManager] Loading: " + nextSceneName);
        UnityEngine.SceneManagement.SceneManager.LoadScene(nextSceneName);
    }

    private void DisplayTowerTitle()
    {
        foreach (GameObject obj in towerTitleObjects)
        {
            if (obj != null)
                obj.SetActive(false);
        }

        if (towerTitleObjects != null &&
            towerIndex >= 0 &&
            towerIndex < towerTitleObjects.Count &&
            towerTitleObjects[towerIndex] != null)
        {
            towerTitleObjects[towerIndex].SetActive(true);
        }
        else
        {
            Debug.LogWarning("[ResultScreenManager] Tower title object not found " +
                "for index: " + towerIndex);
        }
    }

    private void DisplayScores()
    {
        if (correctAnswerValue != null)
            correctAnswerValue.text = correct.ToString();

        if (wrongAnswerValue != null)
            wrongAnswerValue.text = wrong.ToString();

        if (totalScoreValue != null)
            totalScoreValue.text = correct.ToString();

        int highScore = GetHighScore();
        if (highScoreValue != null)
        {
            highScoreValue.gameObject.SetActive(!isFirstAttempt);
            highScoreValue.text = highScore.ToString();
        }
    }

    private void DisplayAchievements()
    {
        AchievementType[] allTypes = new AchievementType[]
        {
        AchievementType.GeniusOfTheTower,
        AchievementType.ConquerorOfTheTower,
        AchievementType.ChallengerOfTheTower,
        AchievementType.StepsTowardsSuccess
        };

        for (int i = 0; i < achievementButtons.Count; i++)
        {
            if (achievementButtons[i] == null) continue;

            AchievementType thisType = allTypes[i];
            bool everEarned = IsBadgeEverEarned(thisType);

            // Always show all badges
            achievementButtons[i].gameObject.SetActive(true);

            if (everEarned)
            {
                // Player earned this badge in some run — full color, clickable
                achievementButtons[i].image.color = Color.white;
                achievementButtons[i].interactable = true;

                int modalIndex = i;
                achievementButtons[i].onClick.RemoveAllListeners();
                achievementButtons[i].onClick.AddListener(() =>
                    OpenAchievementModal(modalIndex));
            }
            else
            {
                // Never earned this badge — greyed out, not clickable
                achievementButtons[i].image.color = new Color(0.4f, 0.4f, 0.4f, 1f);
                achievementButtons[i].interactable = false;
                achievementButtons[i].onClick.RemoveAllListeners();
            }
        }
    }

    private void DisplayRewards()
    {
        if (rewardsSection == null)
        {
            Debug.LogError("[ResultScreenManager] rewardsSection is null!");
            return;
        }

        // Hide all reward buttons first
        foreach (RewardItem reward in rewardItems)
        {
            if (reward.rewardButton != null)
                reward.rewardButton.gameObject.SetActive(false);
        }

        // Check if rewards were already claimed using PlayerPrefs
        string rewardKey = "RewardsClaimed_" + stageID;
        bool rewardsAlreadyClaimed = PlayerPrefs.GetInt(rewardKey, 0) == 1;

        if (!isFirstClear || rewardsAlreadyClaimed)
        {
            rewardsSection.SetActive(false);
            Debug.Log("[ResultScreenManager] Rewards already claimed or not first clear.");
            return;
        }

        // Mark rewards as claimed permanently
        PlayerPrefs.SetInt(rewardKey, 1);
        PlayerPrefs.Save();

        rewardsSection.SetActive(true);
        Debug.Log("[ResultScreenManager] Rewards section activated — first time!");

        foreach (RewardItem reward in rewardItems)
        {
            if (reward == null) continue;

            if (reward.rewardButton != null)
            {
                reward.rewardButton.gameObject.SetActive(true);
                reward.rewardButton.onClick.RemoveAllListeners();

                if (reward.rewardModal != null)
                {
                    ModalWindowScript modal = reward.rewardModal;
                    reward.rewardButton.onClick.AddListener(() => modal.Show());
                }

                Debug.Log("[ResultScreenManager] Reward shown: " + reward.rewardName);
            }
        }
    }
    private void OpenAchievementModal(int index)
    {
        if (index < 0 || index >= achievementModals.Count) return;

        ModalWindowScript modal = achievementModals[index];
        if (modal != null)
            modal.Show();

        Debug.Log("[ResultScreenManager] Opening achievement modal index: " + index);
    }

    private void UpdateHighScore()
    {
        string key = HIGH_SCORE_PREFIX + stageID;
        int current = PlayerPrefs.GetInt(key, 0);
        if (correct > current)
        {
            PlayerPrefs.SetInt(key, correct);
            PlayerPrefs.Save();
        }
    }

    private int GetHighScore()
    {
        return PlayerPrefs.GetInt(HIGH_SCORE_PREFIX + stageID, 0);
    }

    private void OnTapToContinue()
    {
        if (isTransitioning) return;
        StartCoroutine(FadeOutSequence());
    }
}