using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// Builds the admin panel's uGUI hierarchy in code.
///
/// The panel is assembled entirely at runtime rather than shipped as a prefab
/// so that it needs no scene or prefab authoring, and so that every string can
/// be re-rendered in the other language without duplicating objects. The rest
/// of the game localizes its UI by duplicating GameObjects per language
/// (TOWERMODALS ENGLISH / TAGALOG and friends), which does not scale to a
/// table-heavy panel like this one.
///
/// The factory helpers here are deliberately small and dumb: they create one
/// element each and return it. Layout is driven by LayoutGroups so rows can
/// be added and removed at runtime without recalculating anchors.
/// </summary>
public static class AdminUi
{
    public static readonly Color Background = new Color(0.07f, 0.08f, 0.11f, 0.98f);
    public static readonly Color Panel = new Color(0.12f, 0.14f, 0.19f, 1f);
    public static readonly Color Card = new Color(0.17f, 0.19f, 0.25f, 1f);
    public static readonly Color CardAlt = new Color(0.21f, 0.24f, 0.31f, 1f);
    public static readonly Color Accent = new Color(0.24f, 0.68f, 0.44f, 1f);
    public static readonly Color AccentDim = new Color(0.16f, 0.42f, 0.28f, 1f);
    public static readonly Color Danger = new Color(0.78f, 0.31f, 0.31f, 1f);
    public static readonly Color Text = new Color(0.92f, 0.94f, 0.97f, 1f);
    public static readonly Color Muted = new Color(0.60f, 0.65f, 0.72f, 1f);
    public static readonly Color Field = new Color(0.10f, 0.11f, 0.15f, 1f);

    public const int FontSmall = 20;
    public const int FontBody = 24;
    public const int FontHeading = 30;
    public const int FontTitle = 38;

    private static TMP_FontAsset s_font;

    /// <summary>The TMP font used by the panel.</summary>
    public static TMP_FontAsset Font
    {
        get
        {
            if (s_font == null)
            {
                s_font = TMP_Settings.defaultFontAsset;
                if (s_font == null)
                {
                    s_font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
                }
                if (s_font == null)
                {
                    Debug.LogWarning("[AdminUi] No TMP font asset found; panel text may not render.");
                }
            }
            return s_font;
        }
    }

    // ------------------------------------------------------------------
    // Structure
    // ------------------------------------------------------------------

    /// <summary>Creates a screen-space overlay canvas that renders above the game.</summary>
    public static Canvas CreateCanvas(string name, int sortingOrder)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        Canvas canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortingOrder;

