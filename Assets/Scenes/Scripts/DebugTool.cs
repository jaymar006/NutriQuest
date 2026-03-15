using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DebugTool : MonoBehaviour
{
    [Header("UI Panel")]
    [SerializeField] private GameObject debugPanel;

    [Header("Buttons")]
    [SerializeField] private Button togglePanelButton;
    [SerializeField] private Button resetAllButton;
    [SerializeField] private Button resetHintsButton;
    [SerializeField] private Button resetScoresButton;
    [SerializeField] private Button resetAchievementsButton;
    [SerializeField] private Button resetStageProgressButton;
    [SerializeField] private Button closeButton;

    [Header("Feedback Text")]
    [SerializeField] private TMP_Text feedbackText;

    [Header("Settings")]
    [SerializeField] private bool enableInBuild = false;

    private const string HINT_PREFS_KEY = "PlayerHints";
    private const string HIGH_SCORE_PREFIX = "HighScore_";
    private const string FIRST_CLEAR_PREFIX = "FirstClear_";
    private const string ATTEMPT_PREFIX = "Attempts_";
    private const string RESULT_PREFIX = "Result_";

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
        if (debugPanel != null)
            debugPanel.SetActive(false);

        if (togglePanelButton != null)
            togglePanelButton.onClick.AddListener(TogglePanel);

        if (resetAllButton != null)
            resetAllButton.onClick.AddListener(ResetAll);

        if (resetHintsButton != null)
            resetHintsButton.onClick.AddListener(ResetHints);

        if (resetScoresButton != null)
            resetScoresButton.onClick.AddListener(ResetScores);

        if (resetAchievementsButton != null)
            resetAchievementsButton.onClick.AddListener(ResetAchievements);

        if (resetStageProgressButton != null)
            resetStageProgressButton.onClick.AddListener(ResetStageProgress);

        if (closeButton != null)
            closeButton.onClick.AddListener(ClosePanel);
    }

    private void TogglePanel()
    {
        if (debugPanel != null)
            debugPanel.SetActive(!debugPanel.activeSelf);
    }

    private void ClosePanel()
    {
        if (debugPanel != null)
            debugPanel.SetActive(false);
    }

    // RESET METHODS


    public void ResetAll()
    {
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();

        if (HintSystem.Instance != null)
            HintSystem.Instance.ResetHints();

        ShowFeedback("✓ All progress reset!");
        Debug.Log("[DebugTool] All PlayerPrefs cleared.");
    }

    public void ResetHints()
    {
        PlayerPrefs.DeleteKey(HINT_PREFS_KEY);
        PlayerPrefs.Save();
        ShowFeedback("✓ Hints reset!");
        Debug.Log("[DebugTool] Hints reset.");

        if (HintSystem.Instance != null)
            HintSystem.Instance.ResetHints();
    }

    public void ResetScores()
    {
        string[] stageIDs = { "Stage_1", "Stage_2", "Stage_3", "Stage_4" };
        foreach (string id in stageIDs)
        {
            PlayerPrefs.DeleteKey(HIGH_SCORE_PREFIX + id);
            PlayerPrefs.DeleteKey(RESULT_PREFIX + "Correct");
            PlayerPrefs.DeleteKey(RESULT_PREFIX + "Wrong");
            PlayerPrefs.DeleteKey(RESULT_PREFIX + "Total");
            PlayerPrefs.DeleteKey(RESULT_PREFIX + "StageID");
            PlayerPrefs.DeleteKey(RESULT_PREFIX + "TowerIndex");
        }
        PlayerPrefs.Save();
        ShowFeedback("✓ Scores reset!");
        Debug.Log("[DebugTool] Scores reset.");
    }

    public void ResetAchievements()
    {
        string[] stageIDs = { "Stage_1", "Stage_2", "Stage_3", "Stage_4" };
        foreach (string id in stageIDs)
        {
            PlayerPrefs.DeleteKey(FIRST_CLEAR_PREFIX + id);
        }
        PlayerPrefs.Save();
        ShowFeedback("✓ Achievements reset!");
        Debug.Log("[DebugTool] Achievements reset.");
    }

    public void ResetStageProgress()
    {
        string[] stageIDs = { "Stage_1", "Stage_2", "Stage_3", "Stage_4" };
        foreach (string id in stageIDs)
        {
            PlayerPrefs.DeleteKey(FIRST_CLEAR_PREFIX + id);
            PlayerPrefs.DeleteKey(ATTEMPT_PREFIX + id);
            PlayerPrefs.DeleteKey(HIGH_SCORE_PREFIX + id);
        }
        PlayerPrefs.Save();
        ShowFeedback("✓ Stage progress reset!");
        Debug.Log("[DebugTool] Stage progress reset.");
    }

    // HELPERS
  
    private void RefreshLiveSystems()
    {
        if (HintSystem.Instance != null)
            HintSystem.Instance.ResetHints();

        Debug.Log("[DebugTool] Live systems refreshed.");
    }

    private void ShowFeedback(string message)
    {
        if (feedbackText != null)
        {
            feedbackText.text = message;
            CancelInvoke(nameof(ClearFeedback));
            Invoke(nameof(ClearFeedback), 2f);
        }

        Debug.Log("[DebugTool] " + message);
    }

    private void ClearFeedback()
    {
        if (feedbackText != null)
            feedbackText.text = "";
    }
}