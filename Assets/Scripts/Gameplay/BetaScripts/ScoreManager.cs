using UnityEngine;
using TMPro;
using System.Collections;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    [Header("Score UI")]
    [SerializeField] private TMP_Text scoreText;

    [Header("Stage Settings")]
    [SerializeField] private int totalQuestions = 10;
    [SerializeField] private string stageID = "Stage_1";

    [Header("Tower Settings")]
    [Tooltip("0 = Tower1, 1 = Tower2, 2 = Tower3, 3 = Tower4")]
    [SerializeField] private int towerIndex = 0;

    // Public read-only access for TowerLevelTransition
    public int TotalQuestions => totalQuestions;
    public int WrongAnswers => wrongAnswers;
    public string StageID => stageID;
    public int TowerIndex => towerIndex;

    private int correctAnswers = 0;
    private int wrongAnswers = 0;
    private int answeredQuestions = 0;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        correctAnswers = 0;
        wrongAnswers = 0;
        answeredQuestions = 0;
        UpdateScoreUI();
    }

    public void RegisterAnswer(bool isCorrect)
    {
        answeredQuestions++;
        if (isCorrect) correctAnswers++;
        else wrongAnswers++;

        UpdateScoreUI();

        Debug.Log("[ScoreManager] Score: " + correctAnswers +
            " correct, " + wrongAnswers + " wrong.");

        if (answeredQuestions >= totalQuestions)
            OnLevelComplete();
    }

    private void OnLevelComplete()
    {
        if (TowerLevelTransition.Instance != null)
            TowerLevelTransition.Instance.OnLevelComplete();
        else
            Debug.LogError("[ScoreManager] TowerLevelTransition Instance is null!");
    }

    private void UpdateScoreUI()
    {
        if (scoreText != null)
            scoreText.text = "Score: " + correctAnswers;
    }

    public float GetScorePercent()
    {
        if (totalQuestions == 0) return 0f;
        return (float)correctAnswers / totalQuestions;
    }
}