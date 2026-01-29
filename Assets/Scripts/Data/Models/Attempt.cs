using System;
using UnityEngine;

[Serializable]
public class Attempt
{
    public int attemptId;
    public string userId;
    public int towerId;
    public int score;
    public string dateAttemptedString; // DateTime as string for JSON serialization
    public bool cleared;
    public bool perfectScore;
    
    // Non-serialized property for runtime use
    [NonSerialized] private DateTime? _dateAttempted;
    
    public DateTime dateAttempted
    {
        get
        {
            if (!_dateAttempted.HasValue)
            {
                if (!string.IsNullOrEmpty(dateAttemptedString))
                {
                    if (DateTime.TryParse(dateAttemptedString, out DateTime parsed))
                    {
                        _dateAttempted = parsed;
                    }
                    else
                    {
                        _dateAttempted = DateTime.UtcNow;
                    }
                }
                else
                {
                    _dateAttempted = DateTime.UtcNow;
                }
            }
            return _dateAttempted.Value;
        }
        set
        {
            _dateAttempted = value;
            dateAttemptedString = value.ToString("O");
        }
    }
    
    public Attempt()
    {
        score = 0;
        cleared = false;
        perfectScore = false;
        // Android: Use UTC to avoid timezone issues
        dateAttempted = DateTime.UtcNow;
    }
    
    public void PrepareForSerialization()
    {
        dateAttemptedString = dateAttempted.ToString("O");
    }
}
