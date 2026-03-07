using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;


public class QuestionGenerate : MonoBehaviour
{
    public static string correctAnswer;
    public static bool displayQuestion = false;

    void Update()
    {
        if (displayQuestion == false)
        {
            displayQuestion = true;
            QuestionDisplay.newQuestion = "Alin sa mga sumusunod ang healthy food na galing sa halaman?";
            QuestionDisplay.newAnswerA = "A. Hotdog";
            QuestionDisplay.newAnswerB = "B. Saging (Banana)";
            QuestionDisplay.newAnswerC = "C. Candy";
            QuestionDisplay.newAnswerD = "D. Softdrinks";
            correctAnswer = "B";
        }
    }
}
