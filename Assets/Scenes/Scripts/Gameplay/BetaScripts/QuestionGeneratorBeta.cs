using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class QuestionData
{
    public string question;
    public string answerA;
    public string answerB;
    public string answerC;
    public string answerD;
    public string correctAnswer;
}

public class QuestionGeneratorBeta : MonoBehaviour
{
    public static string correctAnswer;
    public static QuestionGeneratorBeta Instance { get; private set; }

    [Header("Question Pool")]
    [Tooltip("Add all 20 questions here")]
    [SerializeField] private List<QuestionData> questionPool = new List<QuestionData>();

    [Header("Settings")]
    [Tooltip("How many questions to pick from the pool per game")]
    [SerializeField] private int questionsPerGame = 10;
    [SerializeField] private float delayBeforeNextQuestion = 2f;

    private List<QuestionData> selectedQuestions = new List<QuestionData>();
    private int currentIndex = 0;
    private bool isWaiting = false;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        if (questionPool.Count == 0)
        {
            Debug.LogError("[QuestionGeneratorBeta] No questions in pool!");
            return;
        }

        if (questionsPerGame > questionPool.Count)
        {
            Debug.LogWarning("[QuestionGeneratorBeta] questionsPerGame exceeds pool size! Using pool size.");
            questionsPerGame = questionPool.Count;
        }

        StartCoroutine(InitWithDelay());
    }

    private IEnumerator InitWithDelay()
    {
        yield return null;

        if (QuestionDisplayBeta.Instance == null)
        {
            Debug.LogError("[QuestionGeneratorBeta] QuestionDisplayBeta.Instance is null!");
            yield break;
        }

        if (AnswerBTNFunction2.Instance == null)
        {
            Debug.LogError("[QuestionGeneratorBeta] AnswerBTNFunction2.Instance is null!");
            yield break;
        }

        PickAndShuffleQuestions();
    }

    private void PickAndShuffleQuestions()
    {
        // Shuffle the pool first
        List<QuestionData> shuffledPool = new List<QuestionData>(questionPool);
        for (int i = shuffledPool.Count - 1; i > 0; i--)
        {
            int randomIndex = Random.Range(0, i + 1);
            QuestionData temp = shuffledPool[i];
            shuffledPool[i] = shuffledPool[randomIndex];
            shuffledPool[randomIndex] = temp;
        }

        // Pick first N questions from shuffled pool
        selectedQuestions = shuffledPool.GetRange(0, questionsPerGame);

        currentIndex = 0;
        DisplayCurrentQuestion();

        Debug.Log("[QuestionGeneratorBeta] Picked " + questionsPerGame +
            " questions from pool of " + questionPool.Count);
    }

    private void DisplayCurrentQuestion()
    {
        if (currentIndex >= selectedQuestions.Count)
        {
            Debug.Log("[QuestionGeneratorBeta] All questions answered!");
            return;
        }

        QuestionData picked = selectedQuestions[currentIndex];
        QuestionDisplayBeta.newQuestion = picked.question;
        QuestionDisplayBeta.newAnswerA = picked.answerA;
        QuestionDisplayBeta.newAnswerB = picked.answerB;
        QuestionDisplayBeta.newAnswerC = picked.answerC;
        QuestionDisplayBeta.newAnswerD = picked.answerD;
        correctAnswer = picked.correctAnswer.Trim();

        QuestionDisplayBeta.Instance.ShowQuestion();

        // Start timer for this question //
        if (QuestionTimer.Instance != null)
            QuestionTimer.Instance.StartTimer();

        if (CatCompanion.Instance != null)
            CatCompanion.Instance.ShowIdle();

        Debug.Log("[QuestionGeneratorBeta] Question " + (currentIndex + 1) +
            "/" + selectedQuestions.Count + " | Correct: " + correctAnswer);
    }

    private IEnumerator WaitThenNextQuestion()
    {
        isWaiting = true;

        // Stop timer when answer is selected //
        if (QuestionTimer.Instance != null)
            QuestionTimer.Instance.StopTimer();

        yield return new WaitForSeconds(delayBeforeNextQuestion);

        currentIndex++;

        if (currentIndex >= selectedQuestions.Count)
        {
            Debug.Log("[QuestionGeneratorBeta] Reached last question. Stopping.");
            isWaiting = false;
            yield break;
        }

        AnswerBTNFunction2.Instance.ResetButtons();
        DisplayCurrentQuestion();
        isWaiting = false;
    }

    public void OnAnswerSelected()
    {
        if (isWaiting) return;
        StartCoroutine(WaitThenNextQuestion());
    }
}