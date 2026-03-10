using UnityEngine;
using TMPro;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    [Header("Score UI")]
    [SerializeField] private TMP_Text scoreText;

    [Header("Settings")]
    [SerializeField] private int totalQuestions = 10;
    [SerializeField] private float replenishThreshold = 0.85f;

    [Header("Stage ID")]
    [Tooltip("Unique name for this stage e.g. Stage_1, Library_1. Used to track first time clear.")]
    [SerializeField] private string stageID = "Stage_1";

    private int correctAnswers = 0;
    private int answeredQuestions = 0;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        correctAnswers = 0;
        answeredQuestions = 0;
        UpdateScoreUI();
    }

    public void RegisterAnswer(bool isCorrect)
    {
        answeredQuestions++;
        if (isCorrect) correctAnswers++;

        UpdateScoreUI();

        Debug.Log("[ScoreManager] Score: " + correctAnswers + "/" + answeredQuestions);

        if (answeredQuestions >= totalQuestions)
            OnLevelComplete();
    }

    private void OnLevelComplete()
    {
        float scorePercent = (float)correctAnswers / totalQuestions;
        int scoreInt = Mathf.RoundToInt(scorePercent * 100);

        Debug.Log("[ScoreManager] Level complete! Score: " + scoreInt + "%");

        // Check if this stage was already cleared before
        string clearedKey = "StageCleared_" + stageID;
        bool alreadyCleared = PlayerPrefs.GetInt(clearedKey, 0) == 1;

        if (scorePercent >= replenishThreshold)
        {
            if (!alreadyCleared)
            {
                // First time clearing with 85%+ — reward 1 hint
                Debug.Log("[ScoreManager] First time 85%+ clear! Rewarding hint.");

                if (HintSystem.Instance != null)
                    HintSystem.Instance.TryReplenishHint();

                // Mark this stage as cleared
                PlayerPrefs.SetInt(clearedKey, 1);
                PlayerPrefs.Save();
            }
            else
            {
                Debug.Log("[ScoreManager] Stage already cleared before — no hint reward.");
            }
        }
    }

    private void UpdateScoreUI()
    {
        if (scoreText != null)
            scoreText.text = correctAnswers + "/" + totalQuestions;
    }

    public float GetScorePercent()
    {
        if (totalQuestions == 0) return 0f;
        return (float)correctAnswers / totalQuestions;
    }
}
