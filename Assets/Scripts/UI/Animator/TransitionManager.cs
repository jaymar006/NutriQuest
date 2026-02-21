using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class TransitionManager : MonoBehaviour
{
    public static TransitionManager Instance;

    [Header("Fade Settings")]
    [SerializeField] private Image fadeImage;
    [SerializeField] private float fadeDuration = 0.5f;

    private bool isTransitioning;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (fadeImage != null)
            StartCoroutine(FadeIn());
    }

    public IEnumerator PlayTransition()
    {
        if (isTransitioning || fadeImage == null)
            yield break;

        isTransitioning = true;
        yield return FadeOut();
    }

    private IEnumerator FadeOut()
    {
        yield return Fade(1);
    }

    private IEnumerator FadeIn()
    {
        yield return Fade(0);
        isTransitioning = false;
    }

    private IEnumerator Fade(float targetAlpha)
    {
        float time = 0f;
        float startAlpha = fadeImage.color.a;

        while (time < fadeDuration)
        {
            time += Time.deltaTime;
            float alpha = Mathf.Lerp(startAlpha, targetAlpha, time / fadeDuration);

            Color color = fadeImage.color;
            color.a = alpha;
            fadeImage.color = color;

            yield return null;
        }

        Color finalColor = fadeImage.color;
        finalColor.a = targetAlpha;
        fadeImage.color = finalColor;
    }
}