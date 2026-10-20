using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Base class for the admin panel's tabs.
///
/// A tab fills a scroll view with rows. The helpers here cover the row shapes
/// the tabs share: a titled card, a labelled number field, a labelled text
/// field, a toggle, and a button.
///
/// Every numeric row shows both the effective value and the built-in default
/// when they differ. That matters because the panel edits an override layer:
/// "no override" and "set to the default value" behave the same in the game
/// but are not the same thing, and the teacher needs to see which one they
/// are looking at.
/// </summary>
public abstract class AdminTabBase
{
    protected readonly AdminPanelRoot Root;

    protected AdminTabBase(AdminPanelRoot root)
    {
        Root = root;
    }

    /// <summary>Key into <see cref="AdminStrings"/> for the tab button label.</summary>
    public abstract string TitleKey { get; }

    /// <summary>Fills the scroll view with this tab's rows.</summary>
    public abstract void Build(RectTransform content);

    protected void Toast(string message)
    {
        if (Root != null) Root.ShowToast(message);
    }

    /// <summary>Records the change in the audit log and writes the file.</summary>
    /// <summary>
    /// Records a change in the audit log and saves the config.
    ///
    /// The tab is recorded so the log reads as "Questions - Edited answerA of
    /// question 3 in Level 1" rather than a bare action.
    /// </summary>
    protected void Commit(string tab, string action)
    {
        AdminContentStore.AddAudit(tab, action, "", "", "");
        AdminContentStore.Save();
    }

    /// <summary>Records a change together with the value before and after.</summary>
    protected void Commit(string tab, string action, string oldValue, string newValue)
    {
        AdminContentStore.AddAudit(tab, action, oldValue, newValue, "");
        AdminContentStore.Save();
    }

    /// <summary>
    /// True when the value differs from the last one seen.
    ///
    /// Typing "15" into a field fires onValueChanged for "1" and then "15".
    /// Without this the audit log would get an entry per keystroke and the
    /// config file would be rewritten per keystroke.
    /// </summary>
    private static bool Changed(ref object last, object value)
    {
        if (last != null && last.Equals(value)) return false;
        last = value;
        return true;
    }

    // ------------------------------------------------------------------
    // Shared row builders
    // ------------------------------------------------------------------

    /// <summary>Creates a titled card to group rows into.</summary>
    protected RectTransform AddCard(RectTransform content, string title)
    {
        Image card = AdminUi.CreatePanel(content, "Card", AdminUi.Card);

        // The card is itself a vertical layout group so the rows inside it
        // stack, and a content size fitter so it grows to fit them. The rows
        // are direct children of the card, not of a nested group, otherwise
        // they would have no size.
        VerticalLayoutGroup group = card.gameObject.AddComponent<VerticalLayoutGroup>();
        group.spacing = 6f;
        group.padding = new RectOffset(10, 10, 10, 10);
        group.childControlWidth = true;
        group.childControlHeight = true;
        group.childForceExpandWidth = true;
        group.childForceExpandHeight = false;

        ContentSizeFitter fitter = card.gameObject.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        if (!string.IsNullOrEmpty(title))
        {
            TMP_Text heading = AdminUi.CreateText(card.transform, "Heading", title, AdminUi.FontHeading,
                                                  TextAlignmentOptions.TopLeft, AdminUi.Text);
            LayoutElement headingLayout = AdminUi.EnsureLayout(heading.gameObject);
            headingLayout.minHeight = 44f;
            headingLayout.preferredHeight = 44f;
            headingLayout.flexibleWidth = 1f;
        }

        return card.rectTransform;
    }

    /// <summary>
    /// Adds a labelled integer field. The value is owned by the caller, which
    /// is what the Progress tab needs: those values live in PlayerPrefs, not
    /// in the override layer.
    /// </summary>
    protected void AddNumberRow(RectTransform card, string label, int current,
                                 Action<int> onChanged, float min = 0f, float max = 9999f)
    {
        RectTransform row = AddRow(card);
        AddRowLabel(row, label);

        TMP_InputField input = AddRowInput(row, current.ToString());
        input.onValueChanged.AddListener(text =>
        {
            int value;
            if (!int.TryParse(text, out value)) return;
            value = Mathf.Clamp(value, (int)min, (int)max);
            if (onChanged != null) onChanged(value);
        });
    }

