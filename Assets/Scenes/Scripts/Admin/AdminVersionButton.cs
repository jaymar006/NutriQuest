using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The version label that opens the teacher/parent admin panel.
///
/// This lives as a real GameObject in the MainMenu scene (next to the
/// "Settings" heading) rather than being injected at runtime, so it is
/// visible in the Editor and can be moved or restyled like any other UI.
///
/// The panel is hidden from normal players: a single tap does nothing. The
/// teacher has to tap the version several times in a row, and only then does
/// the PIN prompt appear.
///
/// The label text is refreshed from <see cref="AdminVersionService"/> whenever
/// the panel is enabled and whenever the displayed version changes, so it
/// always shows the current build version, content revision and the date the
/// content was last changed.
/// </summary>
public class AdminVersionButton : MonoBehaviour
{
    [Tooltip("How many taps in a row are needed to open the panel. One tap does nothing.")]
    [SerializeField] private int requiredTaps = 5;

    [Tooltip("The taps have to come this close together, in seconds.")]
    [SerializeField] private float tapWindowSeconds = 3f;

    [Tooltip("Show the date of the last content change under the version. " +
             "Off by default because the label is often only one line tall.")]
    [SerializeField] private bool showLastChangedDate;

    [Tooltip("Text to show. Leave empty to use the built-in version label.")]
    [SerializeField] private string versionLabel = "";

    [Tooltip("The TextMeshPro label that shows the version. Found automatically when empty.")]
    [SerializeField] private TMP_Text label;

    [Tooltip("The button that receives the tap. Found or created automatically when empty.")]
    [SerializeField] private Button button;

    private int _tapCount;
    private float _lastTapTime = -999f;

    private void Awake()
    {
        if (label == null) label = GetComponentInChildren<TMP_Text>(true);
        if (button == null) button = GetComponent<Button>();

        if (button == null)
        {
            // The title screen's version text has no Button on it, so add one
            // and make the text itself the target graphic.
            Image image = GetComponent<Image>();
            if (image == null) image = gameObject.AddComponent<Image>();
            image.color = new Color(1f, 1f, 1f, 0f);
            image.raycastTarget = true;

            button = gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(HandleTap);
        }
    }

    private void OnEnable()
    {
        AdminVersionService.OnDisplayVersionChanged += RefreshLabel;
        RefreshLabel();
    }

    private void OnDisable()
    {
        AdminVersionService.OnDisplayVersionChanged -= RefreshLabel;
    }

    /// <summary>Rewrites the label from the current version information.</summary>
    public void RefreshLabel()
    {
        if (label == null) label = GetComponentInChildren<TMP_Text>(true);
        if (label == null) return;

        label.text = string.IsNullOrEmpty(versionLabel)
            ? (showLastChangedDate ? AdminVersionService.LabelWithDate : AdminVersionService.Label)
            : versionLabel;
    }

    /// <summary>
    /// Counts a tap. The panel only opens once the required number of taps
    /// have come in a row, which keeps the panel hidden from players who
    /// happen to tap the version label.
    /// </summary>
    public void HandleTap()
    {
        if (!AdminSecurity.IsAdminEnabled) return;

        float now = Time.unscaledTime;
        if (now - _lastTapTime > tapWindowSeconds) _tapCount = 0;
        _lastTapTime = now;
        _tapCount++;

        if (_tapCount < requiredTaps) return;

        _tapCount = 0;
        OpenAdminPanel();
    }

    /// <summary>Called by the Button's OnClick. Opens the admin panel.</summary>
    public void OpenAdminPanel()
    {
        AdminPanelRoot.Open();
    }
}
