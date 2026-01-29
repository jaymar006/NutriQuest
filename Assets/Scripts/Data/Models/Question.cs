using System;
using UnityEngine;

[Serializable]
public class Question
{
    public int questionId;
    public int towerId;
    public string questionText;
    public string optionA;
    public string optionB;
    public string optionC;
    public string optionD;
    public string correctAnswer; // "A", "B", "C", or "D"
    public string language; // "en" or "tl"
    
    public Question()
    {
        language = "en"; // Default to English
    }
    
    /// <summary>
    /// Checks if the selected answer is correct
    /// </summary>
    public bool IsCorrect(string selectedAnswer)
    {
        return selectedAnswer.ToUpper() == correctAnswer.ToUpper();
    }
    
    /// <summary>
    /// Gets the option text by letter (A, B, C, D)
    /// </summary>
    public string GetOption(string letter)
    {
        switch (letter.ToUpper())
        {
            case "A": return optionA;
            case "B": return optionB;
            case "C": return optionC;
            case "D": return optionD;
            default: return "";
        }
    }
}
