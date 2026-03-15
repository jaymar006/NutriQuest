using System;
using UnityEngine;

[Serializable]
public class User
{
    public string userId;
    public string username;
    public string password;
    public int currentTower;
    public int highestScore;
    public int staminaPoints;
    public int maxStamina;
    public string registrationDateString; // DateTime as string for JSON serialization
    public bool soundEnabled;
    
    // Non-serialized property for runtime use
    [NonSerialized] private DateTime? _registrationDate;
    
    public DateTime registrationDate
    {
        get
        {
            if (!_registrationDate.HasValue)
            {
                if (!string.IsNullOrEmpty(registrationDateString))
                {
                    if (DateTime.TryParse(registrationDateString, out DateTime parsed))
                    {
                        _registrationDate = parsed;
                    }
                    else
                    {
                        _registrationDate = DateTime.UtcNow;
                    }
                }
                else
                {
                    _registrationDate = DateTime.UtcNow;
                }
            }
            return _registrationDate.Value;
        }
        set
        {
            _registrationDate = value;
            registrationDateString = value.ToString("O");
        }
    }
    
    public User()
    {
        currentTower = 0;
        highestScore = 0;
        staminaPoints = 100; // Default max stamina
        maxStamina = 100;
        soundEnabled = true;
        // Android: Use UTC to avoid timezone issues
        registrationDate = DateTime.UtcNow;
    }
    
    public void PrepareForSerialization()
    {
        registrationDateString = registrationDate.ToString("O");
    }
}
