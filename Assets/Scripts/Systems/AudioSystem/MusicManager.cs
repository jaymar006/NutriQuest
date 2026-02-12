using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

#if UNITY_EDITOR
using UnityEditor;
#endif

[RequireComponent(typeof(AudioSource))]
public class UniversalMusicManager : MonoBehaviour
{
    public static UniversalMusicManager Instance;

    [System.Serializable]
    public class MusicGroup
    {
        public string groupName;

#if UNITY_EDITOR
        public SceneAsset[] scenes;
#endif

        [HideInInspector] public string[] sceneNames;
        public AudioClip musicClip;
    }

    public MusicGroup[] musicGroups;

    [Header("Fade Settings")]
    public float fadeDuration = 1.5f;

    [Range(0f, 1f)]
    public float maxVolume = 1f;

    private AudioSource audioSource;
    private MusicGroup currentGroup;
    private Coroutine fadeRoutine;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        audioSource = GetComponent<AudioSource>();
        audioSource.loop = true;
        audioSource.playOnAwake = false;
        audioSource.volume = 0f;
    }

    private void Start()
    {
        CheckScene(SceneManager.GetActiveScene().name);
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        foreach (var group in musicGroups)
        {
            if (group.scenes != null)
            {
                group.sceneNames = new string[group.scenes.Length];
                for (int i = 0; i < group.scenes.Length; i++)
                {
                    if (group.scenes[i] != null)
                        group.sceneNames[i] = group.scenes[i].name;
                }
            }
        }
    }
#endif

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        CheckScene(scene.name);
    }

    private void CheckScene(string sceneName)
    {
        MusicGroup newGroup = GetGroup(sceneName);

        if (newGroup == currentGroup)
            return;

        if (fadeRoutine != null)
            StopCoroutine(fadeRoutine);

        if (newGroup != null)
            fadeRoutine = StartCoroutine(FadeToNewMusic(newGroup));
        else
            fadeRoutine = StartCoroutine(FadeOutMusic());

        currentGroup = newGroup;
    }

    private MusicGroup GetGroup(string sceneName)
    {
        foreach (var group in musicGroups)
        {
            if (group.sceneNames == null)
                continue;

            foreach (string name in group.sceneNames)
            {
                if (name == sceneName)
                    return group;
            }
        }
        return null;
    }

    private IEnumerator FadeToNewMusic(MusicGroup newGroup)
    {
        // Fade out current music if playing
        if (audioSource.isPlaying)
            yield return StartCoroutine(Fade(0f));

        // Switch clip
        audioSource.clip = newGroup.musicClip;
        audioSource.Play();

        // Fade in new music
        yield return StartCoroutine(Fade(maxVolume));
    }

    private IEnumerator FadeOutMusic()
    {
        yield return StartCoroutine(Fade(0f));
        audioSource.Stop();
    }

    private IEnumerator Fade(float targetVolume)
    {
        float startVolume = audioSource.volume;
        float time = 0f;

        while (time < fadeDuration)
        {
            time += Time.deltaTime;
            audioSource.volume = Mathf.Lerp(startVolume, targetVolume, time / fadeDuration);
            yield return null;
        }

        audioSource.volume = targetVolume;
    }
}
