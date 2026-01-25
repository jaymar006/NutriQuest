using System;
using UnityEngine;

[Serializable]
public class Cooldown
{
    public int cooldownId;
    public string userId;
    public int towerId;
    public string lastPlayedString; // DateTime as string for JSON serialization
    public int cooldownDuration; // Cooldown duration in seconds
    public bool isAvailable;
    
    // Non-serialized property for runtime use
    [NonSerialized] private DateTime? _lastPlayed;
    
    public DateTime lastPlayed
    {
        get
        {
            if (!_lastPlayed.HasValue)
            {
                if (!string.IsNullOrEmpty(lastPlayedString))
                {
                    if (DateTime.TryParse(lastPlayedString, out DateTime parsed))
                    {
                        _lastPlayed = parsed;
                    }
                    else
                    {
                        _lastPlayed = DateTime.UtcNow;
                    }
                }
                else
                {
                    _lastPlayed = DateTime.UtcNow;
                }
            }
            return _lastPlayed.Value;
        }
        set
        {
            _lastPlayed = value;
            lastPlayedString = value.ToString("O");
        }
    }
    
    public Cooldown()
    {
        isAvailable = true;
        cooldownDuration = 300; // Default: 5 minutes (300 seconds)
        // Android: Use UTC to avoid timezone issues
        lastPlayed = DateTime.UtcNow;
    }
    
    public void PrepareForSerialization()
    {
        lastPlayedString = lastPlayed.ToString("O");
    }
    
    /// <summary>
    /// Checks if the cooldown has expired and updates isAvailable
    /// </summary>
    public void UpdateAvailability()
    {
        // Android: Use UTC for consistent calculations
        TimeSpan timeSinceLastPlayed = DateTime.UtcNow - lastPlayed;
        isAvailable = timeSinceLastPlayed.TotalSeconds >= cooldownDuration;
    }
    
    /// <summary>
    /// Gets the remaining cooldown time in seconds
    /// </summary>
    public double GetRemainingSeconds()
    {
        if (isAvailable) return 0;
        
        TimeSpan timeSinceLastPlayed = DateTime.UtcNow - lastPlayed;
        double remaining = cooldownDuration - timeSinceLastPlayed.TotalSeconds;
        return remaining > 0 ? remaining : 0;
    }
    
    /// <summary>
    /// Gets the remaining cooldown time as TimeSpan
    /// </summary>
    public TimeSpan GetRemainingTime()
    {
        return TimeSpan.FromSeconds(GetRemainingSeconds());
    }
    
    /// <summary>
    /// Starts the cooldown timer
    /// </summary>
    public void StartCooldown()
    {
        lastPlayed = DateTime.UtcNow;
        isAvailable = false;
    }
}
