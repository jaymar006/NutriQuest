using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The Activity Log tab: a record of every change the teacher made.
///
/// A shipped admin panel that can hand out badges and rewrite questions
/// needs to leave a trail, otherwise a mistaken change cannot be traced.
/// The log is kept in the admin configuration file, capped at the most recent
/// entries.
/// </summary>
public class AdminTabAudit : AdminTabBase
{
    public AdminTabAudit(AdminPanelRoot root) : base(root) { }

    public override string TitleKey => "tabAudit";

    public override void Build(RectTransform content)
    {
        List<AdminContentStore.AuditEntry> entries = AdminContentStore.GetAudit();

        RectTransform card = AddCard(content, AdminStrings.Get("audit"));

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

            RectTransform row = AddRow(card);

            TMP_Text stamp = AdminUi.CreateText(row, "Stamp", FormatStamp(entry.utc), AdminUi.FontSmall,
                                                TextAlignmentOptions.MidlineLeft, AdminUi.Muted);
            LayoutElement stampLayout = stamp.gameObject.AddComponent<LayoutElement>();
            stampLayout.preferredWidth = 300f;
            stampLayout.minWidth = 240f;
            stampLayout.flexibleWidth = 0f;

            TMP_Text action = AdminUi.CreateText(row, "Action", entry.action, AdminUi.FontBody,
                                                 TextAlignmentOptions.MidlineLeft, AdminUi.Text);
            LayoutElement actionLayout = action.gameObject.AddComponent<LayoutElement>();
            actionLayout.preferredWidth = 300f;
            actionLayout.minWidth = 220f;
            actionLayout.flexibleWidth = 0f;

            TMP_Text detail = AdminUi.CreateText(row, "Detail", entry.detail, AdminUi.FontSmall,
                                                 TextAlignmentOptions.MidlineLeft, AdminUi.Muted);
            LayoutElement detailLayout = detail.gameObject.AddComponent<LayoutElement>();
            detailLayout.preferredWidth = 520f;
            detailLayout.minWidth = 320f;
            detailLayout.flexibleWidth = 1f;
        }
    }

    private static string FormatStamp(string utc)
    {
        if (string.IsNullOrEmpty(utc)) return "";
        if (!System.DateTime.TryParse(utc, out System.DateTime parsed)) return utc;
        return parsed.ToLocalTime().ToString("yyyy-MM-dd HH:mm");
    }
}
