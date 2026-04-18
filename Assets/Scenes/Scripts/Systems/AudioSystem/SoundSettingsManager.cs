using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SoundSettingsManager : MonoBehaviour
{
    [Header("Music References")]
    [SerializeField] private Slider sliderMusic;
    [SerializeField] private TMP_Text musicTXT;
    [SerializeField] private TMP_Text valueMusicTXT;

    [Header("SFX References")]
    [SerializeField] private Slider sliderSFX;
    [SerializeField] private TMP_Text soundFXTXT;
    [SerializeField] private TMP_Text valueSFXTXT;

    // PlayerPrefs keys
    private const string MUSIC_KEY = "MusicVolume";
    private const string SFX_KEY = "SFXVolume";

    private void Start()
    {
        // Load saved values, default to 1 if none saved
        float savedMusic = PlayerPrefs.GetFloat(MUSIC_KEY, 1f);
        float savedSFX = PlayerPrefs.GetFloat(SFX_KEY, 1f);

        // Init sliders without triggering listeners yet
        sliderMusic.SetValueWithoutNotify(savedMusic);
        sliderSFX.SetValueWithoutNotify(savedSFX);

        // Apply to managers
        ApplyMusicVolume(savedMusic);
        ApplySFXVolume(savedSFX);

        // Update display labels
        UpdateMusicLabel(savedMusic);
        UpdateSFXLabel(savedSFX);

        // Hook up listeners
        sliderMusic.onValueChanged.AddListener(OnMusicSliderChanged);
        sliderSFX.onValueChanged.AddListener(OnSFXSliderChanged);
    }

    private void OnDestroy()
    {
        sliderMusic.onValueChanged.RemoveListener(OnMusicSliderChanged);
        sliderSFX.onValueChanged.RemoveListener(OnSFXSliderChanged);
    }

    // -------------------------------------------------------
    // Slider Callbacks
    // -------------------------------------------------------

    private void OnMusicSliderChanged(float value)
    {
        ApplyMusicVolume(value);
        UpdateMusicLabel(value);
        PlayerPrefs.SetFloat(MUSIC_KEY, value);
        PlayerPrefs.Save();
    }

    private void OnSFXSliderChanged(float value)
    {
        ApplySFXVolume(value);
        UpdateSFXLabel(value);
        PlayerPrefs.SetFloat(SFX_KEY, value);
        PlayerPrefs.Save();
    }

    // -------------------------------------------------------
    // Apply Volume to Managers
    // -------------------------------------------------------

    private void ApplyMusicVolume(float value)
    {
        if (UniversalMusicManager.Instance != null)
            UniversalMusicManager.Instance.SetVolume(value);
        else
            Debug.LogWarning("[SoundSettings] UniversalMusicManager instance not found!");
    }

    private void ApplySFXVolume(float value)
    {
        // UISFXManager - find in scene (not a singleton, so we use FindObjectOfType)
        UISFXManager uiSFX = FindObjectOfType<UISFXManager>();
        if (uiSFX != null)
            uiSFX.SetVolume(value);
        else
            Debug.LogWarning("[SoundSettings] UISFXManager not found in scene!");

        // QuizSFXManager - has a singleton instance
        if (QuizSFXManager.Instance != null)
            QuizSFXManager.Instance.SetVolume(value);
        else
            Debug.LogWarning("[SoundSettings] QuizSFXManager instance not found!");
    }

    // -------------------------------------------------------
    // Label Helpers
    // -------------------------------------------------------

    private void UpdateMusicLabel(float value)
    {
        int percent = Mathf.RoundToInt(value * 100f);
        string display = percent + "%";

        if (musicTXT != null) musicTXT.text = display;
        if (valueMusicTXT != null) valueMusicTXT.text = display;
    }

    private void UpdateSFXLabel(float value)
    {
        int percent = Mathf.RoundToInt(value * 100f);
        string display = percent + "%";

        if (soundFXTXT != null) soundFXTXT.text = display;
        if (valueSFXTXT != null) valueSFXTXT.text = display;
    }

    // -------------------------------------------------------
    // Public helpers (call from buttons if needed)
    // -------------------------------------------------------

    public void ResetToDefaults()
    {
        sliderMusic.value = 1f;
        sliderSFX.value = 1f;
    }
}