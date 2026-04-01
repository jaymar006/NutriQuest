using UnityEngine;
using System.Collections;

public class DialogueCameraAnimator : MonoBehaviour
{
    public static DialogueCameraAnimator Instance { get; private set; }

    [Header("Camera Reference")]
    [Tooltip("Assign your UI Canvas RectTransform to simulate camera zoom.")]
    [SerializeField] private RectTransform canvasRect;

    [Header("Zoom Settings")]
    [SerializeField] private float zoomInScale = 1.2f;
    [SerializeField] private float zoomDuration = 0.4f;

    [Header("Shake Settings")]
    [SerializeField] private float shakeStrength = 12f;
    [SerializeField] private float shakeDuration = 0.5f;
    [SerializeField] private float shakeFrequency = 30f;

    private Vector3 _originalScale;
    private Vector3 _originalPosition;
    private Coroutine _zoomCoroutine;
    private Coroutine _shakeCoroutine;

    private void Awake()
    {
        Instance = this;

        if (canvasRect == null)
            canvasRect = GetComponent<RectTransform>();
    }

    private void Start()
    {
        StartCoroutine(CaptureAfterLayout());
    }

    // Capture after layout settles //
    private IEnumerator CaptureAfterLayout()
    {
        yield return null;
        yield return null;

        _originalScale = canvasRect.localScale;
        _originalPosition = canvasRect.localPosition;
    }

    // Zoom entire scene (canvas) //
    public void PlayZoomIn()
    {
        if (_zoomCoroutine != null)
            StopCoroutine(_zoomCoroutine);

        _zoomCoroutine = StartCoroutine(
            ZoomTo(canvasRect, _originalScale * zoomInScale, zoomDuration));
    }

    public void PlayZoomOut()
    {
        if (_zoomCoroutine != null)
            StopCoroutine(_zoomCoroutine);

        _zoomCoroutine = StartCoroutine(
            ZoomTo(canvasRect, _originalScale, zoomDuration));
    }

    // Shake entire scene //
    public void PlayShake()
    {
        if (_shakeCoroutine != null)
        {
            StopCoroutine(_shakeCoroutine);
            canvasRect.localPosition = _originalPosition;
        }

        _shakeCoroutine = StartCoroutine(ShakeEffect());
    }

    private IEnumerator ZoomTo(RectTransform target, Vector3 targetScale, float duration)
    {
        Vector3 startScale = target.localScale;
        float time = 0f;

        while (time < duration)
        {
            time += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, time / duration);
            target.localScale = Vector3.Lerp(startScale, targetScale, t);
            yield return null;
        }

        target.localScale = targetScale;
        _zoomCoroutine = null;
    }

    private IEnumerator ShakeEffect()
    {
        float elapsed = 0f;

        while (elapsed < shakeDuration)
        {
            elapsed += Time.deltaTime;
            float fade = 1f - (elapsed / shakeDuration);
            float offsetX = Mathf.Sin(elapsed * shakeFrequency) * shakeStrength * fade;
            float offsetY = Mathf.Cos(elapsed * shakeFrequency) * shakeStrength * fade;
            canvasRect.localPosition = _originalPosition + new Vector3(offsetX, offsetY, 0f);
            yield return null;
        }

        canvasRect.localPosition = _originalPosition;
        _shakeCoroutine = null;
    }
}