using UnityEngine;
using System.Collections.Generic;

public class AnswerBTNFunction2 : MonoBehaviour
{
    public static AnswerBTNFunction2 Instance { get; private set; }

    [Header("Answer A")]
    public GameObject answerBlueA;
    public GameObject answerRedA;
    public GameObject answerGreenA;
    public GameObject answerBlockA;

    [Header("Answer B")]
    public GameObject answerBlueB;
    public GameObject answerRedB;
    public GameObject answerGreenB;
    public GameObject answerBlockB;

    [Header("Answer C")]
    public GameObject answerBlueC;
    public GameObject answerRedC;
    public GameObject answerGreenC;
    public GameObject answerBlockC;

    [Header("Answer D")]
    public GameObject answerBlueD;
    public GameObject answerRedD;
    public GameObject answerGreenD;
    public GameObject answerBlockD;

    private bool buttonAAvailable = true;
    private bool buttonBAvailable = true;
    private bool buttonCAvailable = true;
    private bool buttonDAvailable = true;

    private bool answerLocked = false;
    public bool IsAnswerLocked => answerLocked;

    private void Awake()
    {
        Instance = this;
    }

    public bool BlockTwoWrongAnswers()
    {
        string correct = QuestionGeneratorBeta.correctAnswer.Trim().ToUpper();

        List<string> wrongAvailable = new List<string>();
        if (correct != "A" && buttonAAvailable) wrongAvailable.Add("A");
        if (correct != "B" && buttonBAvailable) wrongAvailable.Add("B");
        if (correct != "C" && buttonCAvailable) wrongAvailable.Add("C");
        if (correct != "D" && buttonDAvailable) wrongAvailable.Add("D");

        if (wrongAvailable.Count < 2)
        {
            Debug.Log("[AnswerBTNFunction2] Not enough wrong answers left to block.");
            return false;
        }

        int firstIndex = Random.Range(0, wrongAvailable.Count);
        string firstBlock = wrongAvailable[firstIndex];
        wrongAvailable.RemoveAt(firstIndex);

        int secondIndex = Random.Range(0, wrongAvailable.Count);
        string secondBlock = wrongAvailable[secondIndex];

        BlockButton(firstBlock);
        BlockButton(secondBlock);

        Debug.Log("[AnswerBTNFunction2] Hint blocked: " + firstBlock + " and " + secondBlock);
        return true;
    }

    private void BlockButton(string letter)
    {
        switch (letter)
        {
            case "A":
                SetButtonState(answerBlueA, answerRedA, answerGreenA, answerBlockA,
                    false, false, false, true);
                buttonAAvailable = false;
                break;
            case "B":
                SetButtonState(answerBlueB, answerRedB, answerGreenB, answerBlockB,
                    false, false, false, true);
                buttonBAvailable = false;
                break;
            case "C":
                SetButtonState(answerBlueC, answerRedC, answerGreenC, answerBlockC,
                    false, false, false, true);
                buttonCAvailable = false;
                break;
            case "D":
                SetButtonState(answerBlueD, answerRedD, answerGreenD, answerBlockD,
                    false, false, false, true);
                buttonDAvailable = false;
                break;
        }
    }

    public void ResetButtons()
    {
        answerLocked = false;
        buttonAAvailable = true;
        buttonBAvailable = true;
        buttonCAvailable = true;
        buttonDAvailable = true;

        SetButtonState(answerBlueA, answerRedA, answerGreenA, answerBlockA,
            true, false, false, false);
        SetButtonState(answerBlueB, answerRedB, answerGreenB, answerBlockB,
            true, false, false, false);
        SetButtonState(answerBlueC, answerRedC, answerGreenC, answerBlockC,
            true, false, false, false);
        SetButtonState(answerBlueD, answerRedD, answerGreenD, answerBlockD,
            true, false, false, false);

        if (HintSystem.Instance != null)
            HintSystem.Instance.ResetForNewQuestion();
    }

    private void SetButtonState(
        GameObject blue, GameObject red, GameObject green, GameObject block,
        bool blueState, bool redState, bool greenState, bool blockState)
    {
        if (blue != null) blue.SetActive(blueState);
        if (red != null) red.SetActive(redState);
        if (green != null) green.SetActive(greenState);
        if (block != null) block.SetActive(blockState);
    }

    private bool IsCorrect(string buttonLetter)
    {
        if (string.IsNullOrEmpty(QuestionGeneratorBeta.correctAnswer))
        {
            Debug.LogError("[AnswerBTNFunction2] correctAnswer is null!");
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

        bool correct = IsCorrect(letter);
        ShowResult(correct, blue, red, green);

        // Tell the cat to react
        if (CatCompanion.Instance != null)
            CatCompanion.Instance.ReactToAnswer(correct);

        // Report score
        if (ScoreManager.Instance != null)
            ScoreManager.Instance.RegisterAnswer(correct);

        // Move to next question
        if (QuestionGeneratorBeta.Instance != null)
            QuestionGeneratorBeta.Instance.OnAnswerSelected();
    }

    public void AnswerA() { HandleAnswer("A", answerBlueA, answerRedA, answerGreenA); }
    public void AnswerB() { HandleAnswer("B", answerBlueB, answerRedB, answerGreenB); }
    public void AnswerC() { HandleAnswer("C", answerBlueC, answerRedC, answerGreenC); }
    public void AnswerD() { HandleAnswer("D", answerBlueD, answerRedD, answerGreenD); }
}