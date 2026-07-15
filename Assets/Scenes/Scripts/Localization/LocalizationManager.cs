using System;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

// =============================================================================
// LocalizationManager — NutriQuest
//
// Persistent singleton that wraps Unity's LocalizationSettings.
// Defaults to Filipino on first launch (primary language for this game).
// Saves the player's choice to PlayerPrefs so it persists across sessions.
//
// SETUP
//   1. Install Unity Localization Package via Package Manager.
//   2. Edit > Project Settings > Localization > Create
//   3. Add Locales: Filipino (fil) and English (en)
//   4. Set Filipino as the Active Locale in LocalizationSettings.
//   5. Add this script's prefab to BootstrapManager.
//   6. Create a String Table called "UI_Strings" for your UI labels.
//
// USAGE
//   // Check active language
//   if (LocalizationManager.Instance.IsFilipino) { ... }
//
//   // Switch language
//   LocalizationManager.Instance.SetFilipino();
//   LocalizationManager.Instance.SetEnglish();
//
//   // React to language changes
//   LocalizationManager.OnLanguageChanged += MyRefreshMethod;
// =============================================================================
public class LocalizationManager : MonoBehaviour
{
    public static LocalizationManager Instance { get; private set; }

    // Subscribe to this to refresh text when language changes.
    // DialogueManager, QuestionGeneratorBeta, CatCompanion all use this.
    public static event Action OnLanguageChanged;

    // Language codes must match the Locale identifiers in LocalizationSettings
    private const string FILIPINO_CODE = "fil";
    private const string ENGLISH_CODE = "en";
    private const string PREFS_KEY = "SelectedLanguage";

    public bool IsFilipino => currentLanguage == FILIPINO_CODE;
    public bool IsEnglish => currentLanguage == ENGLISH_CODE;

    private string currentLanguage = FILIPINO_CODE; // Filipino is the default

    // -------------------------------------------------------------------------
    // Unity lifecycle
    // -------------------------------------------------------------------------

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        // Load saved preference, defaulting to Filipino
        currentLanguage = PlayerPrefs.GetString(PREFS_KEY, FILIPINO_CODE);
        ApplyLanguage(currentLanguage, fireEvent: false);
        Debug.Log("[LocalizationManager] Language loaded: " + currentLanguage);
    }

    // -------------------------------------------------------------------------
    // Public API
    // -------------------------------------------------------------------------

    public void SetFilipino()
    {
        if (currentLanguage == FILIPINO_CODE) return;
        currentLanguage = FILIPINO_CODE;
        SaveAndApply();
    }

    public void SetEnglish()
    {
        if (currentLanguage == ENGLISH_CODE) return;
        currentLanguage = ENGLISH_CODE;
        SaveAndApply();
    }

    public void ToggleLanguage()
    {
        if (IsFilipino) SetEnglish();
        else SetFilipino();
    }

    // Returns a localized string from a String Table.
    // Use this for any UI text you want to localize in code.
    // Example: LocalizationManager.Instance.GetString("UI_Strings", "settings_title")
    public string GetString(string tableName, string key)
    {
        try
        {
            return LocalizationSettings.StringDatabase
                .GetLocalizedString(tableName, key);
        }
        catch (Exception e)
        {
            Debug.LogWarning("[LocalizationManager] Missing key '" + key +
                             "' in table '" + tableName + "': " + e.Message);
            return key; // return the key itself as a fallback
        }
    }

    // -------------------------------------------------------------------------
    // Internal
    // -------------------------------------------------------------------------

    private void SaveAndApply()
    {
        PlayerPrefs.SetString(PREFS_KEY, currentLanguage);
        PlayerPrefs.Save();
        ApplyLanguage(currentLanguage, fireEvent: true);
        Debug.Log("[LocalizationManager] Language changed to: " + currentLanguage);
    }

    private void ApplyLanguage(string code, bool fireEvent)
    {
        // Tell Unity Localization Package to switch locale
        foreach (Locale locale in LocalizationSettings.AvailableLocales.Locales)
        {
            if (locale.Identifier.Code == code)
            {
                LocalizationSettings.SelectedLocale = locale;
                break;
            }
        }

        // Notify all subscribers (CatCompanion, QuestionGeneratorBeta, etc.)
        if (fireEvent)
            OnLanguageChanged?.Invoke();
    }
}