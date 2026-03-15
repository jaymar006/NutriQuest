using System;
using System.Collections.Generic;
using UnityEngine;

public class DatabaseManager : MonoBehaviour
{
    private static DatabaseManager _instance;
    public static DatabaseManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<DatabaseManager>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("DatabaseManager");
                    _instance = go.AddComponent<DatabaseManager>();
                    DontDestroyOnLoad(go);
                }
            }
            return _instance;
        }
    }
    
    private LocalSaveManager _localSaveManager;
    private bool _isInitialized = false;
    
    // Auto-save settings (optimized for Android battery life)
    [SerializeField] private bool autoSaveEnabled = true;
    [SerializeField] private float autoSaveInterval = 120f; // 2 minutes - better for Android battery
    private float _lastAutoSaveTime;
    private bool _isSaving = false; // Prevent concurrent saves on Android
    
    // Events
    public event Action<CloudSaveData> OnSaveDataLoaded;
    public event Action OnSaveDataSaved;
    public event Action OnSaveDataDeleted;
    
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
        _localSaveManager = new LocalSaveManager();
        _isInitialized = true;
        _lastAutoSaveTime = Time.time;
        
        Debug.Log("DatabaseManager initialized");
    }
    
    private void Update()
    {
        if (!_isInitialized || !autoSaveEnabled || _isSaving) return;
        
        // Auto-save if data is dirty and enough time has passed
        // Android optimization: Only save when not already saving to prevent blocking
        if (_localSaveManager.IsDirty && Time.time - _lastAutoSaveTime >= autoSaveInterval)
        {
            SaveData();
        }
    }
    
    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus && _localSaveManager.IsDirty)
        {
            SaveData();
        }
    }
    
    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus && _localSaveManager.IsDirty)
        {
            SaveData();
        }
    }
    
    private void OnDestroy()
    {
        // Save on destroy if dirty
        if (_localSaveManager != null && _localSaveManager.IsDirty)
        {
            SaveData();
        }
    }
    
    /// <summary>
    /// Loads save data from disk
    /// </summary>
    public bool LoadData()
    {
        if (!_isInitialized)
        {
            Debug.LogError("DatabaseManager not initialized");
            return false;
        }
        
        bool success = _localSaveManager.LoadSaveData();
        
        if (success)
        {
            OnSaveDataLoaded?.Invoke(_localSaveManager.CurrentSaveData);
        }
        
        return success;
    }
    
    /// <summary>
    /// Saves current data to disk (synchronous - use on main thread)
    /// For Android: Consider using SaveDataAsync for better performance
    /// </summary>
    public bool SaveData(bool createBackup = true)
    {
        if (!_isInitialized)
        {
            Debug.LogError("DatabaseManager not initialized");
            return false;
        }
        
        if (_isSaving)
        {
            Debug.LogWarning("Save already in progress, skipping duplicate save");
            return false;
        }
        
        _isSaving = true;
        bool success = _localSaveManager.SaveSaveData(createBackup);
        _isSaving = false;
        
        if (success)
        {
            _lastAutoSaveTime = Time.time;
            OnSaveDataSaved?.Invoke();
        }
        
        return success;
    }
    
    /// <summary>
    /// Saves data asynchronously (better for Android to avoid blocking main thread)
    /// </summary>
    public void SaveDataAsync(bool createBackup = true, System.Action<bool> onComplete = null)
    {
        if (!_isInitialized)
        {
            Debug.LogError("DatabaseManager not initialized");
            onComplete?.Invoke(false);
            return;
        }
        
        if (_isSaving)
        {
            Debug.LogWarning("Save already in progress");
            onComplete?.Invoke(false);
            return;
        }
        
        _isSaving = true;
        
        // Use Unity's main thread dispatcher for file I/O
        // Note: File I/O still happens on main thread, but we can add coroutine support later
        UnityEngine.WaitForEndOfFrame waitFrame = new UnityEngine.WaitForEndOfFrame();
        StartCoroutine(SaveDataCoroutine(createBackup, onComplete));
    }
    
    private System.Collections.IEnumerator SaveDataCoroutine(bool createBackup, System.Action<bool> onComplete)
    {
        yield return new UnityEngine.WaitForEndOfFrame();
        
        bool success = _localSaveManager.SaveSaveData(createBackup);
        _isSaving = false;
        
        if (success)
        {
            _lastAutoSaveTime = Time.time;
            OnSaveDataSaved?.Invoke();
        }
        
        onComplete?.Invoke(success);
    }
    
    /// <summary>
    /// Gets the current save data
    /// </summary>
    public CloudSaveData GetSaveData()
    {
        if (!_isInitialized || !_localSaveManager.HasSaveData)
        {
            return null;
        }
        
        return _localSaveManager.CurrentSaveData;
    }
    
    /// <summary>
    /// Sets the save data (used by SyncManager)
    /// </summary>
    public void SetSaveData(CloudSaveData saveData)
    {
        if (!_isInitialized)
        {
            Debug.LogError("DatabaseManager not initialized");
            return;
        }
        
        if (saveData == null)
        {
            Debug.LogError("Cannot set null save data");
            return;
        }
        
        _localSaveManager.SetSaveData(saveData);
        MarkDataDirty();
    }
    
    /// <summary>
    /// Gets the current user data
    /// </summary>
    public User GetUserData()
    {
        CloudSaveData saveData = GetSaveData();
        return saveData?.userData;
    }
    
    /// <summary>
    /// Marks save data as modified (triggers auto-save)
    /// </summary>
    public void MarkDataDirty()
    {
        if (_isInitialized)
        {
            _localSaveManager.MarkDirty();
        }
    }
    
    /// <summary>
    /// Creates a new save data
    /// </summary>
    public CloudSaveData CreateNewSaveData()
    {
        if (!_isInitialized)
        {
            Debug.LogError("DatabaseManager not initialized");
            return null;
        }
        
        CloudSaveData newData = _localSaveManager.CreateNewSaveData();
        OnSaveDataLoaded?.Invoke(newData);
        return newData;
    }
    
    /// <summary>
    /// Deletes all save data
    /// </summary>
    public bool DeleteAllData()
    {
        if (!_isInitialized)
        {
            Debug.LogError("DatabaseManager not initialized");
            return false;
        }
        
        bool success = _localSaveManager.DeleteSaveData();
        
        if (success)
        {
            OnSaveDataDeleted?.Invoke();
        }
        
        return success;
    }
    
    /// <summary>
    /// Checks if save data exists
    /// </summary>
    public bool HasSaveData()
    {
        return _isInitialized && _localSaveManager.HasSaveData;
    }
    
    /// <summary>
    /// Forces an immediate save (bypasses auto-save timer)
    /// </summary>
    public bool ForceSave()
    {
        return SaveData();
    }
    
    // Convenience methods for accessing specific data
    
    public List<Tower> GetTowers()
    {
        CloudSaveData saveData = GetSaveData();
        return saveData?.towers ?? new List<Tower>();
    }
    
    public List<Achievement> GetAchievements()
    {
        CloudSaveData saveData = GetSaveData();
        return saveData?.achievements ?? new List<Achievement>();
    }
    
    public Stamina GetStamina()
    {
        CloudSaveData saveData = GetSaveData();
        return saveData?.stamina;
    }
    
    public List<Cooldown> GetCooldowns()
    {
        CloudSaveData saveData = GetSaveData();
        return saveData?.cooldowns ?? new List<Cooldown>();
    }
    
    /// <summary>
    /// Gets cooldown for a specific tower and user
    /// </summary>
    public Cooldown GetCooldownForTower(int towerId, string userId)
    {
        CloudSaveData saveData = GetSaveData();
        return saveData?.GetCooldownForTower(towerId, userId);
    }
}
