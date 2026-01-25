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
        
        // Update bar
        if (staminaBarFill != null)
        {
            staminaBarFill.fillAmount = percentage;
        }
        
        // Update text
        if (staminaText != null)
        {
            staminaText.text = $"{current}/{max}";
        }
        
        // Update regen time
        if (regenTimeText != null && showRegenTime)
        {
            if (current >= max)
            {
                regenTimeText.text = "Full";
            }
            else
            {
                System.TimeSpan timeUntil = saveData.stamina.GetTimeUntilNextRegen();
                regenTimeText.text = Timer.FormatTimeReadable((float)timeUntil.TotalSeconds);
            }
        }
    }
}
