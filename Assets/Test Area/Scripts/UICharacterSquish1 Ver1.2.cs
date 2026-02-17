using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Events;

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
    public bool reverseIdleCurve = true;

    [Header("Per-Axis Scaling")]
    public bool scaleX = true;
    public bool scaleY = true;
    public bool scaleZ = false;

    [Header("Animation Curves")]
    public AnimationCurve reactionCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    public AnimationCurve idleCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    // =========================
    // ⭐ MILESTONE SYSTEM
    // =========================
    [Header("Milestone System")]
    public bool useMilestones = false;
    public float milestoneDisplayDuration = 1.2f;
    public List<TapMilestone> milestones = new List<TapMilestone>();

    private int tapCount = 0;
    private bool isShowingMilestone = false;

    [System.Serializable]
    public class TapMilestone
    {
        public string milestoneName = "Milestone";
        public int tapsRequired = 10;

        public bool useSprite = true;
        public Sprite milestoneSprite;

        public bool useSound = false;
        public AudioClip milestoneSound;

        public bool useEvent = false;
        public UnityEvent milestoneEvent;

        [HideInInspector] public bool triggered;
    }

    // =========================

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

    // =========================
    // TAP INPUT
    // =========================
    public void OnPointerClick(PointerEventData eventData)
    {
        tapCount++;

        if (useMilestones)
            CheckMilestones();

        if (!isBusy && !isShowingMilestone)
            StartCoroutine(Reaction());
    }

    // =========================
    // ⭐ MILESTONE LOGIC
    // =========================
    private void CheckMilestones()
    {
        foreach (var m in milestones)
        {
            if (m.triggered) continue;

            if (tapCount >= m.tapsRequired)
            {
                m.triggered = true;
                StartCoroutine(PlayMilestone(m));
                break;
            }
        }
    }

    private IEnumerator PlayMilestone(TapMilestone m)
    {
        isShowingMilestone = true;
        isBusy = true;

        // STOP DEFAULT AUDIO
        audioSource.Stop();

        // FORCE SPRITE
        if (m.useSprite && m.milestoneSprite != null)
            img.sprite = m.milestoneSprite;

        // PLAY MILESTONE SOUND
        if (m.useSound && m.milestoneSound != null)
            audioSource.PlayOneShot(m.milestoneSound, volume);

        // EVENT
        if (m.useEvent)
            m.milestoneEvent?.Invoke();

        yield return new WaitForSeconds(milestoneDisplayDuration);

        // RETURN TO DEFAULT
        if (idleSprite != null)
            img.sprite = idleSprite;

        isBusy = false;
        isShowingMilestone = false;
    }

    // =========================
    // REACTION ANIMATION
    // =========================
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

    // =========================
    // IDLE LOOP
    // =========================
    private IEnumerator IdlePingPong()
    {
        float t = 0f;
        float direction = 1f;

        while (true)
        {
            if (!isBusy && !isShowingMilestone)
            {
                Vector3 startScale = transform.localScale;

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

                if (t >= idleDuration) direction = -1f;
                else if (t <= 0f) direction = 1f;
            }

            yield return null;
        }
    }

    // =========================
    // GENERIC ANIMATION
    // =========================
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
