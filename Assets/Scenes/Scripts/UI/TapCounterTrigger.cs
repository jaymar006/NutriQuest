using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;

// ---------------------------------------------------------------------------
// TapCounterTrigger
//
// Attach this to any UI element (needs a Graphic component like Image with
// "Raycast Target" enabled, so clicks/taps register) or a world-space object
// with a Collider + Physics Raycaster on the camera.
//
// Counts taps and fires onTapThresholdReached once requiredTaps is reached.
// Drag ANY GameObject/component method into the UnityEvent below in the
// Inspector — e.g. drag your modal's GameObject in and pick
// GameObject.SetActive(true) from the dropdown to open it after N taps.
//
// Requires an EventSystem in the scene (Unity creates one automatically with
// any Canvas) — this is standard for any UGUI click/tap handling.
// ---------------------------------------------------------------------------
public class TapCounterTrigger : MonoBehaviour, IPointerClickHandler
{
    [Header("Tap Settings")]
    [Tooltip("Number of taps needed to fire the event.")]
    [SerializeField] private int requiredTaps = 5;

    [Tooltip("If a tap comes in later than this many seconds after the previous " +
             "one, the count resets back to 0. Set to 0 to disable the timeout " +
             "(taps count no matter how slowly/quickly they happen).")]
    [SerializeField] private float tapTimeoutSeconds = 0f;

    [Tooltip("Reset the counter back to 0 after firing, so it can be triggered again?")]
    [SerializeField] private bool resetAfterTrigger = true;

    [Tooltip("Only allow the event to fire once ever (per play session). Further taps do nothing after that.")]
    [SerializeField] private bool triggerOnlyOnce = false;

    [Header("Event")]
    [Tooltip("Drag any GameObject/component method here — e.g. a modal's " +
             "GameObject to call SetActive(true), or any other public method.")]
    [SerializeField] private UnityEvent onTapThresholdReached;

    [Header("Optional Progress Event")]
    [Tooltip("Optional: fires on every tap with the current tap count, if you " +
             "want to drive a counter label or partial animation. Leave empty if unused.")]
    [SerializeField] private UnityEvent<int> onTapProgress;

    private int currentTaps = 0;
    private float lastTapTime = -999f;
    private bool hasTriggeredOnce = false;

    public void OnPointerClick(PointerEventData eventData)
    {
        if (triggerOnlyOnce && hasTriggeredOnce)
            return;

        // Reset the streak if the player paused too long between taps.
        if (tapTimeoutSeconds > 0f && currentTaps > 0 &&
            Time.unscaledTime - lastTapTime > tapTimeoutSeconds)
        {
            currentTaps = 0;
        }

        currentTaps++;
        lastTapTime = Time.unscaledTime;

        onTapProgress?.Invoke(currentTaps);

        if (currentTaps >= requiredTaps)
        {
            hasTriggeredOnce = true;
            onTapThresholdReached?.Invoke();

            if (resetAfterTrigger)
                currentTaps = 0;
        }
    }

    // Call this externally (e.g. when a modal that this trigger opens gets
    // closed) if you want taps to start counting from 0 again on demand.
    public void ResetTaps()
    {
        currentTaps = 0;
        lastTapTime = -999f;
    }
}