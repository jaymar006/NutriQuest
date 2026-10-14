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
    private Dictionary<int, GameObject> _towerButtonPool = new Dictionary<int, GameObject>();
    private Dictionary<GameObject, Button> _buttonCache = new Dictionary<GameObject, Button>();
    private Dictionary<GameObject, Image> _imageCache = new Dictionary<GameObject, Image>();
    private Dictionary<GameObject, TextMeshProUGUI> _textCache = new Dictionary<GameObject, TextMeshProUGUI>();
    private HashSet<int> _seenTowerIds = new HashSet<int>();
    
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
        
        // Get all towers
        List<Tower> towers = DataManager.Instance?.GetAllTowers();
        if (towers == null || towers.Count == 0)
        {
            Debug.LogWarning("No towers found to display");
            return;
        }
        
        // Sort towers by ID
        towers.Sort((a, b) => a.towerId.CompareTo(b.towerId));

        // Rebuild active list in tower order; reuse pooled buttons where possible.
        _towerButtons.Clear();
        _seenTowerIds.Clear();

        for (int i = 0; i < towers.Count; i++)
        {
            Tower tower = towers[i];
            _seenTowerIds.Add(tower.towerId);

            GameObject buttonObj;
            if (!_towerButtonPool.TryGetValue(tower.towerId, out buttonObj) || buttonObj == null)
            {
                buttonObj = CreateTowerButton(tower);
                if (buttonObj == null) continue;
            }
            else
            {
                buttonObj.SetActive(true);
                UpdateTowerButton(buttonObj, tower);
            }

            _towerButtons.Add(buttonObj);
        }

        // Park extras instead of Destroy.
        foreach (var kvp in _towerButtonPool)
        {
            if (!_seenTowerIds.Contains(kvp.Key) && kvp.Value != null)
            {
                kvp.Value.SetActive(false);
            }
        }
    }
    
    /// <summary>
    /// Creates a button for a tower (only for towers not already pooled)
    /// </summary>
    private GameObject CreateTowerButton(Tower tower)
    {
        GameObject buttonObj = Instantiate(towerButtonPrefab, towerGrid.transform);
        Button button = buttonObj.GetComponent<Button>();
        
        if (button == null)
        {
            Debug.LogError("Tower button prefab must have a Button component");
            Destroy(buttonObj);
            return null;
        }

        // Cache components once at creation time.
        _buttonCache[buttonObj] = button;
        _imageCache[buttonObj] = buttonObj.GetComponent<Image>();
        _textCache[buttonObj] = buttonObj.GetComponentInChildren<TextMeshProUGUI>(true);

        _towerButtonPool[tower.towerId] = buttonObj;

        UpdateTowerButton(buttonObj, tower);

        return buttonObj;
    }

    /// <summary>
    /// Reuses a pooled button: refreshes text/locked state without re-instantiating.
    /// Uses cached Button/Image/TMP_Text components.
    /// </summary>
    private void UpdateTowerButton(GameObject buttonObj, Tower tower)
    {
        Button button;
        if (!_buttonCache.TryGetValue(buttonObj, out button) || button == null)
        {
            button = buttonObj.GetComponent<Button>();
            if (button == null) return;
            _buttonCache[buttonObj] = button;
        }

        TextMeshProUGUI buttonText;
        if (!_textCache.TryGetValue(buttonObj, out buttonText) || buttonText == null)
        {
            buttonText = buttonObj.GetComponentInChildren<TextMeshProUGUI>(true);
            _textCache[buttonObj] = buttonText;
        }
        if (buttonText != null)
        {
            buttonText.text = tower.towerName;
        }

        // Set button interactability based on unlock status
        button.interactable = tower.isUnlocked;

        // Refresh click listener for the current towerId
        button.onClick.RemoveAllListeners();
        int towerId = tower.towerId; // Capture for closure
        button.onClick.AddListener(() => OnTowerButtonClicked(towerId));

        // Visual feedback for locked towers (reset to white when unlocked for reuse)
        Image buttonImage;
        if (!_imageCache.TryGetValue(buttonObj, out buttonImage) || buttonImage == null)
        {
            buttonImage = buttonObj.GetComponent<Image>();
            _imageCache[buttonObj] = buttonImage;
        }
        if (buttonImage != null)
        {
            buttonImage.color = tower.isUnlocked
                ? Color.white
                : new Color(0.5f, 0.5f, 0.5f, 1f); // Gray out
        }
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
