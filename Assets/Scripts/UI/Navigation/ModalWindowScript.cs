using UnityEngine;
using System.Collections;

[RequireComponent(typeof(CanvasGroup))]
public class ModalWindowScript : MonoBehaviour
{
    private CanvasGroup canvasGroup;
    private RectTransform rectTransform;

    [Header("Animation Settings")]
    [SerializeField] private float fadeDuration = 0.25f;
    [SerializeField] private float startScale = 0.8f;

    private Vector3 originalScale;

    void Awake()
    {
        // Auto-get required components
        canvasGroup = GetComponent<CanvasGroup>();
        rectTransform = GetComponent<RectTransform>();

        originalScale = rectTransform.localScale;

        // Start hidden
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        rectTransform.localScale = originalScale * startScale;
    }

    public void Show()
    {
        gameObject.SetActive(true);
        StopAllCoroutines();
        StartCoroutine(AnimateModal(0f, 1f, originalScale * startScale, originalScale));
    }

    public void Hide()
    {
        StopAllCoroutines();
        StartCoroutine(AnimateModal(1f, 0f, originalScale, originalScale * startScale));
    }

    private IEnumerator AnimateModal(float startAlpha, float endAlpha, Vector3 startScaleVec, Vector3 endScaleVec)
    {
        float time = 0f;

        if (endAlpha == 0f)
        {
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        while (time < fadeDuration)
        {
            time += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, time / fadeDuration);

            canvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, t);
            rectTransform.localScale = Vector3.Lerp(startScaleVec, endScaleVec, t);

            yield return null;
        }

        canvasGroup.alpha = endAlpha;
        rectTransform.localScale = endScaleVec;

        if (endAlpha == 1f)
        {
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }
        else
        {
            gameObject.SetActive(false);
        }
    }
}