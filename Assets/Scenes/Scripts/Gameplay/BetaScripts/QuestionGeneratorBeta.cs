using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class QuestionData
{
    [Header("English")]
    public string question;
    public string answerA;
    public string answerB;
    public string answerC;
    public string answerD;
    public string correctAnswer;

    [Header("Filipino (Tagalog)")]
    [Tooltip("Filipino translation of the question. Leave empty to use the English text.")]
    public string questionFilipino;
    [Tooltip("Filipino translation of Answer A. Leave empty to use English.")]
    public string answerAFilipino;
    [Tooltip("Filipino translation of Answer B. Leave empty to use English.")]
    public string answerBFilipino;
    [Tooltip("Filipino translation of Answer C. Leave empty to use English.")]
    public string answerCFilipino;
    [Tooltip("Filipino translation of Answer D. Leave empty to use English.")]
    public string answerDFilipino;
    // correctAnswer does not need translation — it is always "A", "B", "C", or "D"
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

        // Refresh the displayed question immediately when the player
        // switches language mid-game so they don't have to wait for
        // the next question to see the change take effect.
        LocalizationManager.OnLanguageChanged += OnLanguageChanged;
    }

    private void OnDestroy()
    {
        LocalizationManager.OnLanguageChanged -= OnLanguageChanged;
    }

    private void OnLanguageChanged()
    {
        // Re-display the current question in the new language
        if (!isWaiting && currentIndex < selectedQuestions.Count)
            DisplayCurrentQuestion();
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

        // Pick Filipino or English text based on active language.
        // Falls back to English if the Filipino field is empty.
        bool useFilipino = LocalizationManager.Instance != null &&
                           LocalizationManager.Instance.IsFilipino;

        QuestionDisplayBeta.newQuestion = useFilipino && !string.IsNullOrEmpty(picked.questionFilipino)
            ? picked.questionFilipino : picked.question;

        QuestionDisplayBeta.newAnswerA = useFilipino && !string.IsNullOrEmpty(picked.answerAFilipino)
            ? picked.answerAFilipino : picked.answerA;

        QuestionDisplayBeta.newAnswerB = useFilipino && !string.IsNullOrEmpty(picked.answerBFilipino)
            ? picked.answerBFilipino : picked.answerB;

        QuestionDisplayBeta.newAnswerC = useFilipino && !string.IsNullOrEmpty(picked.answerCFilipino)
            ? picked.answerCFilipino : picked.answerC;

        QuestionDisplayBeta.newAnswerD = useFilipino && !string.IsNullOrEmpty(picked.answerDFilipino)
            ? picked.answerDFilipino : picked.answerD;

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