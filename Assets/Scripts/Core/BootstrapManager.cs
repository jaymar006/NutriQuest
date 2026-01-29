using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Bootstrap manager that initializes all core systems on app startup.
/// This should be the first scene loaded in the game.
/// </summary>
public class BootstrapManager : MonoBehaviour
{
    private static BootstrapManager _instance;
    public static BootstrapManager Instance => _instance;
    
    [Header("Initialization Settings")]
    [SerializeField] private float initializationDelay = 0.5f;
    [SerializeField] private bool skipToMainMenu = false; // For testing
    
    [Header("Loading Screen")]
    [SerializeField] private bool showLoadingScreen = true;
    [SerializeField] private float minLoadingTime = 2f; // Minimum time to show loading screen
    
    private bool _isInitialized = false;
    private float _initializationStartTime;
    private int _totalSteps = 9;
    private int _currentStep = 0;
    
    // Events
    public System.Action OnInitializationComplete;
    public System.Action<float, string> OnInitializationProgress; // progress (0-1), status text
    
    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
            return;
        }
    }
    
    private void Start()
    {
        _initializationStartTime = Time.time;
        StartCoroutine(InitializeCoroutine());
    }
    
    private IEnumerator InitializeCoroutine()
    {
        Debug.Log("=== NutriQuest Bootstrap Started ===");
        
        // Show loading screen
        if (showLoadingScreen && LoadingScreenUI.Instance != null)
        {
            LoadingScreenUI.Instance.Show("Initializing NutriQuest...");
        }
        
        // Wait a frame to ensure all systems are ready
        yield return null;
        yield return new WaitForSeconds(initializationDelay);
        
        UpdateProgress(0, "Starting initialization...");
        
        // Step 1: Initialize Database Manager
        _currentStep = 1;
        UpdateProgress(1f / _totalSteps, "Loading game data...");
        Debug.Log("Initializing Database Manager...");
        if (DatabaseManager.Instance != null)
        {
            DatabaseManager.Instance.LoadData();
        }
        yield return null;
        
        // Step 2: Initialize Network Checker
        _currentStep = 2;
        UpdateProgress(2f / _totalSteps, "Checking network connection...");
        Debug.Log("Initializing Network Checker...");
        if (NetworkChecker.Instance != null)
        {
            NetworkChecker.Instance.CheckNetworkStatus();
        }
        yield return null;
        
        // Step 3: Initialize Google Play Games Service (Android)
        _currentStep = 3;
        UpdateProgress(3f / _totalSteps, "Connecting to Google Play Games...");
        Debug.Log("Initializing Google Play Games Service...");
        #if UNITY_ANDROID && !UNITY_EDITOR
        if (GooglePlayGamesService.Instance != null)
        {
            GooglePlayGamesService.Instance.SignInSilently((success) => {
                if (success)
                {
                    Debug.Log("Signed in to Google Play Games");
                }
            });
        }
        #endif
        yield return null;
        
        // Step 4: Initialize Cloud Save Service
        _currentStep = 4;
        UpdateProgress(4f / _totalSteps, "Setting up cloud save...");
        Debug.Log("Initializing Cloud Save Service...");
        if (CloudSaveService.Instance != null)
        {
            CloudSaveService.Instance.CheckCloudSaveAvailability();
        }
        yield return null;
        
        // Step 5: Initialize Achievement Sync Service
        _currentStep = 5;
        UpdateProgress(5f / _totalSteps, "Preparing achievements...");
        Debug.Log("Initializing Achievement Sync Service...");
        if (AchievementSyncService.Instance != null)
        {
            // Service is ready
        }
        yield return null;
        
        // Step 6: Initialize Data Manager
        _currentStep = 6;
        UpdateProgress(6f / _totalSteps, "Loading questions and towers...");
        Debug.Log("Initializing Data Manager...");
        if (DataManager.Instance != null)
        {
            DataManager.Instance.LoadGameData();
        }
        yield return null;
        
        // Step 7: Initialize Game Manager
        _currentStep = 7;
        UpdateProgress(7f / _totalSteps, "Initializing game systems...");
        Debug.Log("Initializing Game Manager...");
        if (GameManager.Instance != null)
        {
            GameManager.Instance.Initialize();
        }
        yield return null;
        
        // Step 8: Initialize Navigation Manager
        _currentStep = 8;
        UpdateProgress(8f / _totalSteps, "Preparing navigation...");
        Debug.Log("Initializing Navigation Manager...");
        if (NavigationManager.Instance != null)
        {
            NavigationManager.Instance.Initialize();
        }
        yield return null;
        
        // Step 9: Initialize Game Systems
        _currentStep = 9;
        UpdateProgress(9f / _totalSteps, "Finalizing setup...");
        Debug.Log("Initializing Game Systems...");
        if (StaminaSystem.Instance != null)
        {
            // System auto-initializes
        }
        if (CooldownSystem.Instance != null)
        {
            // System auto-initializes
        }
        if (TowerSystem.Instance != null)
        {
            // System auto-initializes
        }
        if (QuestionManager.Instance != null)
        {
            // System auto-initializes
        }
        if (AchievementSystem.Instance != null)
        {
            // System auto-initializes
        }
        if (RecipeSystem.Instance != null)
        {
            // System auto-initializes
        }
        if (SyncManager.Instance != null)
        {
            // System auto-initializes
        }
        yield return null;
        
        // Ensure minimum loading time
        float elapsedTime = Time.time - _initializationStartTime;
        if (elapsedTime < minLoadingTime)
        {
            UpdateProgress(1f, "Almost ready...");
            yield return new WaitForSeconds(minLoadingTime - elapsedTime);
        }
        
        UpdateProgress(1f, "Complete!");
        yield return new WaitForSeconds(0.3f);
        
        _isInitialized = true;
        Debug.Log("=== Bootstrap Complete ===");
        
        OnInitializationComplete?.Invoke();
        
        // Hide loading screen
        if (showLoadingScreen && LoadingScreenUI.Instance != null)
        {
            LoadingScreenUI.Instance.Hide();
        }
        
        // Load login page scene (first screen user sees)
        if (!skipToMainMenu)
        {
            yield return new WaitForSeconds(0.5f);
            NavigationManager.Instance?.LoadScene(NavigationManager.SCENE_LOGIN_PAGE);
        }
    }
    
    /// <summary>
    /// Updates the loading progress
    /// </summary>
    private void UpdateProgress(float progress, string statusText)
    {
        if (showLoadingScreen && LoadingScreenUI.Instance != null)
        {
            LoadingScreenUI.Instance.SetProgress(progress, statusText);
        }
        
        OnInitializationProgress?.Invoke(progress, statusText);
    }
    
    public bool IsInitialized()
    {
        return _isInitialized;
    }
    
    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            // Save game when app is paused (Android)
            if (DatabaseManager.Instance != null)
            {
                DatabaseManager.Instance.ForceSave();
            }
        }
    }
    
    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus)
        {
            // Save game when app loses focus (Android)
            if (DatabaseManager.Instance != null)
            {
                DatabaseManager.Instance.ForceSave();
            }
        }
    }
}
