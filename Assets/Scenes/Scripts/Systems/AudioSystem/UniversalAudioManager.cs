using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class UniversalMusicManager : MonoBehaviour
{
    public static UniversalMusicManager Instance;

    public enum PlaybackMode
    {
        Sequential,
        Random
    }

    [System.Serializable]
    public class MusicGroup
    {
        public string groupName;

#if UNITY_EDITOR
        public SceneAsset[] scenes;
#endif
        [HideInInspector] public string[] sceneNames;

        public AudioClip[] musicClips;
        public PlaybackMode playbackMode = PlaybackMode.Sequential;
    }

    [SerializeField] private MusicGroup[] musicGroups;

    [Header("Fade Settings")]
    [SerializeField] private float fadeDuration = 1f;

    [Range(0f, 1f)]
    [SerializeField] private float maxVolume = 1f;

    private AudioSource audioSource;

    private MusicGroup currentGroup;
    private int currentTrackIndex = -1;

    private Coroutine playRoutine;
    private Coroutine fadeRoutine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.loop = false;
        audioSource.playOnAwake = false;
        audioSource.volume = maxVolume;
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Start()
    {
        HandleScene(SceneManager.GetActiveScene().name);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (musicGroups == null)
            return;

        foreach (var group in musicGroups)
        {
            if (group.scenes == null)
                continue;

            group.sceneNames = new string[group.scenes.Length];

            for (int i = 0; i < group.scenes.Length; i++)
            {
                group.sceneNames[i] = group.scenes[i] != null ? group.scenes[i].name : string.Empty;
            }
        }
    }
#endif

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        HandleScene(scene.name);
    }

    private void HandleScene(string sceneName)
    {
        MusicGroup newGroup = GetGroup(sceneName);

        if (newGroup == currentGroup)
            return;

        if (playRoutine != null)
            StopCoroutine(playRoutine);

        if (fadeRoutine != null)
            StopCoroutine(fadeRoutine);

        fadeRoutine = StartCoroutine(SwitchGroupRoutine(newGroup));
    }

    private MusicGroup GetGroup(string sceneName)
    {
        foreach (var group in musicGroups)
        {
            if (group.sceneNames == null)
                continue;

            foreach (var name in group.sceneNames)
            {
                if (!string.IsNullOrEmpty(name) && name == sceneName)
                    return group;
            }
        }

        return null;
    }

    private IEnumerator SwitchGroupRoutine(MusicGroup newGroup)
    {
        yield return StartCoroutine(FadeOut());

        currentGroup = newGroup;
        currentTrackIndex = -1;

        if (currentGroup == null || currentGroup.musicClips == null || currentGroup.musicClips.Length == 0)
            yield break;

        playRoutine = StartCoroutine(PlayGroupRoutine());
    }

    private IEnumerator PlayGroupRoutine()
    {
        while (true)
        {
            SelectNextTrack();

            AudioClip clip = currentGroup.musicClips[currentTrackIndex];

            audioSource.clip = clip;
            audioSource.volume = maxVolume;
            audioSource.Play();

            yield return new WaitForSeconds(clip.length);
        }
    }

    private void SelectNextTrack()
    {
        if (currentGroup.playbackMode == PlaybackMode.Sequential)
        {
            currentTrackIndex++;
            if (currentTrackIndex >= currentGroup.musicClips.Length)
                currentTrackIndex = 0;
        }
        else
        {
            int newIndex;

            do
            {
                newIndex = Random.Range(0, currentGroup.musicClips.Length);
            }
            while (newIndex == currentTrackIndex && currentGroup.musicClips.Length > 1);

            currentTrackIndex = newIndex;
        }
    }

    private IEnumerator FadeOut()
    {
        float startVolume = audioSource.volume;
        float time = 0f;

        if (fadeDuration <= 0f)
        {
            audioSource.Stop();
            yield break;
        }

        while (time < fadeDuration)
        {
            time += Time.deltaTime;
            float t = time / fadeDuration;
            audioSource.volume = Mathf.Lerp(startVolume, 0f, t);
            yield return null;
        }

        audioSource.Stop();
    }

    public void SetVolume(float volume)
    {
        maxVolume = Mathf.Clamp01(volume);
        audioSource.volume = maxVolume;
    }

    public float GetVolume()
    {
        return maxVolume;
    }
}