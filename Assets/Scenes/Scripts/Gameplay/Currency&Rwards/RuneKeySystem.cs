using UnityEngine;
using TMPro;
using System;
using System.Collections;
using System.Collections.Generic;

public class RuneKeySystem : MonoBehaviour
{
    public static RuneKeySystem Instance { get; private set; }

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

    private const string RUNE_KEY = "RuneKeys";
    private const string RUNE_LAST_REGEN = "RuneLastRegen";

    public int CurrentKeys
    {
        get => PlayerPrefs.GetInt(RUNE_KEY, maxRuneKeys);
        private set
        {
            int newValue = Mathf.Clamp(value, 0, maxRuneKeys);
            PlayerPrefs.SetInt(RUNE_KEY, newValue);
            PlayerPrefs.Save();
            UpdateAllKeyDisplays();
        }
    }

    private void Awake()
    {
        // Singleton pattern
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        // Auto-find all text displays
        if (autoFindTexts)
        {
            FindAllRuneKeyTexts();
        }

        ProcessOfflineRegen();
        UpdateAllKeyDisplays();

        // Start timer loop only if we have a timer display
        if (regenTimerText != null)
        {
            StartCoroutine(RegenLoop());
        }
    }

    // Find all text objects that should display rune keys
    private void FindAllRuneKeyTexts()
    {
        runeKeyTexts.Clear();

        // Find all TMP_Text objects in the scene (including inactive)
        TMP_Text[] allTexts = FindObjectsOfType<TMP_Text>(true);

        foreach (TMP_Text text in allTexts)
        {
            string objName = text.gameObject.name;

            // Check if this text object matches any of our tags
            foreach (string tag in textTags)
            {
                if (objName.Contains(tag) || objName.Equals(tag))
                {
                    if (!runeKeyTexts.Contains(text))
                    {
                        runeKeyTexts.Add(text);
                        Debug.Log($"[RuneKeySystem] Found rune key display: {objName}");
                    }
                    break;
                }
            }
        }

        Debug.Log($"[RuneKeySystem] Auto-found {runeKeyTexts.Count} rune key displays");
    }

    // Update all text displays with current key count
    private void UpdateAllKeyDisplays()
    {
        int currentKeys = CurrentKeys;

        // Remove any null references
        runeKeyTexts.RemoveAll(item => item == null);

        // Update all displays
        foreach (TMP_Text text in runeKeyTexts)
        {
            if (text != null)
            {
                text.text = $"{currentKeys}/{maxRuneKeys}";
            }
        }
    }

    // Public method to manually register a text display
    public void RegisterKeyDisplay(TMP_Text textDisplay)
    {
        if (textDisplay == null) return;

        if (!runeKeyTexts.Contains(textDisplay))
        {
            runeKeyTexts.Add(textDisplay);
            textDisplay.text = $"{CurrentKeys}/{maxRuneKeys}";
            Debug.Log($"[RuneKeySystem] Registered key display: {textDisplay.gameObject.name}");
        }
    }

    // Public method to remove a text display
    public void UnregisterKeyDisplay(TMP_Text textDisplay)
    {
        if (textDisplay == null) return;

        if (runeKeyTexts.Contains(textDisplay))
        {
            runeKeyTexts.Remove(textDisplay);
            Debug.Log($"[RuneKeySystem] Unregistered key display: {textDisplay.gameObject.name}");
        }
    }

    // Manually refresh all displays (call if needed)
    public void RefreshAllDisplays()
    {
        UpdateAllKeyDisplays();
    }

    private void ProcessOfflineRegen()
    {
        if (CurrentKeys >= maxRuneKeys) return;

        string lastRegenStr = PlayerPrefs.GetString(RUNE_LAST_REGEN, "");
        if (string.IsNullOrEmpty(lastRegenStr)) return;

        try
        {
            DateTime lastRegen = DateTime.Parse(lastRegenStr);
            double minutesPassed = (DateTime.UtcNow - lastRegen).TotalMinutes;
            int keysToAdd = Mathf.FloorToInt((float)minutesPassed / regenTimeInMinutes);

            if (keysToAdd > 0)
            {
                int newKeys = Mathf.Min(CurrentKeys + keysToAdd, maxRuneKeys);
                PlayerPrefs.SetInt(RUNE_KEY, newKeys);

                float remainingMinutes = (float)(minutesPassed % regenTimeInMinutes);
                DateTime newLastRegen = DateTime.UtcNow.AddMinutes(-remainingMinutes);
                PlayerPrefs.SetString(RUNE_LAST_REGEN, newLastRegen.ToString());
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
            yield return new WaitForSeconds(1f);

            if (CurrentKeys >= maxRuneKeys)
            {
                if (regenTimerText != null)
                    regenTimerText.text = "FULL";
                continue;
            }

            string lastRegenStr = PlayerPrefs.GetString(RUNE_LAST_REGEN, "");

            if (string.IsNullOrEmpty(lastRegenStr))
            {
                PlayerPrefs.SetString(RUNE_LAST_REGEN, DateTime.UtcNow.ToString());
                PlayerPrefs.Save();
                continue;
            }

            try
            {
                DateTime lastRegen = DateTime.Parse(lastRegenStr);
                double minutesPassed = (DateTime.UtcNow - lastRegen).TotalMinutes;

                if (minutesPassed >= regenTimeInMinutes)
                {
                    CurrentKeys++;
                    PlayerPrefs.SetString(RUNE_LAST_REGEN, DateTime.UtcNow.ToString());
                    PlayerPrefs.Save();
                    Debug.Log($"[RuneKeySystem] Rune key regenerated! Now: {CurrentKeys}");
                }
                else if (regenTimerText != null)
                {
                    double remaining = (regenTimeInMinutes - minutesPassed) * 60;
                    int mins = Mathf.FloorToInt((float)remaining / 60f);
                    int secs = Mathf.FloorToInt((float)remaining % 60f);
                    regenTimerText.text = string.Format("{0}:{1:00}", mins, secs);
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

        if (string.IsNullOrEmpty(PlayerPrefs.GetString(RUNE_LAST_REGEN, "")))
        {
            PlayerPrefs.SetString(RUNE_LAST_REGEN, DateTime.UtcNow.ToString());
            PlayerPrefs.Save();
        }

        CurrentKeys -= amount;
        Debug.Log($"[RuneKeySystem] Spent {amount} key(s). Remaining: {CurrentKeys}");
        return true;
    }

    public void AddKey(int amount = 1)
    {
        CurrentKeys = Mathf.Min(CurrentKeys + amount, maxRuneKeys);
        Debug.Log($"[RuneKeySystem] Added {amount} key(s). Now: {CurrentKeys}");
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