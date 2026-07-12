using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
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

    // FIX: Watchdog so isTransitioning can never get permanently stuck true
    // (e.g. if a transition coroutine gets interrupted by a script recompile
    // during Play Mode, an unhandled exception, or any other edge case we
    // haven't anticipated). If a transition has been "in progress" for
    // longer than any real transition should ever take, we force-reset it
    // and log loudly, instead of silently blocking all navigation forever.
    private float transitionStartTime = -1f;
    private const float MAX_TRANSITION_SECONDS = 20f;
    private string lastTransitionStep = "(none)";

    private void SetStep(string step)
    {
        lastTransitionStep = step;
        Debug.Log("[SceneTransitionManager] " + step);
    }

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
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            HandleBackButton();
        }

        // FIX: Watchdog check. If isTransitioning has been true for far
        // longer than any real transition (fades + optional 10s loading-
        // screen wait) should ever take, something went wrong upstream —
        // recover instead of staying stuck forever.
        if (isTransitioning && transitionStartTime > 0f &&
            Time.unscaledTime - transitionStartTime > MAX_TRANSITION_SECONDS)
        {
            Debug.LogError("[SceneTransitionManager] isTransitioning has been stuck true for over " +
                           MAX_TRANSITION_SECONDS + "s. Last known step was: \"" + lastTransitionStep +
                           "\". Force-resetting so navigation can continue. Whatever step is named " +
                           "above is where the coroutine froze — that's the thing to investigate.");
            SetTransitioning(false);
        }
    }

    // FIX: Centralized setter so transitionStartTime always stays in sync
    // with isTransitioning, no matter which coroutine/branch sets it.
    private void SetTransitioning(bool value)
    {
        isTransitioning = value;
        transitionStartTime = value ? Time.unscaledTime : -1f;
    }

    // Manual escape hatch for testing — call from a debug button or the
    // Inspector's context menu if navigation ever seems stuck and you don't
    // want to wait for the watchdog or restart Play Mode.
    [ContextMenu("Force Reset Transition State (debug)")]
    public void ForceResetTransitionState()
    {
        Debug.LogWarning("[SceneTransitionManager] isTransitioning manually force-reset.");
        SetTransitioning(false);
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
            Debug.LogWarning("[SceneTransitionManager] Already transitioning. Ignoring request to " +
                             "navigate to: " + targetScene + ". If this happens repeatedly, " +
                             "isTransitioning may be stuck true from a previous failed transition.");
            return;
        }

        // FIX: Fail loudly and immediately if the target scene isn't valid,
        // instead of discovering it deep inside a coroutine later (which
        // could leave isTransitioning stuck true and the game frozen on a
        // loading/fade screen forever).
        if (string.IsNullOrEmpty(targetScene))
        {
            Debug.LogError("[SceneTransitionManager] NavigateTo called with an empty scene name!");
            return;
        }

        // Validate the target scene and WARN (not block) if it looks like it's
        // not in Build Settings. This used to hard-abort here, but that check
        // can false-positive depending on how scene paths/names are matched,
        // which would silently block every transition. Warn loudly instead so
        // you still get a clear signal in the Console, without the transition
        // itself ever getting stuck or refusing to run.
        if (!IsSceneInBuildSettings(targetScene))
        {
            Debug.LogWarning("[SceneTransitionManager] NavigateTo: scene '" + targetScene + "' " +
                             "did not match any scene in Build Settings during validation. " +
                             "Attempting to load it anyway — if this scene genuinely doesn't " +
                             "exist in Build Settings, the load will fail with its own error " +
                             "below. If it DOES exist, this warning itself points to a bug in " +
                             "the validation check (e.g. case mismatch) rather than your scene setup.");
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

    // FIX: Checks whether a scene name is actually loadable before we commit
    // to a transition. Uses the scene's build index as the source of truth —
    // returns -1 if the scene isn't in Build Settings at all.
    private bool IsSceneInBuildSettings(string sceneName)
    {
        return SceneUtility.GetBuildIndexByScenePath(sceneName) != -1 ||
               IsSceneNameInBuildList(sceneName);
    }

    // GetBuildIndexByScenePath expects a path, not just a name, in some Unity
    // versions/setups. As a fallback, scan all build scenes by name too.
    private bool IsSceneNameInBuildList(string sceneName)
    {
        int count = SceneManager.sceneCountInBuildSettings;
        for (int i = 0; i < count; i++)
        {
            string path = SceneUtility.GetScenePathByBuildIndex(i);
            string name = System.IO.Path.GetFileNameWithoutExtension(path);
            if (name == sceneName)
                return true;
        }
        return false;
    }

    private IEnumerator TransitionDirect(string targetScene)
    {
        SetTransitioning(true);
        SetStep("TransitionDirect: starting fade out");

        yield return Fade(1f);

        SetStep("TransitionDirect: calling LoadSceneAsync('" + targetScene + "')");

        // FIX: Guard against LoadSceneAsync failing (returns null) so we
        // don't throw inside the coroutine and skip the rest of the fade
        // cleanup, which would leave isTransitioning stuck true.
        AsyncOperation op = SceneManager.LoadSceneAsync(targetScene);
        if (op == null)
        {
            Debug.LogError("[SceneTransitionManager] TransitionDirect: LoadSceneAsync returned " +
                           "null for '" + targetScene + "'. Aborting transition and recovering.");
            yield return Fade(0f);
            SetTransitioning(false);
            yield break;
        }

        SetStep("TransitionDirect: waiting on LoadSceneAsync('" + targetScene + "') to finish (isDone)");
        yield return op;

        SetStep("TransitionDirect: scene loaded, fading in");

        yield return null;
        yield return null;

        EnforceCanvasOnTop();

        yield return Fade(0f);

        SetStep("TransitionDirect: complete");
        SetTransitioning(false);
    }

    private IEnumerator TransitionWithLoadingScreen(string targetScene)
    {
        SetTransitioning(true);
        PlayerTappedToContinue = false;
        SetStep("TransitionWithLoadingScreen: starting, target='" + targetScene + "'");

        if (string.IsNullOrEmpty(defaultLoadingSceneName))
        {
            Debug.LogError("[SceneTransitionManager] Loading scene not assigned! Aborting and recovering.");
            SetTransitioning(false);
            yield break;
        }

        // Fade out
        SetStep("TransitionWithLoadingScreen: fading out before loading screen");
        yield return Fade(1f);

        // Load loading scene
        SetStep("TransitionWithLoadingScreen: calling LoadSceneAsync('" + defaultLoadingSceneName + "')");
        AsyncOperation loadingOp = SceneManager.LoadSceneAsync(defaultLoadingSceneName);
        if (loadingOp == null)
        {
            Debug.LogError("[SceneTransitionManager] Could not load loading scene '" +
                           defaultLoadingSceneName + "'. Is it in Build Settings? Aborting and recovering.");
            yield return Fade(0f);
            SetTransitioning(false);
            yield break;
        }

        SetStep("TransitionWithLoadingScreen: waiting on loading scene '" +
                defaultLoadingSceneName + "' to finish loading (isDone)");
        yield return loadingOp;

        SetStep("TransitionWithLoadingScreen: loading scene loaded, fading in");

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

        SetStep("TransitionWithLoadingScreen: waiting for player tap or " + maxWait + "s timeout");
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
        SetStep("TransitionWithLoadingScreen: fading out loading scene");
        yield return Fade(1f);

        // FIX: Guard the actual target scene load too — this is the scene
        // that was already validated in NavigateTo(), but guarding here as
        // well protects against edge cases (e.g. scene removed from Build
        // Settings mid-session in the Editor).
        SetStep("TransitionWithLoadingScreen: calling LoadSceneAsync('" + targetScene + "')");
        AsyncOperation targetOp = SceneManager.LoadSceneAsync(targetScene);
        if (targetOp == null)
        {
            Debug.LogError("[SceneTransitionManager] Could not load target scene '" + targetScene +
                           "' after loading screen. Aborting and recovering.");
            yield return Fade(0f);
            SetTransitioning(false);
            yield break;
        }

        SetStep("TransitionWithLoadingScreen: waiting on target scene '" + targetScene +
                "' to finish loading (isDone)");
        yield return targetOp;

        SetStep("TransitionWithLoadingScreen: target scene loaded, fading in");

        yield return null;
        yield return null;

        EnforceCanvasOnTop();

        // Fade in
        yield return Fade(0f);

        SetStep("TransitionWithLoadingScreen: complete");
        SetTransitioning(false);
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