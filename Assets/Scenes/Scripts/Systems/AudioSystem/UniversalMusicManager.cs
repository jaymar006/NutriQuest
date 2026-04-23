using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;

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
        public string[] sceneNames;
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
    private string currentGroupName = null;
    private int currentTrackIndex = -1;
    private Coroutine playRoutine;
    private Coroutine fadeRoutine;

    private const string MUSIC_KEY = "MusicVolume";

    // Unicode sanitization patterns
    private static readonly Regex unicodeRegex = new Regex(@"[^\u0000-\u007F]+");
    private static readonly Regex invalidFileNameChars = new Regex(@"[<>:""/\\|?*]");

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Instance.MergeGroups(musicGroups);
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.loop = false;
        audioSource.playOnAwake = false;

        // Sanitize volume value when loading
        float savedVolume = PlayerPrefs.GetFloat(MUSIC_KEY, maxVolume);
        maxVolume = Mathf.Clamp01(SanitizeFloat(savedVolume));
        audioSource.volume = maxVolume;
    }

    // Sanitize a string by removing unicode characters
    private string SanitizeString(string input)
    {
        if (string.IsNullOrEmpty(input)) return input;

        // Remove unicode characters
        string sanitized = unicodeRegex.Replace(input, "");

        // Remove invalid file name characters
        sanitized = invalidFileNameChars.Replace(sanitized, "");

        // Remove any remaining non-ASCII characters
        sanitized = new string(sanitized.Where(c => c <= 127).ToArray());

        // Trim and clean up
        sanitized = sanitized.Trim();

        // If result is empty, return a default
        if (string.IsNullOrEmpty(sanitized))
            return "Default";

        return sanitized;
    }

    // Sanitize float values (ensure they're valid)
    private float SanitizeFloat(float value)
    {
        if (float.IsNaN(value) || float.IsInfinity(value))
            return 0.5f;
        return value;
    }

    // Sanitize entire MusicGroup
    private void SanitizeMusicGroup(MusicGroup group)
    {
        if (group == null) return;

        // Sanitize group name
        group.groupName = SanitizeString(group.groupName);

        // Sanitize all scene names
        if (group.sceneNames != null)
        {
            for (int i = 0; i < group.sceneNames.Length; i++)
            {
                group.sceneNames[i] = SanitizeString(group.sceneNames[i]);
            }
        }
    }

    // Clean all music groups
    private void SanitizeAllGroups()
    {
        if (musicGroups == null) return;

        foreach (var group in musicGroups)
        {
            SanitizeMusicGroup(group);
        }
    }

    private void MergeGroups(MusicGroup[] incomingGroups)
    {
        if (incomingGroups == null || incomingGroups.Length == 0) return;

        // First, sanitize incoming groups
        foreach (var group in incomingGroups)
        {
            SanitizeMusicGroup(group);
        }

        List<MusicGroup> merged = new List<MusicGroup>(musicGroups ?? new MusicGroup[0]);

        foreach (MusicGroup incoming in incomingGroups)
        {
            if (incoming == null) continue;

            bool replaced = false;
            for (int i = 0; i < merged.Count; i++)
            {
                if (merged[i] != null && merged[i].groupName == incoming.groupName)
                {
                    merged[i] = incoming;
                    replaced = true;
                    Debug.Log($"[MusicManager] Merged (replaced) group: {incoming.groupName}");
                    break;
                }
            }

            if (!replaced)
            {
                merged.Add(incoming);
                Debug.Log($"[MusicManager] Merged (added) group: {incoming.groupName}");
            }
        }

        musicGroups = merged.ToArray();
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
        SanitizeAllGroups();
        StartCoroutine(HandleSceneNextFrame(SceneManager.GetActiveScene().name));
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (musicGroups == null) return;

        foreach (var group in musicGroups)
        {
            if (group == null) continue;

            // Sanitize group name in editor
            group.groupName = SanitizeString(group.groupName);

            if (group.scenes == null) continue;

            group.sceneNames = new string[group.scenes.Length];
            for (int i = 0; i < group.scenes.Length; i++)
            {
                string sceneName = group.scenes[i] != null ? group.scenes[i].name : string.Empty;
                group.sceneNames[i] = SanitizeString(sceneName);
            }
        }
    }
