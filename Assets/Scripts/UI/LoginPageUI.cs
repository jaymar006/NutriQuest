using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Login page UI - the first screen users see after loading.
/// Handles user authentication (login/register).
/// </summary>
public class LoginPageUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject loginPanel;
    [SerializeField] private Image gameLogo;
    [SerializeField] private TextMeshProUGUI gameTitle;
    [SerializeField] private TMP_InputField usernameInput;
    [SerializeField] private TMP_InputField passwordInput;
    [SerializeField] private Button loginButton;
    [SerializeField] private Button registerButton;
    [SerializeField] private Button guestButton; // Play without account
    [SerializeField] private TextMeshProUGUI errorText;
    [SerializeField] private GameObject loadingIndicator;
    
    [Header("Animation Settings")]
    [SerializeField] private float logoFadeInDuration = 1f;
    [SerializeField] private float titleFadeInDuration = 0.8f;
    [SerializeField] private float formFadeInDuration = 0.6f;
    [SerializeField] private bool logoScaleAnimation = true;
    [SerializeField] private AnimationCurve fadeInCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    
    [Header("Settings")]
    [SerializeField] private bool allowGuestLogin = true;
    [SerializeField] private bool autoFocusUsername = true;
    
    private CanvasGroup _canvasGroup;
    private bool _isInitialized = false;
    private bool _isProcessing = false;
    
    // Events
    public event Action<string, string> OnLoginAttempt; // username, password
    public event Action<string, string> OnRegisterAttempt; // username, password
    public event Action OnGuestLogin;
    public event Action OnLoginSuccess;
    public event Action<string> OnLoginError; // error message
    
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
        StartCoroutine(AnimateEntry());
        SetupButtons();
        
        // Auto-focus username field
        if (autoFocusUsername && usernameInput != null)
        {
            usernameInput.Select();
        }
    }
    
    /// <summary>
    /// Initializes UI elements to hidden state
    /// </summary>
    private void InitializeUI()
    {
        // Hide error text
        if (errorText != null)
        {
            errorText.text = "";
            errorText.gameObject.SetActive(false);
        }
        
        // Hide loading indicator
        if (loadingIndicator != null)
        {
            loadingIndicator.SetActive(false);
        }
        
        // Hide guest button if not allowed
        if (guestButton != null && !allowGuestLogin)
        {
            guestButton.gameObject.SetActive(false);
        }
        
        // Set initial alpha
        if (gameLogo != null)
        {
            SetAlpha(gameLogo, 0f);
        }
        
        if (gameTitle != null)
        {
            SetAlpha(gameTitle, 0f);
        }
        
        SetInputFieldAlpha(usernameInput, 0f);
        SetInputFieldAlpha(passwordInput, 0f);
        SetButtonAlpha(loginButton, 0f);
        SetButtonAlpha(registerButton, 0f);
        if (guestButton != null)
        {
            SetButtonAlpha(guestButton, 0f);
        }
    }
    
    /// <summary>
    /// Sets up button click listeners
    /// </summary>
    private void SetupButtons()
    {
        if (loginButton != null)
        {
            loginButton.onClick.AddListener(OnLoginClicked);
        }
        
        if (registerButton != null)
        {
            registerButton.onClick.AddListener(OnRegisterClicked);
        }
        
        if (guestButton != null)
        {
            guestButton.onClick.AddListener(OnGuestClicked);
        }
        
        // Enter key support for login
        if (passwordInput != null)
        {
            passwordInput.onSubmit.AddListener((value) => {
                if (!string.IsNullOrEmpty(value))
                {
                    OnLoginClicked();
                }
            });
        }
    }
    
    /// <summary>
    /// Animates the entry of all UI elements
    /// </summary>
    private IEnumerator AnimateEntry()
    {
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
        
        // Animate form elements
        yield return new WaitForSeconds(0.3f);
        
        if (usernameInput != null)
        {
            yield return StartCoroutine(FadeInInputField(usernameInput, formFadeInDuration));
        }
        
        if (passwordInput != null)
        {
            yield return new WaitForSeconds(0.1f);
            yield return StartCoroutine(FadeInInputField(passwordInput, formFadeInDuration));
        }
        
        // Animate buttons
        yield return new WaitForSeconds(0.2f);
        
        if (loginButton != null && loginButton.gameObject.activeSelf)
        {
            yield return StartCoroutine(FadeInButton(loginButton, formFadeInDuration));
        }
        
        if (registerButton != null && registerButton.gameObject.activeSelf)
        {
            yield return new WaitForSeconds(0.1f);
            yield return StartCoroutine(FadeInButton(registerButton, formFadeInDuration));
        }
        
        if (guestButton != null && guestButton.gameObject.activeSelf)
        {
            yield return new WaitForSeconds(0.1f);
            yield return StartCoroutine(FadeInButton(guestButton, formFadeInDuration));
        }
        
        _isInitialized = true;
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
            
            SetAlpha(gameLogo, curveValue);
            
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
    /// Fades in an input field
    /// </summary>
    private IEnumerator FadeInInputField(TMP_InputField inputField, float duration)
    {
        if (inputField == null) yield break;
        
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float curveValue = fadeInCurve.Evaluate(t);
            
            SetInputFieldAlpha(inputField, curveValue);
            yield return null;
        }
        
        SetInputFieldAlpha(inputField, 1f);
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
    
    // Button click handlers
    
    private void OnLoginClicked()
    {
        if (_isProcessing) return;
        
        string username = usernameInput != null ? usernameInput.text : "";
        string password = passwordInput != null ? passwordInput.text : "";
        
        // Validate input
        if (string.IsNullOrEmpty(username))
        {
            ShowError("Please enter your username");
            return;
        }
        
        if (string.IsNullOrEmpty(password))
        {
            ShowError("Please enter your password");
            return;
        }
        
        if (username.Length < Constant.MIN_USERNAME_LENGTH)
        {
            ShowError($"Username must be at least {Constant.MIN_USERNAME_LENGTH} characters");
            return;
        }
        
        if (password.Length < Constant.MIN_PASSWORD_LENGTH)
        {
            ShowError($"Password must be at least {Constant.MIN_PASSWORD_LENGTH} characters");
            return;
        }
        
        // Attempt login
        StartCoroutine(ProcessLogin(username, password));
    }
    
    private void OnRegisterClicked()
    {
        if (_isProcessing) return;
        
        string username = usernameInput != null ? usernameInput.text : "";
        string password = passwordInput != null ? passwordInput.text : "";
        
        // Validate input
        if (string.IsNullOrEmpty(username))
        {
            ShowError("Please enter a username");
            return;
        }
        
        if (string.IsNullOrEmpty(password))
        {
            ShowError("Please enter a password");
            return;
        }
        
        if (username.Length < Constant.MIN_USERNAME_LENGTH || username.Length > Constant.MAX_USERNAME_LENGTH)
        {
            ShowError($"Username must be between {Constant.MIN_USERNAME_LENGTH} and {Constant.MAX_USERNAME_LENGTH} characters");
            return;
        }
        
        if (password.Length < Constant.MIN_PASSWORD_LENGTH || password.Length > Constant.MAX_PASSWORD_LENGTH)
        {
            ShowError($"Password must be between {Constant.MIN_PASSWORD_LENGTH} and {Constant.MAX_PASSWORD_LENGTH} characters");
            return;
        }
        
        // Attempt registration
        StartCoroutine(ProcessRegister(username, password));
    }
    
    private void OnGuestClicked()
    {
        if (_isProcessing) return;
        
        OnGuestLogin?.Invoke();
        
        // Create guest user and navigate to main menu
        CreateGuestUser();
        NavigateToMainMenu();
    }
    
    /// <summary>
    /// Processes login attempt
    /// </summary>
    private IEnumerator ProcessLogin(string username, string password)
    {
        _isProcessing = true;
        ShowLoading(true);
        HideError();
        
        OnLoginAttempt?.Invoke(username, password);
        
        // Check if user exists in save data
        CloudSaveData saveData = DatabaseManager.Instance?.GetSaveData();
        
        // For now, simple validation (in real app, this would check against server/database)
        // For local-only: check if username matches existing user
        if (saveData != null && saveData.userData != null)
        {
            if (saveData.userData.username == username)
            {
                // Verify password (in real app, use hashing)
                if (saveData.userData.password == password)
                {
                    // Login successful
                    yield return new WaitForSeconds(0.5f); // Simulate network delay
                    
                    ShowLoading(false);
                    OnLoginSuccess?.Invoke();
                    NavigateToMainMenu();
                    _isProcessing = false;
                    yield break;
                }
            }
        }
        
        // Login failed
        yield return new WaitForSeconds(0.5f);
        ShowLoading(false);
        ShowError("Invalid username or password");
        OnLoginError?.Invoke("Invalid credentials");
        _isProcessing = false;
    }
    
    /// <summary>
    /// Processes registration attempt
    /// </summary>
    private IEnumerator ProcessRegister(string username, string password)
    {
        _isProcessing = true;
        ShowLoading(true);
        HideError();
        
        OnRegisterAttempt?.Invoke(username, password);
        
        // Check if username already exists
        CloudSaveData saveData = DatabaseManager.Instance?.GetSaveData();
        
        if (saveData != null && saveData.userData != null && saveData.userData.username == username)
        {
            ShowLoading(false);
            ShowError("Username already exists");
            _isProcessing = false;
            yield break;
        }
        
        // Create new user
        yield return new WaitForSeconds(0.5f); // Simulate network delay
        
        CreateNewUser(username, password);
        
        ShowLoading(false);
        OnLoginSuccess?.Invoke();
        NavigateToMainMenu();
        _isProcessing = false;
    }
    
    /// <summary>
    /// Creates a new user account
    /// </summary>
    private void CreateNewUser(string username, string password)
    {
        CloudSaveData saveData = DatabaseManager.Instance?.GetSaveData();
        
        if (saveData == null)
        {
            saveData = DatabaseManager.Instance?.CreateNewSaveData();
        }
        
        if (saveData?.userData != null)
        {
            saveData.userData.username = username;
            saveData.userData.password = password; // In production, hash this!
            saveData.userData.registrationDate = System.DateTime.UtcNow;
            
            DatabaseManager.Instance?.MarkDataDirty();
            DatabaseManager.Instance?.ForceSave();
            
            Debug.Log($"New user created: {username}");
        }
    }
    
    /// <summary>
    /// Creates a guest user (no account)
    /// </summary>
    private void CreateGuestUser()
    {
        CloudSaveData saveData = DatabaseManager.Instance?.GetSaveData();
        
        if (saveData == null)
        {
            saveData = DatabaseManager.Instance?.CreateNewSaveData();
        }
        
        if (saveData?.userData != null)
        {
            saveData.userData.username = "Guest";
            saveData.userData.password = "";
            saveData.userData.registrationDate = System.DateTime.UtcNow;
            
            DatabaseManager.Instance?.MarkDataDirty();
            DatabaseManager.Instance?.ForceSave();
            
            Debug.Log("Guest user created");
        }
    }
    
    /// <summary>
    /// Navigates to main menu
    /// </summary>
    private void NavigateToMainMenu()
    {
        if (NavigationManager.Instance != null)
        {
            NavigationManager.Instance.LoadScene(NavigationManager.SCENE_MAIN_MENU);
        }
    }
    
    /// <summary>
    /// Shows error message
    /// </summary>
    private void ShowError(string message)
    {
        if (errorText != null)
        {
            errorText.text = message;
            errorText.gameObject.SetActive(true);
        }
    }
    
    /// <summary>
    /// Hides error message
    /// </summary>
    private void HideError()
    {
        if (errorText != null)
        {
            errorText.text = "";
            errorText.gameObject.SetActive(false);
        }
    }
    
    /// <summary>
    /// Shows/hides loading indicator
    /// </summary>
    private void ShowLoading(bool show)
    {
        if (loadingIndicator != null)
        {
            loadingIndicator.SetActive(show);
        }
        
        // Disable buttons during processing
        if (loginButton != null)
        {
            loginButton.interactable = !show;
        }
        
        if (registerButton != null)
        {
            registerButton.interactable = !show;
        }
        
        if (guestButton != null)
        {
            guestButton.interactable = !show;
        }
    }
    
    // Helper methods for alpha setting
    
    private void SetAlpha(Image image, float alpha)
    {
        if (image != null)
        {
            Color color = image.color;
            color.a = alpha;
            image.color = color;
        }
    }
    
    private void SetAlpha(TextMeshProUGUI text, float alpha)
    {
        if (text != null)
        {
            Color color = text.color;
            color.a = alpha;
            text.color = color;
        }
    }
    
    private void SetInputFieldAlpha(TMP_InputField inputField, float alpha)
    {
        if (inputField == null) return;
        
        Image bgImage = inputField.GetComponent<Image>();
        if (bgImage != null)
        {
            SetAlpha(bgImage, alpha);
        }
        
        TextMeshProUGUI text = inputField.textComponent as TextMeshProUGUI;
        if (text != null)
        {
            SetAlpha(text, alpha);
        }
        
        TextMeshProUGUI placeholder = inputField.placeholder as TextMeshProUGUI;
        if (placeholder != null)
        {
            SetAlpha(placeholder, alpha);
        }
    }
    
    private void SetButtonAlpha(Button button, float alpha)
    {
        if (button == null) return;
        
        Image buttonImage = button.GetComponent<Image>();
        if (buttonImage != null)
        {
            SetAlpha(buttonImage, alpha);
        }
        
        TextMeshProUGUI buttonText = button.GetComponentInChildren<TextMeshProUGUI>();
        if (buttonText != null)
        {
            SetAlpha(buttonText, alpha);
        }
    }
}
