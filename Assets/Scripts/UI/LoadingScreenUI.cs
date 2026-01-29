using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Loading screen UI that displays initialization progress and loading states.
/// Can be used during bootstrap, scene transitions, and data loading.
/// </summary>
public class LoadingScreenUI : MonoBehaviour
{
    private static LoadingScreenUI _instance;
    public static LoadingScreenUI Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<LoadingScreenUI>();
            }
            return _instance;
        }
    }
    
    [Header("UI References")]
    [SerializeField] private GameObject loadingPanel;
    [SerializeField] private Image loadingBarFill;
    [SerializeField] private TextMeshProUGUI loadingText;
    [SerializeField] private TextMeshProUGUI progressText;
    [SerializeField] private GameObject spinnerObject; // Optional spinning animation
    
    [Header("Settings")]
    [SerializeField] private float fadeInDuration = 0.3f;
    [SerializeField] private float fadeOutDuration = 0.3f;
    [SerializeField] private bool showProgressPercentage = true;
    [SerializeField] private bool showSpinner = true;
    
    private CanvasGroup _canvasGroup;
    private bool _isShowing = false;
    private float _currentProgress = 0f;
    private string _currentStatusText = "";
    
    // Events
    public event Action OnLoadingScreenShown;
    public event Action OnLoadingScreenHidden;
    
    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        // Get or create canvas group
        _canvasGroup = GetComponent<CanvasGroup>();
        if (_canvasGroup == null)
        {
            _canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
        
        // Initialize UI state
        if (loadingPanel != null)
        {
            loadingPanel.SetActive(false);
        }
        
        _canvasGroup.alpha = 0f;
        _canvasGroup.blocksRaycasts = false;
    }
    
    /// <summary>
    /// Shows the loading screen with optional status text
    /// </summary>
    public void Show(string statusText = "Loading...")
    {
        if (_isShowing) return;
        
        _isShowing = true;
        _currentStatusText = statusText;
        _currentProgress = 0f;
        
        if (loadingPanel != null)
        {
            loadingPanel.SetActive(true);
        }
        
        UpdateUI();
        StartCoroutine(FadeInCoroutine());
    }
    
    /// <summary>
    /// Hides the loading screen
    /// </summary>
    public void Hide()
    {
        if (!_isShowing) return;
        
        StartCoroutine(FadeOutCoroutine());
    }
    
    /// <summary>
    /// Updates the loading progress (0-1)
    /// </summary>
    public void SetProgress(float progress, string statusText = null)
    {
        _currentProgress = Mathf.Clamp01(progress);
        
        if (!string.IsNullOrEmpty(statusText))
        {
            _currentStatusText = statusText;
        }
        
        UpdateUI();
    }
    
    /// <summary>
    /// Updates the status text without changing progress
    /// </summary>
    public void SetStatusText(string statusText)
    {
        _currentStatusText = statusText;
        UpdateUI();
    }
    
    /// <summary>
    /// Updates all UI elements
    /// </summary>
    private void UpdateUI()
    {
        // Update loading bar
        if (loadingBarFill != null)
        {
            loadingBarFill.fillAmount = _currentProgress;
        }
        
        // Update status text
        if (loadingText != null)
        {
            loadingText.text = _currentStatusText;
        }
        
        // Update progress text
        if (progressText != null)
        {
            if (showProgressPercentage)
            {
                progressText.text = $"{Mathf.RoundToInt(_currentProgress * 100)}%";
            }
            else
            {
                progressText.text = "";
            }
        }
        
        // Show/hide spinner
        if (spinnerObject != null)
        {
            spinnerObject.SetActive(showSpinner);
        }
    }
    
    /// <summary>
    /// Fades in the loading screen
    /// </summary>
    private IEnumerator FadeInCoroutine()
    {
        _canvasGroup.blocksRaycasts = true;
        float elapsed = 0f;
        
        while (elapsed < fadeInDuration)
        {
            elapsed += Time.deltaTime;
            _canvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / fadeInDuration);
            yield return null;
        }
        
        _canvasGroup.alpha = 1f;
        OnLoadingScreenShown?.Invoke();
    }
    
    /// <summary>
    /// Fades out the loading screen
    /// </summary>
    private IEnumerator FadeOutCoroutine()
    {
        float elapsed = 0f;
        
        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.deltaTime;
            _canvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / fadeOutDuration);
            yield return null;
        }
        
        _canvasGroup.alpha = 0f;
        _canvasGroup.blocksRaycasts = false;
        
        if (loadingPanel != null)
        {
            loadingPanel.SetActive(false);
        }
        
        _isShowing = false;
        OnLoadingScreenHidden?.Invoke();
    }
    
    /// <summary>
    /// Shows loading screen during scene transition
    /// </summary>
    public void ShowForSceneTransition(string sceneName)
    {
        Show($"Loading {sceneName}...");
    }
    
    /// <summary>
    /// Checks if loading screen is currently showing
    /// </summary>
    public bool IsShowing()
    {
        return _isShowing;
    }
    
    /// <summary>
    /// Gets current progress (0-1)
    /// </summary>
    public float GetProgress()
    {
        return _currentProgress;
    }
}
