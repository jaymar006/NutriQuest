using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Image))]
[RequireComponent(typeof(AudioSource))]
public class UICharacterSquish : MonoBehaviour, IPointerClickHandler
{
    [Header("Sprites")]
    public Sprite idleSprite;
    public Sprite tapSprite;

    [Header("Audio")]
    public AudioClip tapSound;
    [Range(0f, 1f)] public float volume = 1f;

    [Header("Animation Settings")]
    public float reactionDuration = 0.35f;
    public float reactionMaxScale = 1.3f;
    public bool resetAfterReaction = true;
    public bool reverseReactionCurve = false;

    public float idleDuration = 1f;
    public float idleMaxScale = 1.05f;
    public bool resetAfterIdle = true;
    public bool reverseIdleCurve = true; // reverse ping-pong

    [Header("Per-Axis Scaling")]
    public bool scaleX = true;
    public bool scaleY = true;
    public bool scaleZ = false;

    [Header("Animation Curves")]
    public AnimationCurve reactionCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    public AnimationCurve idleCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private Image img;
    private AudioSource audioSource;
    private Vector3 originalScale;
    private bool isBusy;

    void Awake()
    {
        img = GetComponent<Image>();
        audioSource = GetComponent<AudioSource>();
        originalScale = transform.localScale;

        if (idleSprite != null)
            img.sprite = idleSprite;
    }

    void Start()
    {
        StartCoroutine(IdlePingPong());
    }

    // Tap detection
    public void OnPointerClick(PointerEventData eventData)
    {
        if (!isBusy)
            StartCoroutine(Reaction());
    }

    private IEnumerator Reaction()
    {
        isBusy = true;

        if (tapSprite != null)
            img.sprite = tapSprite;

        if (tapSound != null)
            audioSource.PlayOneShot(tapSound, volume);

        yield return Animate(reactionDuration, reactionMaxScale, reactionCurve, reverseReactionCurve, resetAfterReaction);

        if (idleSprite != null)
            img.sprite = idleSprite;

        isBusy = false;
    }

    /// <summary>
    /// Idle breathing as smooth ping-pong (expand → shrink → expand)
    /// </summary>
    private IEnumerator IdlePingPong()
    {
        float t = 0f;
        float direction = 1f; // 1 = forward, -1 = backward
        Vector3 startScale;

        while (true)
        {
            if (!isBusy)
            {
                startScale = transform.localScale;

                while (!isBusy)
                {
                    t += Time.deltaTime * direction;
                    float normalized = Mathf.Clamp01(t / idleDuration);

                    if (reverseIdleCurve)
                        normalized = 1f - normalized;

                    float curveValue = idleCurve.Evaluate(normalized);
                    float scaleFactor = 1f + curveValue * (idleMaxScale - 1f);

                    Vector3 newScale = startScale;
                    if (scaleX) newScale.x = startScale.x * scaleFactor;
                    if (scaleY) newScale.y = startScale.y * scaleFactor;
                    if (scaleZ) newScale.z = startScale.z * scaleFactor;

                    transform.localScale = newScale;

                    // Reverse direction at ends
                    if (t >= idleDuration)
                        direction = -1f;
                    else if (t <= 0f)
                        direction = 1f;

                    yield return null;
                }

                // Optional: reset to original scale after stopping idle
                if (resetAfterIdle)
                    transform.localScale = originalScale;
            }

            yield return null;
        }
    }

    /// <summary>
    /// Animate a single squash/stretch (reaction or one-time)
    /// </summary>
    private IEnumerator Animate(float duration, float maxScale, AnimationCurve curve, bool reverseCurve, bool resetAfterAnimation)
    {
        float t = 0f;
        Vector3 startScale = transform.localScale;

        while (t < duration)
        {
            t += Time.deltaTime;
            float normalized = Mathf.Clamp01(t / duration);

            if (reverseCurve)
                normalized = 1f - normalized;

            float curveValue = curve.Evaluate(normalized);
            float scaleFactor = 1f + curveValue * (maxScale - 1f);

            Vector3 newScale = startScale;
            if (scaleX) newScale.x = startScale.x * scaleFactor;
            if (scaleY) newScale.y = startScale.y * scaleFactor;
            if (scaleZ) newScale.z = startScale.z * scaleFactor;

            transform.localScale = newScale;

            yield return null;
        }

        if (resetAfterAnimation)
            transform.localScale = originalScale;
    }
}
