using System;
using UnityEngine;

public class LocalizationManager : MonoBehaviour
{
    public static LocalizationManager Instance { get; private set; }

    public static event Action OnLanguageChanged;

    private const string FILIPINO_CODE = "fil";
    private const string ENGLISH_CODE = "en";
    private const string PREFS_KEY = "SelectedLanguage";
    private const string HAS_CHOSEN_KEY = "HasChosenLanguage";

    public bool IsFilipino => currentLanguage == FILIPINO_CODE;
    public bool IsEnglish => currentLanguage == ENGLISH_CODE;
    public bool HasChosenLanguage => PlayerPrefs.GetInt(HAS_CHOSEN_KEY, 0) == 1;

    private string currentLanguage = FILIPINO_CODE;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        currentLanguage = PlayerPrefs.GetString(PREFS_KEY, FILIPINO_CODE);
    }

    public void SetFilipino()
    {
        currentLanguage = FILIPINO_CODE;
        SaveAndNotify();
    }

    public void SetEnglish()
    {
        currentLanguage = ENGLISH_CODE;
        SaveAndNotify();
    }

    private void SaveAndNotify()
    {
        PlayerPrefs.SetString(PREFS_KEY, currentLanguage);
        PlayerPrefs.SetInt(HAS_CHOSEN_KEY, 1);
        PlayerPrefs.Save();
        OnLanguageChanged?.Invoke();
        Debug.Log("[LocalizationManager] Language changed to: " + currentLanguage);
    }
}