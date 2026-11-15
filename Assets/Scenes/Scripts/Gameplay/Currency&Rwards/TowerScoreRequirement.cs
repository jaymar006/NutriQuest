using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// Shows "18 / 26" for unlocking a tower.
/// Hides the panel once the required score is reached.
/// Put this script on an object that stays active (NOT on the panel it hides).
/// </summary>
public class TowerScoreRequirement : MonoBehaviour
{
    [Header("Requirement")]
    [SerializeField] private int towerToUnlock = 4;
    [Tooltip("TowerUnlockManager overrides this with its Tower 4 Required value")]
    [SerializeField] private int requiredScore = 26;

    [Header("Score Source (only if autoReadOnEnable is ON)")]
    [SerializeField] private string totalScoreKey = "TotalCorrectAnswers";

    [Header("UI (all optional)")]
    [Tooltip("The panel that gets hidden when the requirement is met")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private TMP_Text titleText;     // "Unlock Tower 4"
    [SerializeField] private TMP_Text progressText;  // "18 / 26"
    [SerializeField] private Image progressFill;     // Image Type = Filled

    [Header("Progress Text Color")]
    [Tooltip("Turn off to keep the text color you set on the TMP component")]
    [SerializeField] private bool useProgressColor = true;
    [Tooltip("Left side = 0% progress, right side = 100% progress. Click the bar to edit colors.")]
    [SerializeField] private Gradient progressColor = CreateDefaultGradient();

    [Header("Options")]
    [Tooltip("Leave OFF when TowerUnlockManager supplies the score")]
    [SerializeField] private bool autoReadOnEnable = false;
    [SerializeField] private bool animateFill = true;
    [SerializeField] private float fillDuration = 0.5f;
    [SerializeField] private string progressFormat = "{0} / {1}";

    [Header("Events")]
    public UnityEvent onRequirementMet;

    public bool IsMet { get; private set; }

    private float shownFill;

    private static Gradient CreateDefaultGradient()
    {
        Gradient g = new Gradient();
        g.SetKeys(
            new GradientColorKey[]
            {
                new GradientColorKey(new Color(1f, 0.35f, 0.3f), 0f),    // red at 0%
                new GradientColorKey(new Color(1f, 0.75f, 0.2f), 0.5f),  // yellow at 50%
                new GradientColorKey(new Color(0.3f, 0.9f, 0.4f), 1f)    // green at 100%
            },
            new GradientAlphaKey[]
            {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(1f, 1f)
            });
        return g;
    }

    private void OnEnable()
    {
        if (autoReadOnEnable)
            UpdateDisplay(PlayerPrefs.GetInt(totalScoreKey, 0), requiredScore);
    }

    /// <summary>
    /// Called by TowerUnlockManager with the overall score and the required score.
    /// </summary>
    public void UpdateDisplay(int currentScore, int required)
    {
        requiredScore = required;

        bool wasMet = IsMet;
        IsMet = currentScore >= requiredScore;

        if (titleText != null)
            titleText.text = $"Unlock Tower {towerToUnlock}";

        if (progressText != null)
            progressText.text = string.Format(progressFormat, Mathf.Min(currentScore, requiredScore), requiredScore);

        float target = requiredScore > 0 ? Mathf.Clamp01((float)currentScore / requiredScore) : 1f;
        SetFill(target);

        if (IsMet)
        {
            if (!wasMet) onRequirementMet?.Invoke();
            Hide();
        }
        else
        {
            Show();
        }
    }

    /// <summary>Shortcut if the required score never changes.</summary>
    public void UpdateDisplay(int currentScore)
    {
        UpdateDisplay(currentScore, requiredScore);
    }

    private void Show()
    {
        if (panelRoot != null) panelRoot.SetActive(true);
    }

    private void Hide()
    {
        if (panelRoot != null) panelRoot.SetActive(false);
    }

    private void ApplyProgress(float value)
    {
        shownFill = value;

        if (progressFill != null)
            progressFill.fillAmount = value;

        if (useProgressColor && progressText != null && progressColor != null)
            progressText.color = progressColor.Evaluate(value);
    }

    private void SetFill(float target)
    {
        StopAllCoroutines();

        if (animateFill && Application.isPlaying && gameObject.activeInHierarchy && !IsMet)
            StartCoroutine(AnimateFill(target));
        else
            ApplyProgress(target);
    }

    private IEnumerator AnimateFill(float target)
    {
        float start = shownFill;
        float time = 0f;

        while (time < fillDuration)
        {
            time += Time.unscaledDeltaTime;
            ApplyProgress(Mathf.Lerp(start, target, Mathf.SmoothStep(0f, 1f, time / fillDuration)));
            yield return null;
        }

        ApplyProgress(target);
    }
}