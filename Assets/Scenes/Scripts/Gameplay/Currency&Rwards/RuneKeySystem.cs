using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;

public class RuneKeySystem : MonoBehaviour
{
    public static RuneKeySystem Instance { get; private set; }

    // Event so any script (e.g. LevelInfoScreen) can react when keys change
    public static event Action OnKeysChanged;

    [Header("Rune Key Settings")]
    [SerializeField] private int maxRuneKeys = 3;
    [SerializeField] private float regenTimeInMinutes = 30f;

    [Header("UI - Multiple Displays")]
    [SerializeField] private List<TMP_Text> runeKeyTexts = new List<TMP_Text>();

    [Header("Auto Find Settings")]
    [SerializeField] private bool autoFindTexts = true;
    [SerializeField] private string[] textTags = { "RuneKeyText", "KeyCount", "RuneCount", "RuneKey" };

    [Header("Optional Timer UI")]
    [SerializeField] private TMP_Text regenTimerText;

    private List<string> persistentDisplayNames = new List<string>();
    private string persistentTimerName = null;

    // Prevents OnSceneLoaded from clearing the timer ref before Start() saves its name
    private bool hasStartedOnce = false;

    // Track the RegenLoop coroutine so we can stop it reliably
    private Coroutine regenLoopCoroutine = null;

    private const string RUNE_KEY = "RuneKeys";
    private const string RUNE_LAST_REGEN = "RuneLastRegen";

    // Perf: in-memory cache so per-tick code never touches PlayerPrefs / DateTime.Parse.
    // Load once in Awake, update field + PlayerPrefs together on change, getter returns field.
    private int cachedKeys;
    private bool keysLoaded = false;
    private DateTime cachedLastRegen = DateTime.MinValue;
    private bool hasLastRegen = false;

    // Perf: shared yield instruction + timer-text dirty check (avoid TMP rebuild every tick).
    private static readonly WaitForSeconds regenTickWait = new WaitForSeconds(1f);
    private string lastTimerString = null;

