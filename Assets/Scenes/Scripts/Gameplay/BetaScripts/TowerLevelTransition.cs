using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class TowerLevelTransition : MonoBehaviour
{
    public static TowerLevelTransition Instance { get; private set; }

    [Header("Fade Settings")]
    [SerializeField] private Image fadeImage;
    [SerializeField] private float fadeDuration = 0.8f;

    [Header("Timing")]
    [Tooltip("How long the cat speaks before fade starts")]
    [SerializeField] private float catSpeakDuration = 2.5f;

    [Header("Scenes")]
    [SerializeField] private string resultSceneName = "ResultScene";

    private bool isTransitioning = false;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        if (fadeImage == null)
        {
            Debug.LogError("[TowerLevelTransition] Fade Image not assigned in Inspector!");
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

        Canvas canvas = fadeImage.GetComponentInParent<Canvas>();
        if (canvas != null)
        {
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 999;
        }

        fadeImage.color = Color.black;
        fadeImage.gameObject.SetActive(false);
    }

    public void OnLevelComplete()
    {
        if (isTransitioning) return;
        StartCoroutine(LevelCompleteSequence());
    }

    private IEnumerator LevelCompleteSequence()
    {
        isTransitioning = true;

        // Step 1 — Cat speaks completion line
        Debug.Log("[TowerLevelTransition] Cat speaking...");
        if (CatCompanion.Instance != null)
            CatCompanion.Instance.ShowCompletion();

        // Step 2 — Wait for cat to finish speaking
        yield return new WaitForSeconds(catSpeakDuration);

        // Step 3 — Fade to black
        Debug.Log("[TowerLevelTransition] Fading out...");
        yield return StartCoroutine(FadeTo(1f));

        // Step 4 — Save result data
        SaveResults();

        // Step 5 — Load result scene
        Debug.Log("[TowerLevelTransition] Loading: " + resultSceneName);
        yield return SceneManager.LoadSceneAsync(resultSceneName);
    }

    private void SaveResults()
    {
        if (ScoreManager.Instance == null)
        {
            Debug.LogError("[TowerLevelTransition] ScoreManager instance is null!");
            return;
        }

        int correct = ScoreManager.Instance.CorrectAnswers;
        int wrong = ScoreManager.Instance.WrongAnswers;
        int total = ScoreManager.Instance.TotalQuestions;
        string stageID = ScoreManager.Instance.StageID;
        int towerIndex = ScoreManager.Instance.TowerIndex;

        Debug.Log("[TowerLevelTransition] Saving — correct: " + correct +
            " wrong: " + wrong + " total: " + total + " stage: " + stageID);

        ResultData.Save(correct, wrong, total, stageID, towerIndex);
    }

    private IEnumerator FadeTo(float targetAlpha)
    {
        if (fadeImage == null) yield break;

        fadeImage.gameObject.SetActive(true);
        fadeImage.canvasRenderer.SetAlpha(0f);

        float time = 0f;

        while (time < fadeDuration)
        {
            time += Time.deltaTime;
            float alpha = Mathf.Lerp(0f, targetAlpha, time / fadeDuration);
            fadeImage.canvasRenderer.SetAlpha(alpha);
            yield return null;
        }

        fadeImage.canvasRenderer.SetAlpha(targetAlpha);
    }
}