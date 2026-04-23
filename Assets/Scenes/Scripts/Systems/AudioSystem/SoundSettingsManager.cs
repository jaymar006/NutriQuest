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

    private const string MUSIC_KEY = "MusicVolume";
    private const string SFX_KEY = "SFXVolume";

    private void Start()
    {
        float savedMusic = PlayerPrefs.GetFloat(MUSIC_KEY, 1f);
        float savedSFX = PlayerPrefs.GetFloat(SFX_KEY, 1f);

        sliderMusic.SetValueWithoutNotify(savedMusic);
        sliderSFX.SetValueWithoutNotify(savedSFX);

        ApplyMusicVolume(savedMusic);
        ApplySFXVolume(savedSFX);

        UpdateMusicLabel(savedMusic);
        UpdateSFXLabel(savedSFX);

        sliderMusic.onValueChanged.AddListener(OnMusicSliderChanged);
        sliderSFX.onValueChanged.AddListener(OnSFXSliderChanged);
    }

    private void OnDestroy()
    {
        if (sliderMusic != null) sliderMusic.onValueChanged.RemoveListener(OnMusicSliderChanged);
        if (sliderSFX != null) sliderSFX.onValueChanged.RemoveListener(OnSFXSliderChanged);
    }

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

    private void ApplyMusicVolume(float value)
    {
        if (UniversalMusicManager.Instance != null)
            UniversalMusicManager.Instance.SetVolume(value);
        else
            Debug.LogWarning("[SoundSettings] UniversalMusicManager instance not found!");
    }

    private void ApplySFXVolume(float value)
    {
        // FIX: Use FindObjectsOfType (plural) to catch ALL UISFXManagers in the scene,
        //      not just the first one. A scene may have multiple (one per canvas/panel).
        UISFXManager[] uiSFXManagers = FindObjectsOfType<UISFXManager>();
        if (uiSFXManagers.Length > 0)
        {
            foreach (UISFXManager uiSFX in uiSFXManagers)
                uiSFX.SetVolume(value);
        }
        else
        {
            Debug.LogWarning("[SoundSettings] No UISFXManager found in scene!");
        }

        if (QuizSFXManager.Instance != null)
            QuizSFXManager.Instance.SetVolume(value);
        else
            Debug.LogWarning("[SoundSettings] QuizSFXManager instance not found!");
    }

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

    public void ResetToDefaults()
    {
        sliderMusic.value = 1f;
        sliderSFX.value = 1f;
    }
}