        CanvasScaler scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        go.AddComponent<GraphicRaycaster>();
        return canvas;
    }

    public static RectTransform CreateRect(Transform parent, string name)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        RectTransform rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        rt.localScale = Vector3.one;
        return rt;
    }

    /// <summary>
    /// Destroys an object, picking the call that Unity allows.
    ///
    /// Outside play mode Unity refuses Object.Destroy and asks for
    /// Object.DestroyImmediate instead. Routing every destroy through here
    /// means the panel can be built and torn down by the UI audit tool
    /// without entering play mode.
    /// </summary>
    public static void SafeDestroy(UnityEngine.Object target)
    {
        if (target == null) return;

        if (Application.isPlaying) UnityEngine.Object.Destroy(target);
        else UnityEngine.Object.DestroyImmediate(target);
    }

    public static Image CreatePanel(Transform parent, string name, Color color)
    {
        RectTransform rt = CreateRect(parent, name);
        Image image = rt.gameObject.AddComponent<Image>();
        image.color = color;
        return image;
    }

    // ------------------------------------------------------------------
    // Text
    // ------------------------------------------------------------------

    public static TMP_Text CreateText(Transform parent, string name, string value,
                                      int fontSize, TextAlignmentOptions align, Color color)
    {
        RectTransform rt = CreateRect(parent, name);
        TMP_Text text = rt.gameObject.AddComponent<TextMeshProUGUI>();
        if (Font != null) text.font = Font;
        text.fontSize = fontSize;
        text.alignment = align;
        text.color = color;
        text.raycastTarget = false;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.text = value;

        // A label inside a layout group needs a height or the group gives it
        // zero size. Harmless where the text is positioned by anchors.
        LayoutElement layout = EnsureLayout(rt.gameObject);
        layout.minHeight = 60f;
        layout.preferredHeight = 60f;
        layout.flexibleWidth = 1f;

        return text;
    }

    // ------------------------------------------------------------------
    // Layout helpers
    // ------------------------------------------------------------------

    /// <summary>
    /// Returns the LayoutElement on an object, adding one if there is none.
    ///
    /// Always use this instead of AddComponent&lt;LayoutElement&gt;(). A second
    /// LayoutElement on the same object is ignored by Unity's layout system
    /// (GetComponent returns the first), so the width a caller sets would be
    /// silently dropped. And a LayoutElement with no height collapses the
    /// element to zero size inside a layout group, which makes it invisible
    /// and unclickable.
    /// </summary>
    public static LayoutElement EnsureLayout(GameObject go)
    {
        LayoutElement element = go.GetComponent<LayoutElement>();
        if (element == null) element = go.AddComponent<LayoutElement>();
        return element;
    }

    /// <summary>Sets a width on an element while keeping the height it already has.</summary>
    public static LayoutElement SetWidth(GameObject go, float width)
    {
        LayoutElement element = EnsureLayout(go);
        element.preferredWidth = width;
        return element;
    }

    /// <summary>Sets the size of an element, keeping the width it already has.</summary>
    public static LayoutElement SetHeight(GameObject go, float height)
    {
        LayoutElement element = EnsureLayout(go);
        element.minHeight = height;
        element.preferredHeight = height;
        return element;
    }

    // ------------------------------------------------------------------
    // Buttons
    // ------------------------------------------------------------------

    public static Button CreateButton(Transform parent, string name, string label,
                                      UnityAction onClick, Color? background = null, int fontSize = FontBody)
    {
        Image bg = CreatePanel(parent, name, background ?? Card);
        Button button = bg.gameObject.AddComponent<Button>();
        button.targetGraphic = bg;

        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1.15f, 1.15f, 1.15f, 1f);
        colors.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
        colors.selectedColor = Color.white;
        colors.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
        colors.fadeDuration = 0.06f;
        button.colors = colors;

        if (onClick != null) button.onClick.AddListener(onClick);

        TMP_Text text = CreateText(bg.transform, "Label", label, fontSize,
                                   TextAlignmentOptions.Center, Text);
        Stretch(text.rectTransform, 10f, 6f);

        // A button inside a layout group needs a height, otherwise the layout
        // group gives it zero size and it cannot be seen or clicked.
        LayoutElement layout = EnsureLayout(bg.gameObject);
        layout.minHeight = 64f;
        layout.preferredHeight = 64f;
        layout.preferredWidth = 320f;
        layout.flexibleWidth = 0f;

        return button;
    }

    // ------------------------------------------------------------------
    // Input fields
    // ------------------------------------------------------------------

    /// <summary>
    /// Creates a TMP_InputField. Multiline fields grow to fit their content
    /// so that long recipe bodies stay readable.
    /// </summary>
    public static TMP_InputField CreateInput(Transform parent, string name, string value,
                                             bool multiline, int fontSize = FontBody)
    {
        Image bg = CreatePanel(parent, name, Field);
        TMP_InputField input = bg.gameObject.AddComponent<TMP_InputField>();

        RectTransform area = CreateRect(bg.transform, "TextArea");
        area.anchorMin = Vector2.zero;
        area.anchorMax = Vector2.one;
        area.offsetMin = new Vector2(12f, 6f);
        area.offsetMax = new Vector2(-12f, -6f);
        area.gameObject.AddComponent<RectMask2D>();

        TMP_Text text = CreateText(area, "Text", value, fontSize,
                                   multiline ? TextAlignmentOptions.TopLeft : TextAlignmentOptions.Left, Text);
        Stretch(text.rectTransform, 0f, 0f);
        text.textWrappingMode = multiline ? TextWrappingModes.Normal : TextWrappingModes.NoWrap;
        text.richText = false;

        TMP_Text placeholder = CreateText(area, "Placeholder", "", fontSize,
                                          multiline ? TextAlignmentOptions.TopLeft : TextAlignmentOptions.Left, Muted);
        Stretch(placeholder.rectTransform, 0f, 0f);
        placeholder.textWrappingMode = multiline ? TextWrappingModes.Normal : TextWrappingModes.NoWrap;

        input.textViewport = area;
        input.textComponent = text;
        input.placeholder = placeholder;
        input.lineType = multiline ? TMP_InputField.LineType.MultiLineNewline : TMP_InputField.LineType.SingleLine;
        input.contentType = TMP_InputField.ContentType.Standard;
        input.characterLimit = multiline ? 4000 : 200;
        if (Font != null) input.fontAsset = Font;
        input.text = value;

        if (multiline)
        {
            ContentSizeFitter fitter = text.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }

        LayoutElement layout = EnsureLayout(bg.gameObject);
        layout.minHeight = multiline ? 150f : 64f;
        layout.preferredHeight = multiline ? 150f : 64f;
        layout.preferredWidth = 320f;
        layout.flexibleWidth = 1f;

        return input;
    }

    // ------------------------------------------------------------------
    // Toggles
    // ------------------------------------------------------------------

    public static Toggle CreateToggle(Transform parent, string name, bool value, string label = null)
    {
        Image bg = CreatePanel(parent, name, Field);
        Toggle toggle = bg.gameObject.AddComponent<Toggle>();
        toggle.targetGraphic = bg;

        Image check = CreatePanel(bg.transform, "Checkmark", Accent);
        RectTransform checkRect = check.rectTransform;
        checkRect.anchorMin = new Vector2(0.5f, 0.5f);
        checkRect.anchorMax = new Vector2(0.5f, 0.5f);
        checkRect.pivot = new Vector2(0.5f, 0.5f);
        checkRect.anchoredPosition = new Vector2(0f, 0f);
        checkRect.sizeDelta = new Vector2(46f, 46f);
        toggle.graphic = check;
        toggle.isOn = value;

        if (!string.IsNullOrEmpty(label))
        {
            TMP_Text text = CreateText(bg.transform, "Label", label, FontBody,
                                       TextAlignmentOptions.MidlineLeft, Text);
            RectTransform textRect = text.rectTransform;
            textRect.anchorMin = new Vector2(0f, 0f);
            textRect.anchorMax = new Vector2(1f, 1f);
            textRect.offsetMin = new Vector2(70f, 0f);
            textRect.offsetMax = new Vector2(-10f, 0f);
        }

        LayoutElement layout = EnsureLayout(bg.gameObject);
        layout.minHeight = 56f;
        layout.preferredHeight = 56f;
        layout.preferredWidth = 80f;
        layout.flexibleWidth = 0f;

        return toggle;
    }

    // ------------------------------------------------------------------
    // Scroll views and layout
    // ------------------------------------------------------------------

    /// <summary>
    /// Creates a scroll view and hands back its content transform. Pass
    /// horizontal = true for a row of tabs; the content then uses a horizontal
    /// layout group and grows along X instead of Y.
    /// </summary>
    public static ScrollRect CreateScrollView(Transform parent, string name, out RectTransform content, bool horizontal = false)
    {
        Image bg = CreatePanel(parent, name, Color.clear);
        ScrollRect scroll = bg.gameObject.AddComponent<ScrollRect>();

        // The scroll view fills whatever it is put into. Callers that want it
        // somewhere specific override the rect afterwards.
        Stretch(bg.rectTransform);

        RectTransform viewport = CreateRect(bg.transform, "Viewport");
        viewport.anchorMin = Vector2.zero;
        viewport.anchorMax = Vector2.one;
        viewport.offsetMin = Vector2.zero;
        viewport.offsetMax = Vector2.zero;
        viewport.gameObject.AddComponent<RectMask2D>();

        content = CreateRect(viewport, "Content");
        content.anchorMin = new Vector2(0f, 0f);
        content.anchorMax = new Vector2(1f, 1f);
        content.pivot = new Vector2(0.5f, 1f);
        content.offsetMin = new Vector2(0f, 0f);
        content.offsetMax = new Vector2(0f, 0f);

        if (horizontal)
        {
            HorizontalLayoutGroup group = content.gameObject.AddComponent<HorizontalLayoutGroup>();
            group.spacing = 8f;
            group.padding = new RectOffset(4, 4, 4, 4);
            group.childControlWidth = true;
            group.childControlHeight = true;
            group.childForceExpandWidth = false;
            group.childForceExpandHeight = false;
            group.childAlignment = TextAnchor.MiddleLeft;
        }
        else
        {
            VerticalLayoutGroup group = content.gameObject.AddComponent<VerticalLayoutGroup>();
            group.spacing = 10f;
            group.padding = new RectOffset(14, 14, 14, 14);
            group.childControlWidth = true;
            group.childControlHeight = true;
            group.childForceExpandWidth = true;
            group.childForceExpandHeight = false;
        }

        ContentSizeFitter fitter = content.gameObject.AddComponent<ContentSizeFitter>();
        if (horizontal)
        {
            fitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        }
        else
        {
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }

        scroll.viewport = viewport;
        scroll.content = content;
        scroll.horizontal = horizontal;
        scroll.vertical = !horizontal;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 40f;

        return scroll;
    }

    /// <summary>Stretches a rect to fill its parent with the given padding.</summary>
    public static void Stretch(RectTransform rt, float horizontalPadding = 0f, float verticalPadding = 0f)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(horizontalPadding, verticalPadding);
        rt.offsetMax = new Vector2(-horizontalPadding, -verticalPadding);
    }

    /// <summary>Sets the layout size hints used by the parent layout group.</summary>
    public static LayoutElement SetLayout(GameObject go, float minHeight, float preferredHeight = -1f)
    {
        LayoutElement element = go.GetComponent<LayoutElement>();
        if (element == null) element = go.AddComponent<LayoutElement>();
        element.minHeight = minHeight;
        element.preferredHeight = preferredHeight >= 0f ? preferredHeight : minHeight;
        element.flexibleWidth = 1f;
        return element;
    }
}
