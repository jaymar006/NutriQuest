using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections;
using System.Collections.Generic;

public class TowerUnlockManager : MonoBehaviour
{
    public static TowerUnlockManager Instance { get; private set; }

    [System.Serializable]
    public class TowerEntry
    {
        public string towerName;
        public string requiredStageID;
        public int requiredScore;
        public Button towerButton;
        public GameObject lockIcon;
        public GameObject timerDisplay;
        public TMP_Text timerText;
    }

    [Header("Tower Entries")]
    [SerializeField] private List<TowerEntry> towers = new List<TowerEntry>();

    [Header("Tower 4 Settings")]
    [SerializeField] private int tower4RequiredTotal = 26;

    [Header("Wait Time Settings")]
    [Tooltip("Wait time in minutes after beating a level")]
    [SerializeField] private float waitTimeInMinutes = 3f;

    private const string HIGH_SCORE_PREFIX = "HighScore_";
    private const string UNLOCK_TIME_PREFIX = "UnlockTime_";
    private const string UNLOCKED_PREFIX = "Unlocked_";

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        RefreshUnlockStates();
        StartCoroutine(TimerUpdateLoop());
    }

    public void RefreshUnlockStates()
    {
        for (int i = 0; i < towers.Count; i++)
        {
            TowerEntry tower = towers[i];
            UpdateTowerState(i, tower);
        }
    }

    private void UpdateTowerState(int index, TowerEntry tower)
    {
        if (tower.towerButton == null) return;

        // Tower 1 always unlocked
        if (index == 0)
        {
            SetTowerUnlocked(tower, true);
            return;
        }

        // Tower 4 special logic
        if (index == 3)
        {
            bool tower4Unlocked = CheckTower4();
            SetTowerUnlocked(tower, tower4Unlocked);
            return;
        }

        // Tower 2 and 3
        bool scoremet = CheckScoreRequirement(tower);
        bool alreadyUnlocked = PlayerPrefs.GetInt(UNLOCKED_PREFIX + tower.towerName, 0) == 1;

        if (alreadyUnlocked)
        {
            SetTowerUnlocked(tower, true);
            return;
        }

        if (!scoremet)
        {
            SetTowerUnlocked(tower, false);
            ShowTimer(tower, false);
            return;
        }

        // Score met — check wait time
        string unlockTimeStr = PlayerPrefs.GetString(UNLOCK_TIME_PREFIX + tower.towerName, "");

        if (string.IsNullOrEmpty(unlockTimeStr))
        {
            // Start the wait timer
            PlayerPrefs.SetString(UNLOCK_TIME_PREFIX + tower.towerName,
                DateTime.UtcNow.ToString());
            PlayerPrefs.Save();
            SetTowerUnlocked(tower, false);
            ShowTimer(tower, true);
            return;
        }

        DateTime unlockTime = DateTime.Parse(unlockTimeStr);
        double minutesPassed = (DateTime.UtcNow - unlockTime).TotalMinutes;

        if (minutesPassed >= waitTimeInMinutes)
        {
            // Wait complete — unlock!
            PlayerPrefs.SetInt(UNLOCKED_PREFIX + tower.towerName, 1);
            PlayerPrefs.Save();
            SetTowerUnlocked(tower, true);
            ShowTimer(tower, false);
            Debug.Log("[TowerUnlockManager] " + tower.towerName + " unlocked!");
        }
        else
        {
            // Still waiting
            SetTowerUnlocked(tower, false);
            ShowTimer(tower, true);
        }
    }

    private bool CheckScoreRequirement(TowerEntry tower)
    {
        if (string.IsNullOrEmpty(tower.requiredStageID)) return true;
        int highScore = PlayerPrefs.GetInt(HIGH_SCORE_PREFIX + tower.requiredStageID, 0);
        return highScore >= tower.requiredScore;
    }

    private bool CheckTower4()
    {
        int stage1Best = PlayerPrefs.GetInt(HIGH_SCORE_PREFIX + "Stage_1", 0);
        int stage2Best = PlayerPrefs.GetInt(HIGH_SCORE_PREFIX + "Stage_2", 0);
        int stage3Best = PlayerPrefs.GetInt(HIGH_SCORE_PREFIX + "Stage_3", 0);
        int total = stage1Best + stage2Best + stage3Best;

        Debug.Log("[TowerUnlockManager] Tower 4 total: " + total +
            "/" + tower4RequiredTotal);

        return total >= tower4RequiredTotal;
    }

    private void SetTowerUnlocked(TowerEntry tower, bool unlocked)
    {
        if (tower.towerButton != null)
            tower.towerButton.interactable = unlocked;

        if (tower.lockIcon != null)
            tower.lockIcon.SetActive(!unlocked);
    }

    private void ShowTimer(TowerEntry tower, bool show)
    {
        if (tower.timerDisplay != null)
            tower.timerDisplay.SetActive(show);
    }

    private IEnumerator TimerUpdateLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(1f);

            for (int i = 1; i < towers.Count - 1; i++)
            {
                TowerEntry tower = towers[i];
                if (tower.timerText == null) continue;

                string unlockTimeStr = PlayerPrefs.GetString(
                    UNLOCK_TIME_PREFIX + tower.towerName, "");

                if (string.IsNullOrEmpty(unlockTimeStr)) continue;

                bool alreadyUnlocked = PlayerPrefs.GetInt(
                    UNLOCKED_PREFIX + tower.towerName, 0) == 1;

                if (alreadyUnlocked)
                {
                    ShowTimer(tower, false);
                    continue;
                }

                DateTime unlockTime = DateTime.Parse(unlockTimeStr);
                double minutesPassed = (DateTime.UtcNow - unlockTime).TotalMinutes;
                double remaining = (waitTimeInMinutes - minutesPassed) * 60;

                if (remaining <= 0)
                {
                    // Timer expired — unlock
                    PlayerPrefs.SetInt(UNLOCKED_PREFIX + tower.towerName, 1);
                    PlayerPrefs.Save();
                    SetTowerUnlocked(tower, true);
                    ShowTimer(tower, false);
                    Debug.Log("[TowerUnlockManager] " + tower.towerName + " unlocked!");
                }
                else
                {
                    int mins = Mathf.FloorToInt((float)remaining / 60f);
                    int secs = Mathf.FloorToInt((float)remaining % 60f);
                    tower.timerText.text = string.Format("{0}:{1:00}", mins, secs);
                }
            }
        }
    }

    // Call this from ResultScreenManager after level complete
    public void OnLevelCleared(string stageID)
    {
        // Find matching tower and start its wait timer if score is met
        foreach (TowerEntry tower in towers)
        {
            if (tower.requiredStageID == stageID)
            {
                string key = UNLOCK_TIME_PREFIX + tower.towerName;
                bool timerStarted = !string.IsNullOrEmpty(
                    PlayerPrefs.GetString(key, ""));
                bool alreadyUnlocked = PlayerPrefs.GetInt(
                    UNLOCKED_PREFIX + tower.towerName, 0) == 1;

                if (!timerStarted && !alreadyUnlocked)
                {
                    PlayerPrefs.SetString(key, DateTime.UtcNow.ToString());
                    PlayerPrefs.Save();
                    Debug.Log("[TowerUnlockManager] Wait timer started for: " +
                        tower.towerName);
                }
            }
        }

        RefreshUnlockStates();
    }
}