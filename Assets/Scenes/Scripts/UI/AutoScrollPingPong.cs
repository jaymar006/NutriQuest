using UnityEngine;
using UnityEngine.UI;
using System.Collections;

// ---------------------------------------------------------------------------
// AutoScrollPingPong
//
// Attach to the ScrollView GameObject (or anywhere — just drag the ScrollRect
// in). Continuously scrolls the content down to the bottom, pauses, scrolls
// back up to the top, pauses, and repeats — a classic "idle instructions
// box" loop.
//
// Scroll speed is defined in PIXELS per second rather than a normalized
// fraction, so the pace feels the same regardless of how much text is in
// Content — a short paragraph and a long one both scroll at the same visual
// speed instead of the short one finishing awkwardly fast.
//
// IMPORTANT: This does NOT rely on OnEnable/OnDisable to know when it should
// be running, because it may live outside the modal's hierarchy (e.g. its
// own GameObject never gets deactivated even though the modal it scrolls
// closes). Instead, wire StartScrolling()/StopScrolling() to the modal's
// ModalWindowScript.onShown / onHidden UnityEvents in the Inspector:
//
//   ModalWindowScript (Inspector) -> Events
//     On Shown  -> [this component].StartScrolling()
//     On Hidden -> [this component].StopScrolling()
//
// OnEnable/OnDisable are kept as a safety net for the case where this
// component genuinely does sit on a child that gets deactivated with the
// modal — but autoStart defaults to false so it won't spin in the
// background just because its own GameObject happens to be active.
// ---------------------------------------------------------------------------
public class AutoScrollPingPong : MonoBehaviour
{
    [Header("Scroll Rect")]
    [Tooltip("The ScrollRect to auto-scroll. Auto-grabbed from this GameObject if left empty.")]
    [SerializeField] private ScrollRect scrollRect;

    [Header("Scroll Settings")]
    [Tooltip("Scroll speed in pixels per second.")]
    [SerializeField] private float scrollSpeed = 40f;

    [Tooltip("Seconds to pause once it reaches the bottom, before scrolling back up.")]
    [SerializeField] private float pauseAtBottom = 1f;

    [Tooltip("Seconds to pause once it reaches the top, before scrolling down again.")]
    [SerializeField] private float pauseAtTop = 1f;

    [Tooltip("Delay before the very first scroll starts, e.g. to let the panel finish opening.")]
    [SerializeField] private float startDelay = 0f;

    [Header("Behaviour")]
    [Tooltip("Start auto-scrolling automatically when this object becomes active. Leave OFF if a " +
             "ModalWindowScript's onShown/onHidden events are driving StartScrolling()/StopScrolling() " +
             "instead — otherwise it can start scrolling before the modal is actually visible.")]
    [SerializeField] private bool autoStart = false;

    private Coroutine loopCoroutine;
    private bool isPaused = false;

    private void OnEnable()
    {
        if (scrollRect == null)
            scrollRect = GetComponent<ScrollRect>();

        if (autoStart)
            StartScrolling();
    }

    private void OnDisable()
    {
        StopScrolling();
    }

    // -------------------------------------------------------------------------
    // Public controls — hook these up to ModalWindowScript.onShown / onHidden
    // (or call them from any other script/button) to pause/resume the loop.
    // -------------------------------------------------------------------------
    public void StartScrolling()
    {
        if (scrollRect == null)
            scrollRect = GetComponent<ScrollRect>();

        if (scrollRect == null)
        {
            Debug.LogWarning("[AutoScrollPingPong] No ScrollRect assigned/found on " + gameObject.name);
            return;
        }

        if (loopCoroutine != null)
            StopCoroutine(loopCoroutine);

        isPaused = false;
        loopCoroutine = StartCoroutine(ScrollLoop());
    }

    public void StopScrolling()
    {
        if (loopCoroutine != null)
        {
            StopCoroutine(loopCoroutine);
            loopCoroutine = null;
        }
    }

    public void PauseScrolling() => isPaused = true;
    public void ResumeScrolling() => isPaused = false;

    // -------------------------------------------------------------------------
    // Loop
    // -------------------------------------------------------------------------
    private IEnumerator ScrollLoop()
    {
        if (startDelay > 0f)
            yield return new WaitForSecondsRealtime(startDelay);

        // Start from the top every time this loop kicks off.
        scrollRect.verticalNormalizedPosition = 1f;

        while (true)
        {
            yield return ScrollTo(0f); // down to bottom
            yield return WaitRealtime(pauseAtBottom);

            yield return ScrollTo(1f); // back up to top
            yield return WaitRealtime(pauseAtTop);
        }
    }

    private IEnumerator ScrollTo(float targetNormalized)
    {
        float startNormalized = scrollRect.verticalNormalizedPosition;

        float contentHeight = scrollRect.content.rect.height;
        RectTransform viewportRect = scrollRect.viewport != null
            ? scrollRect.viewport
            : scrollRect.transform as RectTransform;
        float viewportHeight = viewportRect != null ? viewportRect.rect.height : 0f;

        // How many pixels separate fully-scrolled-up from fully-scrolled-down.
        // If content is shorter than the viewport there's nothing to scroll —
        // just sit still instead of doing a pointless instant "scroll".
        float scrollableDistance = contentHeight - viewportHeight;
        if (scrollableDistance <= 0f)
            yield break;

        float pixelDistance = Mathf.Abs(targetNormalized - startNormalized) * scrollableDistance;
        float duration = pixelDistance / Mathf.Max(scrollSpeed, 0.01f);

        float elapsed = 0f;
        while (elapsed < duration)
        {
            // Pausing (e.g. player manually grabbed the scrollbar) just
            // freezes progress in place rather than snapping or fighting them.
            if (isPaused)
            {
                yield return null;
                continue;
            }

            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            scrollRect.verticalNormalizedPosition = Mathf.Lerp(startNormalized, targetNormalized, t);
            yield return null;
        }

        scrollRect.verticalNormalizedPosition = targetNormalized;
    }

    private IEnumerator WaitRealtime(float seconds)
    {
        float elapsed = 0f;
        while (elapsed < seconds)
        {
            if (!isPaused)
                elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
    }
}