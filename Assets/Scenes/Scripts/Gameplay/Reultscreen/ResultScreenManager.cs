using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class ResultScreenManager : MonoBehaviour
{
    [Header("Fade Settings")]
    [SerializeField] private Image fadeImage;
    [SerializeField] private float fadeDuration = 0.8f;

    [Header("Tower Title Objects")]
    [Tooltip("Assign in order: Tower1, Tower2, Tower3, Tower4 (English versions)")]
    [SerializeField] private List<GameObject> towerTitleObjectsEnglish = new List<GameObject>();

    [Tooltip("Assign in order: Tower1, Tower2, Tower3, Tower4 (Filipino versions)")]
    [SerializeField] private List<GameObject> towerTitleObjectsFilipino = new List<GameObject>();

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

    // ---------------------------------------------------------------
    // RewardType tells the system what to actually GRANT when a reward
    // button is tapped. Set this per-item in the Inspector.
    // ---------------------------------------------------------------
    public enum RewardType { None, RuneKey, Recipe }

    [System.Serializable]
    public class RewardItem
    {
        public string rewardName;
        [Tooltip("What this reward actually grants when collected")]
        public RewardType rewardType;
        public Button rewardButton;
        public ModalWindowScript rewardModal;
    }

    [SerializeField] private List<RewardItem> rewardItems = new List<RewardItem>();

    [Header("Navigation")]
    [SerializeField] private Button tapToContinueButton;
    [SerializeField] private string nextSceneName = "MapScene";

    // ---------------------------------------------------------------
    // PlayerPrefs key prefixes
    // ---------------------------------------------------------------
    private const string HIGH_SCORE_PREFIX = "HighScore_";
    private const string FIRST_CLEAR_PREFIX = "FirstClear_";
    private const string ATTEMPT_PREFIX = "Attempts_";
    private const string BADGE_PREFIX = "Badge_";
    private const string REWARDS_CLAIMED_PREFIX = "RewardsClaimed_";

    // ---------------------------------------------------------------
    // Runtime state (populated in Start, never changed after)
    // ---------------------------------------------------------------
    private int correct;
    private int wrong;
    private int total;
    private string stageID;
    private int towerIndex;

    // True when this is the very first time the player finishes this stage
    // (read BEFORE we write FirstClear_ to PlayerPrefs)
    private bool isFirstClear;
    private bool isFirstAttempt;
    private bool isTransitioning = false;

    private AchievementType earnedThisRun = AchievementType.None;

    // ---------------------------------------------------------------
    // Start — single entry point, order matters
    // ---------------------------------------------------------------
    private void Start()
    {
        if (tapToContinueButton != null)
            tapToContinueButton.interactable = false;

        // 1. Read result data
        correct = ResultData.GetCorrect();
        wrong = ResultData.GetWrong();
        total = ResultData.GetTotal();
        stageID = ResultData.GetStageID();
        towerIndex = ResultData.GetTowerIndex();

        Debug.Log($"[ResultScreenManager] stageID={stageID} correct={correct}/{total} towerIndex={towerIndex}");

        string firstClearKey = FIRST_CLEAR_PREFIX + stageID;
        string attemptKey = ATTEMPT_PREFIX + stageID;

        // 2. Snapshot flags BEFORE writing anything
        isFirstClear = PlayerPrefs.GetInt(firstClearKey, 0) == 0;
        isFirstAttempt = PlayerPrefs.GetInt(attemptKey, 0) == 0;

        // 3. Increment attempt counter
        int attempts = PlayerPrefs.GetInt(attemptKey, 0);
        PlayerPrefs.SetInt(attemptKey, attempts + 1);
        PlayerPrefs.Save();

        // 4. Update high score
        UpdateHighScore();

        // 5. Evaluate achievement for this run
        earnedThisRun = AchievementEvaluator.Evaluate(correct, total);
        Debug.Log($"[ResultScreenManager] earnedThisRun={earnedThisRun}  isFirstClear={isFirstClear}");

        // 6. Notify tower unlock system
        if (TowerUnlockManager.Instance != null)
            TowerUnlockManager.Instance.OnLevelCleared(stageID);

        // 7. Handle first-clear rewards (rune key + recipe unlock)
        //    This runs regardless of score — clearing ANY tower for the
        //    first time always grants a rune key and unlocks its recipe.
        if (isFirstClear)
        {
            // a) Persist first-clear flag NOW so everything below reads it correctly
            PlayerPrefs.SetInt(firstClearKey, 1);
            PlayerPrefs.Save();
            Debug.Log($"[ResultScreenManager] First clear! Saved {firstClearKey}=1");

            // b) Grant rune key automatically — the reward button just shows the notification
            GrantFirstClearRuneKey();

            // c) Refresh recipe unlock states so the recipe screen reflects the new unlock
            RefreshRecipeUnlocks();
        }

        // 8. Save badge and handle repeat-Genius rune key drop
        SaveBadgeIfEarned(earnedThisRun);

        // 9. Build UI
        DisplayTowerTitle();
        DisplayScores();
        DisplayAchievements();
        DisplayRewards();

        StartCoroutine(FadeInSequence());
    }

    // ---------------------------------------------------------------
    // First-clear rune key — always +1, no conditions
    // ---------------------------------------------------------------
    private void GrantFirstClearRuneKey()
    {
        if (RuneKeySystem.Instance != null)
        {
            RuneKeySystem.Instance.AddKey(1);
            Debug.Log("[ResultScreenManager] First-clear rune key granted (+1).");
        }
        else
        {
            Debug.LogWarning("[ResultScreenManager] RuneKeySystem.Instance is null — rune key NOT granted!");
        }
    }

    // ---------------------------------------------------------------
    // Recipe unlock
    // ---------------------------------------------------------------
    private void RefreshRecipeUnlocks()
    {
        if (RecipeUnlockManager.Instance != null)
        {
            RecipeUnlockManager.Instance.RefreshUnlockStates();
            Debug.Log($"[ResultScreenManager] Recipe unlock refreshed for stage: {stageID}");
        }
        else
        {
            Debug.LogWarning("[ResultScreenManager] RecipeUnlockManager.Instance is null — recipe NOT refreshed!");
        }
    }

    // ---------------------------------------------------------------
    // Badge save + repeat-Genius rune key drop (70 % chance)
    // This is separate from the first-clear reward above.
    // Genius on a RETRY tower = 70 % drop (not guaranteed).
    // ---------------------------------------------------------------
    private void SaveBadgeIfEarned(AchievementType achievement)
    {
        if (achievement == AchievementType.None) return;

        string key = BADGE_PREFIX + stageID + "_" + achievement.ToString();
        bool alreadyEarned = PlayerPrefs.GetInt(key, 0) == 1;

        PlayerPrefs.SetInt(key, 1);
        PlayerPrefs.Save();
        Debug.Log($"[ResultScreenManager] Badge saved: {key}");

        // Only the Genius badge triggers a rune key drop on retries
        if (achievement != AchievementType.GeniusOfTheTower) return;

        if (isFirstClear)
        {
            // First clear already gave a guaranteed rune key above.
            // Genius on first clear does NOT give a second key.
            Debug.Log("[ResultScreenManager] Genius on first clear — rune key already granted, no extra drop.");
            return;
        }

        // Retry run with Genius achievement — 70 % drop chance
        float roll = UnityEngine.Random.Range(0f, 1f);
        bool dropped = roll <= 0.70f;

        Debug.Log($"[ResultScreenManager] Repeat Genius! Roll={roll:F2} — {(dropped ? "KEY DROPPED!" : "No drop.")}");

        if (dropped && RuneKeySystem.Instance != null)
            RuneKeySystem.Instance.AddKey(1);
    }

    // ---------------------------------------------------------------
    // Display: tower title
    //
    // Picks the correct language-specific list first (English or
    // Filipino), then the correct tower index within that list.
    // Hides every title object in BOTH lists first so no stale
    // title from a previous run/language is left active.
    // ---------------------------------------------------------------
    private void DisplayTowerTitle()
    {
        foreach (GameObject obj in towerTitleObjectsEnglish)
            if (obj != null) obj.SetActive(false);
        foreach (GameObject obj in towerTitleObjectsFilipino)
            if (obj != null) obj.SetActive(false);

        bool isFilipino = LocalizationManager.Instance != null && LocalizationManager.Instance.IsFilipino;
        List<GameObject> activeList = isFilipino ? towerTitleObjectsFilipino : towerTitleObjectsEnglish;

        if (towerIndex >= 0 && towerIndex < activeList.Count && activeList[towerIndex] != null)
        {
            activeList[towerIndex].SetActive(true);
        }
        else
        {
            Debug.LogWarning($"[ResultScreenManager] No tower title object for index {towerIndex} (isFilipino={isFilipino})");
        }
    }

    // ---------------------------------------------------------------
    // Display: scores
    // ---------------------------------------------------------------
    private void DisplayScores()
    {
        if (correctAnswerValue != null) correctAnswerValue.text = correct.ToString();
        if (wrongAnswerValue != null) wrongAnswerValue.text = wrong.ToString();
        if (totalScoreValue != null) totalScoreValue.text = correct.ToString();

        int highScore = GetHighScore();
        if (highScoreValue != null)
        {
            highScoreValue.gameObject.SetActive(!isFirstAttempt);
            highScoreValue.text = highScore.ToString();
        }
    }

    // ---------------------------------------------------------------
    // Display: achievement badges
    // ---------------------------------------------------------------
    private void DisplayAchievements()
    {
        AchievementType[] allTypes =
        {
            AchievementType.GeniusOfTheTower,
            AchievementType.ConquerorOfTheTower,
            AchievementType.ChallengerOfTheTower,
            AchievementType.StepsTowardsSuccess
        };

        for (int i = 0; i < achievementButtons.Count; i++)
        {
            if (achievementButtons[i] == null) continue;

            bool everEarned = IsBadgeEverEarned(allTypes[i]);
            achievementButtons[i].gameObject.SetActive(true);

            if (everEarned)
            {
                achievementButtons[i].image.color = Color.white;
                achievementButtons[i].interactable = true;

                int capturedIndex = i;
                achievementButtons[i].onClick.RemoveAllListeners();
                achievementButtons[i].onClick.AddListener(() => OpenAchievementModal(capturedIndex));
            }
            else
            {
                achievementButtons[i].image.color = new Color(0.4f, 0.4f, 0.4f, 1f);
                achievementButtons[i].interactable = false;
                achievementButtons[i].onClick.RemoveAllListeners();
            }
        }
    }

    // ---------------------------------------------------------------
    // Display: first-clear reward buttons
    //
    // The rune key has ALREADY been granted in Start() above.
    // These buttons are just visual notifications / modals —
    // they do NOT grant anything again when tapped.
    // ---------------------------------------------------------------
    private void DisplayRewards()
    {
        if (rewardsSection == null)
        {
            Debug.LogError("[ResultScreenManager] rewardsSection is null!");
            return;
        }

        // Hide all reward buttons first
        foreach (RewardItem reward in rewardItems)
            if (reward?.rewardButton != null)
                reward.rewardButton.gameObject.SetActive(false);

        string rewardKey = REWARDS_CLAIMED_PREFIX + stageID;
        bool rewardsAlreadyShown = PlayerPrefs.GetInt(rewardKey, 0) == 1;

        // Only show rewards panel on the very first clear,
        // and only once (never on subsequent visits to this result screen).
        if (!isFirstClear || rewardsAlreadyShown)
        {
            rewardsSection.SetActive(false);
            Debug.Log($"[ResultScreenManager] Rewards panel hidden. isFirstClear={isFirstClear} alreadyShown={rewardsAlreadyShown}");
            return;
        }

        // Mark as shown permanently
        PlayerPrefs.SetInt(rewardKey, 1);
        PlayerPrefs.Save();

        rewardsSection.SetActive(true);
        Debug.Log("[ResultScreenManager] Showing first-clear rewards panel.");

        foreach (RewardItem reward in rewardItems)
        {
            if (reward?.rewardButton == null) continue;

            reward.rewardButton.gameObject.SetActive(true);
            reward.rewardButton.onClick.RemoveAllListeners();

            // Tapping just opens the info modal — the reward itself was granted in Start()
            if (reward.rewardModal != null)
            {
                ModalWindowScript capturedModal = reward.rewardModal;
                reward.rewardButton.onClick.AddListener(() => capturedModal.Show());
            }

            Debug.Log($"[ResultScreenManager] Reward button shown: {reward.rewardName} ({reward.rewardType})");
        }
    }

    // ---------------------------------------------------------------
    // Helpers
    // ---------------------------------------------------------------
    private void OpenAchievementModal(int index)
    {
        if (index < 0 || index >= achievementModals.Count) return;
        achievementModals[index]?.Show();
    }

    private bool IsBadgeEverEarned(AchievementType achievement)
    {
        return PlayerPrefs.GetInt(BADGE_PREFIX + stageID + "_" + achievement.ToString(), 0) == 1;
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

        int totalAnswered = PlayerPrefs.GetInt("TotalQuestionsAnswered", 0);
        PlayerPrefs.SetInt("TotalQuestionsAnswered", totalAnswered + correct);
        PlayerPrefs.Save();
    }

    private int GetHighScore() => PlayerPrefs.GetInt(HIGH_SCORE_PREFIX + stageID, 0);

    // ---------------------------------------------------------------
    // Fade helpers
    // ---------------------------------------------------------------
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

    private void OnTapToContinue()
    {
        if (isTransitioning) return;
        StartCoroutine(FadeOutSequence());
    }
}