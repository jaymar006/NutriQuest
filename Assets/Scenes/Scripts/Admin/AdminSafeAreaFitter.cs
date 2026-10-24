using UnityEngine;

/// <summary>
/// Keeps a rect inside the device's safe area.
///
/// Android phones with a notch or a gesture bar report a smaller safe area
/// than the screen. Anything drawn under the notch or the bar is either cut
/// off or triggers a system gesture, so the admin panel has to stay inside
/// it. The safe area also changes when the device is rotated, so it is
/// re-applied every frame.
///
/// The panel is built at runtime, so the fitter is added to the canvas that
/// the panel created.
/// </summary>
public class AdminSafeAreaFitter : MonoBehaviour
{
    [Tooltip("The rect to keep inside the safe area. Defaults to this object's own rect.")]
    [SerializeField] private RectTransform target;

    private void Awake()
    {
        if (target == null) target = transform as RectTransform;
    }

    private void Update()
    {
        Apply();
    }

    /// <summary>Re-applies the safe area. Safe to call at any time.</summary>
    public void Apply()
    {
        if (target == null) return;

        Canvas canvas = target.GetComponentInParent<Canvas>();
        if (canvas == null) return;

        Rect safe = Screen.safeArea;
        if (safe.width <= 0f || safe.height <= 0f) return;

        // The safe area is in screen pixels, the canvas in normalised units.
        float minX = safe.xMin / Screen.width;
        float minY = safe.yMin / Screen.height;
        float maxX = safe.xMax / Screen.width;
        float maxY = safe.yMax / Screen.height;

        target.anchorMin = new Vector2(minX, minY);
        target.anchorMax = new Vector2(maxX, maxY);
        target.offsetMin = Vector2.zero;
        target.offsetMax = Vector2.zero;
    }
}
