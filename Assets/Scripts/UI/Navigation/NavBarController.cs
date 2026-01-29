using System;
using UnityEngine;

/// <summary>
/// Controller for navigation bar functionality.
/// Manages navigation between main screens.
/// </summary>
public class NavBarController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private NavBar navBar;
    [SerializeField] private GameObject navBarPanel;
    
    [Header("Settings")]
    [SerializeField] private bool showNavBar = true;
    [SerializeField] private string currentScreen = "home";
    
    // Events (exposed for MainMenuUI)
    public event Action OnHomeClicked;
    public event Action OnRecipesClicked;
    public event Action OnSettingsClicked;
    public event Action OnProfileClicked;
    
    private void Awake()
    {
        if (navBar == null)
        {
            navBar = GetComponent<NavBar>();
        }
    }
    
    private void Start()
    {
        InitializeNavBar();
    }
    
    /// <summary>
    /// Initializes the navigation bar
    /// </summary>
    private void InitializeNavBar()
    {
        if (navBarPanel != null)
        {
            navBarPanel.SetActive(showNavBar);
        }
        
        if (navBar != null)
        {
            // Subscribe to nav bar events
            navBar.OnHomeClicked += HandleHomeClicked;
            navBar.OnRecipesClicked += HandleRecipesClicked;
            navBar.OnSettingsClicked += HandleSettingsClicked;
            navBar.OnProfileClicked += HandleProfileClicked;
            
            // Set initial active button
            navBar.SetActiveButton(currentScreen);
        }
    }
    
    /// <summary>
    /// Sets the current screen (updates nav bar highlight)
    /// </summary>
    public void SetCurrentScreen(string screenName)
    {
        currentScreen = screenName;
        if (navBar != null)
        {
            navBar.SetActiveButton(screenName);
        }
    }
    
    // Event handlers
    
    private void HandleHomeClicked()
    {
        OnHomeClicked?.Invoke();
        
        if (NavigationManager.Instance != null)
        {
            NavigationManager.Instance.LoadScene(NavigationManager.SCENE_MAIN_MENU);
        }
    }
    
    private void HandleRecipesClicked()
    {
        OnRecipesClicked?.Invoke();
        
        if (NavigationManager.Instance != null)
        {
            NavigationManager.Instance.LoadScene(NavigationManager.SCENE_RECIPES);
        }
    }
    
    private void HandleSettingsClicked()
    {
        OnSettingsClicked?.Invoke();
        
        if (NavigationManager.Instance != null)
        {
            NavigationManager.Instance.LoadScene(NavigationManager.SCENE_SETTINGS);
        }
    }
    
    private void HandleProfileClicked()
    {
        OnProfileClicked?.Invoke();
        
        if (NavigationManager.Instance != null)
        {
            NavigationManager.Instance.LoadScene(NavigationManager.SCENE_PROFILE);
        }
    }
    
    /// <summary>
    /// Shows the navigation bar
    /// </summary>
    public void Show()
    {
        showNavBar = true;
        if (navBarPanel != null)
        {
            navBarPanel.SetActive(true);
        }
    }
    
    /// <summary>
    /// Hides the navigation bar
    /// </summary>
    public void Hide()
    {
        showNavBar = false;
        if (navBarPanel != null)
        {
            navBarPanel.SetActive(false);
        }
    }
    
    private void OnDestroy()
    {
        if (navBar != null)
        {
            navBar.OnHomeClicked -= HandleHomeClicked;
            navBar.OnRecipesClicked -= HandleRecipesClicked;
            navBar.OnSettingsClicked -= HandleSettingsClicked;
            navBar.OnProfileClicked -= HandleProfileClicked;
        }
    }
}

