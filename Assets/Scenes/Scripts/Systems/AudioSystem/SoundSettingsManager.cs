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

    // Cached references so slider ticks don't call FindObjectsByType (allocating) per tick.
    private UISFXManager[] _cachedUiSfxManagers = System.Array.Empty<UISFXManager>();
    private Gameplay.CutsceneManager.DialogueManager[] _cachedDialogueManagers = System.Array.Empty<Gameplay.CutsceneManager.DialogueManager>();

    private void Start()
    {
        float savedMusic = PlayerPrefs.GetFloat(MUSIC_KEY, 1f);
        float savedSFX = PlayerPrefs.GetFloat(SFX_KEY, 1f);

        // Cache scene-wide SFX targets once instead of FindObjectsByType per slider tick.
        _cachedUiSfxManagers = FindObjectsByType<UISFXManager>(FindObjectsSortMode.None);
        _cachedDialogueManagers = FindObjectsByType<Gameplay.CutsceneManager.DialogueManager>(FindObjectsSortMode.None);

        sliderMusic.SetValueWithoutNotify(savedMusic);
        sliderSFX.SetValueWithoutNotify(savedSFX);

        ApplyMusicVolume(savedMusic);
        ApplySFXVolume(savedSFX);

        UpdateMusicLabel(savedMusic);
        UpdateSFXLabel(savedSFX);

        sliderMusic.onValueChanged.AddListener(OnMusicSliderChanged);
        sliderSFX.onValueChanged.AddListener(OnSFXSliderChanged);
    }

    private void OnDisable()
    {
        SaveVolumes();
    }

    private void OnDestroy()
    {
        SaveVolumes();
        if (sliderMusic != null) sliderMusic.onValueChanged.RemoveListener(OnMusicSliderChanged);
        if (sliderSFX != null) sliderSFX.onValueChanged.RemoveListener(OnSFXSliderChanged);
    }

    /// <summary>
    /// Single coalesced flush for debounced slider Sets (called on disable/destroy, not per tick).
    /// </summary>
    public void SaveVolumes()
    {
        if (sliderMusic != null)
            PlayerPrefs.SetFloat(MUSIC_KEY, sliderMusic.value);
        if (sliderSFX != null)
            PlayerPrefs.SetFloat(SFX_KEY, sliderSFX.value);
        PlayerPrefs.Save();
    }

    private void OnMusicSliderChanged(float value)
    {
        ApplyMusicVolume(value);
        UpdateMusicLabel(value);
        // Debounced: in-memory Set only, no Save per tick. Flushed in SaveVolumes().
        PlayerPrefs.SetFloat(MUSIC_KEY, value);
    }

    private void OnSFXSliderChanged(float value)
    {
        ApplySFXVolume(value);
        UpdateSFXLabel(value);
        // Debounced: in-memory Set only, no Save per tick. Flushed in SaveVolumes().
        PlayerPrefs.SetFloat(SFX_KEY, value);
    }

    private void ApplyMusicVolume(float value)
    {
        if (UniversalMusicManager.Instance != null)
            UniversalMusicManager.Instance.SetVolume(value, false);
        else
            Debug.LogWarning("[SoundSettings] UniversalMusicManager instance not found!");
    }

    private void ApplySFXVolume(float value)
    {
        // Uses cached references from Start (no FindObjectsByType per tick).
        if (_cachedUiSfxManagers != null && _cachedUiSfxManagers.Length > 0)
        {
            for (int i = 0; i < _cachedUiSfxManagers.Length; i++)
            {
                UISFXManager uiSFX = _cachedUiSfxManagers[i];
                if (uiSFX != null)
                    uiSFX.SetVolume(value);
            }
        }
        else
        {
            Debug.LogWarning("[SoundSettings] No UISFXManager found in scene!");
        }

        if (QuizSFXManager.Instance != null)
            QuizSFXManager.Instance.SetVolume(value);
        else
            Debug.LogWarning("[SoundSettings] QuizSFXManager instance not found!");

        if (_cachedDialogueManagers != null && _cachedDialogueManagers.Length > 0)
        {
            for (int i = 0; i < _cachedDialogueManagers.Length; i++)
            {
                Gameplay.CutsceneManager.DialogueManager dm = _cachedDialogueManagers[i];
                if (dm != null)
                    dm.SetSFXVolume(value);
            }
        }
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