using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Manages scene navigation and transitions.
/// </summary>
public class NavigationManager : MonoBehaviour
{
    private static NavigationManager _instance;
    public static NavigationManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<NavigationManager>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("NavigationManager");
                    _instance = go.AddComponent<NavigationManager>();
                    DontDestroyOnLoad(go);
                }
            }
            return _instance;
        }
    }
    
    private bool _isInitialized = false;
    private string _currentSceneName;
    private string _previousSceneName;
    
    // Scene names
    public const string SCENE_BOOTSTRAP = "Bootstrap";
    public const string SCENE_LANDING_PAGE_SPLASH = "LandingPageSplash";
    public const string SCENE_LOGIN_PAGE = "LoginPage";
    public const string SCENE_LANDING_PAGE = "LandingPage";
    public const string SCENE_MAIN_MENU = "MainMenu";
    public const string SCENE_TOWER_SELECTION = "TowerSelection";
    public const string SCENE_GAMEPLAY = "Gameplay";
    public const string SCENE_RESULTS = "Results";
    public const string SCENE_PROFILE = "Profile";
    public const string SCENE_RECIPES = "Recipes";
    public const string SCENE_SETTINGS = "Settings";
    
    // Events
    public event Action<string> OnSceneLoaded;
    public event Action<string, string> OnSceneChanged; // previous, current
    
    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += OnSceneLoadedCallback;
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
        }
    }
    
    public void Initialize()
    {
        if (_isInitialized) return;
        
        _currentSceneName = SceneManager.GetActiveScene().name;
        _isInitialized = true;
        
        Debug.Log($"NavigationManager initialized - Current scene: {_currentSceneName}");
    }
    
    /// <summary>
    /// Loads a scene by name
    /// </summary>
    public void LoadScene(string sceneName, bool additive = false)
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogError("Cannot load scene - scene name is null or empty");
            return;
        }
        
        LoadSceneMode mode = additive ? LoadSceneMode.Additive : LoadSceneMode.Single;
        
        StartCoroutine(LoadSceneCoroutine(sceneName, mode));
    }
    
    private IEnumerator LoadSceneCoroutine(string sceneName, LoadSceneMode mode)
    {
        _previousSceneName = _currentSceneName;
        
        // Show loading screen
        if (LoadingScreenUI.Instance != null)
        {
            LoadingScreenUI.Instance.ShowForSceneTransition(sceneName);
        }
        
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName, mode);
        asyncLoad.allowSceneActivation = false;
        
        // Update loading progress
        while (asyncLoad.progress < 0.9f)
        {
            if (LoadingScreenUI.Instance != null)
            {
                LoadingScreenUI.Instance.SetProgress(asyncLoad.progress, $"Loading {sceneName}...");
            }
            yield return null;
        }
        
        // Activate the scene
        asyncLoad.allowSceneActivation = true;
        
        while (!asyncLoad.isDone)
        {
            if (LoadingScreenUI.Instance != null)
            {
                LoadingScreenUI.Instance.SetProgress(1f, "Almost ready...");
            }
            yield return null;
        }
        
        _currentSceneName = sceneName;
        OnSceneChanged?.Invoke(_previousSceneName, _currentSceneName);
        
        // Hide loading screen after a brief delay
        if (LoadingScreenUI.Instance != null)
        {
            yield return new WaitForSeconds(0.2f);
            LoadingScreenUI.Instance.Hide();
        }
        
        Debug.Log($"Scene loaded: {sceneName}");
    }
    
    /// <summary>
    /// Reloads the current scene
    /// </summary>
    public void ReloadCurrentScene()
    {
        LoadScene(_currentSceneName);
    }
    
    /// <summary>
    /// Goes back to the previous scene
    /// </summary>
    public void GoBack()
    {
        if (!string.IsNullOrEmpty(_previousSceneName))
        {
            LoadScene(_previousSceneName);
        }
        else
        {
            LoadScene(SCENE_MAIN_MENU);
        }
    }
    
    /// <summary>
    /// Gets the current scene name
    /// </summary>
    public string GetCurrentSceneName()
    {
        return _currentSceneName;
    }
    
    /// <summary>
    /// Gets the previous scene name
    /// </summary>
    public string GetPreviousSceneName()
    {
        return _previousSceneName;
    }
    
    /// <summary>
    /// Checks if a specific scene is currently loaded
    /// </summary>
    public bool IsSceneLoaded(string sceneName)
    {
        return _currentSceneName == sceneName;
    }
    
    private void OnSceneLoadedCallback(Scene scene, LoadSceneMode mode)
    {
        _currentSceneName = scene.name;
        OnSceneLoaded?.Invoke(scene.name);
    }
    
    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoadedCallback;
    }
    
    // Convenience methods for common navigation
    
    public void GoToMainMenu()
    {
        LoadScene(SCENE_MAIN_MENU);
    }
    
    public void GoToTowerSelection()
    {
        LoadScene(SCENE_TOWER_SELECTION);
    }
    
    public void GoToGameplay()
    {
        LoadScene(SCENE_GAMEPLAY);
    }
    
    public void GoToResults()
    {
        LoadScene(SCENE_RESULTS);
    }
    
    public void GoToProfile()
    {
        LoadScene(SCENE_PROFILE);
    }
    
    public void GoToRecipes()
    {
        LoadScene(SCENE_RECIPES);
    }
    
    public void GoToSettings()
    {
        LoadScene(SCENE_SETTINGS);
    }
}
