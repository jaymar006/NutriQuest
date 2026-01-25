using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Manages synchronization between local and cloud save data.
/// Coordinates CloudSaveService, ConflictResolver, and DatabaseManager.
/// </summary>
public class SyncManager : MonoBehaviour
{
    private static SyncManager _instance;
    public static SyncManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<SyncManager>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("SyncManager");
                    _instance = go.AddComponent<SyncManager>();
                    DontDestroyOnLoad(go);
                }
            }
            return _instance;
        }
    }
    
    private bool _isInitialized = false;
    private bool _isSyncing = false;
    private float _lastSyncTime = 0f;
    private float _syncInterval = 300f; // 5 minutes
    
    // Events
    public event Action OnSyncStarted;
    public event Action<bool> OnSyncCompleted; // success
    public event Action<string> OnSyncError;
    
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
        
        // Subscribe to network status changes
        if (NetworkChecker.Instance != null)
        {
            NetworkChecker.Instance.OnNetworkConnected += OnNetworkConnected;
        }
        
        Debug.Log("SyncManager initialized");
    }
    
    private void Update()
    {
        if (!_isInitialized || _isSyncing) return;
        
        // Auto-sync periodically if network is available
        if (NetworkChecker.Instance != null && NetworkChecker.Instance.IsConnected())
        {
            if (Time.time - _lastSyncTime >= _syncInterval)
            {
                SyncToCloud();
            }
        }
    }
    
    /// <summary>
    /// Called when network connects
    /// </summary>
    private void OnNetworkConnected()
    {
        // Auto-sync when network becomes available
        if (!_isSyncing)
        {
            SyncToCloud();
        }
    }
    
    /// <summary>
    /// Syncs local data to cloud
    /// </summary>
    public void SyncToCloud(Action<bool> onComplete = null)
    {
        if (_isSyncing)
        {
            Debug.LogWarning("Sync already in progress");
            onComplete?.Invoke(false);
            return;
        }
        
        if (!CloudSaveService.Instance.IsCloudSaveAvailable())
        {
            Debug.LogWarning("Cloud save not available");
            onComplete?.Invoke(false);
            return;
        }
        
        StartCoroutine(SyncToCloudCoroutine(onComplete));
    }
    
    private IEnumerator SyncToCloudCoroutine(Action<bool> onComplete)
    {
        _isSyncing = true;
        OnSyncStarted?.Invoke();
        
        CloudSaveData localData = DatabaseManager.Instance?.GetSaveData();
        if (localData == null)
        {
            Debug.LogError("No local data to sync");
            _isSyncing = false;
            OnSyncError?.Invoke("No local data available");
            onComplete?.Invoke(false);
            yield break;
        }
        
        bool syncSuccess = false;
        string errorMessage = "";
        
        // Load cloud data first
        CloudSaveData cloudData = null;
        CloudSaveService.Instance.LoadFromCloud(
            onComplete: (data) => { cloudData = data; },
            onError: (error) => { errorMessage = error; }
        );
        
        // Wait for cloud load to complete
        yield return new WaitForSeconds(1f);
        
        // If cloud data exists, resolve conflicts
        if (cloudData != null)
        {
            ConflictResolver resolver = new ConflictResolver();
            CloudSaveData resolvedData = resolver.ResolveConflict(localData, cloudData, ConflictResolver.ConflictResolutionStrategy.UseNewer);
            
            // Save resolved data locally
            if (DatabaseManager.Instance != null)
            {
                DatabaseManager.Instance.SetSaveData(resolvedData);
            }
            
            localData = resolvedData;
        }
        
        // Upload to cloud
        CloudSaveService.Instance.SaveToCloud(
            localData,
            onComplete: (success) => { syncSuccess = success; },
            onError: (error) => { errorMessage = error; }
        );
        
        // Wait for cloud save to complete
        yield return new WaitForSeconds(1f);
        
        _isSyncing = false;
        _lastSyncTime = Time.time;
        
        if (syncSuccess)
        {
            OnSyncCompleted?.Invoke(true);
            Debug.Log("Cloud sync completed successfully");
        }
        else
        {
            OnSyncError?.Invoke(errorMessage);
            Debug.LogError($"Cloud sync failed: {errorMessage}");
        }
        
        onComplete?.Invoke(syncSuccess);
    }
    
    /// <summary>
    /// Syncs cloud data to local
    /// </summary>
    public void SyncFromCloud(Action<bool> onComplete = null)
    {
        if (_isSyncing)
        {
            Debug.LogWarning("Sync already in progress");
            onComplete?.Invoke(false);
            return;
        }
        
        if (!CloudSaveService.Instance.IsCloudSaveAvailable())
        {
            Debug.LogWarning("Cloud save not available");
            onComplete?.Invoke(false);
            return;
        }
        
        StartCoroutine(SyncFromCloudCoroutine(onComplete));
    }
    
    private IEnumerator SyncFromCloudCoroutine(Action<bool> onComplete)
    {
        _isSyncing = true;
        OnSyncStarted?.Invoke();
        
        CloudSaveData localData = DatabaseManager.Instance?.GetSaveData();
        CloudSaveData cloudData = null;
        bool loadSuccess = false;
        string errorMessage = "";
        
        CloudSaveService.Instance.LoadFromCloud(
            onComplete: (data) => 
            { 
                cloudData = data; 
                loadSuccess = true;
            },
            onError: (error) => 
            { 
                errorMessage = error; 
            }
        );
        
        yield return new WaitForSeconds(1f);
        
        if (loadSuccess && cloudData != null)
        {
            // Resolve conflicts
            ConflictResolver resolver = new ConflictResolver();
            CloudSaveData resolvedData = resolver.ResolveConflict(localData, cloudData, ConflictResolver.ConflictResolutionStrategy.UseNewer);
            
            // Apply resolved data
            if (DatabaseManager.Instance != null && resolvedData != null)
            {
                DatabaseManager.Instance.SetSaveData(resolvedData);
                DatabaseManager.Instance.ForceSave();
            }
            
            OnSyncCompleted?.Invoke(true);
            Debug.Log("Cloud sync from cloud completed");
        }
        else
        {
            OnSyncError?.Invoke(errorMessage);
            Debug.LogError($"Cloud sync failed: {errorMessage}");
        }
        
        _isSyncing = false;
        _lastSyncTime = Time.time;
        onComplete?.Invoke(loadSuccess);
    }
    
    /// <summary>
    /// Forces an immediate sync
    /// </summary>
    public void ForceSync()
    {
        SyncToCloud();
    }
    
    /// <summary>
    /// Checks if sync is in progress
    /// </summary>
    public bool IsSyncing()
    {
        return _isSyncing;
    }
}
