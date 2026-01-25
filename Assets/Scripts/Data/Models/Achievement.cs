using System;
using UnityEngine;

[Serializable]
public class Achievement
{
    public int achievementId;
    public string userId;
    public string achievementName;
    public string condition;
    public string dateEarnedString; // DateTime as string for JSON serialization
    
    // Non-serialized property for runtime use
    [NonSerialized] private DateTime? _dateEarned;
    
    public DateTime dateEarned
    {
        get
        {
            if (!_dateEarned.HasValue && !string.IsNullOrEmpty(dateEarnedString))
            {
                if (DateTime.TryParse(dateEarnedString, out DateTime parsed))
                {
                    _dateEarned = parsed;
                }
            }
            return _dateEarned ?? default(DateTime);
        }
        set
        {
            _dateEarned = value;
            dateEarnedString = value.ToString("O");
        }
    }
    
    public Achievement()
    {
        // Default constructor
    }
    
    public void PrepareForSerialization()
    {
        if (_dateEarned.HasValue)
        {
            dateEarnedString = _dateEarned.Value.ToString("O");
        }
    }
    
    /// <summary>
    /// Checks if the achievement has been earned (dateEarned is set)
    /// </summary>
    public bool IsEarned()
    {
        return _dateEarned.HasValue && !string.IsNullOrEmpty(dateEarnedString);
    }
    
    /// <summary>
    /// Marks the achievement as earned
    /// </summary>
    public void EarnAchievement()
    {
        // Android: Use UTC for consistent tracking
        dateEarned = DateTime.UtcNow;
    }
}
