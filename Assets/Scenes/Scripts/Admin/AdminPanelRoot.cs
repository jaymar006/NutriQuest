using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// The teacher/parent admin panel.
///
/// The whole UI is built at runtime, so the feature needs no scene and no
/// prefab: <see cref="Open"/> creates the object, and closing it destroys it
/// again. The panel is persistent so that the Text tab can load a scene to
/// read its strings without the panel disappearing underneath, but it is
/// never a singleton the rest of the game depends on: the game keeps running
/// normally while the panel is open, and closing the panel tears it down.
///
/// Flow: the version label in the Settings panel calls
/// <see cref="Open"/>. The panel shows the PIN gate first. There is no
/// default PIN in the build, so the very first visit asks the teacher to
/// create one. After that, the correct PIN reveals the admin shell.
/// </summary>
public class AdminPanelRoot : MonoBehaviour
{
    private const int CanvasOrder = 5000;

    private Canvas _canvas;
    private RectTransform _shell;
    private RectTransform _gate;
    private RectTransform _content;
    private RectTransform _tabBar;
    private TMP_Text _toastText;
    private Coroutine _toastRoutine;

    private readonly List<AdminTabBase> _allTabs = new List<AdminTabBase>();
    private readonly List<AdminTabBase> _tabs = new List<AdminTabBase>();
    private AdminTabBase _currentTab;

    public static bool IsOpen => FindAnyObjectByType<AdminPanelRoot>() != null;

    // ------------------------------------------------------------------
    // Entry points
    // ------------------------------------------------------------------

    /// <summary>Opens the admin panel, or brings it to the front if it is already open.</summary>
    public static AdminPanelRoot Open()
    {
        AdminPanelRoot existing = FindAnyObjectByType<AdminPanelRoot>();
        if (existing != null)
        {
            existing.SetShellVisible(true);
            return existing;
        }

        GameObject go = new GameObject("[AdminPanel]");
        // The panel is persistent so the Text tab can load a scene to edit
        // its strings without the panel being destroyed underneath.
        DontDestroyOnLoad(go);
        AdminPanelRoot root = go.AddComponent<AdminPanelRoot>();
        root.Build();
        return root;
    }

    /// <summary>Closes and destroys the admin panel.</summary>
    public static void Close()
    {
        AdminPanelRoot existing = FindAnyObjectByType<AdminPanelRoot>();
        if (existing != null) Destroy(existing.gameObject);
    }

    // ------------------------------------------------------------------
    // Build
    // ------------------------------------------------------------------

