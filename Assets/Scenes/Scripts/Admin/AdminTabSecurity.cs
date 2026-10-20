using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The Security tab: switch the admin system off, tune the lockout, change
/// the PIN and erase everything.
///
/// The PIN belongs to the teacher who set it up, so changing it needs the
/// current one. There is no recovery path: if the PIN is forgotten the only
/// option is to erase the admin configuration, which also removes the PIN.
///
/// A fresh device starts on a well known default PIN. Until it is changed
/// the panel only offers this tab, so the default never becomes the real one.
/// </summary>
public class AdminTabSecurity : AdminTabBase
{
    public AdminTabSecurity(AdminPanelRoot root) : base(root) { }

    public override string TitleKey => "tabSecurity";

    public override void Build(RectTransform content)
    {
        BuildMustChangeNotice(content);
        BuildSwitchCard(content);
        BuildStatusCard(content);
        BuildChangePinCard(content);
        BuildEraseCard(content);
    }

    private void BuildMustChangeNotice(RectTransform content)
    {
        if (!AdminSecurity.MustChangePin) return;

        RectTransform card = AddCard(content, AdminStrings.Get("mustChangePin"));
        TMP_Text note = AdminUi.CreateText(card, "Note", AdminStrings.Get("defaultPinNote"),
                                            AdminUi.FontSmall, TextAlignmentOptions.TopLeft, AdminUi.Danger);
        AdminUi.SetLayout(note.gameObject, 80f, 80f);
    }

    private void BuildSwitchCard(RectTransform content)
    {
        RectTransform card = AddCard(content, AdminStrings.Get("adminEnabled"));

        RectTransform row = AddRow(card);
        AddRowLabel(row, AdminStrings.Get("adminEnabled"));

        Toggle toggle = AdminUi.CreateToggle(row, "Enabled", AdminContentStore.IsAdminEnabled);
        toggle.onValueChanged.AddListener(value =>
        {
            AdminContentStore.SetAdminEnabled(value);
            Commit("Security", (value ? "Enabled" : "Disabled") + " the admin system");
            if (Root != null) Root.RefreshCurrentTab();
        });

        TMP_Text note = AdminUi.CreateText(card, "Note", AdminStrings.Get("adminDisabledNote"),
                                            AdminUi.FontSmall, TextAlignmentOptions.TopLeft, AdminUi.Muted);
        AdminUi.SetLayout(note.gameObject, 60f, 60f);

        AddNumberRow(card, AdminStrings.Get("lockoutAttempts"), AdminSecurity.KeyLockoutAttempts,
            AdminSecurity.AllowedAttempts, 1f, 99f);
        AddNumberRow(card, AdminStrings.Get("lockoutMinutes"), AdminSecurity.KeyLockoutMinutes,
            AdminSecurity.LockoutMinutesConfigured, 1f, 1440f);
    }

    private void BuildStatusCard(RectTransform content)
    {
        RectTransform card = AddCard(content, AdminStrings.Get("security"));

        AddStatusRow(card, AdminStrings.Get("failedAttempts"), AdminSecurity.FailedAttempts.ToString());
        AddStatusRow(card, AdminStrings.Get("pinLocked", AdminSecurity.LockoutSecondsRemaining),
            AdminSecurity.IsLockedOut ? AdminStrings.Get("yes") : AdminStrings.Get("no"));
        AddStatusRow(card, AdminStrings.Get("pinSet"), AdminStrings.Get("yes"));

        if (AdminSecurity.IsLockedOut)
        {
            AddButtonRow(card, AdminStrings.Get("reset"), () =>
            {
                AdminSecurity.ResetLockout();
                Commit("Security", "Cleared the PIN lockout");
                if (Root != null) Root.RefreshCurrentTab();
            }, AdminUi.CardAlt);
        }
    }

    private void AddStatusRow(RectTransform card, string label, string value)
    {
        RectTransform row = AddRow(card);
        AddRowLabel(row, label);

        TMP_Text text = AdminUi.CreateText(row, "Value", value, AdminUi.FontBody,
                                           TextAlignmentOptions.MidlineLeft, AdminUi.Muted);
        LayoutElement layout = AdminUi.EnsureLayout(text.gameObject);
        layout.preferredWidth = 320f;
        layout.minWidth = 200f;
        layout.flexibleWidth = 1f;
    }

    /// <summary>A number field that writes straight into the content store.</summary>
    private void AddNumberRow(RectTransform card, string label, string key, int current,
                              float min, float max)
    {
        RectTransform row = AddRow(card);
        AddRowLabel(row, label);

        TMP_InputField input = AddRowInput(row, current.ToString());
        input.contentType = TMP_InputField.ContentType.IntegerNumber;

        int last = int.MinValue;
        input.onValueChanged.AddListener(text =>
        {
            int value;
            if (!int.TryParse(text, out value)) return;
            value = Mathf.Clamp(value, (int)min, (int)max);
            if (last == value) return;
            last = value;

            AdminContentStore.SetInt(key, value);
            Commit("Security", "Set " + label + " to " + value);
        });
    }

    private void BuildChangePinCard(RectTransform content)
    {
        RectTransform card = AddCard(content, AdminStrings.Get("changePin"));

        TMP_InputField current = AddPinField(card, AdminStrings.Get("currentPin"));
        TMP_InputField next = AddPinField(card, AdminStrings.Get("pinNew"));
        TMP_InputField confirm = AddPinField(card, AdminStrings.Get("pinConfirm"));

        TMP_Text error = AdminUi.CreateText(card, "Error", "", AdminUi.FontSmall,
                                             TextAlignmentOptions.TopLeft, AdminUi.Danger);
        AdminUi.SetLayout(error.gameObject, 40f, 40f);

        AddButtonRow(card, AdminStrings.Get("changePin"), () =>
        {
            if (next.text != confirm.text)
            {
                error.text = AdminStrings.Get("pinMismatch");
                return;
            }

            string changeError;
            if (!AdminSecurity.ChangePin(current.text, next.text, out changeError))
            {
                error.text = changeError;
                return;
            }

            error.text = "";
            Commit("Security", "Changed the admin PIN");
            Toast(AdminStrings.Get("pinChanged"));
            if (Root != null) Root.RefreshCurrentTab();
        }, AdminUi.AccentDim);
    }

    private TMP_InputField AddPinField(RectTransform card, string label)
    {
        RectTransform row = AddRow(card);
        AddRowLabel(row, label);

        TMP_InputField input = AddRowInput(row, "");
        input.contentType = TMP_InputField.ContentType.IntegerNumber;
        return input;
    }

    private void BuildEraseCard(RectTransform content)
    {
        RectTransform card = AddCard(content, AdminStrings.Get("wipeConfig"));

        TMP_Text warning = AdminUi.CreateText(card, "Warning", AdminStrings.Get("confirmWipe"),
                                              AdminUi.FontSmall, TextAlignmentOptions.TopLeft, AdminUi.Danger);
        AdminUi.SetLayout(warning.gameObject, 80f, 80f);

        AddButtonRow(card, AdminStrings.Get("wipeConfig"), () =>
        {
            if (Root == null) return;
            Root.Confirm(AdminStrings.Get("wipeConfig"), AdminStrings.Get("confirmWipe"),
                AdminStrings.Get("wipeConfig"), () =>
                {
                    AdminContentStore.ResetAll();
                    AdminSecurity.DeletePin();
                    Commit("Security", "Erased every admin setting and the PIN");
                    Toast(AdminStrings.Get("reset"));
                    AdminPanelRoot.Close();
                });
        }, AdminUi.Danger);
    }
}
