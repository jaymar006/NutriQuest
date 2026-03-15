using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Service for syncing achievements between local data and Google Play Games.
/// </summary>
public class AchievementSyncService : MonoBehaviour
{
    private static AchievementSyncService _instance;
    public static AchievementSyncService Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<AchievementSyncService>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("AchievementSyncService");
                    _instance = go.AddComponent<AchievementSyncService>();
                    DontDestroyOnLoad(go);
                }
            }
            return _instance;
        }
    }
    
    private bool _isInitialized = false;
    private Dictionary<string, string> _achievementMapping; // Maps local achievement IDs to Google Play Games IDs
    
    // Events
    public event Action<string> OnAchievementUnlocked;
    public event Action OnSyncComplete;
    
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
        _achievementMapping = new Dictionary<string, string>();
        
        // Initialize achievement mapping
        // This would typically load from a config file
        InitializeAchievementMapping();
        
        Debug.Log("AchievementSyncService initialized");
    }
    
    /// <summary>
    /// Initializes the mapping between local achievement IDs and Google Play Games achievement IDs
    /// </summary>
    private void InitializeAchievementMapping()
    {
        // Example mappings - these should match your Google Play Console achievement IDs
        // _achievementMapping["first_tower_complete"] = "CgkI..."; // Google Play Games achievement ID
        // _achievementMapping["perfect_score"] = "CgkI...";
        // etc.
        
        Debug.Log("Achievement mapping initialized (empty - needs configuration)");
    }
    
    /// <summary>
    /// Unlocks an achievement locally and syncs with Google Play Games
    /// </summary>
    public void UnlockAchievement(Achievement achievement)
    {
        if (achievement == null)
        {
            Debug.LogError("Cannot unlock null achievement");
            return;
        }
        
        // Mark as earned locally
        if (!achievement.IsEarned())
        {
            achievement.EarnAchievement();
            Debug.Log($"Achievement unlocked locally: {achievement.achievementName}");
        }
        
        // Sync with Google Play Games
        SyncAchievementToGooglePlay(achievement);
        
        OnAchievementUnlocked?.Invoke(achievement.achievementName);
    }
    
    /// <summary>
    /// Syncs a single achievement to Google Play Games
    /// </summary>
    private void SyncAchievementToGooglePlay(Achievement achievement)
    {
        if (!GooglePlayGamesService.Instance.IsSignedIn())
        {
            Debug.LogWarning("Cannot sync achievement - user not signed in to Google Play Games");
            return;
        }
        
        #if UNITY_ANDROID && !UNITY_EDITOR
        // Get Google Play Games achievement ID
        string gpgsAchievementId = GetGooglePlayAchievementId(achievement.achievementId.ToString());
        
        if (string.IsNullOrEmpty(gpgsAchievementId))
        {
            Debug.LogWarning($"No Google Play Games mapping found for achievement: {achievement.achievementId}");
            return;
        }
        
        // Unlock achievement in Google Play Games
        // Social.ReportProgress(gpgsAchievementId, 100.0, (bool success) => {
        //     if (success) {
        //         Debug.Log($"Achievement synced to Google Play Games: {achievement.achievementName}");
        //     } else {
        //         Debug.LogError($"Failed to sync achievement to Google Play Games: {achievement.achievementName}");
        //     }
        // });
        
        Debug.Log($"Achievement sync requested (simulated): {achievement.achievementName} -> {gpgsAchievementId}");
        #else
        Debug.Log("Achievement sync only available on Android");
        #endif
    }
    
    /// <summary>
    /// Syncs all earned achievements to Google Play Games
    /// </summary>
    public void SyncAllAchievements(List<Achievement> achievements)
    {
        if (achievements == null || achievements.Count == 0)
        {
            Debug.LogWarning("No achievements to sync");
            return;
        }
        
        if (!GooglePlayGamesService.Instance.IsSignedIn())
        {
            Debug.LogWarning("Cannot sync achievements - user not signed in to Google Play Games");
            return;
        }
        
        int syncedCount = 0;
        foreach (var achievement in achievements)
        {
            if (achievement.IsEarned())
            {
                SyncAchievementToGooglePlay(achievement);
                syncedCount++;
            }
        }
        
        Debug.Log($"Synced {syncedCount} achievements to Google Play Games");
        OnSyncComplete?.Invoke();
    }
    
    /// <summary>
    /// Gets the Google Play Games achievement ID for a local achievement ID
    /// </summary>
    private string GetGooglePlayAchievementId(string localAchievementId)
    {
        if (_achievementMapping.ContainsKey(localAchievementId))
        {
            return _achievementMapping[localAchievementId];
        }
        return null;
    }
    
    /// <summary>
    /// Adds or updates an achievement mapping
    /// </summary>
    public void SetAchievementMapping(string localId, string googlePlayId)
    {
        _achievementMapping[localId] = googlePlayId;
    }
    
    /// <summary>
    /// Shows the achievements UI
    /// </summary>
    public void ShowAchievementsUI()
    {
        #if UNITY_ANDROID && !UNITY_EDITOR
        // Social.ShowAchievementsUI();
        Debug.Log("Show achievements UI requested");
        #else
        Debug.Log("Achievements UI only available on Android");
        #endif
    }
}