    private void Awake()
    {
        AdminStrings.UseFilipino = LocalizationManager.Instance != null && LocalizationManager.Instance.IsFilipino;
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            Close();
        }
    }

    private void Build()
    {
        _canvas = AdminUi.CreateCanvas("[AdminCanvas]", CanvasOrder);

        // Full screen backdrop that also swallows clicks to the game behind.
        Image backdrop = AdminUi.CreatePanel(_canvas.transform, "Backdrop", AdminUi.Background);
        AdminUi.Stretch(backdrop.rectTransform);
        backdrop.raycastTarget = true;

        BuildShell(backdrop.transform);
        BuildGate(backdrop.transform);
        BuildToast(backdrop.transform);

        ShowGate();
    }

    private void BuildShell(Transform parent)
    {
        Image panel = AdminUi.CreatePanel(parent, "Shell", AdminUi.Panel);
        AdminUi.Stretch(panel.rectTransform);

        // Keep the panel inside the notch and the gesture bar.
        AdminSafeAreaFitter safeArea = panel.gameObject.AddComponent<AdminSafeAreaFitter>();
        safeArea.Apply();

        // ---- Header ----
        Image header = AdminUi.CreatePanel(panel.transform, "Header", AdminUi.Card);
        RectTransform headerRect = header.rectTransform;
        headerRect.anchorMin = new Vector2(0f, 1f);
        headerRect.anchorMax = new Vector2(1f, 1f);
        headerRect.pivot = new Vector2(0.5f, 1f);
        headerRect.offsetMin = new Vector2(0f, -170f);
        headerRect.offsetMax = new Vector2(0f, 0f);

        TMP_Text title = AdminUi.CreateText(header.transform, "Title", AdminStrings.Get("title"),
            AdminUi.FontTitle, TextAlignmentOptions.TopLeft, AdminUi.Text);
        RectTransform titleRect = title.rectTransform;
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.offsetMin = new Vector2(24f, -78f);
        titleRect.offsetMax = new Vector2(-24f, -18f);

        TMP_Text subtitle = AdminUi.CreateText(header.transform, "Subtitle", AdminStrings.Get("subtitle"),
            AdminUi.FontSmall, TextAlignmentOptions.TopLeft, AdminUi.Muted);
        RectTransform subtitleRect = subtitle.rectTransform;
        // Directly under the title, NOT at the bottom of the header: the
        // bottom band is taken by the version label and the language button.
        subtitleRect.anchorMin = new Vector2(0f, 1f);
        subtitleRect.anchorMax = new Vector2(1f, 1f);
        subtitleRect.pivot = new Vector2(0.5f, 1f);
        subtitleRect.offsetMin = new Vector2(24f, -104f);
        subtitleRect.offsetMax = new Vector2(-24f, -78f);

        TMP_Text version = AdminUi.CreateText(header.transform, "Version", AdminVersionService.Label,
            AdminUi.FontSmall, TextAlignmentOptions.BottomLeft, AdminUi.Muted);
        RectTransform versionRect = version.rectTransform;
        versionRect.anchorMin = new Vector2(0f, 0f);
        versionRect.anchorMax = new Vector2(0.55f, 0f);
        versionRect.pivot = new Vector2(0f, 0f);
        versionRect.offsetMin = new Vector2(24f, 12f);
        versionRect.offsetMax = new Vector2(0f, 44f);

        // ---- Header buttons ----
        TMP_Text languageCaption = AdminUi.CreateText(header.transform, "LanguageCaption",
            AdminStrings.Get("language"), AdminUi.FontSmall, TextAlignmentOptions.BottomRight, AdminUi.Muted);
        RectTransform captionRect = languageCaption.rectTransform;
        captionRect.anchorMin = new Vector2(1f, 0f);
        captionRect.anchorMax = new Vector2(1f, 0f);
        captionRect.pivot = new Vector2(1f, 0f);
        captionRect.anchoredPosition = new Vector2(-236f, 24f);
        captionRect.sizeDelta = new Vector2(200f, 40f);

        Button langButton = AdminUi.CreateButton(header.transform, "LanguageButton",
            AdminStrings.Get(AdminStrings.UseFilipino ? "english" : "filipino"),
            () => ToggleLanguage(), AdminUi.CardAlt, AdminUi.FontSmall);
        RectTransform langRect = langButton.GetComponent<RectTransform>();
        langRect.anchorMin = new Vector2(1f, 0f);
        langRect.anchorMax = new Vector2(1f, 0f);
        langRect.pivot = new Vector2(1f, 0f);
        langRect.anchoredPosition = new Vector2(-24f, 14f);
        langRect.sizeDelta = new Vector2(200f, 58f);

        Button closeButton = AdminUi.CreateButton(header.transform, "CloseButton", AdminStrings.Get("close"),
            () => Close(), AdminUi.Danger, AdminUi.FontSmall);
        RectTransform closeRect = closeButton.GetComponent<RectTransform>();
        closeRect.anchorMin = new Vector2(1f, 1f);
        closeRect.anchorMax = new Vector2(1f, 1f);
        closeRect.pivot = new Vector2(1f, 1f);
        closeRect.anchoredPosition = new Vector2(-24f, -20f);
        closeRect.sizeDelta = new Vector2(180f, 60f);

        // ---- Tab bar ----
        Image tabPanel = AdminUi.CreatePanel(panel.transform, "TabBar", AdminUi.CardAlt);
        RectTransform tabRect = tabPanel.rectTransform;
        tabRect.anchorMin = new Vector2(0f, 1f);
        tabRect.anchorMax = new Vector2(1f, 1f);
        tabRect.pivot = new Vector2(0.5f, 1f);
        tabRect.offsetMin = new Vector2(0f, -300f);
        tabRect.offsetMax = new Vector2(0f, -180f);

        RectTransform tabScrollContent;
        ScrollRect tabScroll = AdminUi.CreateScrollView(tabPanel.transform, "TabScroll", out tabScrollContent, true);
        RectTransform tabScrollRect = tabScroll.GetComponent<RectTransform>();
        tabScrollRect.anchorMin = Vector2.zero;
        tabScrollRect.anchorMax = Vector2.one;
        tabScrollRect.offsetMin = new Vector2(10f, 10f);
        tabScrollRect.offsetMax = new Vector2(-10f, -10f);

        _tabBar = tabScrollContent;

        // ---- Content ----
        Image contentPanel = AdminUi.CreatePanel(panel.transform, "Content", Color.clear);
        RectTransform contentRect = contentPanel.rectTransform;
        contentRect.anchorMin = Vector2.zero;
        contentRect.anchorMax = Vector2.one;
        contentRect.offsetMin = new Vector2(0f, 0f);
        contentRect.offsetMax = new Vector2(0f, -310f);

        AdminUi.CreateScrollView(contentPanel.transform, "ContentScroll", out _content);

        _shell = panel.rectTransform;
    }

    private void BuildGate(Transform parent)
    {
        Image gate = AdminUi.CreatePanel(parent, "Gate", new Color(0.04f, 0.05f, 0.07f, 0.99f));
        AdminUi.Stretch(gate.rectTransform);
        gate.raycastTarget = true;

        Image card = AdminUi.CreatePanel(gate.transform, "Card", AdminUi.Panel);
        RectTransform cardRect = card.rectTransform;
        cardRect.anchorMin = new Vector2(0.5f, 0.5f);
        cardRect.anchorMax = new Vector2(0.5f, 0.5f);
        cardRect.pivot = new Vector2(0.5f, 0.5f);
        cardRect.anchoredPosition = Vector2.zero;
        cardRect.sizeDelta = new Vector2(880f, 700f);

        _gate = gate.rectTransform;
        _gateContent = card.rectTransform;
    }

    private RectTransform _gateContent;

    private void BuildToast(Transform parent)
    {
        Image toast = AdminUi.CreatePanel(parent, "Toast", new Color(0f, 0f, 0f, 0.85f));
        RectTransform toastRect = toast.rectTransform;
        toastRect.anchorMin = new Vector2(0.5f, 0f);
        toastRect.anchorMax = new Vector2(0.5f, 0f);
        toastRect.pivot = new Vector2(0.5f, 0f);
        toastRect.anchoredPosition = new Vector2(0f, 60f);
        toastRect.sizeDelta = new Vector2(900f, 80f);
        toast.raycastTarget = false;

        _toastText = AdminUi.CreateText(toast.transform, "Text", "", AdminUi.FontBody,
                                       TextAlignmentOptions.Center, AdminUi.Text);
        AdminUi.Stretch(_toastText.rectTransform, 16f, 8f);

        toast.gameObject.SetActive(false);
        _toastPanel = toast;
    }

    private Image _toastPanel;

    // ------------------------------------------------------------------
    // PIN gate
    // ------------------------------------------------------------------

    private void ShowGate()
    {
        SetShellVisible(false);
        _gate.gameObject.SetActive(true);

        // Rebuild the gate contents each time so the wording matches the state.
        for (int i = _gateContent.childCount - 1; i >= 0; i--)
        {
            GameObject child = _gateContent.GetChild(i).gameObject;
            child.SetActive(false);
            Destroy(child);
        }

        bool creating = !AdminSecurity.HasPin;

        TMP_Text heading = AdminUi.CreateText(_gateContent, "Heading",
            AdminStrings.Get(creating ? "pinCreateTitle" : "pinEnterTitle"),
            AdminUi.FontHeading, TextAlignmentOptions.TopLeft, AdminUi.Text);
        RectTransform headingRect = heading.rectTransform;
        headingRect.anchorMin = new Vector2(0f, 1f);
        headingRect.anchorMax = new Vector2(1f, 1f);
        headingRect.pivot = new Vector2(0.5f, 1f);
        headingRect.offsetMin = new Vector2(30f, -80f);
        headingRect.offsetMax = new Vector2(-30f, -20f);

        TMP_InputField first = null;
        TMP_InputField second = null;
        float cursorY = -100f;

        if (creating)
        {
            TMP_Text explain = AdminUi.CreateText(_gateContent, "Explain", AdminStrings.Get("pinCreateExplain"),
                AdminUi.FontSmall, TextAlignmentOptions.TopLeft, AdminUi.Muted);
            RectTransform explainRect = explain.rectTransform;
            explainRect.anchorMin = new Vector2(0f, 1f);
            explainRect.anchorMax = new Vector2(1f, 1f);
            explainRect.pivot = new Vector2(0.5f, 1f);
            explainRect.offsetMin = new Vector2(30f, -190f);
            explainRect.offsetMax = new Vector2(-30f, -80f);
            cursorY = -200f;
        }

        first = AddGateField(_gateContent, AdminStrings.Get(creating ? "pinNew" : "pinEnter"), cursorY);
        if (creating)
        {
            second = AddGateField(_gateContent, AdminStrings.Get("pinConfirm"), cursorY - 130f);
        }

        TMP_Text error = AdminUi.CreateText(_gateContent, "Error", "", AdminUi.FontSmall,
                                             TextAlignmentOptions.TopLeft, AdminUi.Danger);
        RectTransform errorRect = error.rectTransform;
        errorRect.anchorMin = new Vector2(0f, 1f);
        errorRect.anchorMax = new Vector2(1f, 1f);
        errorRect.pivot = new Vector2(0.5f, 1f);
        errorRect.offsetMin = new Vector2(30f, cursorY - (creating ? 300f : 170f));
        errorRect.offsetMax = new Vector2(-30f, cursorY - (creating ? 260f : 130f));
        Button submit = AdminUi.CreateButton(_gateContent, "Submit",
            AdminStrings.Get(creating ? "create" : "unlock"),
            () => SubmitPin(creating, first, second, error),
            AdminUi.Accent, AdminUi.FontBody);
        RectTransform submitRect = submit.GetComponent<RectTransform>();
        submitRect.anchorMin = new Vector2(0.5f, 0f);
        submitRect.anchorMax = new Vector2(0.5f, 0f);
        submitRect.pivot = new Vector2(0.5f, 0f);
        submitRect.anchoredPosition = new Vector2(0f, 40f);
        submitRect.sizeDelta = new Vector2(420f, 90f);

        if (AdminSecurity.IsLockedOut)
        {
            error.text = AdminStrings.Get("pinLocked", AdminSecurity.LockoutSecondsRemaining);
        }
    }

    private TMP_InputField AddGateField(Transform parent, string label, float topOffset)
    {
        TMP_Text caption = AdminUi.CreateText(parent, label + "Label", label, AdminUi.FontSmall,
                                               TextAlignmentOptions.TopLeft, AdminUi.Muted);
        RectTransform captionRect = caption.rectTransform;
        captionRect.anchorMin = new Vector2(0f, 1f);
        captionRect.anchorMax = new Vector2(1f, 1f);
        captionRect.pivot = new Vector2(0.5f, 1f);
        captionRect.offsetMin = new Vector2(30f, topOffset - 30f);
        captionRect.offsetMax = new Vector2(-30f, topOffset);

        TMP_InputField field = AdminUi.CreateInput(parent, label + "Input", "", false, AdminUi.FontBody);
        RectTransform fieldRect = field.GetComponent<RectTransform>();
        fieldRect.anchorMin = new Vector2(0f, 1f);
        fieldRect.anchorMax = new Vector2(1f, 1f);
        fieldRect.pivot = new Vector2(0.5f, 1f);
        fieldRect.offsetMin = new Vector2(30f, topOffset - 100f);
        fieldRect.offsetMax = new Vector2(-30f, topOffset - 30f);
        field.contentType = TMP_InputField.ContentType.IntegerNumber;

        return field;
    }

    private void SubmitPin(bool creating, TMP_InputField first, TMP_InputField second, TMP_Text error)
    {
        string pin = first != null ? first.text : "";

        if (creating)
        {
            string confirm = second != null ? second.text : "";
            if (pin != confirm)
            {
                error.text = AdminStrings.Get("pinMismatch");
                return;
            }

            string formatError = ValidatePinFormat(pin);
            if (formatError.Length > 0)
            {
                error.text = formatError;
                return;
            }

            string createError;
            if (!AdminSecurity.CreatePin(pin, out createError))
            {
                error.text = createError;
                return;
            }

            AdminContentStore.AddAudit("security", "Created the admin PIN");
            AdminContentStore.Save();
            OnUnlocked();
            return;
        }

        string verifyError;
        if (!AdminSecurity.VerifyPin(pin, out verifyError))
        {
            // AdminSecurity reports in English; the panel shows the teacher
            // their own language.
            error.text = string.IsNullOrEmpty(pin)
                ? AdminStrings.Get("pinRequired")
                : verifyError;
            if (AdminSecurity.IsLockedOut)
            {
                error.text = AdminStrings.Get("pinLocked", AdminSecurity.LockoutSecondsRemaining);
            }
            return;
        }

        OnUnlocked();
    }

    /// <summary>
    /// Checks the PIN format and returns a localized message. AdminSecurity
    /// repeats the check in English as a last line of defence, but the panel
    /// shows the teacher their own language.
    /// </summary>
    private static string ValidatePinFormat(string pin)
    {
        if (string.IsNullOrEmpty(pin)) return AdminStrings.Get("pinRequired");
        if (pin.Length < AdminSecurity.MinPinLength) return AdminStrings.Get("pinTooShort");
        if (pin.Length > AdminSecurity.MaxPinLength) return AdminStrings.Get("pinTooShort");

        for (int i = 0; i < pin.Length; i++)
        {
            if (!char.IsDigit(pin[i])) return AdminStrings.Get("pinDigitsOnly");
        }
        return "";
    }

    private void OnUnlocked()
    {
        _gate.gameObject.SetActive(false);
        SetShellVisible(true);
        BuildTabs();
    }

    private void SetShellVisible(bool visible)
    {
        if (_shell != null) _shell.gameObject.SetActive(visible);
    }

    // ------------------------------------------------------------------
    // Tabs
    // ------------------------------------------------------------------

    private void BuildTabs()
    {
        if (_allTabs.Count == 0)
        {
            _allTabs.Add(new AdminTabProgress(this));
            _allTabs.Add(new AdminTabTimers(this));
            _allTabs.Add(new AdminTabQuestions(this));
            _allTabs.Add(new AdminTabRecipes(this));
            _allTabs.Add(new AdminTabText(this));
            _allTabs.Add(new AdminTabContent(this));
            _allTabs.Add(new AdminTabAudit(this));
            _allTabs.Add(new AdminTabSecurity(this));
        }

        // While the device is still on the default PIN, only the Security
        // tab is reachable. The teacher has to replace the default before
        // anything else can be changed.
        _tabs.Clear();
        for (int i = 0; i < _allTabs.Count; i++)
        {
            if (AdminSecurity.MustChangePin && !(_allTabs[i] is AdminTabSecurity)) continue;
            _tabs.Add(_allTabs[i]);
        }

        for (int i = _tabBar.childCount - 1; i >= 0; i--)
        {
            GameObject child = _tabBar.GetChild(i).gameObject;
            child.SetActive(false);
            Destroy(child);
        }

        for (int i = 0; i < _tabs.Count; i++)
        {
            AdminTabBase tab = _tabs[i];
            Button button = AdminUi.CreateButton(_tabBar, "Tab_" + tab.TitleKey,
                AdminStrings.Get(tab.TitleKey), () => SelectTab(tab), AdminUi.Card, AdminUi.FontSmall);
            LayoutElement layout = AdminUi.EnsureLayout(button.gameObject);
            layout.minWidth = 300f;
            layout.preferredWidth = 300f;
            layout.minHeight = 80f;
            layout.preferredHeight = 80f;
            layout.flexibleWidth = 0f;
        }

        SelectTab(_tabs[0]);
    }

    /// <summary>
    /// Shows a confirmation dialog over the panel. The panel stays visible
    /// behind it, so the teacher can see what they are about to change.
    /// </summary>
    public void Confirm(string title, string message, string confirmLabel, System.Action onConfirm)
    {
        if (_confirmOverlay != null)
        {
            _confirmOverlay.SetActive(false);
            Destroy(_confirmOverlay);
        }

        Image backdrop = AdminUi.CreatePanel(_canvas.transform, "ConfirmOverlay",
                                             new Color(0f, 0f, 0f, 0.6f));
        AdminUi.Stretch(backdrop.rectTransform);
        backdrop.raycastTarget = true;
        _confirmOverlay = backdrop.gameObject;

        Image card = AdminUi.CreatePanel(backdrop.transform, "Card", AdminUi.Panel);
        RectTransform cardRect = card.rectTransform;
        cardRect.anchorMin = new Vector2(0.5f, 0.5f);
        cardRect.anchorMax = new Vector2(0.5f, 0.5f);
        cardRect.pivot = new Vector2(0.5f, 0.5f);
        cardRect.anchoredPosition = Vector2.zero;
        cardRect.sizeDelta = new Vector2(880f, 520f);

        TMP_Text heading = AdminUi.CreateText(card.transform, "Title", title,
            AdminUi.FontHeading, TextAlignmentOptions.TopLeft, AdminUi.Text);
        RectTransform headingRect = heading.rectTransform;
        headingRect.anchorMin = new Vector2(0f, 1f);
        headingRect.anchorMax = new Vector2(1f, 1f);
        headingRect.pivot = new Vector2(0.5f, 1f);
        headingRect.offsetMin = new Vector2(30f, -80f);
        headingRect.offsetMax = new Vector2(-30f, -20f);

        TMP_Text body = AdminUi.CreateText(card.transform, "Message", message,
            AdminUi.FontBody, TextAlignmentOptions.TopLeft, AdminUi.Muted);
        RectTransform bodyRect = body.rectTransform;
        bodyRect.anchorMin = new Vector2(0f, 1f);
        bodyRect.anchorMax = new Vector2(1f, 1f);
        bodyRect.pivot = new Vector2(0.5f, 1f);
        bodyRect.offsetMin = new Vector2(30f, -260f);
        bodyRect.offsetMax = new Vector2(-30f, -90f);

        Button cancel = AdminUi.CreateButton(card.transform, "Cancel", AdminStrings.Get("cancel"),
            () => CloseConfirm(), AdminUi.CardAlt, AdminUi.FontBody);
        RectTransform cancelRect = cancel.GetComponent<RectTransform>();
        cancelRect.anchorMin = new Vector2(0f, 0f);
        cancelRect.anchorMax = new Vector2(0.5f, 0f);
        cancelRect.pivot = new Vector2(0f, 0f);
        cancelRect.anchoredPosition = new Vector2(30f, 30f);
        cancelRect.sizeDelta = new Vector2(380f, 84f);

        Button accept = AdminUi.CreateButton(card.transform, "Accept", confirmLabel,
            () =>
            {
                CloseConfirm();
                if (onConfirm != null) onConfirm();
            }, AdminUi.Danger, AdminUi.FontBody);
        RectTransform acceptRect = accept.GetComponent<RectTransform>();
        acceptRect.anchorMin = new Vector2(1f, 0f);
        acceptRect.anchorMax = new Vector2(1f, 0f);
        acceptRect.pivot = new Vector2(1f, 0f);
        acceptRect.anchoredPosition = new Vector2(-30f, 30f);
        acceptRect.sizeDelta = new Vector2(380f, 84f);
    }

    private void CloseConfirm()
    {
        if (_confirmOverlay == null) return;
        _confirmOverlay.SetActive(false);
        Destroy(_confirmOverlay);
        _confirmOverlay = null;
    }

    private GameObject _confirmOverlay;

    private void SelectTab(AdminTabBase tab)
    {
        _currentTab = tab;

        for (int i = _content.childCount - 1; i >= 0; i--)
        {
            GameObject child = _content.GetChild(i).gameObject;
            Destroy(child);
        }

        if (tab != null) tab.Build(_content);
    }

    /// <summary>Rebuilds the current tab, used after the language changes.</summary>
    public void RefreshCurrentTab()
    {
        if (_currentTab != null) SelectTab(_currentTab);
    }

    // ------------------------------------------------------------------
    // Language
    // ------------------------------------------------------------------

    private void ToggleLanguage()
    {
        AdminStrings.UseFilipino = !AdminStrings.UseFilipino;

        // Rebuild everything so every label flips.
        _shell = null;
        _gate = null;
        _content = null;
        _tabBar = null;
        _gateContent = null;
        _tabs.Clear();

        if (_canvas != null)
        {
            _canvas.gameObject.SetActive(false);
            Destroy(_canvas.gameObject);
        }
        Build();
    }

    // ------------------------------------------------------------------
    // Feedback
    // ------------------------------------------------------------------

    /// <summary>Shows a short confirmation message at the bottom of the panel.</summary>
    public void ShowToast(string message)
    {
        if (_toastPanel == null || _toastText == null) return;

        _toastText.text = message;
        _toastPanel.gameObject.SetActive(true);
        _toastPanel.transform.localScale = Vector3.one;

        if (_toastRoutine != null) StopCoroutine(_toastRoutine);
        _toastRoutine = StartCoroutine(HideToastAfterDelay());
    }

    private IEnumerator HideToastAfterDelay()
    {
        yield return new WaitForSecondsRealtime(2.2f);
        if (_toastPanel != null) _toastPanel.gameObject.SetActive(false);
        _toastRoutine = null;
    }
}
