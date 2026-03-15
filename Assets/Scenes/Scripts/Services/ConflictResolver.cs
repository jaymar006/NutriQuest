using System;
using UnityEngine;

/// <summary>
/// Resolves conflicts between local and cloud save data.
/// Uses a strategy pattern to determine which data to keep.
/// </summary>
public class ConflictResolver
{
    public enum ConflictResolutionStrategy
    {
        UseLocal,      // Always use local data
        UseCloud,      // Always use cloud data
        UseNewer,      // Use the data with the most recent timestamp
        Merge          // Merge both datasets (most complex)
    }
    
    private ConflictResolutionStrategy _defaultStrategy = ConflictResolutionStrategy.UseNewer;
    
    /// <summary>
    /// Resolves a conflict between local and cloud save data
    /// </summary>
    public CloudSaveData ResolveConflict(CloudSaveData localData, CloudSaveData cloudData, ConflictResolutionStrategy strategy = ConflictResolutionStrategy.UseNewer)
    {
        if (localData == null && cloudData == null)
        {
            Debug.LogError("Both local and cloud data are null - cannot resolve conflict");
            return null;
        }
        
        if (localData == null)
        {
            Debug.Log("Local data is null - using cloud data");
            return cloudData;
        }
        
        if (cloudData == null)
        {
            Debug.Log("Cloud data is null - using local data");
            return localData;
        }
        
        // Both exist - resolve based on strategy
        switch (strategy)
        {
            case ConflictResolutionStrategy.UseLocal:
                Debug.Log("Conflict resolved: Using local data");
                return localData;
                
            case ConflictResolutionStrategy.UseCloud:
                Debug.Log("Conflict resolved: Using cloud data");
                return cloudData;
                
            case ConflictResolutionStrategy.UseNewer:
                return ResolveByTimestamp(localData, cloudData);
                
            case ConflictResolutionStrategy.Merge:
                return MergeData(localData, cloudData);
                
            default:
                Debug.LogWarning($"Unknown conflict resolution strategy: {strategy}, using default");
                return ResolveByTimestamp(localData, cloudData);
        }
    }
    
    /// <summary>
    /// Resolves conflict by using the data with the most recent save timestamp
    /// </summary>
    private CloudSaveData ResolveByTimestamp(CloudSaveData localData, CloudSaveData cloudData)
    {
        DateTime localTime = localData.lastSaveTime;
        DateTime cloudTime = cloudData.lastSaveTime;
        
        if (localTime > cloudTime)
        {
            Debug.Log($"Conflict resolved by timestamp: Using local data (local: {localTime}, cloud: {cloudTime})");
            return localData;
        }
        else if (cloudTime > localTime)
        {
            Debug.Log($"Conflict resolved by timestamp: Using cloud data (local: {localTime}, cloud: {cloudTime})");
            return cloudData;
        }
        else
        {
            // Same timestamp - use local as default
            Debug.Log("Conflict resolved: Timestamps are equal, using local data");
            return localData;
        }
    }
    
