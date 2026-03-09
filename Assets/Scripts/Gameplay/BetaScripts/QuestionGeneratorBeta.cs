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

    [Header("Questions")]
    [SerializeField] private List<QuestionData> questions = new List<QuestionData>();

    [Header("Settings")]
    [SerializeField] private float delayBeforeNextQuestion = 2f;

    private List<QuestionData> shuffledQuestions = new List<QuestionData>();
    private int currentIndex = 0;
    private bool isWaiting = false;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        if (questions.Count == 0)
        {
            Debug.LogError("[QuestionGeneratorBeta] No questions added in the Inspector!");
            return;
        }

        // Wait one frame so all other Awake/Start methods finish first
        StartCoroutine(InitWithDelay());
    }

    private IEnumerator InitWithDelay()
    {
        // Wait one frame — guarantees QuestionDisplayBeta.Instance is ready
        yield return null;

        if (QuestionDisplayBeta.Instance == null)
        {
            Debug.LogError("[QuestionGeneratorBeta] QuestionDisplayBeta.Instance is null! " +
                "Make sure QuestionDisplayBeta GameObject is active in the scene.");
            yield break;
        }

        if (AnswerBTNFunction2.Instance == null)
        {
            Debug.LogError("[QuestionGeneratorBeta] AnswerBTNFunction2.Instance is null! " +
                "Make sure AnswerBTNFunction2 GameObject is active in the scene.");
            yield break;
        }

        ShuffleAndStart();
    }

    private void ShuffleAndStart()
    {
        shuffledQuestions = new List<QuestionData>(questions);

        // Fisher-Yates shuffle — no repeats
        for (int i = shuffledQuestions.Count - 1; i > 0; i--)
        {
            int randomIndex = Random.Range(0, i + 1);
            QuestionData temp = shuffledQuestions[i];
            shuffledQuestions[i] = shuffledQuestions[randomIndex];
            shuffledQuestions[randomIndex] = temp;
        }

        currentIndex = 0;
        DisplayCurrentQuestion();
    }

    private void DisplayCurrentQuestion()
    {
        if (currentIndex >= shuffledQuestions.Count)
        {
            Debug.Log("[QuestionGeneratorBeta] All questions answered!");
            return;
        }

        QuestionData picked = shuffledQuestions[currentIndex];

        QuestionDisplayBeta.newQuestion = picked.question;
        QuestionDisplayBeta.newAnswerA = picked.answerA;
        QuestionDisplayBeta.newAnswerB = picked.answerB;
        QuestionDisplayBeta.newAnswerC = picked.answerC;
        QuestionDisplayBeta.newAnswerD = picked.answerD;
        correctAnswer = picked.correctAnswer.Trim();

        QuestionDisplayBeta.Instance.ShowQuestion();

        Debug.Log("[QuestionGeneratorBeta] Question " + (currentIndex + 1) +
            "/" + shuffledQuestions.Count + " | Correct: " + correctAnswer);
    }

    public void OnAnswerSelected()
    {
        if (isWaiting) return;
        StartCoroutine(WaitThenNextQuestion());
    }

    private IEnumerator WaitThenNextQuestion()
    {
        isWaiting = true;

        // Player sees green/red result for this long
        yield return new WaitForSeconds(delayBeforeNextQuestion);

        currentIndex++;

        if (currentIndex >= shuffledQuestions.Count)
        {
            Debug.Log("[QuestionGeneratorBeta] Reached last question. Stopping.");
            isWaiting = false;
            yield break;
        }

        // Reset all buttons back to blue
        AnswerBTNFunction2.Instance.ResetButtons();

        // Load next question
        DisplayCurrentQuestion();

        isWaiting = false;
    }
}