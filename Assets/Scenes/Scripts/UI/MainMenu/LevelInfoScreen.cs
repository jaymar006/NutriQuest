using UnityEngine;
using UnityEngine.UI;
using TMPro;
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
    [SerializeField] private SquishSquashManager warningAnimation; // Assign your animation component here
    [Tooltip("Play animation on the warning text itself")]
    [SerializeField] private bool animateWarningText = true;
    [Tooltip("Play animation on the button when warning appears")]
    [SerializeField] private bool animateButton = false;
    [SerializeField] private SquishSquashManager buttonAnimation; // Optional button animation
    [Tooltip("Play shake animation on warning")]
    [SerializeField] private bool shakeOnWarning = false; // Separate option for shake

    private const string HIGH_SCORE_PREFIX = "HighScore_";
    private const string BADGE_PREFIX = "Badge_";
    private const string ATTEMPT_PREFIX = "Attempts_";

    private readonly Color unlockedColor = Color.white;
    private readonly Color lockedColor = new Color(0.4f, 0.4f, 0.4f, 1f);

    // Cost constants
    private const int COST_NEW_TOWER = 2;
    private const int COST_RECHALLENGE_TOWER = 1;
    private const int COST_NEW_TOWER4 = 3;
    private const int COST_RECHALLENGE_TOWER4 = 2;

    private int currentCost = 1;

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
            // Show warning text
            if (runeKeyWarningText != null)
            {
                runeKeyWarningText.gameObject.SetActive(true);
                runeKeyWarningText.text = "Not enough Rune Keys! Need " + currentCost + ".";

                // Play warning animation
                PlayWarningAnimation();
            }
            return;
        }

        RuneKeySystem.Instance.SpendKey(currentCost);
    }

    // Play the warning animation
    private void PlayWarningAnimation()
    {
        // Animate the warning text
        if (animateWarningText && warningAnimation != null)
        {
            // Play squash and stretch
            warningAnimation.PlaySquashAndStretch();

            // Play shake if enabled
            if (shakeOnWarning)
            {
                warningAnimation.PlayShake();
            }
        }
        else if (animateWarningText && runeKeyWarningText != null)
        {
            // Try to get the component from the warning text itself
            SquishSquashManager textAnim = runeKeyWarningText.GetComponent<SquishSquashManager>();
            if (textAnim != null)
            {
                textAnim.PlaySquashAndStretch();

                if (shakeOnWarning)
                {
                    textAnim.PlayShake();
                }
            }
        }

        // Animate the button
        if (animateButton && buttonAnimation != null)
        {
            buttonAnimation.PlaySquashAndStretch();
        }
    }

    // Public method to manually trigger warning animation (if needed)
    public void TriggerWarningAnimation()
    {
        PlayWarningAnimation();
    }

    private void DisplayCost()
    {
        string label = towerIndex == 3 ? "Rune Key" : "Stamina";
        string costText = $"Cost: {currentCost} {label}{(currentCost > 1 ? "s" : "")}";

        foreach (TMP_Text text in costDisplayTexts)
        {
            if (text != null)
                text.text = costText;
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