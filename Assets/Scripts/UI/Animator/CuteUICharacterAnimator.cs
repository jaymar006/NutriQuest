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

    [Header("Idle Mode")]
    [SerializeField] private bool useBreathing = true;
    [SerializeField] private bool useLevitating = false;

    [Header("Tap Milestones")]
    [SerializeField] private bool enableMilestones = true;
    [SerializeField] private List<TapMilestone> milestones = new List<TapMilestone>();

    [Header("Tap Reset Settings")]
    [SerializeField] private bool enableTapTimeout = true;
    [SerializeField] private float tapResetDelay = 3f;

    [Header("Audio")]
    [SerializeField] private bool enableTapSound = true;
    [SerializeField] private AudioClip tapSound;
    [Range(0f, 1f)]
    [SerializeField] private float volume = 1f;

    [Header("Reaction Animation")]
    [SerializeField] private float reactionTime = 0.35f;
    [SerializeField] private float reactionStretch = 1.3f;

    [Header("Breathing Settings")]
    [SerializeField] private StretchAxis breathAxis = StretchAxis.Y;
    [SerializeField] private float breathDelay = 2f;
    [SerializeField] private float breathTime = 0.8f;
    [SerializeField] private float breathInitialScale = 1f;
    [SerializeField] private float breathMaxScale = 1.05f;
    [SerializeField] private bool reverseBreathCurve;
    [SerializeField]
    private AnimationCurve breathCurve = new AnimationCurve(
        new Keyframe(0f, 0f),
        new Keyframe(0.5f, 1f),
        new Keyframe(1f, 0f)
    );

    [Header("Levitation Settings")]
    [SerializeField] private float levitationHeight = 12f;
    [SerializeField] private float levitationSpeed = 2f;
    [SerializeField] private float levitationBlendSpeed = 3f;

    private Image img;
    private AudioSource audioSource;
    private Vector3 originalScale;
    private Vector3 originalPosition;

    private int currentTapCount;
    private float lastTapTime;
    private bool isBusy;
    private bool breathReversed;

    private float levitationTime = 0f;
    private float levitationWeight = 0f;

    private Coroutine breathRoutine;
    private Coroutine levitationRoutine;
    private Coroutine blendRoutine;

    private bool AffectX => (breathAxis & StretchAxis.X) != 0;
    private bool AffectY => (breathAxis & StretchAxis.Y) != 0;
    private bool AffectZ => (breathAxis & StretchAxis.Z) != 0;

    private void Awake()
    {
        img = GetComponent<Image>();
        audioSource = GetComponent<AudioSource>();
        originalScale = transform.localScale;
        originalPosition = transform.localPosition;

        if (idleSprite != null)
            img.sprite = idleSprite;

        milestones.Sort((a, b) => a.tapsRequired.CompareTo(b.tapsRequired));
    }

    private void OnEnable()
    {
        if (useBreathing)
            breathRoutine = StartCoroutine(BreathingLoop());

        if (useLevitating)
        {
            levitationRoutine = StartCoroutine(LevitationLoop());
            StartCoroutine(BlendLevitation(1f));
        }
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        transform.localScale = originalScale;
        transform.localPosition = originalPosition;
        levitationWeight = 0f;
    }

    //Tap Handling

    public void OnPointerClick(PointerEventData eventData)
    {
        if (isBusy) return;

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
        if (!enableTapTimeout) return;
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

    // Reactions 

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

    private IEnumerator AnimateReaction(float time, float stretch)
    {
        float t = 0f;

        while (t < time)
        {
            t += Time.deltaTime;
            float easedP = Mathf.Sin((t / time) * Mathf.PI);
            transform.localScale = originalScale * (1f + easedP * (stretch - 1f));
            yield return null;
        }

        transform.localScale = originalScale;
    }

    //Breathing 

    private IEnumerator BreathingLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(breathDelay);

            if (!isBusy)
                yield return AnimateBreath();
        }
    }

    private IEnumerator AnimateBreath()
    {
        if (reverseBreathCurve)
            breathReversed = !breathReversed;

        float elapsed = 0f;

        while (elapsed < breathTime)
        {
            elapsed += Time.deltaTime;

            float t = breathReversed
                ? 1 - (elapsed / breathTime)
                : elapsed / breathTime;

            float curve = breathCurve.Evaluate(t);
            float scaleMultiplier = breathInitialScale +
                (curve * (breathMaxScale - breathInitialScale));

            ApplyBreathScale(scaleMultiplier);
            yield return null;
        }

        transform.localScale = originalScale;
    }

    private void ApplyBreathScale(float multiplier)
    {
        Vector3 modified = originalScale;
        if (AffectX) modified.x *= multiplier;
        if (AffectY) modified.y *= multiplier;
        if (AffectZ) modified.z *= multiplier;
        transform.localScale = modified;
    }

    // Levitation 

    private IEnumerator LevitationLoop()
    {
        while (true)
        {
            levitationTime += Time.deltaTime / levitationSpeed;

            float sine = Mathf.Sin(levitationTime * Mathf.PI * 2f);
            float offsetY = sine * levitationHeight * levitationWeight;

            transform.localPosition = new Vector3(
                originalPosition.x,
                originalPosition.y + offsetY,
                originalPosition.z);

            yield return null;
        }
    }

    private IEnumerator BlendLevitation(float target)
    {
        if (blendRoutine != null) StopCoroutine(blendRoutine);

        while (!Mathf.Approximately(levitationWeight, target))
        {
            levitationWeight = Mathf.MoveTowards(
                levitationWeight, target,
                levitationBlendSpeed * Time.deltaTime);
            yield return null;
        }

        levitationWeight = target;

        if (target == 0f)
            transform.localPosition = originalPosition;
    }
}