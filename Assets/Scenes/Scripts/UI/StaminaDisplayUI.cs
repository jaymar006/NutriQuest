using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// UI component for displaying stamina information.
/// Shows current stamina, max stamina, and regeneration progress.
/// </summary>
public class StaminaDisplayUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image staminaBarFill;
    [SerializeField] private TextMeshProUGUI staminaText;
    [SerializeField] private TextMeshProUGUI regenTimeText;
    
    [Header("Settings")]
    [SerializeField] private bool showRegenTime = true;
    [SerializeField] private float updateInterval = 1f; // Update every second
    
    private float _lastUpdateTime = 0f;

    // Perf dirty-check: only assign TMP text / fillAmount when the value changed
    // (TMP text assignment rebuilds layout even for identical strings).
    private int _lastCurrent = -1;
    private int _lastMax = -1;
    private float _lastFill = -1f;
    private string _lastTimeString = null;
    
    private void Update()
    {
        // Update periodically
        if (Time.time - _lastUpdateTime >= updateInterval)
        {
            UpdateStamina();
            _lastUpdateTime = Time.time;
        }
    }
    
    /// <summary>
    /// Updates the stamina display
    /// </summary>
    public void UpdateStamina()
    {
        CloudSaveData saveData = DatabaseManager.Instance?.GetSaveData();
        if (saveData?.stamina == null) return;
        
        int current = saveData.stamina.currentStamina;
        int max = saveData.stamina.maxStamina;
        float percentage = saveData.stamina.GetStaminaPercentage();
        
        // Update bar (dirty-check float to avoid redundant material updates)
        if (staminaBarFill != null && !Mathf.Approximately(percentage, _lastFill))
        {
            staminaBarFill.fillAmount = percentage;
            _lastFill = percentage;
        }
        
        // Update text (only when current/max ints change)
        if (staminaText != null && (current != _lastCurrent || max != _lastMax))
        {
            staminaText.text = string.Concat(current.ToString(), "/", max.ToString());
            _lastCurrent = current;
            _lastMax = max;
        }
        
        // Update regen time (only when displayed string changes)
        if (regenTimeText != null && showRegenTime)
        {
            string next;
            if (current >= max)
            {
                next = "Full";
            }
            else
            {
                System.TimeSpan timeUntil = saveData.stamina.GetTimeUntilNextRegen();
                next = Timer.FormatTimeReadable((float)timeUntil.TotalSeconds);
            }
            if (next != _lastTimeString)
            {
                regenTimeText.text = next;
                _lastTimeString = next;
            }
        }
    }
}
