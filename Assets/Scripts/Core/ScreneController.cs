using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Scene controller for managing scene-specific logic and UI.
/// This is a base class that can be extended for each scene.
/// </summary>
public class SceneController : MonoBehaviour
{
    protected string sceneName;
    protected bool isInitialized = false;
    
    // Events
    public System.Action OnSceneEntered;
    public System.Action OnSceneExited;
    
    protected virtual void Awake()
    {
        sceneName = SceneManager.GetActiveScene().name;
    }
    
    protected virtual void Start()
    {
        Initialize();
    }
    
    /// <summary>
    /// Initializes the scene controller
    /// </summary>
    protected virtual void Initialize()
    {
        if (isInitialized) return;
        
        isInitialized = true;
        OnSceneEntered?.Invoke();
        
        Debug.Log($"SceneController initialized for: {sceneName}");
    }
    
    /// <summary>
    /// Called when scene is entered
    /// </summary>
    protected virtual void OnSceneEnter()
    {
        // Override in derived classes
    }
    
    /// <summary>
    /// Called when scene is exited
    /// </summary>
    protected virtual void OnSceneExit()
    {
        // Override in derived classes
        OnSceneExited?.Invoke();
    }
    
    /// <summary>
    /// Handles Android back button
    /// </summary>
    protected virtual void Update()
    {
        #if UNITY_ANDROID
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            HandleBackButton();
        }
        #endif
    }
    
    /// <summary>
    /// Handles Android back button press
    /// </summary>
    protected virtual void HandleBackButton()
    {
        // Default behavior: go back to main menu
        NavigationManager.Instance?.GoBack();
    }
    
    /// <summary>
    /// Pauses the game (for gameplay scenes)
    /// </summary>
    public virtual void PauseGame()
    {
        Time.timeScale = 0f;
    }
    
    /// <summary>
    /// Resumes the game
    /// </summary>
    public virtual void ResumeGame()
    {
        Time.timeScale = 1f;
    }
    
    protected virtual void OnDestroy()
    {
        OnSceneExit();
    }
    
    protected virtual void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            // Save game when paused
            DatabaseManager.Instance?.ForceSave();
        }
    }
}
