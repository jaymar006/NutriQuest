using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class AutoScrollText : MonoBehaviour
{
    [Header("Scroll Settings")]
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private float scrollSpeed = 0.15f;
    [SerializeField] private float pauseAtBottom = 1.5f;
    [SerializeField] private float pauseAtTop = 1.5f;
    [SerializeField] private bool loop = true;

    private bool isScrolling = false;

    private void Awake()
    {
        // Auto-grab ScrollRect if not assigned //
        if (scrollRect == null)
            scrollRect = GetComponent<ScrollRect>();

        if (scrollRect == null)
        {
            Debug.LogError("[AutoScrollText] No ScrollRect found!");
            return;
        }
    }

    private void OnEnable()
    {
        if (scrollRect != null)
            StartCoroutine(ForceSetupAndScroll());
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        isScrolling = false;
    }

    // Force-fix the entire scroll setup at runtime //
    private IEnumerator ForceSetupAndScroll()
    {
        isScrolling = false;

        RectTransform content = scrollRect.content;
        RectTransform viewport = scrollRect.viewport;
        TMP_Text tmp = content.GetComponentInChildren<TMP_Text>();

        if (tmp == null)
        {
            Debug.LogError("[AutoScrollText] No TMP Text found inside Content!");
            yield break;
        }

        // Force TMP RectTransform to stretch correctly //
        RectTransform tmpRT = tmp.rectTransform;
        tmpRT.anchorMin = new Vector2(0f, 1f);
        tmpRT.anchorMax = new Vector2(1f, 1f);
        tmpRT.pivot = new Vector2(0.5f, 1f);
        tmpRT.offsetMin = new Vector2(10f, tmpRT.offsetMin.y);
        tmpRT.offsetMax = new Vector2(-10f, 0f);
        tmpRT.anchoredPosition = new Vector2(0f, 0f);

        // Force TMP settings //
        tmp.enableWordWrapping = true;
        tmp.overflowMode = TextOverflowModes.Overflow;

        // Force Content RectTransform //
        content.anchorMin = new Vector2(0f, 1f);
        content.anchorMax = new Vector2(1f, 1f);
        content.pivot = new Vector2(0.5f, 1f);
        content.anchoredPosition = new Vector2(0f, 0f);

        // Force add ContentSizeFitter if missing //
        ContentSizeFitter csf = content.GetComponent<ContentSizeFitter>();
        if (csf == null)
            csf = content.gameObject.AddComponent<ContentSizeFitter>();

        csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // Force add VerticalLayoutGroup if missing //
        VerticalLayoutGroup vlg = content.GetComponent<VerticalLayoutGroup>();
        if (vlg == null)
            vlg = content.gameObject.AddComponent<VerticalLayoutGroup>();

        vlg.padding = new RectOffset(10, 10, 10, 10);
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;

        // Wait several frames for everything to rebuild //
        yield return null;
        yield return null;
        yield return null;

        LayoutRebuilder.ForceRebuildLayoutImmediate(content);

        yield return null;
        yield return null;

        // Force ScrollRect settings //
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;

        // Reset scroll to top //
        scrollRect.verticalNormalizedPosition = 1f;

        yield return null;

        float contentHeight = content.rect.height;
        float viewportHeight = viewport.rect.height;

        Debug.Log("[AutoScrollText] Content: " + contentHeight + " | Viewport: " + viewportHeight);

        if (contentHeight <= viewportHeight)
        {
            Debug.LogWarning("[AutoScrollText] Content still not tall enough to scroll. " +
                             "Content: " + contentHeight + " | Viewport: " + viewportHeight);
            yield break;
        }

        Debug.Log("[AutoScrollText] Setup complete — starting scroll!");
        StartAutoScroll();
    }

    public void StartAutoScroll()
    {
        if (isScrolling) return;
        StopAllCoroutines();
        StartCoroutine(AutoScrollLoop());
    }

    public void StopAutoScroll()
    {
        StopAllCoroutines();
        isScrolling = false;
    }

    // Main loop — scroll down, pause, scroll up, pause, repeat //
    private IEnumerator AutoScrollLoop()
    {
        isScrolling = true;

        while (true)
        {
            yield return ScrollTo(0f);
            yield return new WaitForSeconds(pauseAtBottom);

            yield return ScrollTo(1f);
            yield return new WaitForSeconds(pauseAtTop);

            if (!loop)
            {
                isScrolling = false;
                yield break;
            }
        }
    }

    // Smoothly scroll to normalized position (1=top, 0=bottom) //
    private IEnumerator ScrollTo(float target)
    {
        while (Mathf.Abs(scrollRect.verticalNormalizedPosition - target) > 0.001f)
        {
            scrollRect.verticalNormalizedPosition = Mathf.MoveTowards(
                scrollRect.verticalNormalizedPosition,
                target,
                scrollSpeed * Time.deltaTime
            );
            yield return null;
        }

        scrollRect.verticalNormalizedPosition = target;
    }
}