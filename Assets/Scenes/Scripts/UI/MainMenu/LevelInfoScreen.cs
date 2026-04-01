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

    // Cost constants //
    private const int COST_NEW_TOWER = 2;
    private const int COST_RECHALLENGE_TOWER = 1;
    private const int COST_NEW_TOWER4 = 3;
    private const int COST_RECHALLENGE_TOWER4 = 2;

    private int currentCost = 1;
    private Coroutine _warningDismissCoroutine;

    private void Start()
    {
        currentCost = CalculateCost();

        DisplayHighScore();
        DisplayBadges();
        DisplayCost();

        if (challengeButton != null)
            challengeButton.onClick.AddListener(OnChallenge);

        if (runeKeyWarningText != null)
            runeKeyWarningText.gameObject.SetActive(false);
    }

    // Calculate cost based on tower index and attempt history //
    private int CalculateCost()
    {
        bool hasAttempted = PlayerPrefs.GetInt(ATTEMPT_PREFIX + stageID, 0) > 0;

        if (towerIndex == 3)
            return hasAttempted ? COST_RECHALLENGE_TOWER4 : COST_NEW_TOWER4;
        else
            return hasAttempted ? COST_RECHALLENGE_TOWER : COST_NEW_TOWER;
    }

    private void OnChallenge()
    {
        if (RuneKeySystem.Instance == null)
        {
            Debug.LogError("[LevelInfoScreen] RuneKeySystem not found!");
            return;
        }

        currentCost = CalculateCost();

        if (!RuneKeySystem.Instance.HasEnoughKeys(currentCost))
        {
            Debug.Log("[LevelInfoScreen] Not enough keys! Need: " + currentCost);
            ShowWarning("Not enough Rune Keys! Need " + currentCost + ".");
            return;
        }

        // Enough keys — play squash stretch on button then spend //
        if (warningShakeAnimation != null)
        {
            warningShakeAnimation.StopShake();
            warningShakeAnimation.PlaySquashAndStretch();
        }

        HideWarning();
        bool spent = RuneKeySystem.Instance.SpendKey(currentCost);

        if (spent)
            Debug.Log("[LevelInfoScreen] Spent " + currentCost + " key(s).");
    }

    // Show warning text and play shake on warning ONLY //
    private void ShowWarning(string message)
    {
        if (runeKeyWarningText != null)
        {
            runeKeyWarningText.gameObject.SetActive(true);
            runeKeyWarningText.text = message;
        }

        // Not enough keys — play shake //
        if (warningShakeAnimation != null)
        {
            warningShakeAnimation.StopShake();
            warningShakeAnimation.PlayShake();
        }

        // Auto dismiss if set //
        if (warningAutoDismiss > 0f)
        {
            if (_warningDismissCoroutine != null)
                StopCoroutine(_warningDismissCoroutine);

            _warningDismissCoroutine = StartCoroutine(AutoDismissWarning());
        }
    }

    // Hide warning text //
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

    // Auto hide warning after delay //
    private IEnumerator AutoDismissWarning()
    {
        yield return new WaitForSeconds(warningAutoDismiss);
        HideWarning();
    }

    // Show cost as number only //
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
}