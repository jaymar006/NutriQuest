using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

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

    // LoadingSceneController sets this to true when player taps
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

    private void InitializeFade()
    {
        if (fadeImage == null)
        {
            Debug.LogError("[SceneTransitionManager] Fade Image not assigned in Inspector!");
            return;
        }

        // Force fade image to cover full screen
        RectTransform rt = fadeImage.GetComponent<RectTransform>();
        if (rt != null)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        // Force canvas on top of everything
        fadeCanvas = fadeImage.GetComponentInParent<Canvas>();
        if (fadeCanvas != null)
        {
            fadeCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            fadeCanvas.sortingOrder = 999;
        }
        else
        {
            Debug.LogError("[SceneTransitionManager] No Canvas found on or above the fade Image!");
        }

        canvasGroup = fadeImage.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = fadeImage.gameObject.AddComponent<CanvasGroup>();

        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;
        canvasGroup.interactable = false;
    }

    // Called by SceneNavigationSystem only
    public void NavigateTo(string targetScene, bool useLoadingScreen)
    {
        if (isTransitioning)
        {
            Debug.LogWarning("[SceneTransitionManager] Already transitioning, ignoring NavigateTo call.");
            return;
        }

        if (useLoadingScreen && !IsInFadeOnlyList(targetScene))
            StartCoroutine(TransitionWithLoadingScreen(targetScene));
        else
            StartCoroutine(TransitionDirect(targetScene));
    }

    // PATH A: Fade out > load target scene directly > fade in
    private IEnumerator TransitionDirect(string targetScene)
    {
        isTransitioning = true;
        Debug.Log("[SceneTransitionManager] Direct transition to: " + targetScene);

        yield return Fade(1f);

        yield return SceneManager.LoadSceneAsync(targetScene);

        yield return null;
        yield return null;

        EnforceCanvasOnTop();

        yield return Fade(0f);

        isTransitioning = false;
        Debug.Log("[SceneTransitionManager] Direct transition complete.");
    }

    // PATH B: Fade out > load loading scene > fade in > wait for tap > fade out > load target scene > fade in
    private IEnumerator TransitionWithLoadingScreen(string targetScene)
    {
        isTransitioning = true;
        PlayerTappedToContinue = false;

        Debug.Log("[SceneTransitionManager] Transition with loading screen to: " + targetScene);

        // Step 1: Fade out of current scene
        yield return Fade(1f);

        // Step 2: Load the loading scene
        yield return SceneManager.LoadSceneAsync(defaultLoadingSceneName);

        yield return null;
        yield return null;

        EnforceCanvasOnTop();

        // Step 3: Fade into loading scene
        yield return Fade(0f);

        // Step 4: Tell LoadingSceneController to start loading the target in background
        // LoadingSceneController picks up targetScene from LoadingTargetScene static store
        LoadingTargetScene.SetTarget(targetScene);

        // Step 5: Wait until LoadingSceneController says the player tapped
        Debug.Log("[SceneTransitionManager] Waiting for player tap...");
        while (!PlayerTappedToContinue)
            yield return null;

        Debug.Log("[SceneTransitionManager] Player tapped — transitioning to: " + targetScene);

        // Step 6: Fade out of loading scene
        yield return Fade(1f);

        // Step 7: Load the actual target scene
        yield return SceneManager.LoadSceneAsync(targetScene);

        yield return null;
        yield return null;

        EnforceCanvasOnTop();

        // Step 8: Fade into target scene
        yield return Fade(0f);

        isTransitioning = false;
        Debug.Log("[SceneTransitionManager] Loading screen transition complete.");
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