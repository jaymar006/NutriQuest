using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

#if UNITY_EDITOR
using UnityEditor;
#endif

// ---------------------------------------------------------------------------
// CutsceneLauncher
//
// WHY THIS EXISTS:
// SceneTransitionManager uses one shared "isTransitioning" flag for every
// transition in the game. If that flag is stuck true for ANY reason, every
// future NavigateTo() call — including the intro cutscene — gets silently
// dropped. This script sidesteps that problem completely: it has its OWN
// fade image and its OWN "am I busy" flag, totally separate from
// SceneTransitionManager. It cannot be blocked by unrelated transition
// state anywhere else in the game.
//
// FIX: Now also routes through the Loading Scene first, matching the
// intended pipeline:
//   Challenge -> Loading Scene -> Intro Cutscene -> Gameplay
// The only thing borrowed from SceneTransitionManager is its public
// PlayerTappedToContinue bool (just read/set — never NavigateTo/
// isTransitioning), since your existing Loading Scene's tap button is
// already wired to set that same property. Everything else — the fade,
// the scene loads, the busy flag — is fully independent.
//
// SETUP:
// 1. Put this on the SAME persistent GameObject as SceneTransitionManager
//    (the one with DontDestroyOnLoad).
// 2. Assign a Fade Image in the Inspector.
// 3. Assign the Loading Scene in the Inspector (drag the SceneAsset; must
//    also be added to Build Settings).
// 4. Call CutsceneLauncher.Instance.LaunchCutscene("SceneName", useLoadingScreen)
//    from anywhere (CutsceneTrigger.Play() already does this).
// ---------------------------------------------------------------------------
public class CutsceneLauncher : MonoBehaviour
{
    public static CutsceneLauncher Instance { get; private set; }

    [Header("Fade Settings")]
    [SerializeField] private Image fadeImage;
    [SerializeField] private float fadeDuration = 0.4f;

    [Header("Loading Scene")]
#if UNITY_EDITOR
    [SerializeField] private SceneAsset loadingSceneAsset;
#endif
    [SerializeField] private string loadingSceneName;

    [Tooltip("Max seconds to wait on the Loading Scene for a player tap before auto-continuing. " +
             "Must be comfortably longer than LoadingSceneController's own minimumLoadTime (plus " +
             "its 0.3s buffer), or this timeout can fire before the Loading Scene ever sets " +
             "PlayerTappedToContinue itself.")]
    [SerializeField] private float maxLoadingWait = 15f;

    // Independent of SceneTransitionManager.isTransitioning on purpose.
    private bool isBusy = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        // FIX: If this GameObject isn't a root object (e.g. someone nests it
        // as a child under SceneTransitionManager's object instead of adding
        // the component directly on that same root object), DontDestroyOnLoad
        // silently fails and this object gets destroyed on the very next
        // scene load — killing this coroutine mid-flight with no error the
        // rest of the game notices. Targeting transform.root guarantees the
        // whole hierarchy this object belongs to actually persists, and we
        // log loudly if a fix is even needed so this can't fail silently again.
        if (transform.parent != null)
        {
            Debug.LogWarning("[CutsceneLauncher] This GameObject is nested under '" +
                              transform.parent.name + "' instead of being a root object. " +
                              "Persisting via transform.root ('" + transform.root.name +
                              "') instead — but please move CutsceneLauncher onto a root " +
                              "GameObject directly to avoid relying on this fallback.");
        }

        DontDestroyOnLoad(transform.root.gameObject);

