using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The Progress tab: let the teacher grant, clear or correct a student's
/// progress.
///
/// These values live in PlayerPrefs, not in the override layer, so a change
/// here is written straight to the device and is picked up by the game the
/// next time it reads the value. The profile screen and the tower locks
/// refresh themselves when they are shown again, so a change made here shows
/// up as soon as the teacher navigates back.
/// </summary>
public class AdminTabProgress : AdminTabBase
{
    private bool _resetArmed;

    public AdminTabProgress(AdminPanelRoot root) : base(root) { }

    public override string TitleKey => "tabProgress";

    public override void Build(RectTransform content)
    {
        IReadOnlyList<AdminGameConfig.LevelEntry> levels = AdminGameConfig.Levels;

        for (int i = 0; i < levels.Count; i++)
        {
            BuildStageCard(content, levels[i]);
        }

        BuildGlobalCard(content);
    }

    private void BuildStageCard(RectTransform content, AdminGameConfig.LevelEntry level)
    {
        AdminProgressService.StageSnapshot snap = AdminProgressService.Snapshot(level.stageId);
        RectTransform card = AddCard(content, level.labelEn + "  (" + level.stageId + ")");

        AddNumberRow(card, AdminStrings.Get("highScore"), snap.highScore,
            value => AdminProgressService.SetHighScore(level.stageId, value), 0f, 999f);

        AddToggleRow(card, AdminStrings.Get("firstClear"), snap.firstCleared,
            value => AdminProgressService.SetFirstCleared(level.stageId, value));

        AddNumberRow(card, AdminStrings.Get("attempts"), snap.attempts,
            value => AdminProgressService.SetAttempts(level.stageId, value), 0f, 999f);

        AddNumberRow(card, AdminStrings.Get("hints"), snap.hints,
            value => AdminProgressService.SetHints(level.stageId, value), 0f, 99f);

        AddToggleRow(card, level.towerName, snap.unlocked,
            value => AdminProgressService.SetTowerUnlocked(level.towerName, value));

        BuildBadgeRow(card, level, snap);

        AddButtonRow(card, AdminStrings.Get("reset") + " " + level.labelEn, () =>
        {
            AdminProgressService.ResetStage(level.stageId);
            AdminProgressService.SetTowerUnlocked(level.towerName, false);
            Commit("progress", "Reset the progress of " + level.stageId);
            Toast(AdminStrings.Get("reset"));
            if (Root != null) Root.RefreshCurrentTab();
        }, AdminUi.Danger);
    }

    private void BuildBadgeRow(RectTransform card, AdminGameConfig.LevelEntry level,
                                AdminProgressService.StageSnapshot snap)
    {
        RectTransform row = AddRow(card);

        TMP_Text label = AdminUi.CreateText(row, "Label", AdminStrings.Get("badges"), AdminUi.FontBody,
                                            TextAlignmentOptions.MidlineLeft, AdminUi.Text);
        LayoutElement labelLayout = label.gameObject.AddComponent<LayoutElement>();
        labelLayout.preferredWidth = 520f;
        labelLayout.minWidth = 320f;
        labelLayout.flexibleWidth = 1f;

        AchievementType[] types = AdminGameConfig.BadgeTypes;
        for (int i = 0; i < types.Length; i++)
        {
            AchievementType type = types[i];
            bool earned = snap.badges != null && i < snap.badges.Length && snap.badges[i];

            Toggle toggle = AdminUi.CreateToggle(row, "Badge_" + type, earned);
            LayoutElement layout = toggle.gameObject.AddComponent<LayoutElement>();
            layout.preferredWidth = 76f;
            layout.minWidth = 76f;
            layout.flexibleWidth = 0f;

            TMP_Text caption = AdminUi.CreateText(toggle.transform, "Caption",
                AdminGameConfig.BadgeLetter(type), AdminUi.FontSmall,
                TextAlignmentOptions.Center, AdminUi.Text);
            AdminUi.Stretch(caption.rectTransform, 0f, 0f);

            string stageId = level.stageId;
            toggle.onValueChanged.AddListener(value =>
            {
                AdminProgressService.SetBadge(stageId, type, value);
                Commit("progress", "Set the badge " + type + " for " + stageId);
            });
        }
    }

    private void BuildGlobalCard(RectTransform content)
    {
        RectTransform card = AddCard(content, AdminStrings.Get("runeKeys"));

        AddNumberRow(card, AdminStrings.Get("runeKeys"), AdminProgressService.GetRuneKeys(),
            value => AdminProgressService.SetRuneKeys(value), 0f, 999f);

        // Resetting everything is destructive, so it takes two taps.
        AddButtonRow(card, _resetArmed ? AdminStrings.Get("confirmReset") : AdminStrings.Get("resetProgress"),
            () =>
            {
                if (!_resetArmed)
                {
                    _resetArmed = true;
                    if (Root != null) Root.RefreshCurrentTab();
                    return;
                }

                _resetArmed = false;
                AdminProgressService.ResetAllProgress();
                Commit("progress", "Reset all player progress");
                Toast(AdminStrings.Get("reset"));
                if (Root != null) Root.RefreshCurrentTab();
            }, AdminUi.Danger);
    }
}
