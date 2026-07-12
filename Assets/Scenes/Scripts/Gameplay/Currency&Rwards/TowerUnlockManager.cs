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
    [SerializeField] private float waitTimeInMinutes = 3f;

    private const string HIGH_SCORE_PREFIX = "HighScore_";
    private const string UNLOCK_TIME_PREFIX = "UnlockTime_";
    private const string UNLOCKED_PREFIX = "Unlocked_";

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        // FIX: Wait one frame before refreshing so all GameObjects in the
        // scene finish their own Awake/Start before we call SetActive on them.
        // Without this, SetActive(true/false) on Lock and Timer GameObjects
        // that haven't fully initialized yet gets silently ignored.
        StartCoroutine(InitWithDelay());
        StartCoroutine(TimerUpdateLoop());
    }

    private IEnumerator InitWithDelay()
    {
        // Wait for SceneTransitionManager's fade-in to fully complete
        // before calling SetActive on any GameObjects. Without this,
        // RefreshUnlockStates() fires while the fade canvas is still
        // covering the screen (blocksRaycasts = true), which makes it
        // look like the lock/timer never appeared even though SetActive
        // was called correctly.
        //
        // The delay matches SceneTransitionManager.fadeDuration (0.4s)
        // plus two extra frames as a safety buffer. If you change
        // fadeDuration in SceneTransitionManager, update this to match.
        yield return null;
        yield return null;
        yield return new WaitForSeconds(0.5f);
        RefreshUnlockStates();
    }

    public void RefreshUnlockStates()
    {
        // TEMP DEBUG — remove after fixing
        Debug.Log("[TowerUnlock] HighScore_Stage_1 = " +
            PlayerPrefs.GetInt("HighScore_Stage_1", 0));
        Debug.Log("[TowerUnlock] HighScore_Stage_2 = " +
            PlayerPrefs.GetInt("HighScore_Stage_2", 0));
        Debug.Log("[TowerUnlock] Unlocked_Tower_2 = " +
            PlayerPrefs.GetInt("Unlocked_Tower_2", 0));
        Debug.Log("[TowerUnlock] LOCKImg1 null? " +
            (towers.Count > 1 ? (towers[1].lockIcon == null).ToString() : "no tower[1]"));
        Debug.Log("[TowerUnlock] TimerDisplay1 null? " +
            (towers.Count > 1 ? (towers[1].timerDisplay == null).ToString() : "no tower[1]"));

        for (int i = 0; i < towers.Count; i++)
            UpdateTowerState(i);
    }

    private void UpdateTowerState(int index)
    {
        if (index < 0 || index >= towers.Count) return;

        TowerEntry tower = towers[index];
        if (tower.towerButton == null) return;

        if (index == 0)
        {
            SetTowerUnlocked(tower, true);
            ShowLockIcon(tower, false);
            ShowTimer(tower, false);
            return;
        }

        if (index == 3)
        {
            bool unlocked = CheckTower4();
            SetTowerUnlocked(tower, unlocked);
            ShowLockIcon(tower, !unlocked);
            ShowTimer(tower, false);
            return;
        }

        bool alreadyUnlocked = PlayerPrefs.GetInt(UNLOCKED_PREFIX + tower.towerName, 0) == 1;
        if (alreadyUnlocked)
        {
            SetTowerUnlocked(tower, true);
            ShowLockIcon(tower, false);
            ShowTimer(tower, false);
            return;
        }

        // Tower dependency check: each tower only reveals its lock and timer
        // once the PREVIOUS tower is fully unlocked. Before that, the lock
        // and timer are completely hidden so the player isn't confused by a
        // locked tower they can't do anything about yet.
        //
        // index 1 = Tower_2 requires Tower_1 to be playable (always true)
        // index 2 = Tower_3 requires Tower_2 to be fully unlocked first
        if (index == 2)
        {
            // Tower_3 stays completely hidden until Tower_2 is fully unlocked
            bool tower2Unlocked = towers.Count > 1 &&
                PlayerPrefs.GetInt(UNLOCKED_PREFIX + towers[1].towerName, 0) == 1;

            if (!tower2Unlocked)
            {
                SetTowerUnlocked(tower, false);
                ShowLockIcon(tower, false);  // hide lock entirely
                ShowTimer(tower, false);     // hide timer entirely
                return;
            }
        }

        bool scoreMet = CheckScoreRequirement(tower);
        if (!scoreMet)
        {
            SetTowerUnlocked(tower, false);
            ShowLockIcon(tower, true);
            ShowTimer(tower, false);
            return;
        }

        string unlockTimeStr = PlayerPrefs.GetString(UNLOCK_TIME_PREFIX + tower.towerName, "");
        if (string.IsNullOrEmpty(unlockTimeStr))
        {
            PlayerPrefs.SetString(UNLOCK_TIME_PREFIX + tower.towerName, DateTime.UtcNow.ToString());
            PlayerPrefs.Save();
            SetTowerUnlocked(tower, false);
            ShowLockIcon(tower, true);
            ShowTimer(tower, true);
            return;
        }

        DateTime unlockTime = DateTime.Parse(unlockTimeStr);
        double minutesPassed = (DateTime.UtcNow - unlockTime).TotalMinutes;

        if (minutesPassed >= waitTimeInMinutes)
        {
            PlayerPrefs.SetInt(UNLOCKED_PREFIX + tower.towerName, 1);
            PlayerPrefs.Save();
            SetTowerUnlocked(tower, true);
            ShowLockIcon(tower, false);
            ShowTimer(tower, false);
            Debug.Log("[TowerUnlockManager] " + tower.towerName + " fully unlocked!");
        }
        else
        {
            SetTowerUnlocked(tower, false);
            ShowLockIcon(tower, true);
            ShowTimer(tower, true);
            UpdateTimerText(tower, minutesPassed);
        }
    }

    private void UpdateTimerText(TowerEntry tower, double minutesPassed)
    {
        if (tower.timerText == null) return;

        double remainingSeconds = (waitTimeInMinutes - minutesPassed) * 60;
        if (remainingSeconds < 0) remainingSeconds = 0;

        int mins = Mathf.FloorToInt((float)remainingSeconds / 60f);
        int secs = Mathf.FloorToInt((float)remainingSeconds % 60f);
        tower.timerText.text = string.Format("{0}:{1:00}", mins, secs);
    }

    private bool CheckScoreRequirement(TowerEntry tower)
    {
        if (string.IsNullOrEmpty(tower.requiredStageID)) return true;

        // Primary check: did the player clear this stage at least once?
        // ResultScreenManager writes FirstClear_<stageID> = 1 on first completion.
        // This guarantees the timer starts regardless of what score they got.
        bool firstCleared = PlayerPrefs.GetInt("FirstClear_" + tower.requiredStageID, 0) == 1;
        if (firstCleared) return true;

        // Fallback: also unlock if they hit the required score threshold,
        // in case FirstClear_ wasn't written yet (e.g. old save data).
        int highScore = PlayerPrefs.GetInt(HIGH_SCORE_PREFIX + tower.requiredStageID, 0);
        return highScore >= tower.requiredScore;
    }

    private bool CheckTower4()
    {
        int s1 = PlayerPrefs.GetInt(HIGH_SCORE_PREFIX + "Stage_1", 0);
        int s2 = PlayerPrefs.GetInt(HIGH_SCORE_PREFIX + "Stage_2", 0);
        int s3 = PlayerPrefs.GetInt(HIGH_SCORE_PREFIX + "Stage_3", 0);
        return (s1 + s2 + s3) >= tower4RequiredTotal;
    }

    private void SetTowerUnlocked(TowerEntry tower, bool unlocked)
    {
        if (tower.towerButton != null)
            tower.towerButton.interactable = unlocked;
    }

    private void ShowLockIcon(TowerEntry tower, bool show)
    {
        if (tower.lockIcon != null)
            tower.lockIcon.SetActive(show);
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

            for (int i = 1; i <= 2; i++)
            {
                if (i >= towers.Count) continue;

                TowerEntry tower = towers[i];
                bool alreadyUnlocked = PlayerPrefs.GetInt(
                    UNLOCKED_PREFIX + tower.towerName, 0) == 1;

                if (alreadyUnlocked)
                {
                    ShowTimer(tower, false);
                    continue;
                }

                string unlockTimeStr = PlayerPrefs.GetString(
                    UNLOCK_TIME_PREFIX + tower.towerName, "");

                if (string.IsNullOrEmpty(unlockTimeStr)) continue;

                DateTime unlockTime = DateTime.Parse(unlockTimeStr);
                double minutesPassed = (DateTime.UtcNow - unlockTime).TotalMinutes;

                if (minutesPassed >= waitTimeInMinutes)
                {
                    PlayerPrefs.SetInt(UNLOCKED_PREFIX + tower.towerName, 1);
                    PlayerPrefs.Save();
                    SetTowerUnlocked(tower, true);
                    ShowLockIcon(tower, false);
                    ShowTimer(tower, false);
                    Debug.Log("[TowerUnlockManager] " + tower.towerName + " unlocked by timer!");
                }
                else
                {
                    ShowTimer(tower, true);
                    UpdateTimerText(tower, minutesPassed);
                }
            }
        }
    }

    public void OnLevelCleared(string stageID)
    {
        RefreshUnlockStates();
    }
}