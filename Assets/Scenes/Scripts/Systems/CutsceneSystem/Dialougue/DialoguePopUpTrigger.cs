using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using Gameplay.CutsceneManager;

// ---------------------------------------------------------------------------
// DialoguePopupTrigger
//
// Bridges a short "popup" dialogue (e.g. "You don't have enough rune keys!")
// to any button/UnityEvent — including LevelInfoScreen's onNotEnoughKeys and
// TapCounterTrigger's onTapThresholdReached, since TriggerDialogue() below
// has the exact no-argument signature both of those (and any Button.onClick)
// expect.
//
// Flow:
//   1. TriggerDialogue() is called (hook it up to a button, onNotEnoughKeys,
//      a TapCounterTrigger event, whatever).
//   2. This activates popupRoot and calls dialogueManager.StartDialogue().
//   3. It then watches dialogueManager.dialogueFinished every frame.
//   4. The instant it flips true, popupRoot is deactivated automatically.
//
// SETUP REQUIRED on the DialogueManager INSIDE popupRoot (not a code change,
// just Inspector settings on that specific popup's DialogueManager):
//   - Set "Auto Advance On End" to FALSE. That setting is meant for
//     full-scene cutscenes that load a new scene when dialogue ends — a
//     popup like this should just quietly finish, not try to load a scene.
//   - "Auto Start On Awake" can stay on or off; it doesn't matter, since
//     this script explicitly calls StartDialogue() itself every time the
//     popup opens (Unity's Start() only auto-fires once per GameObject
//     lifetime, so relying on it alone would only work the very first time
//     the popup was shown).
// ---------------------------------------------------------------------------
public class DialoguePopupTrigger : MonoBehaviour
{
    [Header("Popup")]
    [Tooltip("The GameObject to show while this dialogue plays, then auto-hide once it finishes. " +
             "Usually the same object the DialogueManager below lives on, or its parent panel.")]
    [SerializeField] private GameObject popupRoot;

    [Tooltip("The DialogueManager that lives on/under popupRoot and plays this popup's line(s).")]
    [SerializeField] private Gameplay.CutsceneManager.DialogueManager dialogueManager;

    [Header("Optional")]
    [Tooltip("Fires once the popup has fully closed, in case something else needs to react " +
             "(e.g. re-enabling a button behind it). Leave empty if unused.")]
    [SerializeField] private UnityEvent onPopupClosed;

    // Guards against re-triggering while the popup is already open/playing —
    // same pattern as isNavigating in LevelInfoScreen and isAdvancing in
    // DialogueManager, so a double-tap on the triggering button can't stack
    // a second StartDialogue() call on top of the one already running.
    private bool isShowing = false;
    private Coroutine watchCoroutine;

    // The single public entry point — hook this into:
    //   - a Button's OnClick
    //   - LevelInfoScreen's "On Not Enough Keys" UnityEvent
    //   - TapCounterTrigger's "On Tap Threshold Reached" UnityEvent
    public void TriggerDialogue()
    {
        if (isShowing)
            return;

        if (popupRoot == null || dialogueManager == null)
        {
            Debug.LogWarning("[DialoguePopupTrigger] popupRoot or dialogueManager not assigned on " + gameObject.name);
            return;
        }

        isShowing = true;

        popupRoot.SetActive(true);
        dialogueManager.StartDialogue();

        if (watchCoroutine != null)
            StopCoroutine(watchCoroutine);
        watchCoroutine = StartCoroutine(WatchForDialogueEnd());
    }

    private IEnumerator WatchForDialogueEnd()
    {
        yield return new WaitUntil(() => dialogueManager.dialogueFinished);

        popupRoot.SetActive(false);
        isShowing = false;
        watchCoroutine = null;

        onPopupClosed?.Invoke();
    }
}