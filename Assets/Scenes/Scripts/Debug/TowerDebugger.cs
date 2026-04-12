using UnityEngine;
using System;

public class TowerDebugger : MonoBehaviour
{
    private const string HIGH_SCORE_PREFIX = "HighScore_";
    private const string UNLOCK_TIME_PREFIX = "UnlockTime_";
    private const string UNLOCKED_PREFIX = "Unlocked_";

    private void Start()
    {
        PrintAll();
    }

    // Call this from Inspector via right click -> TowerDebugger -> PrintAll //
    [ContextMenu("Print Tower Debug Info")]
    public void PrintAll()
    {
        Debug.Log("========= TOWER DEBUGGER =========");

        // Check scores //
        Debug.Log("Stage_1 Score: " + PlayerPrefs.GetInt(HIGH_SCORE_PREFIX + "Stage_1", 0));
        Debug.Log("Stage_2 Score: " + PlayerPrefs.GetInt(HIGH_SCORE_PREFIX + "Stage_2", 0));
        Debug.Log("Stage_3 Score: " + PlayerPrefs.GetInt(HIGH_SCORE_PREFIX + "Stage_3", 0));

        // Check unlock keys //
        string[] towerNames = { "Tower_1", "Tower_2", "Tower_3", "Tower_4" };
        foreach (string tower in towerNames)
        {
            bool unlocked = PlayerPrefs.GetInt(UNLOCKED_PREFIX + tower, 0) == 1;
            string timerStr = PlayerPrefs.GetString(UNLOCK_TIME_PREFIX + tower, "NONE");

            Debug.Log("--- " + tower + " ---");
            Debug.Log("  Unlocked Key: " + unlocked);
            Debug.Log("  Timer Key: " + timerStr);

            if (timerStr != "NONE")
            {
                DateTime unlockTime = DateTime.Parse(timerStr);
                double minutesPassed = (DateTime.UtcNow - unlockTime).TotalMinutes;
                Debug.Log("  Minutes Passed: " + minutesPassed.ToString("F2"));
            }
        }

        // Check TowerUnlockManager //
        if (TowerUnlockManager.Instance == null)
            Debug.LogError("  TowerUnlockManager.Instance is NULL!");
        else
            Debug.Log("TowerUnlockManager.Instance found OK");

        Debug.Log("==================================");
    }
}