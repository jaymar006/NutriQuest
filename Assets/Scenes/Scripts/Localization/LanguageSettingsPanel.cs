using UnityEngine;
using UnityEngine.UI;
using TMPro;

// =============================================================================
// LanguageSettingsPanel — NutriQuest
//
// Place this in your settings modal alongside SoundSettingsManager.
// Assign two buttons (Filipino / English) and their label colors in the
// Inspector — the active language button stays highlighted automatically.
//
// No other setup needed. LocalizationManager handles the actual switching
// and fires OnLanguageChanged so everything else updates automatically.
// =============================================================================
public class LanguageSettingsPanel : MonoBehaviour
{
    [Header("Language Buttons")]
    [Tooltip("Button that switches to Filipino")]
    [SerializeField] private Button filipinoButton;

    [Tooltip("Button that switches to English")]
    [SerializeField] private Button englishButton;

    [Header("Button Labels (optional)")]
    [SerializeField] private TMP_Text filipinoLabel;
    [SerializeField] private TMP_Text englishLabel;

    [Header("Active / Inactive Colors")]
    [Tooltip("Button image color when this language IS selected")]
    [SerializeField] private Color activeColor   = new Color(1f,   0.85f, 0.2f,  1f); // gold
    [Tooltip("Button image color when this language is NOT selected")]
    [SerializeField] private Color inactiveColor = new Color(0.6f, 0.6f,  0.6f,  1f); // grey

    // -------------------------------------------------------------------------
    // Unity lifecycle
    // -------------------------------------------------------------------------

    private void OnEnable()
    {
        // Refresh highlight whenever the panel opens
        RefreshButtonStates();
    }

    private void Start()
    {
        if (filipinoButton != null)
            filipinoButton.onClick.AddListener(OnFilipinoClicked);

        if (englishButton != null)
            englishButton.onClick.AddListener(OnEnglishClicked);

        RefreshButtonStates();
    }

    private void OnDestroy()
    {
        if (filipinoButton != null)
            filipinoButton.onClick.RemoveListener(OnFilipinoClicked);

        if (englishButton != null)
            englishButton.onClick.RemoveListener(OnEnglishClicked);
    }

    // -------------------------------------------------------------------------
    // Button callbacks
    // -------------------------------------------------------------------------

    private void OnFilipinoClicked()
    {
        if (LocalizationManager.Instance == null)
        {
            Debug.LogWarning("[LanguageSettingsPanel] LocalizationManager not found!");
            return;
        }

        LocalizationManager.Instance.SetFilipino();
        RefreshButtonStates();
    }

    private void OnEnglishClicked()
    {
        if (LocalizationManager.Instance == null)
        {
            Debug.LogWarning("[LanguageSettingsPanel] LocalizationManager not found!");
            return;
        }

        LocalizationManager.Instance.SetEnglish();
        RefreshButtonStates();
    }

    // -------------------------------------------------------------------------
    // UI refresh
    // -------------------------------------------------------------------------

    private void RefreshButtonStates()
    {
        if (LocalizationManager.Instance == null) return;

        bool isFilipino = LocalizationManager.Instance.IsFilipino;

        SetButtonActive(filipinoButton, isFilipino);
        SetButtonActive(englishButton,  !isFilipino);
    }

    private void SetButtonActive(Button button, bool isActive)
    {
        if (button == null) return;

        // Tint the button image
        Image img = button.GetComponent<Image>();
        if (img != null)
            img.color = isActive ? activeColor : inactiveColor;

        // Bold the label when active
        TMP_Text label = button.GetComponentInChildren<TMP_Text>();
        if (label != null)
            label.fontStyle = isActive ? FontStyles.Bold : FontStyles.Normal;
    }
}
