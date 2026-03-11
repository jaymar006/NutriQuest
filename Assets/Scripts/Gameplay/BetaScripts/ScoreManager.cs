using UnityEngine;
using TMPro;

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

    [Header("Navigation")]
    [SerializeField] private string resultSceneName = "ResultScene";

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
        Debug.Log("[ScoreManager] Level complete! Loading result scene.");

        // Save result data for result scene
        ResultData.Save(
            correctAnswers,
            wrongAnswers,
            totalQuestions,
            stageID,
            towerIndex
        );

        // Load result scene
        if (SceneTransitionManager.Instance != null)
            SceneTransitionManager.Instance.NavigateTo(resultSceneName, false);
        else
            UnityEngine.SceneManagement.SceneManager.LoadScene(resultSceneName);
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