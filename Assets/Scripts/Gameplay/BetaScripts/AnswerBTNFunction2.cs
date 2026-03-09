using UnityEngine;

public class AnswerBTNFunction2 : MonoBehaviour
{
    public static AnswerBTNFunction2 Instance { get; private set; }

    [Header("Answer A")]
    public GameObject answerBlueA;
    public GameObject answerRedA;
    public GameObject answerGreenA;

    [Header("Answer B")]
    public GameObject answerBlueB;
    public GameObject answerRedB;
    public GameObject answerGreenB;

    [Header("Answer C")]
    public GameObject answerBlueC;
    public GameObject answerRedC;
    public GameObject answerGreenC;

    [Header("Answer D")]
    public GameObject answerBlueD;
    public GameObject answerRedD;
    public GameObject answerGreenD;

    private bool answerLocked = false;

    private void Awake()
    {
        Instance = this;
    }

    public void ResetButtons()
    {
        answerLocked = false;

        SetButton(answerBlueA, answerRedA, answerGreenA, true, false, false);
        SetButton(answerBlueB, answerRedB, answerGreenB, true, false, false);
        SetButton(answerBlueC, answerRedC, answerGreenC, true, false, false);
        SetButton(answerBlueD, answerRedD, answerGreenD, true, false, false);
    }

    private void SetButton(GameObject blue, GameObject red, GameObject green,
        bool blueState, bool redState, bool greenState)
    {
        if (blue != null) blue.SetActive(blueState);
        if (red != null) red.SetActive(redState);
        if (green != null) green.SetActive(greenState);
    }

    private bool IsCorrect(string buttonLetter)
    {
        if (string.IsNullOrEmpty(QuestionGeneratorBeta.correctAnswer))
        {
            Debug.LogError("[AnswerBTNFunction2] correctAnswer is null! " +
                "Make sure QuestionGeneratorBeta is active in the scene.");
            return false;
        }

        return string.Compare(
            buttonLetter.Trim(),
            QuestionGeneratorBeta.correctAnswer.Trim(),
            ignoreCase: true) == 0;
    }

    private void ShowResult(bool correct,
        GameObject blue, GameObject red, GameObject green)
    {
        if (blue == null || red == null || green == null)
        {
            Debug.LogError("[AnswerBTNFunction2] Answer GameObject not assigned in Inspector!");
            return;
        }

        blue.SetActive(false);
        red.SetActive(!correct);
        green.SetActive(correct);
    }

    private void HandleAnswer(string letter,
        GameObject blue, GameObject red, GameObject green)
    {
        if (answerLocked) return;
        answerLocked = true;

        ShowResult(IsCorrect(letter), blue, red, green);

        if (QuestionGeneratorBeta.Instance != null)
            QuestionGeneratorBeta.Instance.OnAnswerSelected();
    }

    public void AnswerA()
    {
        HandleAnswer("A", answerBlueA, answerRedA, answerGreenA);
    }

    public void AnswerB()
    {
        HandleAnswer("B", answerBlueB, answerRedB, answerGreenB);
    }

    public void AnswerC()
    {
        HandleAnswer("C", answerBlueC, answerRedC, answerGreenC);
    }

    public void AnswerD()
    {
        HandleAnswer("D", answerBlueD, answerRedD, answerGreenD);
    }
}