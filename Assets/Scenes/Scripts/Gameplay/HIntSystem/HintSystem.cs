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

    // Hints are tracked PER TOWER. The tower is identified by the Stage ID that
    // ScoreManager already uses for progress, so each tower keeps its own hints.
    private string TowerKey =>
        ScoreManager.Instance != null ? ScoreManager.Instance.StageID : "Default";

    public int RemainingHints
    {
        get => HintStorage.Get(TowerKey);
        private set => HintStorage.Set(TowerKey, value);
    }

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        if (debugResetButton != null)
            debugResetButton.onClick.AddListener(ResetHints);

        // Tell the shared storage the cap, so the result scene can respect it too.
        HintStorage.SetMaxHints(maxHints);

        UpdateHintUI();
        Debug.Log("[HintSystem] Tower '" + TowerKey + "' hints remaining: " + RemainingHints);
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

    // DEBUG: refills the hints of the CURRENT tower only.
    // (This used to also call PlayerPrefs.DeleteAll(), which wiped ALL progress.
    //  DevCheatTool.ResetEverything() still does a full reset in debug builds.)
    public void ResetHints()
    {
        RemainingHints = maxHints;
        UpdateHintUI();
        Debug.Log("[HintSystem] DEBUG - Hints for '" + TowerKey + "' reset to " + maxHints);
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