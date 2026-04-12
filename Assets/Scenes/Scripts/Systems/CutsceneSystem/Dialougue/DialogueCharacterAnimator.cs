using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class DialogueCharacterAnimator : MonoBehaviour
{
    [Header("Character Reference")]
    [SerializeField] private RectTransform characterRect;
    [SerializeField] private Image characterImage;

    [Header("Zoom Settings")]
    [SerializeField] private float zoomScale = 1.3f;
    [SerializeField] private float zoomDuration = 0.3f;

    [Header("Shake Settings")]
    [SerializeField] private float shakeStrength = 8f;
    [SerializeField] private float shakeDuration = 0.4f;
    [SerializeField] private float shakeFrequency = 25f;

    [Header("Idle Breathing")]
    [SerializeField] private float breathScale = 1.04f;
    [SerializeField] private float breathSpeed = 1.2f;
    [SerializeField] private bool breathOnStart = true;

    [Header("Highlight Settings")]
    [SerializeField] private Color activeColor = Color.white;
    [SerializeField] private Color dimColor = new Color(0.3f, 0.3f, 0.3f, 1f);
    [SerializeField] private float highlightSpeed = 0.15f;

    private Vector3 _originalScale;
    private Vector3 _originalPosition;

    private Coroutine _breathCoroutine;
    private Coroutine _shakeCoroutine;
    private Coroutine _zoomCoroutine;
    private Coroutine _highlightCoroutine;

    private void Awake()
    {
        if (characterRect == null) characterRect = GetComponent<RectTransform>();
        if (characterImage == null) characterImage = GetComponent<Image>();
    }

    private void Start()
    {
        StartCoroutine(CaptureAfterLayout());
    }

    private IEnumerator CaptureAfterLayout()
    {
        yield return null; // Wait for layout
        _originalScale = characterRect.localScale;
        _originalPosition = characterRect.localPosition;

        if (breathOnStart)
            StartBreathing();
    }

    public void StartBreathing()
    {
        StopBreathing();
        _breathCoroutine = StartCoroutine(BreathingLoop());
    }

    public void StopBreathing()
    {
        if (_breathCoroutine != null)
        {
            StopCoroutine(_breathCoroutine);
            _breathCoroutine = null;
        }
        if (characterRect != null)
            characterRect.localScale = _originalScale;
    }

    private IEnumerator BreathingLoop()
    {
        float time = 0f;
        while (true)
        {
            time += Time.deltaTime * breathSpeed;
            float scale = 1f + Mathf.Sin(time) * (breathScale - 1f);
            characterRect.localScale = _originalScale * scale;
            yield return null;
        }
    }

    public void PlayShake()
    {
        if (_shakeCoroutine != null)
        {
            StopCoroutine(_shakeCoroutine);
            characterRect.localPosition = _originalPosition;
        }
        _shakeCoroutine = StartCoroutine(ShakeEffect());
    }

    private IEnumerator ShakeEffect()
    {
        float elapsed = 0f;
        while (elapsed < shakeDuration)
        {
            elapsed += Time.deltaTime;
            float fade = 1f - (elapsed / shakeDuration);
            float offset = Mathf.Sin(elapsed * shakeFrequency) * shakeStrength * fade;
            characterRect.localPosition = _originalPosition + new Vector3(offset, 0f, 0f);
            yield return null;
        }
        characterRect.localPosition = _originalPosition;
    }

    public void PlayZoomIn() => PlayZoom(_originalScale * zoomScale);
    public void PlayZoomOut() => PlayZoom(_originalScale);

    private void PlayZoom(Vector3 targetScale)
    {
        if (_zoomCoroutine != null) StopCoroutine(_zoomCoroutine);
        _zoomCoroutine = StartCoroutine(ZoomTo(targetScale, zoomDuration));
    }

    private IEnumerator ZoomTo(Vector3 targetScale, float duration)
    {
        Vector3 startScale = characterRect.localScale;
        float time = 0f;
        while (time < duration)
        {
            time += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, time / duration);
            characterRect.localScale = Vector3.Lerp(startScale, targetScale, t);
            yield return null;
        }
        characterRect.localScale = targetScale;
    }

    public void SetHighlight(bool isActive)
    {
        if (_highlightCoroutine != null)
            StopCoroutine(_highlightCoroutine);

        _highlightCoroutine = StartCoroutine(LerpColor(isActive ? activeColor : dimColor));
    }

    private IEnumerator LerpColor(Color target)
    {
        if (characterImage == null) yield break;

        Color start = characterImage.color;
        float time = 0f;
        while (time < highlightSpeed)
        {
            time += Time.deltaTime;
            characterImage.color = Color.Lerp(start, target, time / highlightSpeed);
            yield return null;
        }
        characterImage.color = target;
    }

    public void SetSprite(Sprite sprite)
    {
        if (characterImage != null && sprite != null)
            characterImage.sprite = sprite;
    }
}