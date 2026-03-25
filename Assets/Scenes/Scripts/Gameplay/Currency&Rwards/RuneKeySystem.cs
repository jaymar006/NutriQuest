using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections;

public class RuneKeySystem : MonoBehaviour
{
    public static RuneKeySystem Instance { get; private set; }

    [Header("Rune Key Settings")]
    [SerializeField] private int maxRuneKeys = 3;
    [SerializeField] private float regenTimeInMinutes = 30f;

    [Header("UI")]
    [SerializeField] private TMP_Text runeKeyCountText;
    [SerializeField] private TMP_Text regenTimerText;

    private const string RUNE_KEY = "RuneKeys";
    private const string RUNE_LAST_REGEN = "RuneLastRegen";

    public int CurrentKeys
    {
        get => PlayerPrefs.GetInt(RUNE_KEY, maxRuneKeys);
        private set
        {
            PlayerPrefs.SetInt(RUNE_KEY, Mathf.Clamp(value, 0, maxRuneKeys));
            PlayerPrefs.Save();
        }
    }

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        ProcessOfflineRegen();
        UpdateUI();
        StartCoroutine(RegenLoop());
    }

    private void ProcessOfflineRegen()
    {
        if (CurrentKeys >= maxRuneKeys) return;

        string lastRegenStr = PlayerPrefs.GetString(RUNE_LAST_REGEN, "");
        if (string.IsNullOrEmpty(lastRegenStr)) return;

        DateTime lastRegen = DateTime.Parse(lastRegenStr);
        double minutesPassed = (DateTime.UtcNow - lastRegen).TotalMinutes;
        int keysToAdd = Mathf.FloorToInt((float)minutesPassed / regenTimeInMinutes);

        if (keysToAdd > 0)
        {
            CurrentKeys = Mathf.Min(CurrentKeys + keysToAdd, maxRuneKeys);

            // Update last regen time accounting for partial progress
            float remainingMinutes = (float)(minutesPassed % regenTimeInMinutes);
            DateTime newLastRegen = DateTime.UtcNow.AddMinutes(-remainingMinutes);
            PlayerPrefs.SetString(RUNE_LAST_REGEN, newLastRegen.ToString());
            PlayerPrefs.Save();

            Debug.Log("[RuneKeySystem] Offline regen: +" + keysToAdd + " keys.");
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

            DateTime lastRegen = DateTime.Parse(lastRegenStr);
            double minutesPassed = (DateTime.UtcNow - lastRegen).TotalMinutes;

            if (minutesPassed >= regenTimeInMinutes)
            {
                CurrentKeys++;
                PlayerPrefs.SetString(RUNE_LAST_REGEN, DateTime.UtcNow.ToString());
                PlayerPrefs.Save();
                Debug.Log("[RuneKeySystem] Rune key regenerated! Now: " + CurrentKeys);
            }
            else
            {
                // Show countdown timer
                double remaining = (regenTimeInMinutes - minutesPassed) * 60;
                int mins = Mathf.FloorToInt((float)remaining / 60f);
                int secs = Mathf.FloorToInt((float)remaining % 60f);

                if (regenTimerText != null)
                    regenTimerText.text = string.Format("{0}:{1:00}", mins, secs);
            }

            UpdateUI();
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
        UpdateUI();
        Debug.Log("[RuneKeySystem] Spent " + amount + " key(s). Remaining: " + CurrentKeys);
        return true;
    }

    public void AddKey(int amount = 1)
    {
        CurrentKeys = Mathf.Min(CurrentKeys + amount, maxRuneKeys);
        UpdateUI();
        Debug.Log("[RuneKeySystem] Added " + amount + " key(s). Now: " + CurrentKeys);
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
            Debug.Log("[RuneKeySystem] Already at max keys — genius reward skipped.");
        }
    }

    private void UpdateUI()
    {
        if (runeKeyCountText != null)
            runeKeyCountText.text = CurrentKeys + "/" + maxRuneKeys;
    }

    public bool HasEnoughKeys(int amount = 1) => CurrentKeys >= amount;
}