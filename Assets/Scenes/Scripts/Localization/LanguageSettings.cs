using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

// =============================================================================
// LanguageSettings — NutriQuest
//
// Single reusable component for switching language. Supports either or both
// UI styles at once — leave whichever one you don't need unassigned:
//
//   - Dropdown  (languageDropdown)
//   - Buttons   (filipinoButton / englishButton, with active/inactive tinting)
//
// Attach this wherever the player picks a language (Settings modal, first
// launch screen, etc). All variants stay in sync automatically because this
// listens for LocalizationManager.OnLanguageChanged.
//
// FIRST LAUNCH MODE:
// Turn on "Is First Launch Screen" when this lives on the dedicated
// first-launch language screen (not the normal Settings modal). In that mode:
//   - Returning players (LocalizationManager.HasChosenLanguage == true) are
//     skipped straight to mainMenuSceneName on Awake, before they ever see
//     this screen.
//   - Picking a language (dropdown or button) immediately navigates to
//     mainMenuSceneName afterward.
// Leave it OFF for a normal Settings screen — language switches then just
// update state in place with no scene load.
// =============================================================================
public class LanguageSettings : MonoBehaviour
{
    [Header("Dropdown (optional)")]
    [Tooltip("Assign if this screen uses a dropdown. Index 0 = English, 1 = Filipino.")]
    [SerializeField] private TMP_Dropdown languageDropdown;

    [Header("Buttons (optional)")]
    [Tooltip("Assign if this screen uses tap-to-select buttons instead of/alongside the dropdown.")]
    [SerializeField] private Button filipinoButton;
    [SerializeField] private Button englishButton;

    [Header("Button Labels (optional)")]
    [SerializeField] private TMP_Text filipinoLabel;
    [SerializeField] private TMP_Text englishLabel;

    [Header("Active / Inactive Colors (buttons only)")]
    [SerializeField] private Color activeColor = new Color(1f, 0.85f, 0.2f, 1f); // gold
    [SerializeField] private Color inactiveColor = new Color(0.6f, 0.6f, 0.6f, 1f); // grey

    [Header("First Launch Mode")]
    [Tooltip("Enable if this lives on the dedicated first-launch language screen. " +
             "Leave OFF when used inside the normal Settings modal.")]
    [SerializeField] private bool isFirstLaunchScreen = false;
    [Tooltip("Scene to load after a language is picked (first-launch mode only), " +
             "and the scene returning players get skipped straight to.")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";
    [Tooltip("Use SceneTransitionManager (fades/loading screen) if present in the scene, " +
             "instead of loading directly.")]
    [SerializeField] private bool useSceneTransitionManager = true;

    // -------------------------------------------------------------------------
    // Unity lifecycle
    // -------------------------------------------------------------------------
    private void Awake()
    {
        // Returning players skip straight past this screen.
        if (isFirstLaunchScreen &&
            LocalizationManager.Instance != null &&
            LocalizationManager.Instance.HasChosenLanguage)
        {
            LoadMainMenu();
        }
    }

    private void OnEnable()
    {
        LocalizationManager.OnLanguageChanged += RefreshUI;
        RefreshUI();
    }

    private void Start()
    {
        if (languageDropdown != null)
            languageDropdown.onValueChanged.AddListener(OnDropdownChanged);

        if (filipinoButton != null)
            filipinoButton.onClick.AddListener(OnFilipinoClicked);
        if (englishButton != null)
            englishButton.onClick.AddListener(OnEnglishClicked);

        RefreshUI();
    }

    private void OnDisable()
    {
        LocalizationManager.OnLanguageChanged -= RefreshUI;
    }

    private void OnDestroy()
    {
        if (languageDropdown != null)
            languageDropdown.onValueChanged.RemoveListener(OnDropdownChanged);
        if (filipinoButton != null)
            filipinoButton.onClick.RemoveListener(OnFilipinoClicked);
        if (englishButton != null)
            englishButton.onClick.RemoveListener(OnEnglishClicked);
    }

    // -------------------------------------------------------------------------
    // Callbacks
    // -------------------------------------------------------------------------
    private void OnDropdownChanged(int index)
    {
        if (LocalizationManager.Instance == null) return;

        if (index == 1) LocalizationManager.Instance.SetFilipino();
        else LocalizationManager.Instance.SetEnglish();
        // No manual RefreshUI() call needed — OnLanguageChanged handles it.

        if (isFirstLaunchScreen)
            LoadMainMenu();
    }

    private void OnFilipinoClicked()
    {
        if (LocalizationManager.Instance == null)
        {
            Debug.LogWarning("[LanguageSettings] LocalizationManager not found!");
            return;
        }

        LocalizationManager.Instance.SetFilipino();

        if (isFirstLaunchScreen)
            LoadMainMenu();
    }

    private void OnEnglishClicked()
    {
        if (LocalizationManager.Instance == null)
        {
            Debug.LogWarning("[LanguageSettings] LocalizationManager not found!");
            return;
        }

        LocalizationManager.Instance.SetEnglish();

        if (isFirstLaunchScreen)
            LoadMainMenu();
    }

    // -------------------------------------------------------------------------
    // First-launch navigation
    // -------------------------------------------------------------------------
    private void LoadMainMenu()
    {
        if (useSceneTransitionManager && SceneTransitionManager.Instance != null)
        {
            SceneTransitionManager.Instance.NavigateTo(mainMenuSceneName, false);
        }
        else
        {
            SceneManager.LoadScene(mainMenuSceneName);
        }
    }

    // -------------------------------------------------------------------------
    // UI refresh — keeps dropdown + buttons in sync with each other and with
    // LocalizationManager, no matter which one triggered the change.
    // -------------------------------------------------------------------------
    private void RefreshUI()
    {
        if (LocalizationManager.Instance == null) return;

        bool isFilipino = LocalizationManager.Instance.IsFilipino;

        if (languageDropdown != null)
            languageDropdown.SetValueWithoutNotify(isFilipino ? 1 : 0);

        SetButtonActive(filipinoButton, isFilipino);
        SetButtonActive(englishButton, !isFilipino);
    }

    private void SetButtonActive(Button button, bool isActive)
    {
        if (button == null) return;

        Image img = button.GetComponent<Image>();
        if (img != null)
            img.color = isActive ? activeColor : inactiveColor;

        TMP_Text label = button.GetComponentInChildren<TMP_Text>();
        if (label != null)
            label.fontStyle = isActive ? FontStyles.Bold : FontStyles.Normal;
    }
}