    /// <summary>
    /// Merges local and cloud data intelligently
    /// </summary>
    private CloudSaveData MergeData(CloudSaveData localData, CloudSaveData cloudData)
    {
        Debug.Log("Merging local and cloud data");
        
        // Create merged data starting with local
        CloudSaveData merged = new CloudSaveData(localData.userData);
        merged.userId = localData.userId;
        
        // Merge user data - use highest values
        if (localData.userData != null && cloudData.userData != null)
        {
            merged.userData.highestScore = Mathf.Max(localData.userData.highestScore, cloudData.userData.highestScore);
            merged.userData.currentTower = Mathf.Max(localData.userData.currentTower, cloudData.userData.currentTower);
            merged.userData.staminaPoints = Mathf.Max(localData.userData.staminaPoints, cloudData.userData.staminaPoints);
        }
        
        // Merge towers - combine unique towers
        if (localData.towers != null && cloudData.towers != null)
        {
            merged.towers = new System.Collections.Generic.List<Tower>();
            var towerDict = new System.Collections.Generic.Dictionary<int, Tower>();
            
            // Add local towers
            foreach (var tower in localData.towers)
            {
                towerDict[tower.towerId] = tower;
            }
            
            // Add/update with cloud towers (cloud takes precedence for unlock status)
            foreach (var tower in cloudData.towers)
            {
                if (towerDict.ContainsKey(tower.towerId))
                {
                    // Merge tower data - use most recent unlock status
                    var existing = towerDict[tower.towerId];
                    if (tower.isUnlocked && !existing.isUnlocked)
                    {
                        towerDict[tower.towerId] = tower;
                    }
                }
                else
                {
                    towerDict[tower.towerId] = tower;
                }
            }
            
            merged.towers.AddRange(towerDict.Values);
        }
        
        // Merge achievements - combine all earned achievements
        if (localData.achievements != null && cloudData.achievements != null)
        {
            merged.achievements = new System.Collections.Generic.List<Achievement>();
            var achievementDict = new System.Collections.Generic.Dictionary<int, Achievement>();
            
            // Add local achievements
            foreach (var achievement in localData.achievements)
            {
                if (achievement.IsEarned())
                {
                    achievementDict[achievement.achievementId] = achievement;
                }
            }
            
            // Add cloud achievements
            foreach (var achievement in cloudData.achievements)
            {
                if (achievement.IsEarned())
                {
                    if (!achievementDict.ContainsKey(achievement.achievementId))
                    {
                        achievementDict[achievement.achievementId] = achievement;
                    }
                    else
                    {
                        // Use the one with earlier date (first earned)
                        var existing = achievementDict[achievement.achievementId];
                        if (achievement.dateEarned < existing.dateEarned)
                        {
                            achievementDict[achievement.achievementId] = achievement;
                        }
                    }
                }
            }
            
            merged.achievements.AddRange(achievementDict.Values);
        }
        
        // Merge attempts - keep most recent attempts
        if (localData.recentAttempts != null && cloudData.recentAttempts != null)
        {
            merged.recentAttempts = new System.Collections.Generic.List<Attempt>();
            var allAttempts = new System.Collections.Generic.List<Attempt>();
            allAttempts.AddRange(localData.recentAttempts);
            allAttempts.AddRange(cloudData.recentAttempts);
            
            // Sort by date and keep most recent (limit to reasonable number)
            allAttempts.Sort((a, b) => b.dateAttempted.CompareTo(a.dateAttempted));
            int maxAttempts = 50; // Keep last 50 attempts
            for (int i = 0; i < Mathf.Min(maxAttempts, allAttempts.Count); i++)
            {
                merged.recentAttempts.Add(allAttempts[i]);
            }
        }
        
        // Merge stamina - use the one with more stamina points
        if (localData.stamina != null && cloudData.stamina != null)
        {
            merged.stamina = localData.stamina.currentStamina > cloudData.stamina.currentStamina 
                ? localData.stamina 
                : cloudData.stamina;
        }
        else if (localData.stamina != null)
        {
            merged.stamina = localData.stamina;
        }
        else if (cloudData.stamina != null)
        {
            merged.stamina = cloudData.stamina;
        }
        
        // Merge cooldowns - use most recent lastPlayed times
        if (localData.cooldowns != null && cloudData.cooldowns != null)
        {
            merged.cooldowns = new System.Collections.Generic.List<Cooldown>();
            var cooldownDict = new System.Collections.Generic.Dictionary<int, Cooldown>();
            
            foreach (var cooldown in localData.cooldowns)
            {
                cooldownDict[cooldown.towerId] = cooldown;
            }
            
            foreach (var cooldown in cloudData.cooldowns)
            {
                if (cooldownDict.ContainsKey(cooldown.towerId))
                {
                    // Use the one with more recent lastPlayed
                    var existing = cooldownDict[cooldown.towerId];
                    if (cooldown.lastPlayed > existing.lastPlayed)
                    {
                        cooldownDict[cooldown.towerId] = cooldown;
                    }
                }
                else
                {
                    cooldownDict[cooldown.towerId] = cooldown;
                }
            }
            
            merged.cooldowns.AddRange(cooldownDict.Values);
        }
        
        // Update save time to now
        merged.UpdateSaveTime();
        
        Debug.Log("Data merge completed");
        return merged;
    }
    
    /// <summary>
    /// Sets the default conflict resolution strategy
    /// </summary>
    public void SetDefaultStrategy(ConflictResolutionStrategy strategy)
    {
        _defaultStrategy = strategy;
    }
    
    /// <summary>
    /// Gets the default conflict resolution strategy
    /// </summary>
    public ConflictResolutionStrategy GetDefaultStrategy()
    {
        return _defaultStrategy;
    }
}
