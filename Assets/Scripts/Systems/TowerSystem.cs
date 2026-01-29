using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// System for managing tower progression, unlocking, and completion.
/// </summary>
public class TowerSystem : MonoBehaviour
{
    private static TowerSystem _instance;
    public static TowerSystem Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<TowerSystem>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("TowerSystem");
                    _instance = go.AddComponent<TowerSystem>();
                    DontDestroyOnLoad(go);
                }
            }
            return _instance;
        }
    }
    
    private bool _isInitialized = false;
    
    // Events
    public event Action<int> OnTowerUnlocked; // towerId
    public event Action<int> OnTowerCompleted; // towerId
    
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
        Debug.Log("TowerSystem initialized");
    }
    
    /// <summary>
    /// Unlocks a tower for the user
    /// </summary>
    public bool UnlockTower(int towerId)
    {
        Tower tower = DataManager.Instance?.GetTower(towerId);
        if (tower == null)
        {
            Debug.LogError($"Tower {towerId} not found");
            return false;
        }
        
        CloudSaveData saveData = DatabaseManager.Instance?.GetSaveData();
        if (saveData == null) return false;
        
        // Check if tower is already unlocked
        if (tower.isUnlocked) return true;
        
        // Check if user meets requirements
        if (!CanUnlockTower(towerId))
        {
            Debug.LogWarning($"Cannot unlock tower {towerId} - requirements not met");
            return false;
        }
        
        tower.isUnlocked = true;
        OnTowerUnlocked?.Invoke(towerId);
        DatabaseManager.Instance?.MarkDataDirty();
        
        Debug.Log($"Tower {towerId} unlocked");
        return true;
    }
    
    /// <summary>
    /// Checks if a tower can be unlocked
    /// </summary>
    public bool CanUnlockTower(int towerId)
    {
        Tower tower = DataManager.Instance?.GetTower(towerId);
        if (tower == null) return false;
        
        CloudSaveData saveData = DatabaseManager.Instance?.GetSaveData();
        if (saveData?.userData == null) return false;
        
        // Check required score
        if (saveData.userData.highestScore < tower.requiredScore)
        {
            return false;
        }
        
        // Check if previous towers are completed (if any)
        // This would need tower ordering logic based on your game design
        
        return true;
    }
    
    /// <summary>
    /// Marks a tower as completed after successful attempt
    /// </summary>
    public void CompleteTower(int towerId)
    {
        Tower tower = DataManager.Instance?.GetTower(towerId);
        if (tower == null) return;
        
        CloudSaveData saveData = DatabaseManager.Instance?.GetSaveData();
        if (saveData?.userData == null) return;
        
        // Update user's current tower progress
        if (towerId > saveData.userData.currentTower)
        {
            saveData.userData.currentTower = towerId;
        }
        
        // Start cooldown for this tower
        CooldownSystem.Instance?.StartCooldown(towerId, Constant.DEFAULT_COOLDOWN_DURATION);
        
        OnTowerCompleted?.Invoke(towerId);
        DatabaseManager.Instance?.MarkDataDirty();
        
        // Check for achievements
        AchievementSystem.Instance?.CheckTowerCompletionAchievements(towerId);
        
        Debug.Log($"Tower {towerId} completed");
    }
    
    /// <summary>
    /// Gets all unlocked towers
    /// </summary>
    public List<Tower> GetUnlockedTowers()
    {
        List<Tower> allTowers = DataManager.Instance?.GetAllTowers();
        if (allTowers == null) return new List<Tower>();
        
        return allTowers.Where(t => t.isUnlocked).ToList();
    }
    
    /// <summary>
    /// Gets the next available tower for the user
    /// </summary>
    public Tower GetNextAvailableTower()
    {
        CloudSaveData saveData = DatabaseManager.Instance?.GetSaveData();
        if (saveData?.userData == null) return null;
        
        int currentTower = saveData.userData.currentTower;
        List<Tower> allTowers = DataManager.Instance?.GetAllTowers();
        
        if (allTowers == null) return null;
        
        // Find next tower after current
        Tower nextTower = allTowers.FirstOrDefault(t => t.towerId > currentTower);
        
        if (nextTower != null && CanUnlockTower(nextTower.towerId))
        {
            UnlockTower(nextTower.towerId);
        }
        
        return nextTower;
    }
    
    /// <summary>
    /// Gets tower progress for user
    /// </summary>
    public int GetTowerProgress()
    {
        CloudSaveData saveData = DatabaseManager.Instance?.GetSaveData();
        return saveData?.userData?.currentTower ?? 0;
    }
}
