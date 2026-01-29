using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Main menu UI with navigation bar and level/tower selection.
/// This is the hub screen after login.
/// </summary>
public class MainMenuUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private NavBarController navBarController;
    [SerializeField] private TextMeshProUGUI welcomeText;
    [SerializeField] private TextMeshProUGUI playerLevelText;
    [SerializeField] private TextMeshProUGUI playerScoreText;
    [SerializeField] private GameObject towerGrid; // Container for tower buttons
    [SerializeField] private GameObject towerButtonPrefab; // Prefab for tower selection buttons
    [SerializeField] private ScrollRect towerScrollView; // Scroll view for towers
    
    [Header("Stamina Display")]
    [SerializeField] private StaminaDisplayUI staminaDisplay;
    
    [Header("Settings")]
    [SerializeField] private int towersPerRow = 2;
    [SerializeField] private float towerButtonSpacing = 20f;
    
    private List<GameObject> _towerButtons = new List<GameObject>();
    
    // Events
    public event Action<int> OnTowerSelected; // towerId
    public event Action OnProfileClicked;
    public event Action OnRecipesClicked;
    public event Action OnSettingsClicked;
    
    private void Start()
    {
        InitializeUI();
        LoadTowers();
        UpdatePlayerInfo();
        
        // Subscribe to nav bar events if available
        if (navBarController != null)
        {
            navBarController.OnHomeClicked += HandleHomeClicked;
            navBarController.OnRecipesClicked += HandleRecipesClicked;
            navBarController.OnSettingsClicked += HandleSettingsClicked;
            navBarController.OnProfileClicked += HandleProfileClicked;
        }
    }
    
    /// <summary>
    /// Initializes the UI
    /// </summary>
    private void InitializeUI()
    {
        // Update welcome message
        if (welcomeText != null)
        {
            CloudSaveData saveData = DatabaseManager.Instance?.GetSaveData();
            string username = saveData?.userData?.username ?? "Player";
            welcomeText.text = $"Welcome, {username}!";
        }
    }
    
    /// <summary>
    /// Updates player information display
    /// </summary>
    private void UpdatePlayerInfo()
    {
        CloudSaveData saveData = DatabaseManager.Instance?.GetSaveData();
        if (saveData?.userData == null) return;
        
        // Update level
        if (playerLevelText != null)
        {
            playerLevelText.text = $"Level {saveData.userData.currentTower}";
        }
        
        // Update score
        if (playerScoreText != null)
        {
            playerScoreText.text = $"Score: {saveData.userData.highestScore}";
        }
        
        // Update stamina display
        if (staminaDisplay != null)
        {
            staminaDisplay.UpdateStamina();
        }
    }
    
    /// <summary>
    /// Loads and displays available towers
    /// </summary>
    private void LoadTowers()
    {
        if (towerGrid == null || towerButtonPrefab == null) return;
        
        // Clear existing towers
        foreach (var button in _towerButtons)
        {
            if (button != null)
            {
                Destroy(button);
            }
        }
        _towerButtons.Clear();
        
        // Get all towers
        List<Tower> towers = DataManager.Instance?.GetAllTowers();
        if (towers == null || towers.Count == 0)
        {
            Debug.LogWarning("No towers found to display");
            return;
        }
        
        // Sort towers by ID
        towers.Sort((a, b) => a.towerId.CompareTo(b.towerId));
        
        // Create tower buttons
        foreach (var tower in towers)
        {
            CreateTowerButton(tower);
        }
    }
    
    /// <summary>
    /// Creates a button for a tower
    /// </summary>
    private void CreateTowerButton(Tower tower)
    {
        GameObject buttonObj = Instantiate(towerButtonPrefab, towerGrid.transform);
        Button button = buttonObj.GetComponent<Button>();
        
        if (button == null)
        {
            Debug.LogError("Tower button prefab must have a Button component");
            Destroy(buttonObj);
            return;
        }
        
        // Set button text
        TextMeshProUGUI buttonText = buttonObj.GetComponentInChildren<TextMeshProUGUI>();
        if (buttonText != null)
        {
            buttonText.text = tower.towerName;
        }
        
        // Set button interactability based on unlock status
        button.interactable = tower.isUnlocked;
        
        // Add click listener
        int towerId = tower.towerId; // Capture for closure
        button.onClick.AddListener(() => OnTowerButtonClicked(towerId));
        
        // Visual feedback for locked towers
        if (!tower.isUnlocked)
        {
            Image buttonImage = buttonObj.GetComponent<Image>();
            if (buttonImage != null)
            {
                buttonImage.color = new Color(0.5f, 0.5f, 0.5f, 1f); // Gray out
            }
        }
        
        _towerButtons.Add(buttonObj);
    }
    
    /// <summary>
    /// Handles tower button click
    /// </summary>
    private void OnTowerButtonClicked(int towerId)
    {
        OnTowerSelected?.Invoke(towerId);
        
        // Navigate to tower selection or gameplay
        if (NavigationManager.Instance != null)
        {
            NavigationManager.Instance.LoadScene(NavigationManager.SCENE_TOWER_SELECTION);
        }
    }
    
    // Nav bar event handlers
    
    private void HandleHomeClicked()
    {
        // Already on home, do nothing or refresh
        UpdatePlayerInfo();
        LoadTowers();
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
    /// Refreshes the main menu display
    /// </summary>
    public void Refresh()
    {
        UpdatePlayerInfo();
        LoadTowers();
    }
    
    private void OnDestroy()
    {
        // Unsubscribe from nav bar events
        if (navBarController != null)
        {
            navBarController.OnHomeClicked -= HandleHomeClicked;
            navBarController.OnRecipesClicked -= HandleRecipesClicked;
            navBarController.OnSettingsClicked -= HandleSettingsClicked;
            navBarController.OnProfileClicked -= HandleProfileClicked;
        }
    }
}
