using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Events;
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

    [Header("Events")]
    [Tooltip("Fired once the modal has fully finished appearing (fade-in complete, interactable).")]
    public UnityEvent onShown;
    [Tooltip("Fired once the modal has fully finished disappearing (fade-out complete, GameObject deactivated).")]
    public UnityEvent onHidden;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        rectTransform = GetComponent<RectTransform>();
        originalScale = rectTransform.localScale;

        // Start modal hidden //
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
        rectTransform.localScale = originalScale * startScale;
    }

    public void Show()
    {
        gameObject.SetActive(true);
        StopAllCoroutines();
        StartCoroutine(AnimateModal(canvasGroup.alpha, 1f, rectTransform.localScale, originalScale));
    }

    public void Hide()
    {
        // Clear the EventSystem's selected/pressed object so it doesn't hold a
        // stale reference to a button that's about to be deactivated. Without
        // this, clicks on buttons inside the modal can silently stop firing
        // the next time the modal is shown.
        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
        }

        StopAllCoroutines();
        StartCoroutine(AnimateModal(canvasGroup.alpha, 0f, rectTransform.localScale, originalScale * startScale));
    }

    private IEnumerator AnimateModal(float startAlpha, float endAlpha,
                                     Vector3 startScaleVec, Vector3 endScaleVec)
    {
        float time = 0f;
        bool isHiding = endAlpha == 0f;

        if (isHiding)
        {
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        while (time < fadeDuration)
        {
            time += Time.unscaledDeltaTime; // pause-safe
            float t = Mathf.SmoothStep(0f, 1f, time / fadeDuration);
            canvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, t);
            rectTransform.localScale = Vector3.Lerp(startScaleVec, endScaleVec, t);
            yield return null;
        }

        // Snap to final values //
        canvasGroup.alpha = endAlpha;
        rectTransform.localScale = endScaleVec;

        if (!isHiding)
        {
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
            onShown?.Invoke();
        }
        else
        {
            gameObject.SetActive(false);
            onHidden?.Invoke();
        }
    }
}