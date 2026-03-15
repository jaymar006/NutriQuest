using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// System for managing achievements, tracking progress, and unlocking achievements.
/// Integrates with AchievementSyncService for Google Play Games sync.
/// </summary>
public class AchievementSystem : MonoBehaviour
{
    private static AchievementSystem _instance;
    public static AchievementSystem Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<AchievementSystem>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("AchievementSystem");
                    _instance = go.AddComponent<AchievementSystem>();
                    DontDestroyOnLoad(go);
                }
            }
            return _instance;
        }
    }
    
    private bool _isInitialized = false;
    
    // Events
    public event Action<Achievement> OnAchievementUnlocked;
    public event Action<int> OnAchievementProgressUpdated; // achievementId
    
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
        Debug.Log("AchievementSystem initialized");
    }
    
    /// <summary>
    /// Checks and updates achievements based on game events
    /// </summary>
    public void CheckAchievements()
    {
        CloudSaveData saveData = DatabaseManager.Instance?.GetSaveData();
        if (saveData == null) return;
        
        List<Achievement> allAchievements = DataManager.Instance?.GetAllAchievements();
        if (allAchievements == null) return;
        
        // Initialize achievements for user if needed
        if (saveData.achievements == null)
        {
            saveData.achievements = new List<Achievement>();
        }
        
        foreach (var achievementTemplate in allAchievements)
        {
            // Check if user already has this achievement
            Achievement userAchievement = saveData.achievements.FirstOrDefault(a => 
                a.achievementId == achievementTemplate.achievementId && a.userId == saveData.userId);
            
            if (userAchievement == null)
            {
                // Create new achievement instance for user
                userAchievement = new Achievement
                {
                    achievementId = achievementTemplate.achievementId,
                    userId = saveData.userId,
                    achievementName = achievementTemplate.achievementName,
                    condition = achievementTemplate.condition
                };
                saveData.achievements.Add(userAchievement);
            }
            
            // Check if achievement should be unlocked based on condition
            if (!userAchievement.IsEarned() && CheckAchievementCondition(userAchievement, saveData))
            {
                UnlockAchievement(userAchievement);
            }
        }
        
        DatabaseManager.Instance?.MarkDataDirty();
    }
    
    /// <summary>
    /// Checks if an achievement condition is met
    /// </summary>
    private bool CheckAchievementCondition(Achievement achievement, CloudSaveData saveData)
    {
        if (saveData?.userData == null) return false;
        
        // Parse condition string (e.g., "highestScore >= 100", "towersCompleted >= 5")
        string condition = achievement.condition.ToLower();
        
        if (condition.Contains("highestscore"))
        {
            int targetScore = ExtractNumber(condition);
            return saveData.userData.highestScore >= targetScore;
        }
        else if (condition.Contains("towerscompleted") || condition.Contains("currenttower"))
        {
            int targetTowers = ExtractNumber(condition);
            return saveData.userData.currentTower >= targetTowers;
        }
        else if (condition.Contains("perfectscore"))
        {
            // Check recent attempts for perfect scores
            if (saveData.recentAttempts != null)
            {
                return saveData.recentAttempts.Any(a => a.perfectScore);
            }
        }
        else if (condition.Contains("questionsanswered"))
        {
            // This would need to be tracked in user data
            // For now, check attempts
            if (saveData.recentAttempts != null)
            {
                int totalQuestions = saveData.recentAttempts.Sum(a => a.score / Constant.POINTS_PER_CORRECT_ANSWER);
                int target = ExtractNumber(condition);
                return totalQuestions >= target;
            }
        }
        
        return false;
    }
    
    /// <summary>
    /// Extracts number from condition string
    /// </summary>
    private int ExtractNumber(string condition)
    {
        string number = "";
        foreach (char c in condition)
        {
            if (char.IsDigit(c))
            {
                number += c;
            }
        }
        return int.TryParse(number, out int result) ? result : 0;
    }
    
    /// <summary>
    /// Unlocks an achievement
    /// </summary>
    public void UnlockAchievement(Achievement achievement)
    {
        if (achievement == null || achievement.IsEarned()) return;
        
        achievement.EarnAchievement();
        OnAchievementUnlocked?.Invoke(achievement);
        
        // Sync with Google Play Games
        AchievementSyncService.Instance?.UnlockAchievement(achievement);
        
        DatabaseManager.Instance?.MarkDataDirty();
        
        Debug.Log($"Achievement unlocked: {achievement.achievementName}");
    }
    
    /// <summary>
    /// Checks tower completion achievements
    /// </summary>
    public void CheckTowerCompletionAchievements(int towerId)
    {
        CheckAchievements();
    }
    
    /// <summary>
    /// Gets all unlocked achievements for user
    /// </summary>
    public List<Achievement> GetUnlockedAchievements()
    {
        CloudSaveData saveData = DatabaseManager.Instance?.GetSaveData();
        if (saveData?.achievements == null) return new List<Achievement>();
        
        return saveData.achievements.Where(a => a.userId == saveData.userId && a.IsEarned()).ToList();
    }
    
    /// <summary>
    /// Gets achievement progress for user
    /// </summary>
    public int GetAchievementProgress()
    {
        List<Achievement> unlocked = GetUnlockedAchievements();
        List<Achievement> all = DataManager.Instance?.GetAllAchievements();
        
        if (all == null || all.Count == 0) return 0;
        
        return (int)((float)unlocked.Count / all.Count * 100f);
    }
}
