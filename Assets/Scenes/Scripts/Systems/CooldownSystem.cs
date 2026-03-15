using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// System for managing tower cooldowns.
/// Tracks when towers can be played again after completion.
/// </summary>
public class CooldownSystem : MonoBehaviour
{
    private static CooldownSystem _instance;
    public static CooldownSystem Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<CooldownSystem>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("CooldownSystem");
                    _instance = go.AddComponent<CooldownSystem>();
                    DontDestroyOnLoad(go);
                }
            }
            return _instance;
        }
    }
    
    private bool _isInitialized = false;
    private float _lastUpdateTime = 0f;
    private float _updateInterval = 1f; // Update every second
    
    // Events
    public event Action<int> OnCooldownExpired; // towerId
    public event Action<int, TimeSpan> OnCooldownUpdated; // towerId, remainingTime
    
    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
            Initialize();
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
        }
    }
    
    private void Initialize()
    {
        _isInitialized = true;
        _lastUpdateTime = Time.time;
        Debug.Log("CooldownSystem initialized");
    }
    
    private void Update()
    {
        if (!_isInitialized) return;
        
        // Update cooldowns periodically
        if (Time.time - _lastUpdateTime >= _updateInterval)
        {
            UpdateAllCooldowns();
            _lastUpdateTime = Time.time;
        }
    }
    
    /// <summary>
    /// Updates all cooldowns and checks for expired ones
    /// </summary>
    private void UpdateAllCooldowns()
    {
        CloudSaveData saveData = DatabaseManager.Instance?.GetSaveData();
        if (saveData?.cooldowns == null) return;
        
        bool dataChanged = false;
        
        foreach (var cooldown in saveData.cooldowns)
        {
            bool wasAvailable = cooldown.isAvailable;
            cooldown.UpdateAvailability();
            
            if (!wasAvailable && cooldown.isAvailable)
            {
                // Cooldown just expired
                OnCooldownExpired?.Invoke(cooldown.towerId);
                dataChanged = true;
            }
            
            if (!cooldown.isAvailable)
            {
                TimeSpan remaining = cooldown.GetRemainingTime();
                OnCooldownUpdated?.Invoke(cooldown.towerId, remaining);
            }
        }
        
        if (dataChanged)
        {
            DatabaseManager.Instance?.MarkDataDirty();
        }
    }
    
    /// <summary>
    /// Starts a cooldown for a tower
    /// </summary>
    public void StartCooldown(int towerId, int durationSeconds)
    {
        CloudSaveData saveData = DatabaseManager.Instance?.GetSaveData();
        if (saveData == null) return;
        
        if (saveData.cooldowns == null)
        {
            saveData.cooldowns = new List<Cooldown>();
        }
        
        // Find existing cooldown or create new one
        Cooldown cooldown = saveData.cooldowns.FirstOrDefault(c => c.towerId == towerId && c.userId == saveData.userId);
        
        if (cooldown == null)
        {
            cooldown = new Cooldown
            {
                cooldownId = UnityEngine.Random.Range(1000, 9999),
                userId = saveData.userId,
                towerId = towerId,
                cooldownDuration = durationSeconds
            };
            saveData.cooldowns.Add(cooldown);
        }
        
        cooldown.StartCooldown();
        DatabaseManager.Instance?.MarkDataDirty();
        
        Debug.Log($"Cooldown started for tower {towerId}: {durationSeconds} seconds");
    }
    
    /// <summary>
    /// Checks if a tower is available (not on cooldown)
    /// </summary>
    public bool IsTowerAvailable(int towerId)
    {
        CloudSaveData saveData = DatabaseManager.Instance?.GetSaveData();
        if (saveData?.cooldowns == null) return true;
        
        Cooldown cooldown = saveData.cooldowns.FirstOrDefault(c => c.towerId == towerId && c.userId == saveData.userId);
        
        if (cooldown == null) return true;
        
        cooldown.UpdateAvailability();
        return cooldown.isAvailable;
    }
    
    /// <summary>
    /// Gets remaining cooldown time for a tower
    /// </summary>
    public TimeSpan GetRemainingCooldown(int towerId)
    {
        CloudSaveData saveData = DatabaseManager.Instance?.GetSaveData();
        if (saveData?.cooldowns == null) return TimeSpan.Zero;
        
        Cooldown cooldown = saveData.cooldowns.FirstOrDefault(c => c.towerId == towerId && c.userId == saveData.userId);
        
        if (cooldown == null) return TimeSpan.Zero;
        
        cooldown.UpdateAvailability();
        return cooldown.GetRemainingTime();
    }
    
    /// <summary>
    /// Gets cooldown for a tower
    /// </summary>
    public Cooldown GetCooldown(int towerId)
    {
        CloudSaveData saveData = DatabaseManager.Instance?.GetSaveData();
        if (saveData?.cooldowns == null) return null;
        
        return saveData.cooldowns.FirstOrDefault(c => c.towerId == towerId && c.userId == saveData.userId);
    }
}
