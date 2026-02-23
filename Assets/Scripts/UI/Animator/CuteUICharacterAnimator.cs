using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(Image))]
[RequireComponent(typeof(AudioSource))]
public class CuteUICharacterAnimator : MonoBehaviour, IPointerClickHandler
{
    [System.Serializable]
    public class TapMilestone
    {
        public string milestoneName;
        public int tapsRequired;

        public Sprite milestoneSprite;
        public AudioClip milestoneSound;

        public bool triggerEvent;
        public UnityEvent onMilestoneTriggered;
    }

    [System.Flags]
    public enum StretchAxis
    {
        None = 0,
        X = 1,
        Y = 2,
        Z = 4
    }

    [Header("Base Sprites")]
    [SerializeField] private Sprite idleSprite;
    [SerializeField] private Sprite tapSprite;

    [Header("Tap Milestones")]
    [SerializeField] private bool enableMilestones = true;
    [SerializeField] private List<TapMilestone> milestones = new List<TapMilestone>();

    [Header("Tap Reset Settings")]
    [SerializeField] private bool enableTapTimeout = true;
    [SerializeField] private float tapResetDelay = 3f;

    [Header("Audio")]
    [SerializeField] private bool enableTapSound = true;
    [SerializeField] private AudioClip tapSound;
    [Range(0f, 1f)][SerializeField] private float volume = 1f;

    [Header("Reaction Animation")]
    [SerializeField] private float reactionTime = 0.35f;
    [SerializeField] private float reactionStretch = 1.3f;

    [Header("Idle Breathing")]
    [SerializeField] private StretchAxis idleAxis = StretchAxis.Y;
    [SerializeField] private float idleDelay = 2f;
    [SerializeField] private float idleTime = 0.8f;
    [SerializeField] private float idleInitialScale = 1f;
    [SerializeField] private float idleMaxScale = 1.05f;
    [SerializeField] private bool reverseIdleCurve;

    [SerializeField]
    private AnimationCurve idleCurve = new AnimationCurve(
        new Keyframe(0f, 0f),
        new Keyframe(0.5f, 1f),
        new Keyframe(1f, 0f)
    );

    private Image img;
    private AudioSource audioSource;
    private Vector3 originalScale;

    private int currentTapCount;
    private float lastTapTime;
    private bool isBusy;
    private bool idleReversed;

    private Coroutine idleRoutine;

    private bool AffectX => (idleAxis & StretchAxis.X) != 0;
    private bool AffectY => (idleAxis & StretchAxis.Y) != 0;
    private bool AffectZ => (idleAxis & StretchAxis.Z) != 0;

    private void Awake()
    {
        img = GetComponent<Image>();
        audioSource = GetComponent<AudioSource>();
        originalScale = transform.localScale;

        if (idleSprite != null)
            img.sprite = idleSprite;

        milestones.Sort((a, b) => a.tapsRequired.CompareTo(b.tapsRequired));
    }

    private void OnEnable()
    {
        idleRoutine = StartCoroutine(IdleLoop());
    }

    private void OnDisable()
    {
        if (idleRoutine != null)
            StopCoroutine(idleRoutine);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (isBusy)
            return;

        HandleTapTimeout();

        currentTapCount++;
        lastTapTime = Time.time;

        TapMilestone milestone = GetTriggeredMilestone(currentTapCount);

        if (enableMilestones && milestone != null)
            StartCoroutine(MilestoneReaction(milestone));
        else
            StartCoroutine(NormalReaction());
    }

    private void HandleTapTimeout()
    {
        if (!enableTapTimeout)
            return;

        if (Time.time - lastTapTime > tapResetDelay)
            currentTapCount = 0;
    }

    private TapMilestone GetTriggeredMilestone(int tapCount)
    {
        foreach (var milestone in milestones)
        {
            if (tapCount == milestone.tapsRequired)
                return milestone;
        }
        return null;
    }

    private IEnumerator NormalReaction()
    {
        isBusy = true;

        if (tapSprite != null)
            img.sprite = tapSprite;

        if (enableTapSound && tapSound != null)
            audioSource.PlayOneShot(tapSound, volume);

        yield return AnimateReaction(reactionTime, reactionStretch);

        if (idleSprite != null)
            img.sprite = idleSprite;

        isBusy = false;
    }

    private IEnumerator MilestoneReaction(TapMilestone milestone)
    {
        isBusy = true;

        if (milestone.milestoneSprite != null)
            img.sprite = milestone.milestoneSprite;

        if (milestone.milestoneSound != null)
            audioSource.PlayOneShot(milestone.milestoneSound, volume);

        if (milestone.triggerEvent)
            milestone.onMilestoneTriggered?.Invoke();

        yield return AnimateReaction(reactionTime * 1.5f, reactionStretch * 1.2f);

        if (idleSprite != null)
            img.sprite = idleSprite;

        isBusy = false;
    }

    private IEnumerator IdleLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(idleDelay);

            if (!isBusy)
                yield return AnimateIdle();
        }
    }

    private IEnumerator AnimateIdle()
    {
        if (reverseIdleCurve)
            idleReversed = !idleReversed;

        float elapsed = 0f;

        while (elapsed < idleTime)
        {
            elapsed += Time.deltaTime;

            float t = idleReversed
                ? 1 - (elapsed / idleTime)
                : elapsed / idleTime;

            float curveValue = idleCurve.Evaluate(t);
            float scaleMultiplier = idleInitialScale + (curveValue * (idleMaxScale - idleInitialScale));

            ApplyScale(scaleMultiplier);

            yield return null;
        }

        transform.localScale = originalScale;
    }

    private IEnumerator AnimateReaction(float time, float stretch)
    {
        float t = 0f;

        while (t < time)
        {
            t += Time.deltaTime;
            float p = t / time;

            float s = 1f + (p * (stretch - 1f));
            transform.localScale = originalScale * s;

            yield return null;
        }

        transform.localScale = originalScale;
    }

    private void ApplyScale(float multiplier)
    {
        Vector3 modified = originalScale;

        if (AffectX)
            modified.x *= multiplier;
        if (AffectY)
            modified.y *= multiplier;
        if (AffectZ)
            modified.z *= multiplier;

        transform.localScale = modified;
    }
}