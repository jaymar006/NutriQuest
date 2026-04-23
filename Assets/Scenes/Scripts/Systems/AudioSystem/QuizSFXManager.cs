using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(AudioSource))]
public class QuizSFXManager : MonoBehaviour
{
    public static QuizSFXManager Instance { get; private set; }

    [Header("SFX Clips")]
    [SerializeField] private AudioClip correctSFX;
    [SerializeField] private AudioClip wrongSFX;

    [Header("Settings")]
    [Range(0f, 1f)]
    [SerializeField] private float volume = 1f;

    private AudioSource audioSource;

    // Shared PlayerPrefs key — same key SoundSettingsManager uses
    private const string SFX_KEY = "SFXVolume";

    private void Awake()
    {
        // FIX: Proper singleton with DontDestroyOnLoad so this persists across scenes.
        //      Previously Instance was overwritten every scene, losing the set volume.
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;

        // FIX: Restore saved SFX volume so it's correct from the first scene onward
        volume = PlayerPrefs.GetFloat(SFX_KEY, volume);
    }

    public void PlayCorrect()
    {
        if (correctSFX == null)
        {
            Debug.LogWarning("[QuizSFXManager] Correct SFX not assigned!");
            return;
        }
        audioSource.PlayOneShot(correctSFX, volume);
    }

    public void PlayWrong()
    {
        if (wrongSFX == null)
        {
            Debug.LogWarning("[QuizSFXManager] Wrong SFX not assigned!");
            return;
        }
        audioSource.PlayOneShot(wrongSFX, volume);
    }

    public void SetVolume(float newVolume)
    {
        volume = Mathf.Clamp01(newVolume);
    }
}