#endif

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        string sanitizedSceneName = SanitizeString(scene.name);
        StartCoroutine(HandleSceneNextFrame(sanitizedSceneName));
    }

    private IEnumerator HandleSceneNextFrame(string sceneName)
    {
        yield return null;
        HandleScene(sceneName);
    }

    private void HandleScene(string sceneName, bool forceSwitch = false)
    {
        MusicGroup newGroup = GetGroup(sceneName);
        string newGroupName = newGroup != null ? newGroup.groupName : null;

        Debug.Log("[MusicManager] Scene: " + sceneName +
            " | Group: " + (newGroupName ?? "NONE") +
            " | Current: " + (currentGroupName ?? "NONE"));

        bool isSameGroup = newGroupName != null && newGroupName == currentGroupName;

        if (!forceSwitch && isSameGroup)
        {
            Debug.Log("[MusicManager] Same group — music continues uninterrupted.");
            return;
        }

        if (playRoutine != null) StopCoroutine(playRoutine);
        if (fadeRoutine != null) StopCoroutine(fadeRoutine);

        fadeRoutine = StartCoroutine(SwitchGroupRoutine(newGroup, newGroupName));
    }

    private MusicGroup GetGroup(string sceneName)
    {
        if (musicGroups == null) return null;

        string sanitizedTarget = SanitizeString(sceneName);

        foreach (var group in musicGroups)
        {
            if (group == null || group.sceneNames == null) continue;

            foreach (var name in group.sceneNames)
            {
                string sanitizedName = SanitizeString(name);
                if (!string.IsNullOrEmpty(sanitizedName) && sanitizedName == sanitizedTarget)
                    return group;
            }
        }

        return null;
    }

    private IEnumerator SwitchGroupRoutine(MusicGroup newGroup, string newGroupName)
    {
        if (newGroup == null)
        {
            yield return StartCoroutine(FadeOut());
            currentGroup = null;
            Debug.Log("[MusicManager] No group for this scene — music paused, group memory kept: " + currentGroupName);
            yield break;
        }

        yield return StartCoroutine(FadeOut());

        currentGroup = newGroup;
        currentGroupName = newGroupName;

        currentTrackIndex = -1;

        if (currentGroup.musicClips == null || currentGroup.musicClips.Length == 0)
        {
            Debug.LogWarning("[MusicManager] No music clips assigned for group: " + currentGroupName);
            yield break;
        }

        playRoutine = StartCoroutine(PlayGroupRoutine());
    }

    private IEnumerator PlayGroupRoutine()
    {
        while (true)
        {
            SelectNextTrack();

            if (currentTrackIndex < 0 || currentTrackIndex >= currentGroup.musicClips.Length)
            {
                yield return new WaitForSeconds(1f);
                continue;
            }

            AudioClip clip = currentGroup.musicClips[currentTrackIndex];

            if (clip == null)
            {
                Debug.LogWarning("[MusicManager] Clip at index " + currentTrackIndex + " is null!");
                yield return new WaitForSeconds(1f);
                continue;
            }

            audioSource.clip = clip;
            audioSource.volume = maxVolume;
            audioSource.Play();

            Debug.Log("[MusicManager] Playing: " + clip.name);

            yield return new WaitForSeconds(clip.length);
        }
    }

    private void SelectNextTrack()
    {
        if (currentGroup.musicClips == null || currentGroup.musicClips.Length == 0)
            return;

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
        if (fadeDuration <= 0f || !audioSource.isPlaying)
        {
            audioSource.Stop();
            yield break;
        }

        float startVolume = audioSource.volume;
        float time = 0f;

        while (time < fadeDuration)
        {
            time += Time.deltaTime;
            audioSource.volume = Mathf.Lerp(startVolume, 0f, time / fadeDuration);
            yield return null;
        }

        audioSource.Stop();
    }

    public void SetVolume(float volume)
    {
        volume = SanitizeFloat(volume);
        maxVolume = Mathf.Clamp01(volume);
        audioSource.volume = maxVolume;
        PlayerPrefs.SetFloat(MUSIC_KEY, maxVolume);
        PlayerPrefs.Save();
    }

    public float GetVolume() => maxVolume;

    // Optional: Method to manually sanitize a scene name
    public static string GetSanitizedSceneName(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName)) return sceneName;

        // Remove unicode characters
        string sanitized = unicodeRegex.Replace(sceneName, "");

        // Remove invalid characters
        sanitized = invalidFileNameChars.Replace(sanitized, "");

        // Keep only ASCII
        sanitized = new string(sanitized.Where(c => c <= 127).ToArray());

        return sanitized.Trim();
    }
}