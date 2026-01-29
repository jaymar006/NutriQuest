using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Service for handling cloud save operations.
/// Integrates with Google Play Games Cloud Save for Android.
/// </summary>
public class CloudSaveService : MonoBehaviour
{
    private static CloudSaveService _instance;
    public static CloudSaveService Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<CloudSaveService>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("CloudSaveService");
                    _instance = go.AddComponent<CloudSaveService>();
                    DontDestroyOnLoad(go);
                }
            }
            return _instance;
        }
    }
    
    private bool _isInitialized = false;
    private bool _isSignedIn = false;
    private bool _isCloudSaveEnabled = false;
    
    // Events
    public event Action<bool> OnCloudSaveStatusChanged;
    public event Action<CloudSaveData> OnCloudDataLoaded;
    public event Action<bool> OnCloudDataSaved;
    public event Action<string> OnCloudSaveError;
    
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
        Debug.Log("CloudSaveService initialized");
        
        // Check if Google Play Games is available
        #if UNITY_ANDROID && !UNITY_EDITOR
        CheckCloudSaveAvailability();
        #else
        // In editor, simulate cloud save as disabled
        _isCloudSaveEnabled = false;
        #endif
    }
    
    /// <summary>
    /// Checks if cloud save is available
    /// </summary>
    public void CheckCloudSaveAvailability()
    {
        #if UNITY_ANDROID && !UNITY_EDITOR
        // Check Google Play Games sign-in status
        // This would integrate with GooglePlayGamesService
        if (GooglePlayGamesService.Instance != null && GooglePlayGamesService.Instance.IsSignedIn())
        {
            _isCloudSaveEnabled = true;
            _isSignedIn = true;
        }
        else
        {
            _isCloudSaveEnabled = false;
            _isSignedIn = false;
        }
        #else
        _isCloudSaveEnabled = false;
        #endif
        
        OnCloudSaveStatusChanged?.Invoke(_isCloudSaveEnabled);
    }
    
    /// <summary>
    /// Loads save data from cloud
    /// </summary>
    public void LoadFromCloud(Action<CloudSaveData> onComplete = null, Action<string> onError = null)
    {
        if (!_isCloudSaveEnabled)
        {
            string error = "Cloud save is not available. User may not be signed in to Google Play Games.";
            Debug.LogWarning(error);
            onError?.Invoke(error);
            return;
        }
        
        StartCoroutine(LoadFromCloudCoroutine(onComplete, onError));
    }
    
    private IEnumerator LoadFromCloudCoroutine(Action<CloudSaveData> onComplete, Action<string> onError)
    {
        #if UNITY_ANDROID && !UNITY_EDITOR
        // This would use Google Play Games Cloud Save API
        // For now, we'll simulate the operation
        
        yield return new WaitForSeconds(0.5f); // Simulate network delay
        
        // In real implementation, this would:
        // 1. Request cloud save data from Google Play Games
        // 2. Deserialize the JSON
        // 3. Return the CloudSaveData
        
        Debug.Log("Cloud save load requested (simulated)");
        onError?.Invoke("Cloud save not fully implemented - requires Google Play Games SDK integration");
        #else
        onError?.Invoke("Cloud save only available on Android");
        #endif
        
        yield return null;
    }
    
    /// <summary>
    /// Saves data to cloud
    /// </summary>
    public void SaveToCloud(CloudSaveData saveData, Action<bool> onComplete = null, Action<string> onError = null)
    {
        if (!_isCloudSaveEnabled)
        {
            string error = "Cloud save is not available. User may not be signed in to Google Play Games.";
            Debug.LogWarning(error);
            onError?.Invoke(error);
            onComplete?.Invoke(false);
            return;
        }
        
        if (saveData == null)
        {
            string error = "Cannot save null data to cloud";
            Debug.LogError(error);
            onError?.Invoke(error);
            onComplete?.Invoke(false);
            return;
        }
        
        StartCoroutine(SaveToCloudCoroutine(saveData, onComplete, onError));
    }
    
    private IEnumerator SaveToCloudCoroutine(CloudSaveData saveData, Action<bool> onComplete, Action<string> onError)
    {
        #if UNITY_ANDROID && !UNITY_EDITOR
        // Prepare data for serialization
        saveData.PrepareForSerialization();
        string json = SaveDataSerializer.Serialize(saveData);
        
        if (string.IsNullOrEmpty(json))
        {
            string error = "Failed to serialize save data for cloud";
            Debug.LogError(error);
            onError?.Invoke(error);
            onComplete?.Invoke(false);
            yield break;
        }
        
        yield return new WaitForSeconds(0.5f); // Simulate network delay
        
        // In real implementation, this would:
        // 1. Serialize CloudSaveData to JSON
        // 2. Upload to Google Play Games Cloud Save
        // 3. Handle success/failure
        
        Debug.Log($"Cloud save requested (simulated) - Data size: {json.Length} bytes");
        onError?.Invoke("Cloud save not fully implemented - requires Google Play Games SDK integration");
        #else
        onError?.Invoke("Cloud save only available on Android");
        #endif
        
        yield return null;
    }
    
    /// <summary>
    /// Checks if cloud save is enabled and user is signed in
    /// </summary>
    public bool IsCloudSaveAvailable()
    {
        return _isCloudSaveEnabled && _isSignedIn;
    }
    
    /// <summary>
    /// Sets the cloud save enabled status (called by GooglePlayGamesService)
    /// </summary>
    public void SetCloudSaveEnabled(bool enabled, bool signedIn)
    {
        _isCloudSaveEnabled = enabled;
        _isSignedIn = signedIn;
        OnCloudSaveStatusChanged?.Invoke(_isCloudSaveEnabled);
    }
}
