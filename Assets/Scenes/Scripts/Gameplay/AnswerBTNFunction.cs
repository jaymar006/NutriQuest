using UnityEngine;
using System.Collections;
using System.Collections.Generic;


public class AnswerBTNFunction : MonoBehaviour
{
    public GameObject answerBlueA;
    public GameObject answerRedA;
    public GameObject answerGreenA;

    public GameObject answerBlueB;
    public GameObject answerRedB;
    public GameObject answerGreenB;

    public GameObject answerBlueC;
    public GameObject answerRedC;
    public GameObject answerGreenC;

    public GameObject answerBlueD;
    public GameObject answerRedD;
    public GameObject answerGreenD;


    public void AnswerA()
    {
        if (QuestionGenerate.correctAnswer == "A")
        {
            answerBlueA.SetActive(false);
            answerRedA.SetActive(false);
            answerGreenA.SetActive(true);
        }
        else
        {
            answerBlueA.SetActive(false);
            answerRedA.SetActive(true);
            answerGreenA.SetActive(false);

        }
    }
    public void AnswerB()
    {
        if (QuestionGenerate.correctAnswer == "B")
        {
            answerBlueB.SetActive(false);
            answerRedB.SetActive(false);
            answerGreenB.SetActive(true);
        }
        else
        {
            answerBlueB.SetActive(false);
            answerRedB.SetActive(true);
            answerGreenB.SetActive(false);
        }
    }
    public void AnswerC()
    {
        if (QuestionGenerate.correctAnswer == "C")
        {
            answerBlueC.SetActive(false);
            answerRedC.SetActive(false);
            answerGreenC.SetActive(true);
        }
        else
        {
            answerBlueC.SetActive(false);
            answerRedC.SetActive(true);
            answerGreenC.SetActive(false);
        }
    }
    public void AnswerD()
    {
        if (QuestionGenerate.correctAnswer == "D")
        {
            answerBlueD.SetActive(false);
            answerRedD.SetActive(false);
            answerGreenD.SetActive(true);
        }
        else
        {
            answerBlueD.SetActive(false);
            answerRedD.SetActive(true);
            answerGreenD.SetActive(false);
        }
    }
}