using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class SceneTransitionManager : MonoBehaviour
{
    public static SceneTransitionManager Instance { get; private set; }

    [Header("Fade Settings")]
    [SerializeField] private Image fadeImage;
    [SerializeField] private float fadeDuration = 0.4f;

    [Header("Loading Scene")]
#if UNITY_EDITOR
    [SerializeField] private SceneAsset defaultLoadingSceneAsset;
#endif
    [SerializeField] private string defaultLoadingSceneName;

#if UNITY_EDITOR
    [SerializeField] private SceneAsset[] fadeOnlyScenes;
#endif
    [SerializeField] private string[] fadeOnlySceneNames;

    private CanvasGroup canvasGroup;
    private Canvas fadeCanvas;
    private bool isTransitioning;

    // Back system
    private Stack<string> sceneHistory = new Stack<string>();
    private bool isGoingBack = false;

    // Loading control
    public bool PlayerTappedToContinue { get; set; }

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

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            HandleBackButton();
        }
    }

    private void InitializeFade()
    {
        if (fadeImage == null)
        {
            Debug.LogError("[SceneTransitionManager] Fade Image not assigned!");
            return;
        }

        RectTransform rt = fadeImage.GetComponent<RectTransform>();
        if (rt != null)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        fadeCanvas = fadeImage.GetComponentInParent<Canvas>();
        if (fadeCanvas != null)
        {
            fadeCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            fadeCanvas.sortingOrder = 999;
        }

        canvasGroup = fadeImage.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = fadeImage.gameObject.AddComponent<CanvasGroup>();

        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;
        canvasGroup.interactable = false;
    }

    public void NavigateTo(string targetScene, bool useLoadingScreen)
    {
        if (isTransitioning)
        {
            Debug.LogWarning("[SceneTransitionManager] Already transitioning.");
            return;
        }

        // Save history only if not going back
        if (!isGoingBack)
        {
            string currentScene = SceneManager.GetActiveScene().name;

            if (sceneHistory.Count == 0 || sceneHistory.Peek() != currentScene)
            {
                sceneHistory.Push(currentScene);
            }
        }

        if (useLoadingScreen && !IsInFadeOnlyList(targetScene))
            StartCoroutine(TransitionWithLoadingScreen(targetScene));
        else
            StartCoroutine(TransitionDirect(targetScene));
    }

    private IEnumerator TransitionDirect(string targetScene)
    {
        isTransitioning = true;

        yield return Fade(1f);

        yield return SceneManager.LoadSceneAsync(targetScene);

        yield return null;
        yield return null;

        EnforceCanvasOnTop();

        yield return Fade(0f);

        isTransitioning = false;
    }

    private IEnumerator TransitionWithLoadingScreen(string targetScene)
    {
        isTransitioning = true;
        PlayerTappedToContinue = false;

        if (string.IsNullOrEmpty(defaultLoadingSceneName))
        {
            Debug.LogError("Loading scene not assigned!");
            yield break;
        }

        // Fade out
        yield return Fade(1f);

        // Load loading scene
        yield return SceneManager.LoadSceneAsync(defaultLoadingSceneName);

        yield return null;
        yield return null;

        EnforceCanvasOnTop();

        // Fade in loading scene
        yield return Fade(0f);

        // Set target for loading scene
        LoadingTargetScene.SetTarget(targetScene);

        // Wait for tap OR timeout
        float timer = 0f;
        float maxWait = 10f;

        Debug.Log("[SceneTransitionManager] Waiting for player tap...");

        while (!PlayerTappedToContinue && timer < maxWait)
        {
            timer += Time.deltaTime;
            yield return null;
        }

        if (!PlayerTappedToContinue)
        {
            Debug.LogWarning("[SceneTransitionManager] Auto-continue after timeout.");
        }

        // Fade out loading scene
        yield return Fade(1f);

        // Load actual scene
        yield return SceneManager.LoadSceneAsync(targetScene);

        yield return null;
        yield return null;

        EnforceCanvasOnTop();

        // Fade in
        yield return Fade(0f);

        isTransitioning = false;
    }

    public void GoBack()
    {
        if (isTransitioning) return;

        if (sceneHistory.Count > 0)
        {
            string previousScene = sceneHistory.Pop();

            isGoingBack = true;

            NavigateTo(previousScene, false);

            isGoingBack = false;
        }
        else
        {
            Application.Quit();
        }
    }

    private void HandleBackButton()
    {
        if (isTransitioning) return;

        string current = SceneManager.GetActiveScene().name;

        if (current == "MainMenu")
        {
            Application.Quit();
        }
        else
        {
            GoBack();
        }
    }

    private void EnforceCanvasOnTop()
    {
        if (fadeCanvas != null)
        {
            fadeCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            fadeCanvas.sortingOrder = 999;
        }
    }

    private bool IsInFadeOnlyList(string sceneName)
    {
        foreach (string name in fadeOnlySceneNames)
        {
            if (name == sceneName) return true;
        }
        return false;
    }

    private IEnumerator Fade(float target)
    {
        if (canvasGroup == null) yield break;

        if (fadeDuration <= 0f)
        {
            canvasGroup.alpha = target;
            yield break;
        }

        float start = canvasGroup.alpha;
        float time = 0f;

        canvasGroup.blocksRaycasts = true;

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

        if (defaultLoadingSceneAsset != null)
            defaultLoadingSceneName = defaultLoadingSceneAsset.name;
    }
#endif
}