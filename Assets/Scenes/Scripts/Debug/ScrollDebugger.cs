using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ScrollDebugger : MonoBehaviour
{
    private ScrollRect scrollRect;
    private RectTransform content;
    private RectTransform viewport;
    private TMP_Text tmpText;

    private void Start()
    {
        
        scrollRect = GetComponentInParent<ScrollRect>();
        content = scrollRect.content;
        viewport = scrollRect.viewport;
        tmpText = GetComponentInChildren<TMP_Text>();

        if (scrollRect == null) Debug.LogError("[ScrollDebugger] No ScrollRect found!");
        if (content == null) Debug.LogError("[ScrollDebugger] No Content found!");
        if (viewport == null) Debug.LogError("[ScrollDebugger] No Viewport found!");
        if (tmpText == null) Debug.LogError("[ScrollDebugger] No TMP Text found!");

        StartCoroutine(PrintDebugInfo());
    }

    private System.Collections.IEnumerator PrintDebugInfo()
    {
        
        yield return null;
        yield return null;
        yield return null;

        LayoutRebuilder.ForceRebuildLayoutImmediate(content);

        yield return null;

        Debug.Log("========= SCROLL DEBUGGER =========");
        Debug.Log("ScrollRect GameObject: " + scrollRect.gameObject.name);
        Debug.Log("Content Height: " + content.rect.height);
        Debug.Log("Viewport Height: " + viewport.rect.height);
        Debug.Log("Content Anchors Min: " + content.anchorMin);
        Debug.Log("Content Anchors Max: " + content.anchorMax);
        Debug.Log("Content Pivot: " + content.pivot);
        Debug.Log("Content SizeDelta: " + content.sizeDelta);
        Debug.Log("TMP Text: " + (tmpText != null ? tmpText.text.Substring(0, Mathf.Min(40, tmpText.text.Length)) + "..." : "NULL"));
        Debug.Log("TMP Rect Height: " + (tmpText != null ? tmpText.rectTransform.rect.height.ToString() : "NULL"));
        Debug.Log("TMP Anchors Min: " + (tmpText != null ? tmpText.rectTransform.anchorMin.ToString() : "NULL"));
        Debug.Log("TMP Anchors Max: " + (tmpText != null ? tmpText.rectTransform.anchorMax.ToString() : "NULL"));
        Debug.Log("ContentSizeFitter: " + (content.GetComponent<ContentSizeFitter>() != null ? "Found ✓" : "MISSING ✗"));
        Debug.Log("VerticalLayoutGroup: " + (content.GetComponent<VerticalLayoutGroup>() != null ? "Found ✓" : "MISSING ✗"));
        Debug.Log("Can Scroll: " + (content.rect.height > viewport.rect.height ? "YES ✓" : "NO ✗ — Content not tall enough!"));
        Debug.Log("===================================");
    }
}