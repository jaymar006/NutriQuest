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
        public UnityEvent onMilestoneTriggered;
    }

    [Header("Sprites")]
    [SerializeField] private Sprite idleSprite;
    [SerializeField] private Sprite tapSprite;

    [Header("Idle Animations")]
    [SerializeField] private bool useBreathing = true;
    [SerializeField] private bool useLevitating = false;

    [Header("Breathing")]
    [SerializeField] private float breathDelay = 2f;
    [SerializeField] private float breathDuration = 0.8f;
    [SerializeField] private float breathScale = 1.05f;

    [Header("Levitation")]
    [SerializeField] private float levitationHeight = 12f;
    [SerializeField] private float levitationSpeed = 2f;

    [Header("Tap Reaction")]
    [SerializeField] private float reactionDuration = 0.35f;
    [SerializeField] private float reactionScale = 1.3f;
    [SerializeField] private float tapResetDelay = 3f;

    [Header("Audio")]
    [SerializeField] private AudioClip tapSound;
    [Range(0f, 1f)]
    [SerializeField] private float volume = 1f;

    [Header("Milestones")]
    [SerializeField] private bool enableMilestones = true;
    [SerializeField] private List<TapMilestone> milestones = new List<TapMilestone>();

    private Image img;
    private AudioSource audioSource;
    private Vector3 originalScale;
    private Vector3 originalPosition;

    private int tapCount;
    private float lastTapTime;
    private bool isBusy;
    private float levitationTime;

    // Cached to avoid per-frame Vector3 allocs; single transform writer rule:
    // Update owns localPosition (levitation) while coroutines own localScale.
    // Update already skips when isBusy, BreathingLoop skips while isBusy — kept. //
    private static readonly Vector3 Up = Vector3.up;
    private Vector3 _levitationOffset;
    private Vector3 _breathScale;

    private void Awake()
    {
        if (!Application.isPlaying) return;

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
        if (!Application.isPlaying) return;
        StartIdleAnimations();
    }

    private void OnDisable()
    {
        if (!Application.isPlaying) return;
        StopAllCoroutines();
        transform.localScale = originalScale;
        transform.localPosition = originalPosition;
    }

    private void Update()
    {
        if (!Application.isPlaying) return;
        // Only one transform writer active: levitation (position) yields while busy;
        // breathing/reactions (scale) run in coroutines and skip while busy. //
        if (!useLevitating || isBusy) return;

        levitationTime += Time.deltaTime * levitationSpeed;
        float offset = Mathf.Sin(levitationTime) * levitationHeight;
        _levitationOffset = Up * offset;
        transform.localPosition = originalPosition + _levitationOffset;
    }

    private void StartIdleAnimations()
    {
        StopAllCoroutines();
        if (useBreathing) StartCoroutine(BreathingLoop());
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!Application.isPlaying) return;
        if (isBusy) return;

        if (Time.time - lastTapTime > tapResetDelay)
            tapCount = 0;

        tapCount++;
        lastTapTime = Time.time;

        TapMilestone milestone = GetMilestone(tapCount);

        if (enableMilestones && milestone != null)
            StartCoroutine(MilestoneReaction(milestone));
        else
            StartCoroutine(NormalReaction());
    }

    private TapMilestone GetMilestone(int count)
    {
        foreach (TapMilestone m in milestones)
            if (m.tapsRequired == count) return m;
        return null;
    }

    private IEnumerator NormalReaction()
    {
        isBusy = true;

        if (tapSprite != null) img.sprite = tapSprite;
        if (tapSound != null) audioSource.PlayOneShot(tapSound, volume);

        yield return ScaleAnimation(reactionDuration, reactionScale);

        if (idleSprite != null) img.sprite = idleSprite;
        isBusy = false;
    }

    private IEnumerator MilestoneReaction(TapMilestone milestone)
    {
        isBusy = true;

        if (milestone.milestoneSprite != null) img.sprite = milestone.milestoneSprite;
        if (milestone.milestoneSound != null)
            audioSource.PlayOneShot(milestone.milestoneSound, volume);
        milestone.onMilestoneTriggered?.Invoke();

        yield return ScaleAnimation(reactionDuration * 1.5f, reactionScale * 1.2f);

        if (idleSprite != null) img.sprite = idleSprite;
        isBusy = false;
    }

    private IEnumerator ScaleAnimation(float duration, float targetScale)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Sin((elapsed / duration) * Mathf.PI);
            transform.localScale = originalScale * (1f + t * (targetScale - 1f));
            yield return null;
        }
        transform.localScale = originalScale;
    }

    private IEnumerator BreathingLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(breathDelay);
            if (isBusy) continue;

            float elapsed = 0f;
            while (elapsed < breathDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Sin((elapsed / breathDuration) * Mathf.PI);
                float scale = 1f + t * (breathScale - 1f);
                _breathScale.x = originalScale.x;
                _breathScale.y = originalScale.y * scale;
                _breathScale.z = originalScale.z;
                transform.localScale = _breathScale;
                yield return null;
            }

            transform.localScale = originalScale;
        }
    }
}