        if (fadeImage != null)
        {
            Color c = fadeImage.color;
            c.a = 0f;
            fadeImage.color = c;
            fadeImage.raycastTarget = false;

            RectTransform rt = fadeImage.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
            }
        }
        else
        {
            Debug.LogError("[CutsceneLauncher] Fade Image not assigned! Cutscene will still " +
                            "load, just without a fade.");
        }
    }

    // Call this to launch a cutscene scene directly. Bypasses
    // SceneTransitionManager entirely — cannot be blocked by its state.
    // useLoadingScreen: if true and a Loading Scene is assigned, routes
    // through it first (Challenge -> Loading Scene -> Cutscene). If false,
    // or no Loading Scene is assigned, fades straight to the cutscene.
    public void LaunchCutscene(string sceneName, bool useLoadingScreen = true)
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogError("[CutsceneLauncher] LaunchCutscene called with an empty scene name!");
            return;
        }

        if (isBusy)
        {
            // This can ONLY happen if LaunchCutscene itself is called twice
            // in a row — never because of some unrelated transition
            // elsewhere in the game.
            Debug.LogWarning("[CutsceneLauncher] Already launching a cutscene. Ignoring " +
                              "duplicate request for: " + sceneName);
            return;
        }

        bool goThroughLoadingScene = useLoadingScreen && !string.IsNullOrEmpty(loadingSceneName);

        StartCoroutine(goThroughLoadingScene
            ? LaunchWithLoadingScreen(sceneName)
            : LaunchDirect(sceneName));
    }

    private IEnumerator LaunchDirect(string sceneName)
    {
        isBusy = true;

        Debug.Log("[CutsceneLauncher] Launching cutscene (direct, no loading screen): " + sceneName);

        yield return Fade(1f);

        AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);
        if (op == null)
        {
            Debug.LogError("[CutsceneLauncher] LoadSceneAsync returned null for '" + sceneName +
                            "'. Is it added and enabled in File > Build Settings? Aborting.");
            yield return Fade(0f);
            isBusy = false;
            yield break;
        }

        yield return op;

        // Let the new scene's Awake/Start/OnEnable run before fading in.
        yield return null;
        yield return null;

        yield return Fade(0f);

        Debug.Log("[CutsceneLauncher] Cutscene launch complete: " + sceneName);
        isBusy = false;
    }

    private IEnumerator LaunchWithLoadingScreen(string targetSceneName)
    {
        isBusy = true;

        Debug.Log("[CutsceneLauncher] Launching cutscene via Loading Scene. Target: " + targetSceneName);

        // Fade out before loading scene
        yield return Fade(1f);

        AsyncOperation loadingOp = SceneManager.LoadSceneAsync(loadingSceneName);
        if (loadingOp == null)
        {
            Debug.LogError("[CutsceneLauncher] Could not load Loading Scene '" + loadingSceneName +
                            "'. Is it in Build Settings? Falling back to direct load of '" +
                            targetSceneName + "'.");
            yield return LaunchDirectNoBusyReset(targetSceneName);
            isBusy = false;
            yield break;
        }

        yield return loadingOp;

        yield return null;
        yield return null;

        // Fade in on the Loading Scene
        yield return Fade(0f);

        // Tell the Loading Scene which scene name to display / hand off to,
        // reusing your existing LoadingTargetScene helper.
        LoadingTargetScene.SetTarget(targetSceneName);

        // Reuse SceneTransitionManager's public tap flag — your Loading
        // Scene's Continue button is presumably already wired to set this.
        // We only read/write this one bool; we never call NavigateTo() or
        // touch isTransitioning, so this can't be blocked by that flag.
        if (SceneTransitionManager.Instance != null)
            SceneTransitionManager.Instance.PlayerTappedToContinue = false;

        float timer = 0f;
        Debug.Log("[CutsceneLauncher] Waiting for player tap on Loading Scene (or " +
                  maxLoadingWait + "s timeout)...");

        while (timer < maxLoadingWait)
        {
            bool tapped = SceneTransitionManager.Instance != null &&
                          SceneTransitionManager.Instance.PlayerTappedToContinue;

            if (tapped) break;

            timer += Time.unscaledDeltaTime;
            yield return null;
        }

        // Fade out Loading Scene
        yield return Fade(1f);

        AsyncOperation targetOp = SceneManager.LoadSceneAsync(targetSceneName);
        if (targetOp == null)
        {
            Debug.LogError("[CutsceneLauncher] LoadSceneAsync returned null for '" + targetSceneName +
                            "'. Is it added and enabled in File > Build Settings? Aborting.");
            yield return Fade(0f);
            isBusy = false;
            yield break;
        }

        yield return targetOp;

        yield return null;
        yield return null;

        yield return Fade(0f);

        Debug.Log("[CutsceneLauncher] Cutscene launch complete: " + targetSceneName);
        isBusy = false;
    }

    // Used only as a fallback inside LaunchWithLoadingScreen if the Loading
    // Scene itself fails to load — does the fade/load/fade for the target
    // scene without touching isBusy (caller resets it).
    private IEnumerator LaunchDirectNoBusyReset(string sceneName)
    {
        AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);
        if (op == null)
        {
            Debug.LogError("[CutsceneLauncher] LoadSceneAsync returned null for '" + sceneName + "'.");
            yield return Fade(0f);
            yield break;
        }

        yield return op;
        yield return null;
        yield return null;
        yield return Fade(0f);
    }

    private IEnumerator Fade(float target)
    {
        if (fadeImage == null) yield break;

        float start = fadeImage.color.a;
        float time = 0f;

        fadeImage.raycastTarget = target > 0f;

        if (fadeDuration <= 0f)
        {
            Color instant = fadeImage.color;
            instant.a = target;
            fadeImage.color = instant;
            yield break;
        }

        while (time < fadeDuration)
        {
            time += Time.unscaledDeltaTime;
            Color c = fadeImage.color;
            c.a = Mathf.Lerp(start, target, time / fadeDuration);
            fadeImage.color = c;
            yield return null;
        }

        Color final = fadeImage.color;
        final.a = target;
        fadeImage.color = final;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (loadingSceneAsset != null)
            loadingSceneName = loadingSceneAsset.name;
    }
#endif
}