using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The Content and Version tab: the displayed version, exporting the
/// configuration to another device, importing one, and restoring the
/// built-in settings.
///
/// Export and import go through the clipboard rather than a file picker.
/// Android gives an app no user-visible place to put a file, and the project
/// has no file-picker plugin, so the clipboard is the one mechanism that
/// always works: the teacher copies the JSON, pastes it into a chat app, and
/// the other device pastes it straight back in.
/// </summary>
public class AdminTabContent : AdminTabBase
{
    public AdminTabContent(AdminPanelRoot root) : base(root) { }

    public override string TitleKey => "tabContent";

    public override void Build(RectTransform content)
    {
        BuildVersionCard(content);
        BuildExportCard(content);
        BuildImportCard(content);
        BuildRestoreCard(content);
    }

    private void BuildVersionCard(RectTransform content)
    {
        RectTransform card = AddCard(content, AdminStrings.Get("content"));

        AddOverrideTextRow(card, AdminStrings.Get("displayVersion"), AdminGameConfig.KeyDisplayVersion,
            AdminVersionService.BuildVersion, null);

        RectTransform row = AddRow(card);
        AddRowLabel(row, AdminStrings.Get("buildVersion"));
        TMP_Text build = AdminUi.CreateText(row, "Value", AdminVersionService.BuildVersion,
                                            AdminUi.FontBody, TextAlignmentOptions.MidlineLeft, AdminUi.Muted);
        LayoutElement layout = build.gameObject.AddComponent<LayoutElement>();
        layout.preferredWidth = 320f;
        layout.minWidth = 200f;
        layout.flexibleWidth = 1f;

        RectTransform revisionRow = AddRow(card);
        AddRowLabel(revisionRow, AdminStrings.Get("revision"));
        TMP_Text revision = AdminUi.CreateText(revisionRow, "Value",
            AdminVersionService.ContentRevision.ToString(), AdminUi.FontBody,
            TextAlignmentOptions.MidlineLeft, AdminUi.Muted);
        LayoutElement revisionLayout = revision.gameObject.AddComponent<LayoutElement>();
        revisionLayout.preferredWidth = 320f;
        revisionLayout.minWidth = 200f;
        revisionLayout.flexibleWidth = 1f;
    }

    private void BuildExportCard(RectTransform content)
    {
        RectTransform card = AddCard(content, AdminStrings.Get("exportConfig"));

        string json = AdminContentStore.ExportJson();
        if (json.Length > 400) json = json.Substring(0, 400) + "\n...";

        RectTransform row = AddRow(card);
        AddRowLabel(row, AdminStrings.Get("exportConfig"));

        TMP_Text preview = AdminUi.CreateText(row, "Preview", json, AdminUi.FontSmall,
                                              TextAlignmentOptions.TopLeft, AdminUi.Muted);
        LayoutElement layout = preview.gameObject.AddComponent<LayoutElement>();
        layout.preferredWidth = 520f;
        layout.minWidth = 320f;
        layout.flexibleWidth = 1f;

        AddButtonRow(card, AdminStrings.Get("copyClipboard"), () =>
        {
            GUIUtility.systemCopyBuffer = AdminContentStore.ExportJson();
            Commit("content", "Exported the admin configuration");
            Toast(AdminStrings.Get("copied"));
        }, AdminUi.AccentDim);
    }

    private void BuildImportCard(RectTransform content)
    {
        RectTransform card = AddCard(content, AdminStrings.Get("importConfig"));

        RectTransform row = AddRow(card);
        AddRowLabel(row, AdminStrings.Get("importConfig"));

        TMP_InputField input = AddRowInput(row, "", true);
        LayoutElement layout = input.gameObject.AddComponent<LayoutElement>();
        layout.preferredWidth = 520f;
        layout.minWidth = 320f;
        layout.flexibleWidth = 1f;

        RectTransform hintRow = AddRow(card);
        AddRowLabel(hintRow, AdminStrings.Get("importHint"));

        AddButtonRow(card, AdminStrings.Get("pasteClipboard"), () =>
        {
            input.text = GUIUtility.systemCopyBuffer;
        }, AdminUi.CardAlt);

        AddButtonRow(card, AdminStrings.Get("importConfig"), () =>
        {
            string error;
            if (!AdminContentStore.ImportJson(input.text, out error))
            {
                Toast(AdminStrings.Get("importFailed", error));
                return;
            }

            Commit("content", "Imported an admin configuration");
            Toast(AdminStrings.Get("imported"));
            if (Root != null) Root.RefreshCurrentTab();
        }, AdminUi.AccentDim);
    }

    private void BuildRestoreCard(RectTransform content)
    {
        RectTransform card = AddCard(content, AdminStrings.Get("restoreSection"));

        AddButtonRow(card, AdminStrings.Get("restoreSection"), () =>
        {
            AdminContentStore.ResetAll();
            Commit("content", "Restored every built-in admin setting");
            Toast(AdminStrings.Get("reset"));
            if (Root != null) Root.RefreshCurrentTab();
        }, AdminUi.Danger);
    }
}
