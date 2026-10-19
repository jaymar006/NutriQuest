using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The Security tab: change the PIN, check the lockout state, and erase
/// everything.
///
/// The PIN belongs to the teacher who set it up, so changing it needs the
/// current one. There is no recovery path: if the PIN is forgotten the only
/// option is to erase the admin configuration, which also removes the PIN.
/// </summary>
public class AdminTabSecurity : AdminTabBase
{
    public AdminTabSecurity(AdminPanelRoot root) : base(root) { }

    public override string TitleKey => "tabSecurity";

    public override void Build(RectTransform content)
    {
        BuildStatusCard(content);
        BuildChangePinCard(content);
        BuildEraseCard(content);
    }

    private void BuildStatusCard(RectTransform content)
    {
        RectTransform card = AddCard(content, AdminStrings.Get("security"));

        AddStatusRow(card, AdminStrings.Get("failedAttempts"), AdminSecurity.FailedAttempts.ToString());
        AddStatusRow(card, AdminStrings.Get("pinLocked", AdminSecurity.LockoutSecondsRemaining),
            AdminSecurity.IsLockedOut ? AdminStrings.Get("yes") : AdminStrings.Get("no"));

        if (AdminSecurity.IsLockedOut)
        {
            AddButtonRow(card, AdminStrings.Get("reset"), () =>
            {
                AdminSecurity.ResetLockout();
                Commit("security", "Cleared the PIN lockout");
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
        LayoutElement layout = text.gameObject.AddComponent<LayoutElement>();
        layout.preferredWidth = 320f;
        layout.minWidth = 200f;
        layout.flexibleWidth = 1f;
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
            Commit("security", "Changed the admin PIN");
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
            AdminContentStore.ResetAll();
            AdminSecurity.DeletePin();
            Commit("security", "Erased every admin setting and the PIN");
            Toast(AdminStrings.Get("reset"));
            AdminPanelRoot.Close();
        }, AdminUi.Danger);
    }
}