    public int CurrentKeys
    {
        get
        {
            if (!keysLoaded)
            {
                cachedKeys = PlayerPrefs.GetInt(RUNE_KEY, maxRuneKeys);
                keysLoaded = true;
            }
            return cachedKeys;
        }
        private set
        {
            int newValue = Mathf.Clamp(value, 0, maxRuneKeys);
            cachedKeys = newValue;
            keysLoaded = true;
            PlayerPrefs.SetInt(RUNE_KEY, newValue);
            PlayerPrefs.Save();
            UpdateAllKeyDisplays();
            // Broadcast so LevelInfoScreen buttons update immediately
            OnKeysChanged?.Invoke();
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        LoadCachedState();
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void LoadCachedState()
    {
        cachedKeys = PlayerPrefs.GetInt(RUNE_KEY, maxRuneKeys);
        keysLoaded = true;
        string s = PlayerPrefs.GetString(RUNE_LAST_REGEN, "");
        if (!string.IsNullOrEmpty(s))
        {
            try
            {
                cachedLastRegen = DateTime.Parse(s, null, DateTimeStyles.RoundtripKind);
                hasLastRegen = true;
            }
            catch
            {
                hasLastRegen = false;
            }
        }
    }

    private void SetLastRegenNoSave(DateTime utcNow)
    {
        cachedLastRegen = utcNow;
        hasLastRegen = true;
        PlayerPrefs.SetString(RUNE_LAST_REGEN, utcNow.ToString("O"));
    }

    private void SetTimerText(string value)
    {
        if (lastTimerString == value) return;
        lastTimerString = value;
        if (regenTimerText != null)
            regenTimerText.text = value;
    }

    private void OnDestroy()
    {
        if (regenLoopCoroutine != null)
        {
            StopCoroutine(regenLoopCoroutine);
            regenLoopCoroutine = null;
        }
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"[RuneKeySystem] Scene loaded: {scene.name}");

        runeKeyTexts.RemoveAll(item => item == null);

        // Only wipe the timer ref on scene TRANSITIONS (after first Start() ran).
        // On the very first scene load, Start() hasn't fired yet so persistentTimerName
        // hasn't been saved � clearing here would destroy the manually assigned reference.
        if (hasStartedOnce)
            regenTimerText = null;

        StartCoroutine(RefreshAfterSceneLoad());
    }

    private IEnumerator RefreshAfterSceneLoad()
    {
        yield return new WaitForEndOfFrame();

        RelinkPersistentDisplays();

        if (autoFindTexts)
            FindAllRuneKeyTexts();

        UpdateAllKeyDisplays();

        // Notify listeners so buttons reflect the current key count on new scene
        OnKeysChanged?.Invoke();

        if (regenLoopCoroutine != null)
        {
            StopCoroutine(regenLoopCoroutine);
            regenLoopCoroutine = null;
        }

        regenLoopCoroutine = StartCoroutine(RegenLoop());
    }

    private void Start()
    {
        // Save inspector-assigned names BEFORE anything else can clear them
        SavePersistentDisplayNames();

        // Mark that Start has run � OnSceneLoaded can now safely clear timer on future loads
        hasStartedOnce = true;

        if (autoFindTexts && runeKeyTexts.Count == 0)
            FindAllRuneKeyTexts();

        ProcessOfflineRegen();
        UpdateAllKeyDisplays();

        OnKeysChanged?.Invoke();

        regenLoopCoroutine = StartCoroutine(RegenLoop());
    }

    private void SavePersistentDisplayNames()
    {
        foreach (TMP_Text t in runeKeyTexts)
        {
            if (t != null && !persistentDisplayNames.Contains(t.gameObject.name))
            {
                persistentDisplayNames.Add(t.gameObject.name);
                Debug.Log($"[RuneKeySystem] Remembered display name: {t.gameObject.name}");
            }
        }

        if (regenTimerText != null)
        {
            persistentTimerName = regenTimerText.gameObject.name;
            Debug.Log($"[RuneKeySystem] Remembered timer name: {persistentTimerName}");
        }
    }

    private void RelinkPersistentDisplays()
    {
        if (persistentDisplayNames.Count == 0 && persistentTimerName == null) return;

        TMP_Text[] allTexts = Resources.FindObjectsOfTypeAll<TMP_Text>();

        foreach (TMP_Text text in allTexts)
        {
            if (text.gameObject.scene.name == null) continue;
            if (text.gameObject.scene.name == "DontDestroyOnLoad") continue;

            string objName = text.gameObject.name;

            if (persistentDisplayNames.Contains(objName) && !runeKeyTexts.Contains(text))
            {
                runeKeyTexts.Add(text);
                Debug.Log($"[RuneKeySystem] Re-linked display: {objName}");
            }

            if (persistentTimerName != null && objName == persistentTimerName && regenTimerText == null)
            {
                regenTimerText = text;
                Debug.Log($"[RuneKeySystem] Re-linked timer: {objName}");
            }
        }
    }

    private void FindAllRuneKeyTexts()
    {
        runeKeyTexts.RemoveAll(item => item == null);

        TMP_Text[] allTexts = Resources.FindObjectsOfTypeAll<TMP_Text>();

        foreach (TMP_Text text in allTexts)
        {
            if (text.gameObject.scene.name == null) continue;
            if (!text.gameObject.activeInHierarchy && text.gameObject.hideFlags == HideFlags.HideAndDontSave) continue;
            if (text.gameObject.scene.name == "DontDestroyOnLoad") continue;

            string objName = text.gameObject.name;
            bool found = false;

            foreach (string tag in textTags)
            {
                if (objName.Contains(tag) || objName.Equals(tag))
                {
                    if (!runeKeyTexts.Contains(text))
                    {
                        runeKeyTexts.Add(text);
                        Debug.Log($"[RuneKeySystem] Found rune key display: {objName}");
                        found = true;
                        break;
                    }
                }
            }

            if (!found && text.transform.parent != null)
            {
                string parentName = text.transform.parent.name;
                foreach (string tag in textTags)
                {
                    if (parentName.Contains(tag) || parentName.Equals(tag))
                    {
                        if (!runeKeyTexts.Contains(text))
                        {
                            runeKeyTexts.Add(text);
                            Debug.Log($"[RuneKeySystem] Found rune key display via parent: {parentName} -> {objName}");

                            if (regenTimerText == null && objName.Contains("Timer"))
                                regenTimerText = text;
                        }
                        break;
                    }
                }
            }
        }

        Debug.Log($"[RuneKeySystem] Auto-found {runeKeyTexts.Count} rune key displays");

        if (runeKeyTexts.Count == 0)
            Debug.LogWarning("[RuneKeySystem] No displays found. Name your texts with: " + string.Join(", ", textTags));
    }

    private void UpdateAllKeyDisplays()
    {
        int currentKeys = CurrentKeys;
        runeKeyTexts.RemoveAll(item => item == null);

        foreach (TMP_Text text in runeKeyTexts)
        {
            if (text != null)
                text.text = $"{currentKeys}/{maxRuneKeys}";
        }

        if (runeKeyTexts.Count > 0)
            Debug.Log($"[RuneKeySystem] Updated {runeKeyTexts.Count} displays to: {currentKeys}/{maxRuneKeys}");
    }

    public void RegisterKeyDisplay(TMP_Text textDisplay)
    {
        if (textDisplay == null) return;
        if (!runeKeyTexts.Contains(textDisplay))
        {
            runeKeyTexts.Add(textDisplay);
            textDisplay.text = $"{CurrentKeys}/{maxRuneKeys}";
            if (!persistentDisplayNames.Contains(textDisplay.gameObject.name))
                persistentDisplayNames.Add(textDisplay.gameObject.name);
            Debug.Log($"[RuneKeySystem] Registered key display: {textDisplay.gameObject.name}");
        }
    }

    public void UnregisterKeyDisplay(TMP_Text textDisplay)
    {
        if (textDisplay == null) return;
        if (runeKeyTexts.Contains(textDisplay))
        {
            runeKeyTexts.Remove(textDisplay);
            Debug.Log($"[RuneKeySystem] Unregistered key display: {textDisplay.gameObject.name}");
        }
    }

    public void RefreshAllDisplays()
    {
        UpdateAllKeyDisplays();
        OnKeysChanged?.Invoke();
    }

    private void ProcessOfflineRegen()
    {
        if (CurrentKeys >= maxRuneKeys) return;

        if (!hasLastRegen) return;

        try
        {
            DateTime lastRegen = cachedLastRegen;
            double minutesPassed = (DateTime.UtcNow - lastRegen).TotalMinutes;
            int keysToAdd = Mathf.FloorToInt((float)minutesPassed / regenTimeInMinutes);

            if (keysToAdd > 0)
            {
                int newKeys = Mathf.Min(CurrentKeys + keysToAdd, maxRuneKeys);
                // Batch: single PlayerPrefs.Save() per regen.
                cachedKeys = newKeys;
                keysLoaded = true;
                PlayerPrefs.SetInt(RUNE_KEY, newKeys);

                float remainingMinutes = (float)(minutesPassed % regenTimeInMinutes);
                DateTime newLastRegen = DateTime.UtcNow.AddMinutes(-remainingMinutes);
                SetLastRegenNoSave(newLastRegen);
                PlayerPrefs.Save();

                UpdateAllKeyDisplays();
                Debug.Log($"[RuneKeySystem] Offline regen: +{keysToAdd} keys. Now: {newKeys}");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[RuneKeySystem] Error processing offline regen: {e.Message}");
        }
    }

    private IEnumerator RegenLoop()
    {
        while (true)
        {
            yield return regenTickWait;

            if (CurrentKeys >= maxRuneKeys)
            {
                SetTimerText("FULL");
                continue;
            }

            if (!hasLastRegen)
            {
                SetLastRegenNoSave(DateTime.UtcNow);
                PlayerPrefs.Save();
                continue;
            }

            try
            {
                DateTime lastRegen = cachedLastRegen;
                double minutesPassed = (DateTime.UtcNow - lastRegen).TotalMinutes;

                if (minutesPassed >= regenTimeInMinutes)
                {
                    // Batch: update memory + PlayerPrefs once, single Save() per regen
                    // (previously CurrentKeys++ saved, then SetString saved again = double save).
                    cachedKeys = Mathf.Clamp(cachedKeys + 1, 0, maxRuneKeys);
                    keysLoaded = true;
                    PlayerPrefs.SetInt(RUNE_KEY, cachedKeys);
                    SetLastRegenNoSave(DateTime.UtcNow);
                    PlayerPrefs.Save();
                    UpdateAllKeyDisplays();
                    OnKeysChanged?.Invoke();
                }
                else if (regenTimerText != null)
                {
                    double remaining = (regenTimeInMinutes - minutesPassed) * 60;
                    int mins = Mathf.FloorToInt((float)remaining / 60f);
                    int secs = Mathf.FloorToInt((float)remaining % 60f);
                    // Dirty-check: only assign TMP text when the displayed second changes.
                    SetTimerText(mins + ":" + secs.ToString("00"));
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[RuneKeySystem] Error in regen loop: {e.Message}");
            }
        }
    }

    public bool SpendKey(int amount = 1)
    {
        if (CurrentKeys < amount)
        {
            Debug.Log("[RuneKeySystem] Not enough rune keys!");
            return false;
        }

        // FIX: Always restart the regen timer on spend, not just when it was
        // previously empty. Before this fix, a player who'd been sitting
        // below max for a while (regen timer already old/stale) could spend
        // a key and have RegenLoop() immediately hand it right back on its
        // very next 1-second tick, since the stale timestamp already looked
        // "overdue" for a regen that had nothing to do with this spend. That
        // showed up as "my rune key didn't get spent."
        // Perf: keep memory cache in sync; single batched Save (setter also saves,
        // so write memory + prefs directly here to avoid double save).
        SetLastRegenNoSave(DateTime.UtcNow);
        cachedKeys = Mathf.Clamp(cachedKeys - amount, 0, maxRuneKeys);
        keysLoaded = true;
        PlayerPrefs.SetInt(RUNE_KEY, cachedKeys);
        PlayerPrefs.Save();
        UpdateAllKeyDisplays();
        OnKeysChanged?.Invoke();
        Debug.Log($"[RuneKeySystem] Spent {amount} key(s). Remaining: {CurrentKeys}");
        return true;
    }

    public void AddKey(int amount = 1)
    {
        int newValue = Mathf.Clamp(CurrentKeys + amount, 0, maxRuneKeys);
        cachedKeys = newValue;
        keysLoaded = true;
        PlayerPrefs.SetInt(RUNE_KEY, newValue);
        PlayerPrefs.Save();
        UpdateAllKeyDisplays();
        OnKeysChanged?.Invoke();
        Debug.Log("[RuneKeySystem] Added keys. Now: " + newValue);
    }

    public void GeniusReward()
    {
        if (CurrentKeys < maxRuneKeys)
        {
            AddKey(1);
            Debug.Log("[RuneKeySystem] Genius reward! +1 rune key.");
        }
        else
        {
            Debug.Log("[RuneKeySystem] Already at max keys - genius reward skipped.");
        }
    }

    public bool HasEnoughKeys(int amount = 1) => CurrentKeys >= amount;
}