using System.Collections;
using UnityEngine;
using TMPro;

public class QuestionDisplayBeta : MonoBehaviour
{
    public static QuestionDisplayBeta Instance { get; private set; }

    [Header("Text References")]
    public TMP_Text questionText;
    public TMP_Text answerA;
    public TMP_Text answerB;
    public TMP_Text answerC;
    public TMP_Text answerD;

    public static string newQuestion;
    public static string newAnswerA;
    public static string newAnswerB;
    public static string newAnswerC;
    public static string newAnswerD;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        StartCoroutine(DisplayQuestion());
    }

    public void ShowQuestion()
    {
        StartCoroutine(DisplayQuestion());
    }

    private IEnumerator DisplayQuestion()
    {
        yield return new WaitForSeconds(0.25f);

        if (questionText != null) questionText.text = newQuestion;
        if (answerA != null) answerA.text = newAnswerA;
        if (answerB != null) answerB.text = newAnswerB;
        if (answerC != null) answerC.text = newAnswerC;
        if (answerD != null) answerD.text = newAnswerD;
    }
}