    /// <summary>Adds a labelled toggle. The value is owned by the caller.</summary>
    protected void AddToggleRow(RectTransform card, string label, bool current, Action<bool> onChanged)
    {
        RectTransform row = AddRow(card);
        AddRowLabel(row, label);

        Toggle toggle = AdminUi.CreateToggle(row, "Toggle", current);
        RectTransform toggleRect = toggle.GetComponent<RectTransform>();
        toggleRect.anchorMin = new Vector2(1f, 0.5f);
        toggleRect.anchorMax = new Vector2(1f, 0.5f);
        toggleRect.pivot = new Vector2(1f, 0.5f);
        toggleRect.anchoredPosition = Vector2.zero;
        toggleRect.sizeDelta = new Vector2(70f, 50f);

        toggle.onValueChanged.AddListener(value =>
        {
            if (onChanged != null) onChanged(value);
        });
    }

    /// <summary>Adds a labelled integer field backed by an override key.</summary>
    protected void AddOverrideNumberRow(RectTransform card, string label, string key, int defaultValue,
                                        Action<int> onChanged, float min = 0f, float max = 9999f)
    {
        RectTransform row = AddRow(card);
        AddRowLabel(row, label);

        int current = AdminContentStore.GetInt(key, defaultValue);
        TMP_InputField input = AddRowInput(row, current.ToString());

        if (AdminContentStore.HasOverride(key))
        {
            AddDefaultHint(row, defaultValue.ToString());
            AddResetButton(row, key);
        }

        object last = null;
        input.onValueChanged.AddListener(text =>
        {
            int value;
            if (!int.TryParse(text, out value)) return;
            value = Mathf.Clamp(value, (int)min, (int)max);
            if (!Changed(ref last, value)) return;
            AdminContentStore.SetInt(key, value);
            Commit("Settings", "Set " + key + " to " + value);
            if (onChanged != null) onChanged(value);
        });
    }

    /// <summary>Adds a labelled float field backed by an override key.</summary>
    protected void AddOverrideFloatRow(RectTransform card, string label, string key, float defaultValue,
                                       Action<float> onChanged, float min = 0f, float max = 9999f)
    {
        RectTransform row = AddRow(card);
        AddRowLabel(row, label);

        float current = AdminContentStore.GetFloat(key, defaultValue);
        TMP_InputField input = AddRowInput(row, current.ToString("0.##"));

        if (AdminContentStore.HasOverride(key))
        {
            AddDefaultHint(row, defaultValue.ToString("0.##"));
            AddResetButton(row, key);
        }

        object last = null;
        input.onValueChanged.AddListener(text =>
        {
            float value;
            if (!float.TryParse(text, out value)) return;
            value = Mathf.Clamp(value, min, max);
            if (!Changed(ref last, value)) return;
            AdminContentStore.SetFloat(key, value);
            Commit("Settings", "Set " + key + " to " + value);
            if (onChanged != null) onChanged(value);
        });
    }

    /// <summary>Adds a labelled text field backed by an override key.</summary>
    protected void AddOverrideTextRow(RectTransform card, string label, string key, string defaultValue,
                                      Action<string> onChanged, bool multiline = false)
    {
        RectTransform row = AddRow(card, multiline ? 170f : 72f);
        AddRowLabel(row, label);

        string current = AdminContentStore.GetString(key, defaultValue);
        TMP_InputField input = AddRowInput(row, current, multiline);

        if (AdminContentStore.HasOverride(key))
        {
            AddDefaultHint(row, defaultValue);
            AddResetButton(row, key);
        }

        object last = null;
        input.onValueChanged.AddListener(text =>
        {
            if (!Changed(ref last, text)) return;
            AdminContentStore.SetString(key, text);
            Commit("settings", "Set " + key);
            if (onChanged != null) onChanged(text);
        });
    }

    /// <summary>Adds a labelled toggle backed by an override key.</summary>
    protected void AddOverrideToggleRow(RectTransform card, string label, string key, bool defaultValue,
                                        Action<bool> onChanged)
    {
        RectTransform row = AddRow(card);
        AddRowLabel(row, label);

        bool current = AdminContentStore.GetBool(key, defaultValue);
        Toggle toggle = AdminUi.CreateToggle(row, "Toggle", current);
        RectTransform toggleRect = toggle.GetComponent<RectTransform>();
        toggleRect.anchorMin = new Vector2(1f, 0.5f);
        toggleRect.anchorMax = new Vector2(1f, 0.5f);
        toggleRect.pivot = new Vector2(1f, 0.5f);
        toggleRect.anchoredPosition = Vector2.zero;
        toggleRect.sizeDelta = new Vector2(70f, 50f);

        if (AdminContentStore.HasOverride(key))
        {
            AddDefaultHint(row, defaultValue ? AdminStrings.Get("yes") : AdminStrings.Get("no"));
            AddResetButton(row, key);
        }

        object last = null;
        toggle.onValueChanged.AddListener(value =>
        {
            if (!Changed(ref last, value)) return;
            AdminContentStore.SetBool(key, value);
            Commit("Settings", "Set " + key + " to " + value);
            if (onChanged != null) onChanged(value);
        });
    }

