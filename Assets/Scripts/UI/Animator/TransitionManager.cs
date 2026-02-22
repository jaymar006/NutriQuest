using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

[RequireComponent(typeof(Canvas))]
public class TransitionManager : MonoBehaviour
{
    public static TransitionManager Instance { get; private set; }

    [Header("Fade Settings")]
    [SerializeField] private Image fadeImage;
    [SerializeField] private float fadeDuration = 0.4f;

    private CanvasGroup canvasGroup;
    private bool isTransitioning;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        Initialize();
    }

    private void Initialize()
    {
        if (fadeImage == null)
        {
            Debug.LogError("TransitionManager: Fade Image is not assigned.");
            return;
        }

        canvasGroup = fadeImage.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = fadeImage.gameObject.AddComponent<CanvasGroup>();

        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;
        canvasGroup.interactable = false;
    }

    private void Start()
    {
        StartCoroutine(Fade(0f));
    }

    public void LoadScene(string sceneName)
    {
        if (isTransitioning) return;

        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogError("TransitionManager: Scene name is empty.");
            return;
        }

        if (SceneUtility.GetBuildIndexByScenePath(sceneName) == -1 &&
            !Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogError("TransitionManager: Scene not found in Build Settings: " + sceneName);
            return;
        }

        StartCoroutine(LoadRoutine(sceneName));
    }

    public void LoadScene(int buildIndex)
    {
        if (isTransitioning) return;

        if (buildIndex < 0 || buildIndex >= SceneManager.sceneCountInBuildSettings)
        {
            Debug.LogError("TransitionManager: Invalid scene index: " + buildIndex);
            return;
        }

        StartCoroutine(LoadRoutine(buildIndex));
    }

    private IEnumerator LoadRoutine(object sceneReference)
    {
        isTransitioning = true;

        yield return Fade(1f);

        AsyncOperation operation;

        if (sceneReference is string)
            operation = SceneManager.LoadSceneAsync((string)sceneReference);
        else
            operation = SceneManager.LoadSceneAsync((int)sceneReference);

        operation.allowSceneActivation = true;

        while (!operation.isDone)
            yield return null;

        yield return Fade(0f);

        isTransitioning = false;
    }

    private IEnumerator Fade(float target)
    {
        if (fadeDuration <= 0f)
        {
            canvasGroup.alpha = target;
            yield break;
        }

        canvasGroup.blocksRaycasts = true;

        float start = canvasGroup.alpha;
        float time = 0f;

        while (time < fadeDuration)
        {
            time += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(start, target, time / fadeDuration);
            yield return null;
        }

        canvasGroup.alpha = target;

        if (target == 0f)
            canvasGroup.blocksRaycasts = false;
    }
}