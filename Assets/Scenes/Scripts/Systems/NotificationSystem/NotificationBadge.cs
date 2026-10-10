using UnityEngine;
using UnityEngine.UI;
using TMPro;

// =============================================================================
// NotificationBadge  NutriQuest
//
// Universal "new!" bubble. Put it on any button / icon / tab. It shows while a
// notification in NotificationService is active and hides when it is cleared.
//
// TWO MODES
//   This Notification   Shows while ONE specific ID is active.
//                       By default it is cleared when the player presses the
//                       button this component sits on.
//   Any Under Prefix    Parent / tab badge. Shows while ANY ID starting with the
//                       prefix is active (e.g. "recipe/" for a Recipes tab).
//                       Can show a count. Children clear themselves.
//
// SETUP
//   1. Add a child UI > Image named "Badge" (your red bubble sprite, anchored to
//      a corner). UNTICK Raycast Target so it never blocks taps.
//   2. Add this component to the button/icon.
//   3. Drag the Badge into the "Badge" slot, type the Notification ID.
//   4. Optional: fill "Watch PlayerPrefs Key" so it raises itself, e.g.
//      FirstClear_Tower1_Stage1 for a recipe unlock. Otherwise raise it from
//      code with NotificationService.Raise("your/id").
// =============================================================================
public class NotificationBadge : MonoBehaviour
{
    public enum BadgeMode { ThisNotification, AnyUnderPrefix }

    [Header("What to show")]
    [SerializeField] private BadgeMode mode = BadgeMode.ThisNotification;

    [Tooltip("This Notification: the exact ID (e.g. recipe/Tower1_Stage1).\n" +
             "Any Under Prefix: the prefix (e.g. recipe/).")]
    [SerializeField] private string notificationID;

    [Header("Drag & drop")]
    [Tooltip("The bubble object. Empty = auto-use a child named 'Badge'.")]
    [SerializeField] private GameObject badge;

    [Tooltip("Optional number label (shows how many are active). Leave empty to skip.")]
    [SerializeField] private TMP_Text countText;

    [Tooltip("Optional. Empty = Button on this GameObject.")]
    [SerializeField] private Button targetButton;

    [Header("Clearing (This Notification mode)")]
    [Tooltip("Clear the notification when the button is pressed.")]
    [SerializeField] private bool clearOnButtonClick = true;

    [Header("Auto-raise (optional, This Notification mode)")]
    [Tooltip("PlayerPrefs int key that means 'this is now new', e.g. FirstClear_Tower1_Stage1. " +
             "Leave empty if you raise it from code.")]
    [SerializeField] private string watchPlayerPrefsKey;
    [SerializeField] private int watchValue = 1;

    [Header("Animation (optional)")]
    [SerializeField] private bool pulse = true;
    [SerializeField] private float pulseSpeed = 4f;
    [SerializeField] private float pulseAmount = 0.1f;

    private Vector3 baseScale = Vector3.one;

    private void Awake()
    {
        if (targetButton == null) targetButton = GetComponent<Button>();

        if (badge == null)
        {
            Transform child = transform.Find("Badge");
            if (child != null) badge = child.gameObject;
        }
        if (badge != null) baseScale = badge.transform.localScale;

        if (targetButton != null && clearOnButtonClick && mode == BadgeMode.ThisNotification)
            targetButton.onClick.AddListener(Dismiss);
    }

    private void OnDestroy()
    {
        if (targetButton != null) targetButton.onClick.RemoveListener(Dismiss);
    }

    private void OnEnable()
    {
        NotificationService.OnChanged += HandleChanged;
        Refresh();
    }

    private void OnDisable()
    {
        NotificationService.OnChanged -= HandleChanged;
    }

    private void Update()
    {
        if (!pulse || badge == null || !badge.activeSelf) return;
        badge.transform.localScale =
            baseScale * (1f + Mathf.Sin(Time.unscaledTime * pulseSpeed) * pulseAmount);
    }

    private void HandleChanged(string changedID) => UpdateVisual();

    /// <summary>Syncs with the watched PlayerPrefs flag (if any), then updates the bubble.</summary>
    public void Refresh()
    {
        if (mode == BadgeMode.ThisNotification && !string.IsNullOrEmpty(watchPlayerPrefsKey))
            NotificationService.SyncWithFlag(notificationID,
                PlayerPrefs.GetInt(watchPlayerPrefsKey, 0) == watchValue);

        UpdateVisual();
    }

    /// <summary>Clears this notification. Also usable from any UnityEvent.</summary>
    public void Dismiss()
    {
        if (mode == BadgeMode.ThisNotification) NotificationService.Clear(notificationID);
    }

    private void UpdateVisual()
    {
        if (badge == null) return;

        int count;
        if (mode == BadgeMode.ThisNotification)
            count = NotificationService.IsActive(notificationID) ? 1 : 0;
        else
            count = NotificationService.CountActive(notificationID ?? "");

        badge.SetActive(count > 0);
        if (count > 0) badge.transform.localScale = baseScale;
        if (countText != null) countText.text = count > 0 ? count.ToString() : "";
    }
}
