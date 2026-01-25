using System;
using UnityEngine;

[Serializable]
public class Stamina
{
    public int staminaId;
    public string userId;
    public int currentStamina;
    public int maxStamina;
    public string lastRegenTimeString; // DateTime as string for JSON serialization
    public int regenRate; // Regeneration rate in seconds
    
    // Non-serialized property for runtime use
    [NonSerialized] private DateTime? _lastRegenTime;
    
    public DateTime lastRegenTime
    {
        get
        {
            if (!_lastRegenTime.HasValue)
            {
                if (!string.IsNullOrEmpty(lastRegenTimeString))
                {
                    if (DateTime.TryParse(lastRegenTimeString, out DateTime parsed))
                    {
                        _lastRegenTime = parsed;
                    }
                    else
                    {
                        _lastRegenTime = DateTime.UtcNow;
                    }
                }
                else
                {
                    _lastRegenTime = DateTime.UtcNow;
                }
            }
            return _lastRegenTime.Value;
        }
        set
        {
            _lastRegenTime = value;
            lastRegenTimeString = value.ToString("O");
        }
    }
    
    public Stamina()
    {
        maxStamina = 100;
        currentStamina = maxStamina;
        regenRate = 60; // Default: 1 stamina per 60 seconds (1 per minute)
        // Android: Use UTC to avoid timezone issues
        lastRegenTime = DateTime.UtcNow;
    }
    
    public void PrepareForSerialization()
    {
        lastRegenTimeString = lastRegenTime.ToString("O");
    }
    
    /// <summary>
    /// Regenerates stamina based on time passed since last regeneration
    /// </summary>
    public void RegenerateStamina()
    {
        if (currentStamina >= maxStamina) return;
        
        // Android: Use UTC for consistent calculations
        TimeSpan timeSinceLastRegen = DateTime.UtcNow - lastRegenTime;
        double secondsPassed = timeSinceLastRegen.TotalSeconds;
        
        // Calculate how much stamina should regenerate (1 per regenRate seconds)
        int staminaToAdd = Mathf.FloorToInt((float)(secondsPassed / regenRate));
        
        if (staminaToAdd > 0)
        {
            currentStamina = Mathf.Min(maxStamina, currentStamina + staminaToAdd);
            lastRegenTime = DateTime.UtcNow;
        }
    }
    
    public float GetStaminaPercentage()
    {
        return (float)currentStamina / maxStamina;
    }
    
    public TimeSpan GetTimeUntilNextRegen()
    {
        if (currentStamina >= maxStamina) return TimeSpan.Zero;
        
        TimeSpan timeSinceLastRegen = DateTime.UtcNow - lastRegenTime;
        double secondsUntilNext = regenRate - (timeSinceLastRegen.TotalSeconds % regenRate);
        return TimeSpan.FromSeconds(secondsUntilNext);
    }
}
