using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
public class BackgroundFitter : MonoBehaviour
{
    private RectTransform rectTransform;
    private CanvasScaler canvasScaler;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasScaler = GetComponentInParent<CanvasScaler>();

        SetFullStretchAnchors();
    }

    void Start()
    {
        FitBackground();
    }

    // Call this if screen rotates or resolution changes at runtime
    void OnRectTransformDimensionsChange()
    {
        FitBackground();
    }

    void SetFullStretchAnchors()
    {
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
    }

    void FitBackground()
    {
        if (canvasScaler == null) return;

        float referenceWidth = canvasScaler.referenceResolution.x;
        float referenceHeight = canvasScaler.referenceResolution.y;

        float screenAspect = (float)Screen.width / Screen.height;
        float referenceAspect = referenceWidth / referenceHeight;

        float extraWidth = 0f;
        float extraHeight = 0f;

        if (screenAspect > referenceAspect)
        {
            // Screen is wider — expand left and right
            extraWidth = (screenAspect - referenceAspect) * referenceHeight;
        }
        else if (screenAspect < referenceAspect)
        {
            // Screen is taller — expand top and bottom
            extraHeight = (referenceAspect - screenAspect) * referenceWidth;
        }

        rectTransform.offsetMin = new Vector2(-extraWidth / 2f, -extraHeight / 2f);
        rectTransform.offsetMax = new Vector2(extraWidth / 2f, extraHeight / 2f);
    }
}