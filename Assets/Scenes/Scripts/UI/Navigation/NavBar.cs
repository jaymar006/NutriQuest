using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Navigation bar UI component.
/// Provides bottom navigation with Home, Recipes, Settings, Profile buttons.
/// </summary>
public class NavBar : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Button homeButton;
    [SerializeField] private Button recipesButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button profileButton;
    
    [Header("Button Icons/Text")]
    [SerializeField] private TextMeshProUGUI homeButtonText;
    [SerializeField] private TextMeshProUGUI recipesButtonText;
    [SerializeField] private TextMeshProUGUI settingsButtonText;
    [SerializeField] private TextMeshProUGUI profileButtonText;
    
    [Header("Settings")]
    [SerializeField] private Color selectedColor = Color.white;
    [SerializeField] private Color unselectedColor = Color.gray;
    
    private string _currentActiveButton = "";
    
    // Events
    public System.Action OnHomeClicked;
    public System.Action OnRecipesClicked;
    public System.Action OnSettingsClicked;
    public System.Action OnProfileClicked;
    
    private void Start()
    {
        SetupButtons();
    }
    
    /// <summary>
    /// Sets up button click listeners
    /// </summary>
    private void SetupButtons()
    {
        if (homeButton != null)
        {
            homeButton.onClick.AddListener(() => {
                SetActiveButton("home");
                OnHomeClicked?.Invoke();
            });
        }
        
        if (recipesButton != null)
        {
            recipesButton.onClick.AddListener(() => {
                SetActiveButton("recipes");
                OnRecipesClicked?.Invoke();
            });
        }
        
        if (settingsButton != null)
        {
            settingsButton.onClick.AddListener(() => {
                SetActiveButton("settings");
                OnSettingsClicked?.Invoke();
            });
        }
        
        if (profileButton != null)
        {
            profileButton.onClick.AddListener(() => {
                SetActiveButton("profile");
                OnProfileClicked?.Invoke();
            });
        }
    }
    
    /// <summary>
    /// Sets the active button (visual feedback)
    /// </summary>
    public void SetActiveButton(string buttonName)
    {
        _currentActiveButton = buttonName;
        UpdateButtonColors();
    }
    
    /// <summary>
    /// Updates button colors based on active state
    /// </summary>
    private void UpdateButtonColors()
    {
        SetButtonColor(homeButton, homeButtonText, _currentActiveButton == "home");
        SetButtonColor(recipesButton, recipesButtonText, _currentActiveButton == "recipes");
        SetButtonColor(settingsButton, settingsButtonText, _currentActiveButton == "settings");
        SetButtonColor(profileButton, profileButtonText, _currentActiveButton == "profile");
    }
    
    /// <summary>
    /// Sets button and text color
    /// </summary>
    private void SetButtonColor(Button button, TextMeshProUGUI text, bool isActive)
    {
        Color color = isActive ? selectedColor : unselectedColor;
        
        if (text != null)
        {
            text.color = color;
        }
        
        // Optionally change button image color
        Image buttonImage = button?.GetComponent<Image>();
        if (buttonImage != null)
        {
            buttonImage.color = isActive ? selectedColor : unselectedColor;
        }
    }
    
    /// <summary>
    /// Gets the currently active button
    /// </summary>
    public string GetActiveButton()
    {
        return _currentActiveButton;
    }
}

