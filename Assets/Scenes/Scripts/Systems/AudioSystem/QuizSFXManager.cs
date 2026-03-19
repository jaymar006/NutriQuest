using UnityEngine;

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

    private void Awake()
    {
        Instance = this;
        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
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
}