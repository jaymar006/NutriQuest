using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Landing page UI - the first screen users see after loading.
/// Features game title, logo, and main navigation buttons.
/// </summary>
public class LandingPageUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject landingPanel;
    [SerializeField] private Image gameLogo;
    [SerializeField] private TextMeshProUGUI gameTitle;
    [SerializeField] private TextMeshProUGUI gameSubtitle;
    [SerializeField] private Button playButton;
    [SerializeField] private Button continueButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button profileButton;
    [SerializeField] private GameObject versionText;
    
    [Header("Animation Settings")]
    [SerializeField] private float logoFadeInDuration = 1f;
    [SerializeField] private float titleFadeInDuration = 0.8f;
    [SerializeField] private float buttonFadeInDuration = 0.6f;
    [SerializeField] private float buttonStaggerDelay = 0.2f;
    [SerializeField] private bool logoScaleAnimation = true;
    [SerializeField] private AnimationCurve fadeInCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    
    [Header("Settings")]
    [SerializeField] private bool showContinueButton = true;
    [SerializeField] private bool autoHideAfterDelay = false;
    [SerializeField] private float autoHideDelay = 5f;
    
    private CanvasGroup _canvasGroup;
    private bool _isInitialized = false;
    private bool _hasSaveData = false;
    
    // Events
    public event Action OnPlayButtonClicked;
    public event Action OnContinueButtonClicked;
    public event Action OnSettingsButtonClicked;
    public event Action OnProfileButtonClicked;
    
    private void Awake()
    {
        // Get or create canvas group
        _canvasGroup = GetComponent<CanvasGroup>();
        if (_canvasGroup == null)
        {
            _canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
        
        // Initialize UI state
        InitializeUI();
    }
    
    private void Start()
    {
        CheckSaveData();
        StartCoroutine(AnimateEntry());
        
        // Setup button listeners
        SetupButtons();
        
        // Show version if available
        if (versionText != null)
        {
            TextMeshProUGUI version = versionText.GetComponent<TextMeshProUGUI>();
            if (version != null)
            {
                version.text = $"Version {Constant.GAME_VERSION}";
            }
        }
    }
    
    /// <summary>
    /// Initializes UI elements to hidden state
    /// </summary>
    private void InitializeUI()
    {
        // Hide all elements initially
        if (gameLogo != null)
        {
            SetAlpha(gameLogo, 0f);
        }
        
        if (gameTitle != null)
        {
            SetAlpha(gameTitle, 0f);
        }
        
        if (gameSubtitle != null)
        {
            SetAlpha(gameSubtitle, 0f);
        }
        
        // Hide buttons
        SetButtonAlpha(playButton, 0f);
        SetButtonAlpha(continueButton, 0f);
        SetButtonAlpha(settingsButton, 0f);
        SetButtonAlpha(profileButton, 0f);
        
        // Disable continue button if no save data
        if (continueButton != null)
        {
            continueButton.interactable = false;
        }
    }
    
    /// <summary>
    /// Checks if user has save data
    /// </summary>
    private void CheckSaveData()
    {
        if (DatabaseManager.Instance != null)
        {
            _hasSaveData = DatabaseManager.Instance.HasSaveData();
            
            if (continueButton != null)
            {
                continueButton.interactable = _hasSaveData && showContinueButton;
                
                // Hide continue button if no save data
                if (!_hasSaveData && showContinueButton)
                {
                    continueButton.gameObject.SetActive(false);
                }
            }
        }
    }
    
    /// <summary>
    /// Sets up button click listeners
    /// </summary>
    private void SetupButtons()
    {
        if (playButton != null)
        {
            playButton.onClick.AddListener(OnPlayClicked);
        }
        
        if (continueButton != null)
        {
            continueButton.onClick.AddListener(OnContinueClicked);
        }
        
        if (settingsButton != null)
        {
            settingsButton.onClick.AddListener(OnSettingsClicked);
        }
        
        if (profileButton != null)
        {
            profileButton.onClick.AddListener(OnProfileClicked);
        }
    }
    
    /// <summary>
    /// Animates the entry of all UI elements
    /// </summary>
    private IEnumerator AnimateEntry()
    {
        // Wait a frame
        yield return null;
        
        // Animate logo
        if (gameLogo != null)
        {
            yield return StartCoroutine(AnimateLogo());
        }
        
        // Animate title
        if (gameTitle != null)
        {
            yield return StartCoroutine(FadeInText(gameTitle, titleFadeInDuration));
        }
        
        // Animate subtitle
        if (gameSubtitle != null)
        {
            yield return StartCoroutine(FadeInText(gameSubtitle, titleFadeInDuration));
        }
        
        // Animate buttons with stagger
        yield return new WaitForSeconds(0.3f);
        
        if (playButton != null && playButton.gameObject.activeSelf)
        {
            yield return StartCoroutine(FadeInButton(playButton, buttonFadeInDuration));
        }
        
        if (continueButton != null && continueButton.gameObject.activeSelf && _hasSaveData)
        {
            yield return new WaitForSeconds(buttonStaggerDelay);
            yield return StartCoroutine(FadeInButton(continueButton, buttonFadeInDuration));
        }
        
        if (settingsButton != null && settingsButton.gameObject.activeSelf)
        {
            yield return new WaitForSeconds(buttonStaggerDelay);
            yield return StartCoroutine(FadeInButton(settingsButton, buttonFadeInDuration));
        }
        
        if (profileButton != null && profileButton.gameObject.activeSelf)
        {
            yield return new WaitForSeconds(buttonStaggerDelay);
            yield return StartCoroutine(FadeInButton(profileButton, buttonFadeInDuration));
        }
        
        _isInitialized = true;
        
        // Auto-hide if enabled
        if (autoHideAfterDelay)
        {
            yield return new WaitForSeconds(autoHideDelay);
            OnPlayClicked(); // Auto-navigate to main menu
        }
    }
    
    /// <summary>
    /// Animates logo with scale and fade
    /// </summary>
    private IEnumerator AnimateLogo()
    {
        if (gameLogo == null) yield break;
        
        float elapsed = 0f;
        Vector3 startScale = logoScaleAnimation ? Vector3.zero : Vector3.one;
        Vector3 endScale = Vector3.one;
        
        while (elapsed < logoFadeInDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / logoFadeInDuration;
            float curveValue = fadeInCurve.Evaluate(t);
            
            // Fade
            SetAlpha(gameLogo, curveValue);
            
            // Scale
            if (logoScaleAnimation)
            {
                gameLogo.transform.localScale = Vector3.Lerp(startScale, endScale, curveValue);
            }
            
            yield return null;
        }
        
        SetAlpha(gameLogo, 1f);
        if (logoScaleAnimation)
        {
            gameLogo.transform.localScale = endScale;
        }
    }
    
    /// <summary>
    /// Fades in a text element
    /// </summary>
    private IEnumerator FadeInText(TextMeshProUGUI text, float duration)
    {
        if (text == null) yield break;
        
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float curveValue = fadeInCurve.Evaluate(t);
            
            SetAlpha(text, curveValue);
            yield return null;
        }
        
        SetAlpha(text, 1f);
    }
    
    /// <summary>
    /// Fades in a button
    /// </summary>
    private IEnumerator FadeInButton(Button button, float duration)
    {
        if (button == null) yield break;
        
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float curveValue = fadeInCurve.Evaluate(t);
            
            SetButtonAlpha(button, curveValue);
            yield return null;
        }
        
        SetButtonAlpha(button, 1f);
    }
    
    /// <summary>
    /// Sets alpha for an image
    /// </summary>
    private void SetAlpha(Image image, float alpha)
    {
        if (image != null)
        {
            Color color = image.color;
            color.a = alpha;
            image.color = color;
        }
    }
    
    /// <summary>
    /// Sets alpha for text
    /// </summary>
    private void SetAlpha(TextMeshProUGUI text, float alpha)
    {
        if (text != null)
        {
            Color color = text.color;
            color.a = alpha;
            text.color = color;
        }
    }
    
    /// <summary>
    /// Sets alpha for a button (affects all child images and texts)
    /// </summary>
    private void SetButtonAlpha(Button button, float alpha)
    {
        if (button == null) return;
        
        // Set button image alpha
        Image buttonImage = button.GetComponent<Image>();
        if (buttonImage != null)
        {
            SetAlpha(buttonImage, alpha);
        }
        
        // Set button text alpha
        TextMeshProUGUI buttonText = button.GetComponentInChildren<TextMeshProUGUI>();
        if (buttonText != null)
        {
            SetAlpha(buttonText, alpha);
        }
    }
    
    // Button click handlers
    
    private void OnPlayClicked()
    {
        OnPlayButtonClicked?.Invoke();
        
        // Navigate to main menu or tower selection
        if (NavigationManager.Instance != null)
        {
            NavigationManager.Instance.LoadScene(NavigationManager.SCENE_MAIN_MENU);
        }
    }
    
    private void OnContinueClicked()
    {
        OnContinueButtonClicked?.Invoke();
        
        // Navigate to main menu (or directly to last played tower)
        if (NavigationManager.Instance != null)
        {
            NavigationManager.Instance.LoadScene(NavigationManager.SCENE_MAIN_MENU);
        }
    }
    
    private void OnSettingsClicked()
    {
        OnSettingsButtonClicked?.Invoke();
        
        // Navigate to settings
        if (NavigationManager.Instance != null)
        {
            NavigationManager.Instance.LoadScene(NavigationManager.SCENE_SETTINGS);
        }
    }
    
    private void OnProfileClicked()
    {
        OnProfileButtonClicked?.Invoke();
        
        // Navigate to profile
        if (NavigationManager.Instance != null)
        {
            NavigationManager.Instance.LoadScene(NavigationManager.SCENE_PROFILE);
        }
    }
    
    /// <summary>
    /// Shows the landing page
    /// </summary>
    public void Show()
    {
        if (landingPanel != null)
        {
            landingPanel.SetActive(true);
        }
        
        CheckSaveData();
        StartCoroutine(AnimateEntry());
    }
    
    /// <summary>
    /// Hides the landing page
    /// </summary>
    public void Hide()
    {
        if (landingPanel != null)
        {
            landingPanel.SetActive(false);
        }
    }
}
