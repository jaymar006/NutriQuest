using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;

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

    // Perf: in-memory cache loaded in Start so the 1s TimerUpdateLoop never
    // touches PlayerPrefs or DateTime.Parse per tick.
    private readonly Dictionary<string, bool> cachedUnlocked = new Dictionary<string, bool>();
    private readonly Dictionary<string, DateTime> cachedUnlockTime = new Dictionary<string, DateTime>();
    private readonly Dictionary<string, string> lastTimerStrings = new Dictionary<string, string>();

    // Perf: cached yield instructions (avoid per-tick / per-init allocation).
    private static readonly WaitForSeconds timerTickWait = new WaitForSeconds(1f);
    private static readonly WaitForSeconds initDelayWait = new WaitForSeconds(0.5f);

    // Track coroutine handles so OnDestroy can stop them (no orphan loops after destroy).
    private Coroutine initCoroutine = null;
    private Coroutine timerCoroutine = null;

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
        LoadUnlockCache();
        // FIX: Wait one frame before refreshing so all GameObjects in the
        // scene finish their own Awake/Start before we call SetActive on them.
        // Without this, SetActive(true/false) on Lock and Timer GameObjects
        // that haven't fully initialized yet gets silently ignored.
        initCoroutine = StartCoroutine(InitWithDelay());
        timerCoroutine = StartCoroutine(TimerUpdateLoop());
    }

    private void OnDestroy()
    {
        if (initCoroutine != null) { StopCoroutine(initCoroutine); initCoroutine = null; }
        if (timerCoroutine != null) { StopCoroutine(timerCoroutine); timerCoroutine = null; }
    }

    private void LoadUnlockCache()
    {
        cachedUnlocked.Clear();
        cachedUnlockTime.Clear();
        foreach (TowerEntry t in towers)
        {
            if (t == null || string.IsNullOrEmpty(t.towerName)) continue;
            cachedUnlocked[t.towerName] = PlayerPrefs.GetInt(UNLOCKED_PREFIX + t.towerName, 0) == 1;
            string s = PlayerPrefs.GetString(UNLOCK_TIME_PREFIX + t.towerName, "");
            if (!string.IsNullOrEmpty(s))
            {
                try { cachedUnlockTime[t.towerName] = DateTime.Parse(s, null, DateTimeStyles.RoundtripKind); }
                catch { /* ignore corrupt timestamp; treated as missing */ }
            }
        }
    }

    private bool IsUnlockedCached(string towerName)
    {
        bool v;
        if (cachedUnlocked.TryGetValue(towerName, out v)) return v;
        v = PlayerPrefs.GetInt(UNLOCKED_PREFIX + towerName, 0) == 1;
        cachedUnlocked[towerName] = v;
        return v;
    }

    private void SetUnlockedCached(string towerName)
    {
        cachedUnlocked[towerName] = true;
        PlayerPrefs.SetInt(UNLOCKED_PREFIX + towerName, 1);
        PlayerPrefs.Save();
    }

    private bool TryGetUnlockTimeCached(string towerName, out DateTime unlockTime)
    {
        if (cachedUnlockTime.TryGetValue(towerName, out unlockTime)) return true;
        string s = PlayerPrefs.GetString(UNLOCK_TIME_PREFIX + towerName, "");
        if (string.IsNullOrEmpty(s)) { unlockTime = DateTime.MinValue; return false; }
        try
        {
            unlockTime = DateTime.Parse(s, null, DateTimeStyles.RoundtripKind);
            cachedUnlockTime[towerName] = unlockTime;
            return true;
        }
        catch { unlockTime = DateTime.MinValue; return false; }
    }

    private void SetUnlockTimeCached(string towerName, DateTime utcNow)
    {
        cachedUnlockTime[towerName] = utcNow;
        PlayerPrefs.SetString(UNLOCK_TIME_PREFIX + towerName, utcNow.ToString("O"));
        PlayerPrefs.Save();
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
        yield return initDelayWait;
        RefreshUnlockStates();
    }

    public void RefreshUnlockStates()
    {
        // TEMP DEBUG � remove after fixing
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

        bool alreadyUnlocked = IsUnlockedCached(tower.towerName);
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
                IsUnlockedCached(towers[1].towerName);

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

        DateTime cachedTime;
        if (!TryGetUnlockTimeCached(tower.towerName, out cachedTime))
        {
            SetUnlockTimeCached(tower.towerName, DateTime.UtcNow);
            SetTowerUnlocked(tower, false);
            ShowLockIcon(tower, true);
            ShowTimer(tower, true);
            return;
        }

        DateTime unlockTime = cachedTime;
        double minutesPassed = (DateTime.UtcNow - unlockTime).TotalMinutes;

        if (minutesPassed >= waitTimeInMinutes)
        {
            SetUnlockedCached(tower.towerName);
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
        // Dirty-check: only assign TMP text when the displayed second changes.
        string next = mins + ":" + secs.ToString("00");
        string prev;
        if (lastTimerStrings.TryGetValue(tower.towerName, out prev) && prev == next) return;
        lastTimerStrings[tower.towerName] = next;
        tower.timerText.text = next;
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
            yield return timerTickWait;

            for (int i = 1; i <= 2; i++)
            {
                if (i >= towers.Count) continue;

                TowerEntry tower = towers[i];
                // Perf: memory cache, no PlayerPrefs / DateTime.Parse per tick.
                if (IsUnlockedCached(tower.towerName))
                {
                    ShowTimer(tower, false);
                    continue;
                }

                DateTime unlockTime;
                if (!TryGetUnlockTimeCached(tower.towerName, out unlockTime)) continue;

                double minutesPassed = (DateTime.UtcNow - unlockTime).TotalMinutes;

                if (minutesPassed >= waitTimeInMinutes)
                {
                    SetUnlockedCached(tower.towerName);
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