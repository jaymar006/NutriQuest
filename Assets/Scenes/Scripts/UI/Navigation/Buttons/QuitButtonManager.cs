using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class QuitButton : MonoBehaviour
{
    [Header("Navigation")]
#if UNITY_EDITOR
    [SerializeField] private SceneAsset targetScene;
#endif
    [SerializeField] private string targetSceneName;

    [Header("Button Reference")]
    [SerializeField] private Button quitButton;

    [Header("Fade Settings")]
    [SerializeField] private bool useFade = true;
    [SerializeField] private Image fadeImage;
    [SerializeField] private float fadeDuration = 0.8f;

    private void OnValidate()
    {
#if UNITY_EDITOR
        if (targetScene != null)
            targetSceneName = targetScene.name;
#endif
    }

    private void Start()
    {
        if (quitButton != null)
            quitButton.onClick.AddListener(OnQuitPressed);
        else
            Debug.LogWarning("[QuitButton] Quit Button not assigned in Inspector!");

        // Make sure fade image starts hidden
        if (fadeImage != null)
        {
            SetupFadeImage();
            fadeImage.gameObject.SetActive(false);
        }
    }

    private void OnQuitPressed()
    {
        // Disable button to prevent multiple presses
        if (quitButton != null)
            quitButton.interactable = false;

        ClearSessionData();

        if (useFade && fadeImage != null)
            StartCoroutine(FadeOutThenLoad());
        else
            LoadScene();
    }

    private void ClearSessionData()
    {
        // Wipe result data so no score is passed to the Result Scene
        ResultData.Save(0, 0, 0, "", 0);

        // Reset in-memory button states
        if (AnswerBTNFunction2.Instance != null)
            AnswerBTNFunction2.Instance.ResetButtons();

        Debug.Log("[QuitButton] Session data cleared. Progress not recorded.");
    }

    private void SetupFadeImage()
    {
        if (fadeImage == null) return;

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
    }

    private IEnumerator FadeOutThenLoad()
    {
        // Fade to black
        fadeImage.gameObject.SetActive(true);
        fadeImage.canvasRenderer.SetAlpha(0f);

        float time = 0f;
        while (time < fadeDuration)
        {
            time += Time.deltaTime;
            fadeImage.canvasRenderer.SetAlpha(Mathf.Lerp(0f, 1f, time / fadeDuration));
            yield return null;
        }

        fadeImage.canvasRenderer.SetAlpha(1f);

        LoadScene();
    }

    private void LoadScene()
    {
        if (string.IsNullOrEmpty(targetSceneName))
        {
            Debug.LogError("[QuitButton] No target scene assigned!");

            // Re-enable button if scene load fails
            if (quitButton != null)
                quitButton.interactable = true;

            return;
        }

        Debug.Log("[QuitButton] Quitting to: " + targetSceneName);
        SceneManager.LoadScene(targetSceneName);
    }
}