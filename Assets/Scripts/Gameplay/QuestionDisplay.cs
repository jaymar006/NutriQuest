using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;


public class QuestionDisplay : MonoBehaviour
{
    // Assign these directly to the TextMeshProUGUI components in the Inspector
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

    void Start()
    {
        StartCoroutine(DisplayQuestion());
    }

    void Update()
    {
        
    }

    IEnumerator DisplayQuestion()
    {
        yield return new WaitForSeconds(0.25f);
        QuestionGenerate.displayQuestion = false;
        if (questionText != null)
            questionText.text = newQuestion;

        if (answerA != null)
            answerA.text = newAnswerA;

        if (answerB != null)
            answerB.text = newAnswerB;

        if (answerC != null)
            answerC.text = newAnswerC;

        if (answerD != null)
            answerD.text = newAnswerD;
    }
}
