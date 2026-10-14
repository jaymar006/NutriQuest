using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Simple landing page splash screen with game title sprite and tap-to-continue.
/// Shows game logo/title in center and "Tap the screen to continue" at bottom.
/// </summary>
public class LandingPageSplashUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject splashPanel;
    [SerializeField] private Image gameTitleSprite; // Title as sprite image
    [SerializeField] private TextMeshProUGUI tapToContinueText;
    
    [Header("Animation Settings")]
    [SerializeField] private float titleFadeInDuration = 1.5f;
    [SerializeField] private float tapTextFadeInDuration = 1f;
    [SerializeField] private float tapTextDelay = 0.5f; // Delay before showing tap text
    [SerializeField] private bool titleScaleAnimation = true;
    [SerializeField] private AnimationCurve fadeInCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    
    [Header("Tap Text Animation")]
    [SerializeField] private bool animateTapText = true;
    [SerializeField] private float tapTextPulseSpeed = 2f;
    [SerializeField] private float tapTextMinAlpha = 0.3f;
    [SerializeField] private float tapTextMaxAlpha = 1f;
    
    [Header("Settings")]
    [SerializeField] private float minDisplayTime = 2f; // Minimum time before allowing tap
    [SerializeField] private string nextScene = NavigationManager.SCENE_LOGIN_PAGE;
    
    private CanvasGroup _canvasGroup;
    private CanvasGroup _tapTextCanvasGroup;
    private float _lastTapAlpha = -1f;
    private bool _isInitialized = false;
    private bool _canTap = false;
    private float _displayStartTime = 0f;
    private bool _isTransitioning = false;
    
    // Events
    public event Action OnSplashComplete;
    public event Action OnTapDetected;
    
    private void Awake()
    {
        // Get or create canvas group
        _canvasGroup = GetComponent<CanvasGroup>();
        if (_canvasGroup == null)
        {
            _canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        // Cache tap-text CanvasGroup for cheap alpha pulse (no TMP mesh rebuild) //
        if (tapToContinueText != null)
            _tapTextCanvasGroup = tapToContinueText.GetComponent<CanvasGroup>();
        
        InitializeUI();
    }
    
    private void Start()
    {
        _displayStartTime = Time.time;
        StartCoroutine(AnimateEntry());
    }
    
    private void Update()
    {
        // Dirty-check: no work until tappable and not transitioning //
        if (!_canTap || _isTransitioning)
            return;

        // Check for tap/click after minimum display time
        // Check for touch input (mobile)
        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
        {
            HandleTap();
        }
        // Check for mouse click (editor/testing)
        else if (Input.GetMouseButtonDown(0))
        {
            HandleTap();
        }
        
        // Animate tap text if enabled
        if (animateTapText && tapToContinueText != null)
        {
            AnimateTapText();
        }
    }
    
    /// <summary>
    /// Initializes UI elements to hidden state
    /// </summary>
    private void InitializeUI()
    {
        // Hide title initially
        if (gameTitleSprite != null)
        {
            SetAlpha(gameTitleSprite, 0f);
            if (titleScaleAnimation)
            {
                gameTitleSprite.transform.localScale = Vector3.zero;
            }
        }
        
        // Hide tap text initially
        if (tapToContinueText != null)
        {
            if (_tapTextCanvasGroup != null)
                _tapTextCanvasGroup.alpha = 0f;
            else
                SetAlpha(tapToContinueText, 0f);
            _lastTapAlpha = -1f;
        }
    }
    
    /// <summary>
    /// Animates the entry of all UI elements
    /// </summary>
    private IEnumerator AnimateEntry()
    {
        yield return null;
        
        // Animate title sprite
        if (gameTitleSprite != null)
        {
            yield return StartCoroutine(AnimateTitle());
        }
        
        // Wait before showing tap text
        yield return new WaitForSeconds(tapTextDelay);
        
        // Animate tap text
        if (tapToContinueText != null)
        {
            yield return StartCoroutine(FadeInText(tapToContinueText, tapTextFadeInDuration));
        }
        
        // Wait for minimum display time
        float elapsed = Time.time - _displayStartTime;
        if (elapsed < minDisplayTime)
        {
            yield return new WaitForSeconds(minDisplayTime - elapsed);
        }
        
        _canTap = true;
        _isInitialized = true;
    }
    
    /// <summary>
    /// Animates title with scale and fade
    /// </summary>
    private IEnumerator AnimateTitle()
    {
        if (gameTitleSprite == null) yield break;
        
        float elapsed = 0f;
        Vector3 startScale = titleScaleAnimation ? Vector3.zero : Vector3.one;
        Vector3 endScale = Vector3.one;
        
        while (elapsed < titleFadeInDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / titleFadeInDuration;
            float curveValue = fadeInCurve.Evaluate(t);
            
            SetAlpha(gameTitleSprite, curveValue);
            
            if (titleScaleAnimation)
            {
                gameTitleSprite.transform.localScale = Vector3.Lerp(startScale, endScale, curveValue);
            }
            
            yield return null;
        }
        
        SetAlpha(gameTitleSprite, 1f);
        if (titleScaleAnimation)
        {
            gameTitleSprite.transform.localScale = endScale;
        }
    }
    
    /// <summary>
    /// Fades in a text element
    /// </summary>
    private IEnumerator FadeInText(TextMeshProUGUI text, float duration)
    {
        if (text == null) yield break;

        // Prefer the cached CanvasGroup so fade-in and pulse write the same channel //
        CanvasGroup group = (text == tapToContinueText) ? _tapTextCanvasGroup : text.GetComponent<CanvasGroup>();
        
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float curveValue = fadeInCurve.Evaluate(t);
            
            if (group != null)
                group.alpha = curveValue;
            else
                SetAlpha(text, curveValue);
            yield return null;
        }
        
        if (group != null)
            group.alpha = 1f;
        else
            SetAlpha(text, 1f);
        _lastTapAlpha = -1f;
    }
    
    /// <summary>
    /// Animates tap text with pulsing effect via CanvasGroup (no TMP rebuild).
    /// Falls back to color alpha only when no CanvasGroup is present.
    /// </summary>
    private void AnimateTapText()
    {
        if (tapToContinueText == null) return;
        
        float alpha = Mathf.Lerp(tapTextMinAlpha, tapTextMaxAlpha, 
            (Mathf.Sin(Time.time * tapTextPulseSpeed) + 1f) / 2f);

        // Dirty-check: skip Canvas/TMP writes when alpha barely changed //
        if (Mathf.Abs(alpha - _lastTapAlpha) < 0.001f)
            return;
        _lastTapAlpha = alpha;

        if (_tapTextCanvasGroup != null)
            _tapTextCanvasGroup.alpha = alpha;
        else
            SetAlpha(tapToContinueText, alpha);
    }
    
    /// <summary>
    /// Handles tap/click input
    /// </summary>
    private void HandleTap()
    {
        if (!_canTap || _isTransitioning) return;
        
        _isTransitioning = true;
        OnTapDetected?.Invoke();
        
        // Fade out and navigate
        StartCoroutine(FadeOutAndNavigate());
    }
    
    /// <summary>
    /// Fades out and navigates to next scene
    /// </summary>
    private IEnumerator FadeOutAndNavigate()
    {
        float fadeOutDuration = 0.5f;
        float elapsed = 0f;
        
        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fadeOutDuration;
            _canvasGroup.alpha = Mathf.Lerp(1f, 0f, t);
            yield return null;
        }
        
        _canvasGroup.alpha = 0f;
        OnSplashComplete?.Invoke();
        
        // Navigate to next scene
        if (NavigationManager.Instance != null && !string.IsNullOrEmpty(nextScene))
        {
            NavigationManager.Instance.LoadScene(nextScene);
        }
    }
    
    /// <summary>
    /// Sets the alpha of an image
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
    /// Sets the alpha of a text element
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
    /// Manually triggers continue (for testing or programmatic use)
    /// </summary>
    public void Continue()
    {
        HandleTap();
    }
    
    /// <summary>
    /// Checks if splash is ready for tap
    /// </summary>
    public bool CanTap()
    {
        return _canTap && !_isTransitioning;
    }
}
