using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The Content tab: the displayed version, the other editable game content
/// (categories, levels and rewards), and the import and export tools.
///
/// Everything here writes through AdminContentStore, so the game picks the
/// change up the next time it reads the value, and the built-in content is
/// never touched.
/// </summary>
public class AdminTabContent : AdminTabBase
{
    public AdminTabContent(AdminPanelRoot root) : base(root) { }

    public override string TitleKey => "tabContent";

    public override void Build(RectTransform content)
    {
        BuildVersionCard(content);
        BuildContentGroup(content, "categories", AdminStrings.Get("categories"));
        BuildContentGroup(content, "levels", AdminStrings.Get("levels"));
        BuildContentGroup(content, "rewards", AdminStrings.Get("rewards"));
        BuildTransferCard(content);
        BuildPatchCard(content);
        BuildBackupCard(content);
        BuildRestoreCard(content);
    }

    // ------------------------------------------------------------------
    // Version
    // ------------------------------------------------------------------

    private void BuildVersionCard(RectTransform content)
    {
        RectTransform card = AddCard(content, AdminStrings.Get("content"));

        RectTransform versionRow = AddRow(card);
        AddRowLabel(versionRow, AdminStrings.Get("displayVersion"));

        TMP_InputField input = AddRowInput(versionRow, AdminVersionService.DisplayVersion, false, 420f);
        string last = null;
        input.onValueChanged.AddListener(text =>
        {
            if (last == text) return;
            last = text;
            AdminVersionService.SetDisplayVersion(text);
            Commit("Content", "Set the displayed version to " + text, AdminVersionService.BuildVersion, text);
        });

        AddStatusRow(card, AdminStrings.Get("buildVersion"), AdminVersionService.BuildVersion);
        AddStatusRow(card, AdminStrings.Get("revision"),
            AdminVersionService.ContentRevision + " (" + AdminVersionService.LastChangedShort + ")");
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

    // ------------------------------------------------------------------
    // Categories, levels and rewards
    // ------------------------------------------------------------------

    private void BuildContentGroup(RectTransform content, string group, string title)
    {
        RectTransform card = AddCard(content, title);

        List<AdminContentStore.ContentEntry> entries = AdminContentStore.GetContent(group);
        if (entries.Count == 0)
        {
            // Nothing has been changed yet, so show the content the game
            // ships with. The teacher edits that, and the change becomes an
            // override.
            entries = AdminGameConfig.DefaultContent(group);
        }

        if (entries.Count == 0)
        {
            AddButtonRow(card, AdminStrings.Get("none"), () => { }, AdminUi.CardAlt);
        }
        else
        {
            for (int i = 0; i < entries.Count; i++)
            {
                AdminContentStore.ContentEntry entry = entries[i];
                if (entry == null) continue;

                RectTransform row = AddRow(card);
                AddRowLabel(row, entry.name);

                TMP_InputField input = AddRowInput(row, entry.value, false, 420f);
                string entryId = entry.id;
                string last = null;
                input.onValueChanged.AddListener(text =>
                {
                    if (last == text) return;
                    last = text;
                    AdminContentStore.ContentEntry live = EnsureEntry(group, entryId, entry.name);
                    if (live == null) return;
                    string old = live.value;
                    live.value = text;
                    Commit("Content", "Changed " + title + " '" + entry.name + "'", old, text);
                });

                Toggle toggle = AdminUi.CreateToggle(row, "Enabled", entry.enabled);
                toggle.onValueChanged.AddListener(value =>
                {
                    AdminContentStore.ContentEntry live = EnsureEntry(group, entryId, entry.name);
                    if (live == null) return;
                    bool old = live.enabled;
                    live.enabled = value;
                    Commit("Content", (value ? "Enabled" : "Disabled") + " " + title + " '" + entry.name + "'",
                        old.ToString(), value.ToString());
                });
            }
        }

        AddButtonRow(card, AdminStrings.Get("add"), () =>
        {
            AdminContentStore.ContentEntry fresh = new AdminContentStore.ContentEntry
            {
                group = group,
                id = group + "_" + System.DateTime.Now.ToString("HHmmssfff"),
                name = "New " + title,
                value = "",
                enabled = true
            };
            AdminContentStore.SetContentEntry(fresh);
            Commit("Content", "Added " + title + " '" + fresh.name + "'");
            if (Root != null) Root.RefreshCurrentTab();
        }, AdminUi.AccentDim);
    }

    /// <summary>
    /// Returns the stored entry, creating it from the shipped content the
    /// first time the teacher changes it.
    /// </summary>
    private static AdminContentStore.ContentEntry EnsureEntry(string group, string id, string name)
    {
        AdminContentStore.ContentEntry live = AdminContentStore.GetContentEntry(group, id);
        if (live != null) return live;

        live = new AdminContentStore.ContentEntry
        {
            group = group,
            id = id,
            name = name,
            value = "",
            enabled = true
        };
        AdminContentStore.SetContentEntry(live);
        return live;
    }

    // ------------------------------------------------------------------
    // Export and import
    // ------------------------------------------------------------------

    private void BuildTransferCard(RectTransform content)
    {
        RectTransform card = AddCard(content, AdminStrings.Get("exportConfig"));

        AddButtonRow(card, AdminStrings.Get("exportConfig"), () =>
        {
            string path = AdminContentStore.ExportToFile();
            GUIUtility.systemCopyBuffer = AdminContentStore.ExportJson();
            Toast(AdminStrings.Get("copied") + (path == null ? "" : "\n" + path));
            Commit("Content", "Exported the admin configuration");
        }, AdminUi.AccentDim);

        AddButtonRow(card, AdminStrings.Get("copyClipboard"), () =>
        {
            GUIUtility.systemCopyBuffer = AdminContentStore.ExportJson();
            Toast(AdminStrings.Get("copied"));
        }, AdminUi.CardAlt);

        RectTransform row = AddRow(card);
        AddRowLabel(row, AdminStrings.Get("importConfig"));

        TMP_InputField input = AddRowInput(row, "", true, 520f);

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
            Commit("Content", "Imported an admin configuration");
            Toast(AdminStrings.Get("imported"));
            if (Root != null) Root.RefreshCurrentTab();
        }, AdminUi.AccentDim);
    }

    // ------------------------------------------------------------------
    // Patches
    // ------------------------------------------------------------------

    private void BuildPatchCard(RectTransform content)
    {
        RectTransform card = AddCard(content, AdminStrings.Get("exportPatch"));

        AddButtonRow(card, AdminStrings.Get("exportPatch"), () =>
        {
            string path = AdminPatchService.ExportPatchToFile();
            GUIUtility.systemCopyBuffer = AdminPatchService.ExportPatchJson();
            Toast(AdminStrings.Get("copied") + (path == null ? "" : "\n" + path));
            Commit("Content", "Exported a content patch");
        }, AdminUi.AccentDim);

        RectTransform row = AddRow(card);
        AddRowLabel(row, AdminStrings.Get("importPatch"));

        TMP_InputField input = AddRowInput(row, "", true, 520f);

        AddButtonRow(card, AdminStrings.Get("pasteClipboard"), () =>
        {
            input.text = GUIUtility.systemCopyBuffer;
        }, AdminUi.CardAlt);

        AddButtonRow(card, AdminStrings.Get("importPatch"), () =>
        {
            string error;
            if (!AdminPatchService.ValidatePatchJson(input.text, out error))
            {
                Toast(error);
                return;
            }

            string applyError;
            if (!AdminPatchService.ApplyPatchJson(input.text, out applyError))
            {
                Toast(applyError);
                return;
            }

            Toast(AdminStrings.Get("patchValid"));
            if (Root != null) Root.RefreshCurrentTab();
        }, AdminUi.AccentDim);
    }

    // ------------------------------------------------------------------
    // Backups
    // ------------------------------------------------------------------

    private void BuildBackupCard(RectTransform content)
    {
        List<string> backups = AdminContentStore.GetBackups();
        if (backups.Count == 0) return;

        RectTransform card = AddCard(content, AdminStrings.Get("backups"));

        for (int i = 0; i < backups.Count; i++)
        {
            string path = backups[i];
            string label = System.IO.Path.GetFileName(path);

            AddButtonRow(card, AdminStrings.Get("restoreBackup") + " (" + label + ")", () =>
            {
                if (Root == null) return;
                Root.Confirm(AdminStrings.Get("restoreBackup"), label, AdminStrings.Get("apply"), () =>
                {
                    if (AdminContentStore.RestoreBackup(path))
                    {
                        Toast(AdminStrings.Get("patchValid"));
                        if (Root != null) Root.RefreshCurrentTab();
                    }
                });
            }, AdminUi.CardAlt);
        }
    }

    // ------------------------------------------------------------------
    // Reset
    // ------------------------------------------------------------------

    private void BuildRestoreCard(RectTransform content)
    {
        RectTransform card = AddCard(content, AdminStrings.Get("restoreSection"));

        AddButtonRow(card, AdminStrings.Get("restoreSection"), () =>
        {
            if (Root == null) return;
            Root.Confirm(AdminStrings.Get("restoreSection"), AdminStrings.Get("confirmWipe"),
                AdminStrings.Get("restoreSection"), () =>
                {
                    AdminContentStore.ResetAll();
                    Commit("Content", "Restored every built-in admin setting");
                    Toast(AdminStrings.Get("reset"));
                    if (Root != null) Root.RefreshCurrentTab();
                });
        }, AdminUi.Danger);
    }
}