    /// <summary>Adds a button row.</summary>
    protected void AddButtonRow(RectTransform card, string label, UnityEngine.Events.UnityAction onClick,
                                 Color? color = null, string key = null)
    {
        RectTransform row = AddRow(card);
        Button button = AdminUi.CreateButton(row, "Button", label, onClick, color ?? AdminUi.AccentDim, AdminUi.FontBody);
        RectTransform buttonRect = button.GetComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0f, 0.5f);
        buttonRect.anchorMax = new Vector2(1f, 0.5f);
        buttonRect.pivot = new Vector2(0.5f, 0.5f);
        buttonRect.anchoredPosition = Vector2.zero;
        buttonRect.sizeDelta = new Vector2(0f, 76f);
        AdminUi.SetLayout(button.gameObject, 76f, 76f);

        if (key != null && AdminContentStore.HasOverride(key))
        {
            AddResetButton(row, key);
        }
    }

    // ------------------------------------------------------------------
    // Row primitives
    // ------------------------------------------------------------------

    /// <summary>Creates a horizontal row inside a card.</summary>
    protected RectTransform AddRow(RectTransform card, float height = 72f)
    {
        RectTransform row = AdminUi.CreateRect(card, "Row");
        HorizontalLayoutGroup group = row.gameObject.AddComponent<HorizontalLayoutGroup>();
        group.spacing = 10f;
        group.padding = new RectOffset(6, 6, 4, 4);
        group.childControlWidth = true;
        group.childControlHeight = true;
        group.childForceExpandWidth = false;
        group.childForceExpandHeight = false;
        group.childAlignment = TextAnchor.MiddleLeft;

        LayoutElement layout = AdminUi.EnsureLayout(row.gameObject);
        layout.minHeight = height;
        layout.preferredHeight = height;
        layout.flexibleWidth = 1f;
        return row;
    }

    protected void AddRowLabel(RectTransform row, string label)
    {
        TMP_Text text = AdminUi.CreateText(row, "Label", label, AdminUi.FontBody,
                                           TextAlignmentOptions.MidlineLeft, AdminUi.Text);
        LayoutElement layout = AdminUi.EnsureLayout(text.gameObject);
        layout.preferredWidth = 520f;
        layout.minWidth = 320f;
        layout.flexibleWidth = 1f;
    }

    protected TMP_InputField AddRowInput(RectTransform row, string value, bool multiline = false, float preferredWidth = 320f)
    {
        TMP_InputField input = AdminUi.CreateInput(row, "Input", value, multiline, AdminUi.FontBody);
        LayoutElement layout = AdminUi.EnsureLayout(input.gameObject);
        layout.preferredWidth = preferredWidth;
        layout.minWidth = 200f;
        layout.flexibleWidth = 1f;
        if (multiline)
        {
            layout.minHeight = 150f;
            layout.preferredHeight = 150f;
        }
        return input;
    }

    /// <summary>Small muted text showing the built-in default of an overridden row.</summary>
    protected void AddDefaultHint(RectTransform row, string defaultText)
    {
        TMP_Text hint = AdminUi.CreateText(row, "Default", AdminStrings.Get("builtIn") + ": " + defaultText,
                                           AdminUi.FontSmall, TextAlignmentOptions.MidlineRight, AdminUi.Muted);
        LayoutElement layout = AdminUi.EnsureLayout(hint.gameObject);
        layout.preferredWidth = 300f;
        layout.minWidth = 200f;
        layout.flexibleWidth = 0f;
    }

    /// <summary>A small button that removes the override for a key.</summary>
    protected void AddResetButton(RectTransform row, string key)
    {
        Button button = AdminUi.CreateButton(row, "Reset", AdminStrings.Get("reset"),
            () =>
            {
                AdminContentStore.ClearKey(key);
                Commit("Settings", "Restored the built-in value of " + key);
                Toast(AdminStrings.Get("reset"));
                if (Root != null) Root.RefreshCurrentTab();
            },
            AdminUi.CardAlt, AdminUi.FontSmall);
        LayoutElement layout = AdminUi.EnsureLayout(button.gameObject);
        layout.preferredWidth = 150f;
        layout.minWidth = 120f;
        layout.flexibleWidth = 0f;
    }
}
