using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class LevelInfoScreen : MonoBehaviour
{
    [Header("Score Display")]
    [SerializeField] private TMP_Text highScoreText;

    [Header("Badge Display")]
    [Tooltip("Assign in order: Genius, Conqueror, Challenger, Steps")]
    [SerializeField] private List<Image> badgeImages = new List<Image>();

    [Header("Stage Settings")]
    [SerializeField] private string stageID = "Stage_1";
    [Tooltip("Tower index: 0 = Tower1, 1 = Tower2, 2 = Tower3, 3 = Tower4")]
    [SerializeField] private int towerIndex = 0;

    [Header("UI")]
    [SerializeField] private Button challengeButton;
    [SerializeField] private TMP_Text runeKeyWarningText;

    [Header("Navigation")]
    [SerializeField] private SceneNavigationSystem sceneNavigation;

    [Header("Cost Display")]
    [SerializeField] private List<TMP_Text> costDisplayTexts = new List<TMP_Text>();

    [Header("Warning Animation")]
    [Tooltip("Assign the SquishSquashManager on the WARNING TEXT object only.")]
    [SerializeField] private SquishSquashManager warningShakeAnimation;
    [Tooltip("Auto-hide warning text after this many seconds. 0 = never hide.")]
    [SerializeField] private float warningAutoDismiss = 2f;

    private const string HIGH_SCORE_PREFIX = "HighScore_";
    private const string BADGE_PREFIX = "Badge_";
    private const string ATTEMPT_PREFIX = "Attempts_";

    private readonly Color unlockedColor = Color.white;
    private readonly Color lockedColor = new Color(0.4f, 0.4f, 0.4f, 1f);

    private const int COST_NEW_TOWER = 3;
    private const int COST_RECHALLENGE_TOWER = 2;
    private const int COST_NEW_TOWER4 = 4;
    private const int COST_RECHALLENGE_TOWER4 = 3;

    private int currentCost = 1;
    private Coroutine _warningDismissCoroutine;

    private void OnEnable()
    {
        // FIX: Subscribe to key change events so the button updates the moment keys change,
        //      even when this modal is already open
        RuneKeySystem.OnKeysChanged += OnKeysChanged;
        RefreshDisplay();
    }

    private void OnDisable()
    {
        // FIX: Always unsubscribe to avoid ghost callbacks after the modal closes
        RuneKeySystem.OnKeysChanged -= OnKeysChanged;
    }

    private void Start()
    {
        if (challengeButton != null)
        {
            challengeButton.onClick.RemoveAllListeners();
            challengeButton.onClick.AddListener(OnChallengeButtonClicked);
        }

        if (runeKeyWarningText != null)
            runeKeyWarningText.gameObject.SetActive(false);
    }

    // FIX: Called every time RuneKeySystem fires OnKeysChanged
    private void OnKeysChanged()
    {
        currentCost = CalculateCost();
        UpdateButtonState();
        DisplayCost();
    }

    // Called when challenge button is clicked
    // FIX: No key-check blocking here — the button is already disabled if keys are insufficient.
    //      This only runs when the player legitimately has enough keys.
    private void OnChallengeButtonClicked()
    {
        if (RuneKeySystem.Instance == null)
        {
            Debug.LogError("[LevelInfoScreen] RuneKeySystem not found!");
            return;
        }

        currentCost = CalculateCost();

        bool spent = RuneKeySystem.Instance.SpendKey(currentCost);

        if (spent)
        {
            Debug.Log("[LevelInfoScreen] Spent " + currentCost + " key(s). Proceeding to level...");
            HideWarning();

            if (warningShakeAnimation != null)
                warningShakeAnimation.PlaySquashAndStretch();

            NavigateToScene();
        }
        else
        {
            // Safety fallback — should not normally happen since button was interactable
            Debug.LogWarning("[LevelInfoScreen] SpendKey failed unexpectedly. Refreshing button state.");
            UpdateButtonState();
        }
    }

    private void NavigateToScene()
    {
        if (sceneNavigation != null)
            sceneNavigation.Navigate();
        else
            Debug.LogError("[LevelInfoScreen] SceneNavigationSystem not assigned!");
    }

    public void RefreshDisplay()
    {
        currentCost = CalculateCost();
        DisplayHighScore();
        DisplayBadges();
        DisplayCost();
        UpdateButtonState();
    }

    // FIX: Button interactability is the sole gatekeeper — no warning popup needed for blocking.
    //      The button is simply unclickable when the player lacks keys.
    private void UpdateButtonState()
    {
        if (challengeButton == null) return;

        bool hasEnough = RuneKeySystem.Instance != null && RuneKeySystem.Instance.HasEnoughKeys(currentCost);
        challengeButton.interactable = hasEnough;

        ColorBlock colors = challengeButton.colors;
        colors.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
        challengeButton.colors = colors;

        Debug.Log($"[LevelInfoScreen] Button interactable: {hasEnough} (need {currentCost} key(s), have {(RuneKeySystem.Instance != null ? RuneKeySystem.Instance.CurrentKeys : 0)})");
    }

    private int CalculateCost()
    {
        bool hasAttempted = PlayerPrefs.GetInt(ATTEMPT_PREFIX + stageID, 0) > 0;

        if (towerIndex == 3)
            return hasAttempted ? COST_RECHALLENGE_TOWER4 : COST_NEW_TOWER4;
        else
            return hasAttempted ? COST_RECHALLENGE_TOWER : COST_NEW_TOWER;
    }

    private void ShowWarning(string message)
    {
        if (runeKeyWarningText != null)
        {
            runeKeyWarningText.gameObject.SetActive(true);
            runeKeyWarningText.text = message;
        }

        if (warningAutoDismiss > 0f)
        {
            if (_warningDismissCoroutine != null)
                StopCoroutine(_warningDismissCoroutine);
            _warningDismissCoroutine = StartCoroutine(AutoDismissWarning());
        }
    }

    private void HideWarning()
    {
        if (_warningDismissCoroutine != null)
        {
            StopCoroutine(_warningDismissCoroutine);
            _warningDismissCoroutine = null;
        }

        if (runeKeyWarningText != null)
            runeKeyWarningText.gameObject.SetActive(false);
    }

    private IEnumerator AutoDismissWarning()
    {
        yield return new WaitForSeconds(warningAutoDismiss);
        HideWarning();
    }

    private void DisplayCost()
    {
        foreach (TMP_Text text in costDisplayTexts)
        {
            if (text != null)
                text.text = currentCost.ToString();
        }
    }

    private void DisplayHighScore()
    {
        int highScore = PlayerPrefs.GetInt(HIGH_SCORE_PREFIX + stageID, 0);
        if (highScoreText != null)
            highScoreText.text = highScore.ToString();
    }

    private void DisplayBadges()
    {
        AchievementType[] allTypes = new AchievementType[]
        {
            AchievementType.GeniusOfTheTower,
            AchievementType.ConquerorOfTheTower,
            AchievementType.ChallengerOfTheTower,
            AchievementType.StepsTowardsSuccess
        };

        for (int i = 0; i < badgeImages.Count; i++)
        {
            if (badgeImages[i] == null) continue;
            bool earned = IsBadgeEarned(allTypes[i]);
            badgeImages[i].color = earned ? unlockedColor : lockedColor;
        }
    }

    private bool IsBadgeEarned(AchievementType type)
    {
        string key = BADGE_PREFIX + stageID + "_" + type.ToString();
        return PlayerPrefs.GetInt(key, 0) == 1;
    }

    public bool CanProceed()
    {
        if (RuneKeySystem.Instance == null) return false;
        currentCost = CalculateCost();
        return RuneKeySystem.Instance.HasEnoughKeys(currentCost);
    }
}