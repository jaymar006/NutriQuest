using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HintSystem : MonoBehaviour
{
    public static HintSystem Instance { get; private set; }

    [Header("Hint Settings")]
    [SerializeField] private int maxHints = 3;

    [Header("Hint Button")]
    [SerializeField] private Button hintButton;
    [SerializeField] private TMP_Text hintCountText;

    [Header("Debug")]
    [SerializeField] private Button debugResetButton;

    private const string HINT_PREFS_KEY = "PlayerHints";

    public int RemainingHints
    {
        get => PlayerPrefs.GetInt(HINT_PREFS_KEY, maxHints);
        private set
        {
            PlayerPrefs.SetInt(HINT_PREFS_KEY, Mathf.Clamp(value, 0, maxHints));
            PlayerPrefs.Save();
        }
    }

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        if (debugResetButton != null)
            debugResetButton.onClick.AddListener(ResetHints);

        UpdateHintUI();
        Debug.Log("[HintSystem] Hints remaining: " + RemainingHints);
    }

    public void UseHint()
    {
        if (RemainingHints <= 0)
        {
            Debug.Log("[HintSystem] No hints remaining!");
            return;
        }

        if (AnswerBTNFunction2.Instance == null)
        {
            Debug.LogError("[HintSystem] AnswerBTNFunction2 Instance is null!");
            return;
        }

        if (AnswerBTNFunction2.Instance.IsAnswerLocked)
        {
            Debug.Log("[HintSystem] Answer already selected - hint not used.");
            return;
        }

        bool success = AnswerBTNFunction2.Instance.BlockTwoWrongAnswers();

        if (success)
        {
            RemainingHints--;
            UpdateHintUI();

            if (CatCompanion.Instance != null)
                CatCompanion.Instance.ShowHint();

            Debug.Log("[HintSystem] Hint used! Remaining: " + RemainingHints);
        }
    }

    public void TryReplenishHint()
    {
        if (RemainingHints < maxHints)
        {
            RemainingHints++;
            Debug.Log("[HintSystem] Hint replenished! Now: " + RemainingHints);
        }
        else
        {
            Debug.Log("[HintSystem] Already at max hints (" + maxHints + ").");
        }

        UpdateHintUI();
    }

    public void ResetHints()
    {
        RemainingHints = maxHints;
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();
        UpdateHintUI();
        Debug.Log("[HintSystem] DEBUG - Hints reset to " + maxHints);
    }

    public void ResetForNewQuestion()
    {
        UpdateHintUI();
    }

    private void UpdateHintUI()
    {
        if (hintCountText != null)
            hintCountText.text = "x" + RemainingHints;

        if (hintButton != null)
            hintButton.interactable = RemainingHints > 0;
    }
}