using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;

[RequireComponent(typeof(Image))]
[RequireComponent(typeof(AudioSource))]
public class CuteUICharacterCuteUICharacterAnimator : MonoBehaviour, IPointerClickHandler
{
    [Header("Sprites")]
    public Sprite idleSprite;
    public Sprite tapSprite;

    [Header("Audio")]
    public AudioClip tapSound;
    [Range(0f, 1f)] public float volume = 1f;

    [Header("Animation")]
    public float reactionTime = 0.35f;
    public float reactionStretch = 1.3f;

    [Header("Idle Breathing")]
    public float idleDelay = 2f;
    public float idleTime = 0.8f;
    public float idleStretch = 1.05f;

    public AnimationCurve curve = new AnimationCurve(
        new Keyframe(0f, 0f),
        new Keyframe(0.4f, 1f),
        new Keyframe(0.7f, -0.25f),
        new Keyframe(1f, 0f)
    );

    private Image img;
    private AudioSource audioSource;
    private Vector3 originalScale;
    private bool isBusy = false;

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
        StartCoroutine(IdleLoop());
    }

    // -----------------------------
    // Tap Detection
    // -----------------------------
    public void OnPointerClick(PointerEventData eventData)
    {
        if (!isBusy)
            StartCoroutine(Reaction());
    }

    // -----------------------------
    IEnumerator Reaction()
    {
        isBusy = true;

        // Change face
        if (tapSprite != null)
            img.sprite = tapSprite;

        // Play audio
        if (tapSound != null)
            audioSource.PlayOneShot(tapSound, volume);

        // Squish animation
        yield return Animate(reactionTime, reactionStretch);

        // Return to idle face
        if (idleSprite != null)
            img.sprite = idleSprite;

        isBusy = false;
    }

    // -----------------------------
    IEnumerator IdleLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(idleDelay);

            if (!isBusy)
                yield return Animate(idleTime, idleStretch);
        }
    }

    // -----------------------------
    IEnumerator Animate(float time, float stretch)
    {
        float t = 0f;

        while (t < time)
        {
            t += Time.deltaTime;
            float p = t / time;

            float c = curve.Evaluate(p);
            float s = 1f + c * (stretch - 1f);

            transform.localScale = originalScale * s;

            yield return null;
        }

        transform.localScale = originalScale;
    }
}
