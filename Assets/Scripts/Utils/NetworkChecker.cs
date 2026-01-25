using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Utility class for checking network connectivity.
/// Optimized for Android with proper network state detection.
/// </summary>
public class NetworkChecker : MonoBehaviour
{
    private static NetworkChecker _instance;
    public static NetworkChecker Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<NetworkChecker>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("NetworkChecker");
                    _instance = go.AddComponent<NetworkChecker>();
                    DontDestroyOnLoad(go);
                }
            }
            return _instance;
        }
    }
    
    private bool _isConnected = false;
    private bool _isChecking = false;
    private float _lastCheckTime = 0f;
    private float _checkInterval = Constant.NETWORK_CHECK_INTERVAL;
    
    // Events
    public event Action<bool> OnNetworkStatusChanged;
    public event Action OnNetworkConnected;
    public event Action OnNetworkDisconnected;
    
    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
            CheckNetworkStatus();
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
        }
    }
    
    private void Update()
    {
        // Periodically check network status
        if (Time.time - _lastCheckTime >= _checkInterval)
        {
            CheckNetworkStatus();
            _lastCheckTime = Time.time;
        }
    }
    
    /// <summary>
    /// Checks the current network connectivity status
    /// </summary>
    public void CheckNetworkStatus()
    {
        if (_isChecking) return;
        
        StartCoroutine(CheckNetworkCoroutine());
    }
    
    private IEnumerator CheckNetworkCoroutine()
    {
        _isChecking = true;
        
        // Check Unity's network reachability
        bool reachable = Application.internetReachability != NetworkReachability.NotReachable;
        
        if (reachable)
        {
            // Perform a quick connectivity test
            using (UnityWebRequest request = UnityWebRequest.Head("https://www.google.com"))
            {
                request.timeout = Constant.NETWORK_TIMEOUT_SECONDS;
                yield return request.SendWebRequest();
                
                bool wasConnected = _isConnected;
                _isConnected = request.result == UnityWebRequest.Result.Success;
                
                if (wasConnected != _isConnected)
                {
                    OnNetworkStatusChanged?.Invoke(_isConnected);
                    
                    if (_isConnected)
                    {
                        OnNetworkConnected?.Invoke();
                        Debug.Log("Network connected");
                    }
                    else
                    {
                        OnNetworkDisconnected?.Invoke();
                        Debug.LogWarning("Network disconnected");
                    }
                }
            }
        }
        else
        {
            bool wasConnected = _isConnected;
            _isConnected = false;
            
            if (wasConnected != _isConnected)
            {
                OnNetworkStatusChanged?.Invoke(false);
                OnNetworkDisconnected?.Invoke();
                Debug.LogWarning("No network reachability");
            }
        }
        
        _isChecking = false;
    }
    
    /// <summary>
    /// Checks if device is currently connected to the internet
    /// </summary>
    public bool IsConnected()
    {
        return _isConnected;
    }
    
    /// <summary>
    /// Checks if device has network reachability (may be connected but no internet)
    /// </summary>
    public bool HasNetworkReachability()
    {
        return Application.internetReachability != NetworkReachability.NotReachable;
    }
    
    /// <summary>
    /// Gets the current network reachability type
    /// </summary>
    public NetworkReachability GetNetworkReachability()
    {
        return Application.internetReachability;
    }
    
    /// <summary>
    /// Gets a human-readable network status string
    /// </summary>
    public string GetNetworkStatusString()
    {
        if (!HasNetworkReachability())
        {
            return "No Network";
        }
        
        if (!IsConnected())
        {
            return "No Internet";
        }
        
        switch (GetNetworkReachability())
        {
            case NetworkReachability.ReachableViaCarrierDataNetwork:
                return "Mobile Data";
            case NetworkReachability.ReachableViaLocalAreaNetwork:
                return "Wi-Fi";
            default:
                return "Connected";
        }
    }
    
    /// <summary>
    /// Sets the network check interval
    /// </summary>
    public void SetCheckInterval(float interval)
    {
        _checkInterval = Mathf.Max(1f, interval); // Minimum 1 second
    }
    
    /// <summary>
    /// Forces an immediate network check
    /// </summary>
    public void ForceCheck(Action<bool> onComplete = null)
    {
        StartCoroutine(ForceCheckCoroutine(onComplete));
    }
    
    private IEnumerator ForceCheckCoroutine(Action<bool> onComplete)
    {
        yield return CheckNetworkCoroutine();
        onComplete?.Invoke(_isConnected);
    }
}
