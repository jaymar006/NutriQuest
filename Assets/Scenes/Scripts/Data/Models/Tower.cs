using System;
using UnityEngine;

[Serializable]
public class Tower
{
    public int towerId;
    public string towerName;
    public string gradeRange;
    public int totalQuestions;
    public int requiredScore;
    public bool isUnlocked;
    public int staminaCost;
    public int totalHints;
    
    public Tower()
    {
        isUnlocked = false;
        staminaCost = 10; // Default stamina cost
        totalHints = 3; // Default hints per tower
    }
}
