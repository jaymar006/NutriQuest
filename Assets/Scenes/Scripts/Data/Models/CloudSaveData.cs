using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable]
public class CloudSaveData
{
    public string userId;
    public User userData;
    public List<Tower> towers;
    public List<Achievement> achievements;
    public List<Attempt> recentAttempts;
    public Stamina stamina;
    public List<Cooldown> cooldowns; // List of cooldowns (one per tower per user)
    public string lastSaveTimeString; // DateTime as string for JSON serialization
    public int saveVersion; // For handling save data migrations
    
    // Non-serialized property for runtime use
    [NonSerialized] private DateTime? _lastSaveTime;
    
    public DateTime lastSaveTime
    {
        get
        {
            if (!_lastSaveTime.HasValue)
            {
                if (!string.IsNullOrEmpty(lastSaveTimeString))
                {
                    if (DateTime.TryParse(lastSaveTimeString, out DateTime parsed))
                    {
                        _lastSaveTime = parsed;
                    }
                    else
                    {
                        _lastSaveTime = DateTime.UtcNow;
                    }
                }
                else
                {
                    _lastSaveTime = DateTime.UtcNow;
                }
            }
            return _lastSaveTime.Value;
        }
        set
        {
            _lastSaveTime = value;
            lastSaveTimeString = value.ToString("O"); // ISO 8601 format
        }
    }
    
    public CloudSaveData()
    {
        towers = new List<Tower>();
        achievements = new List<Achievement>();
        recentAttempts = new List<Attempt>();
        cooldowns = new List<Cooldown>();
        lastSaveTime = DateTime.UtcNow;
        saveVersion = 1;
    }
    
    public CloudSaveData(User user)
    {
        userId = user.userId;
        userData = user;
        towers = new List<Tower>();
        achievements = new List<Achievement>();
        recentAttempts = new List<Attempt>();
        stamina = new Stamina { userId = user.userId };
        cooldowns = new List<Cooldown>();
        lastSaveTime = DateTime.UtcNow;
        saveVersion = 1;
    }
    
    public void UpdateSaveTime()
    {
        // Android: Use UTC to avoid timezone issues when device timezone changes
        lastSaveTime = DateTime.UtcNow;
    }
    
    /// <summary>
    /// Prepares data for serialization
    /// </summary>
    public void PrepareForSerialization()
    {
        // Prepare all nested objects for serialization
        if (userData != null)
        {
            userData.PrepareForSerialization();
        }
        
        if (stamina != null)
        {
            stamina.PrepareForSerialization();
        }
        
        foreach (var cooldown in cooldowns)
        {
            cooldown.PrepareForSerialization();
        }
        
        foreach (var attempt in recentAttempts)
        {
            attempt.PrepareForSerialization();
        }
        
        foreach (var achievement in achievements)
        {
            achievement.PrepareForSerialization();
        }
        
        lastSaveTimeString = lastSaveTime.ToString("O");
    }
    
    /// <summary>
    /// Gets cooldown for a specific tower
    /// </summary>
    public Cooldown GetCooldownForTower(int towerId, string userId)
    {
        return cooldowns?.FirstOrDefault(c => c.towerId == towerId && c.userId == userId);
    }
}
