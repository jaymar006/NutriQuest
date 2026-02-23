using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

#if UNITY_EDITOR
using UnityEditor;
#endif

public enum TransitionMode
{
    FadeOnly,
    FadeWithLoadingScene
}

public class SceneTransitionManager : MonoBehaviour
{
    public static SceneTransitionManager Instance { get; private set; }

    [Header("Fade Settings")]
    [SerializeField] private Image fadeImage;
    [SerializeField] private float fadeDuration = 0.4f;

#if UNITY_EDITOR
    [SerializeField] private SceneAsset[] fadeOnlyScenes;
#endif
    [SerializeField] private string[] fadeOnlySceneNames;

    [Header("Loading Scene Settings")]
#if UNITY_EDITOR
    [SerializeField] private SceneAsset[] loadingScenes;
#endif
    [SerializeField] private string[] loadingSceneNames;

#if UNITY_EDITOR
    [SerializeField] private SceneAsset defaultLoadingSceneAsset;
#endif
    [SerializeField] private string defaultLoadingSceneName;

    private CanvasGroup canvasGroup;
    private bool isTransitioning;
    private string pendingTargetScene;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        InitializeFade();
    }

    private void Start()
    {
        StartCoroutine(Fade(0f));
    }

    private void InitializeFade()
    {
        if (fadeImage == null)
        {
            Debug.LogError("Fade Image not assigned.");
            return;
        }

        canvasGroup = fadeImage.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = fadeImage.gameObject.AddComponent<CanvasGroup>();

        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;
    }

    public void NavigateTo(string targetScene)
    {
        if (isTransitioning)
            return;

        StartCoroutine(TransitionRoutine(targetScene));
    }

    private IEnumerator TransitionRoutine(string targetScene)
    {
        isTransitioning = true;

        TransitionMode mode = GetTransitionMode(targetScene);

        yield return Fade(1f);

        if (mode == TransitionMode.FadeOnly)
        {
            yield return SceneManager.LoadSceneAsync(targetScene);
        }
        else
        {
            pendingTargetScene = targetScene;
            yield return SceneManager.LoadSceneAsync(defaultLoadingSceneName);
        }

        yield return Fade(0f);

        isTransitioning = false;
    }

    public void LoadPendingScene()
    {
        if (string.IsNullOrEmpty(pendingTargetScene))
            return;

        StartCoroutine(LoadPendingRoutine());
    }

    private IEnumerator LoadPendingRoutine()
    {
        yield return Fade(1f);

        AsyncOperation operation = SceneManager.LoadSceneAsync(pendingTargetScene);

        while (!operation.isDone)
            yield return null;

        yield return Fade(0f);
    }

    private TransitionMode GetTransitionMode(string sceneName)
    {
        foreach (string name in fadeOnlySceneNames)
        {
            if (name == sceneName)
                return TransitionMode.FadeOnly;
        }

        foreach (string name in loadingSceneNames)
        {
            if (name == sceneName)
                return TransitionMode.FadeWithLoadingScene;
        }

        return TransitionMode.FadeOnly;
    }

    private IEnumerator Fade(float target)
    {
        float start = canvasGroup.alpha;
        float time = 0f;

        while (time < fadeDuration)
        {
            time += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(start, target, time / fadeDuration);
            yield return null;
        }

        canvasGroup.alpha = target;
        canvasGroup.blocksRaycasts = target == 1f;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (fadeOnlyScenes != null)
        {
            fadeOnlySceneNames = new string[fadeOnlyScenes.Length];
            for (int i = 0; i < fadeOnlyScenes.Length; i++)
            {
                if (fadeOnlyScenes[i] != null)
                    fadeOnlySceneNames[i] = fadeOnlyScenes[i].name;
            }
        }

        if (loadingScenes != null)
        {
            loadingSceneNames = new string[loadingScenes.Length];
            for (int i = 0; i < loadingScenes.Length; i++)
            {
                if (loadingScenes[i] != null)
                    loadingSceneNames[i] = loadingScenes[i].name;
            }
        }

        if (defaultLoadingSceneAsset != null)
            defaultLoadingSceneName = defaultLoadingSceneAsset.name;
    }
#endif
}