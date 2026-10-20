using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The Activity Log tab: an append-only record of every admin change.
///
/// Each line records when it happened, which tab made the change, what was
/// changed, and the value before and after. The log lives in the admin
/// config file, so it survives a restart.
///
/// The log is append only: nothing in the panel edits a line. It can be
/// cleared, which is the one exception, and that is recorded in the log too
/// by the tab that does it.
/// </summary>
public class AdminTabAudit : AdminTabBase
{
    public AdminTabAudit(AdminPanelRoot root) : base(root) { }

    public override string TitleKey => "tabAudit";

    public override void Build(RectTransform content)
    {
        List<AdminContentStore.AuditEntry> entries = AdminContentStore.GetAudit();

        RectTransform card = AddCard(content, AdminStrings.Get("audit") + " (" + entries.Count + ")");

        if (entries.Count == 0)
        {
            AddButtonRow(card, AdminStrings.Get("auditEmpty"), () => { }, AdminUi.CardAlt);
            return;
        }

        // Newest first.
        for (int i = entries.Count - 1; i >= 0; i--)
        {
            AdminContentStore.AuditEntry entry = entries[i];
            if (entry == null) continue;

            RectTransform row = AddRow(card, 80f);

            TMP_Text stamp = AdminUi.CreateText(row, "Stamp", FormatStamp(entry.utc),
                                                AdminUi.FontSmall, TextAlignmentOptions.MidlineLeft, AdminUi.Muted);
            LayoutElement stampLayout = AdminUi.EnsureLayout(stamp.gameObject);
            stampLayout.preferredWidth = 220f;
            stampLayout.minWidth = 180f;
            stampLayout.flexibleWidth = 0f;

            TMP_Text action = AdminUi.CreateText(row, "Action", Describe(entry),
                                                 AdminUi.FontSmall, TextAlignmentOptions.MidlineLeft, AdminUi.Text);
            LayoutElement actionLayout = AdminUi.EnsureLayout(action.gameObject);
            actionLayout.preferredWidth = 320f;
            actionLayout.minWidth = 220f;
            actionLayout.flexibleWidth = 0f;

            TMP_Text change = AdminUi.CreateText(row, "Change", DescribeChange(entry),
                                                 AdminUi.FontSmall, TextAlignmentOptions.MidlineLeft, AdminUi.Muted);
            LayoutElement changeLayout = AdminUi.EnsureLayout(change.gameObject);
            changeLayout.preferredWidth = 520f;
            changeLayout.minWidth = 300f;
            changeLayout.flexibleWidth = 1f;
        }

        AddButtonRow(card, AdminStrings.Get("clearAudit"), () =>
        {
            if (Root == null) return;
            Root.Confirm(AdminStrings.Get("clearAudit"), AdminStrings.Get("confirmClearAudit"),
                AdminStrings.Get("clearAudit"), () =>
                {
                    AdminContentStore.ClearAudit();
                    Toast(AdminStrings.Get("auditCleared"));
                    if (Root != null) Root.RefreshCurrentTab();
                });
        }, AdminUi.Danger);
    }

    private static string FormatStamp(string utc)
    {
        if (string.IsNullOrEmpty(utc)) return "";

        // The stamp is stored in UTC, so it is read back as UTC and then
        // converted for display.
        if (!System.DateTime.TryParse(utc, System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.RoundtripKind, out System.DateTime parsed))
        {
            return utc;
        }

        if (parsed.Kind == System.DateTimeKind.Unspecified)
        {
            parsed = System.DateTime.SpecifyKind(parsed, System.DateTimeKind.Utc);
        }

        return parsed.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss");
    }

    /// <summary>The tab and the action, for example "Security - Changed the admin PIN".</summary>
    private static string Describe(AdminContentStore.AuditEntry entry)
    {
        string tab = string.IsNullOrEmpty(entry.tab) ? "" : entry.tab + " - ";
        return tab + entry.action;
    }

    /// <summary>The old and the new value, when the entry recorded them.</summary>
    private static string DescribeChange(AdminContentStore.AuditEntry entry)
    {
        if (string.IsNullOrEmpty(entry.oldValue) && string.IsNullOrEmpty(entry.newValue))
        {
            return entry.detail;
        }
        return entry.oldValue + "  ->  " + entry.newValue;
    }
}
