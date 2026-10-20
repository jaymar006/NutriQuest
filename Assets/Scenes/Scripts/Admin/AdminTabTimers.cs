using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The Timers and Difficulty tab.
///
/// Every row here writes to the override layer, so the change applies the
/// next time the game reads the value and survives a restart. The built-in
/// default is shown next to any row that has been changed, and a reset
/// button puts that row back.
/// </summary>
public class AdminTabTimers : AdminTabBase
{
    public AdminTabTimers(AdminPanelRoot root) : base(root) { }

    public override string TitleKey => "tabTimers";

    public override void Build(RectTransform content)
    {
        BuildTimerCard(content);
        BuildDifficultyCard(content);
        BuildRewardCard(content);
        BuildTargetCard(content);
    }

    private void BuildTimerCard(RectTransform content)
    {
        RectTransform card = AddCard(content, AdminStrings.Get("timers"));

        AddOverrideFloatRow(card, AdminStrings.Get("questionSeconds"), AdminGameConfig.KeyQuestionSeconds,
            AdminGameConfig.DefaultQuestionSeconds, null, 1f, 300f);

        AddOverrideNumberRow(card, AdminStrings.Get("questionsPerGame"), AdminGameConfig.KeyQuestionsPerGame,
            AdminGameConfig.DefaultQuestionsPerGame, null, 1f, 200f);

        AddOverrideFloatRow(card, AdminStrings.Get("delayNext"), AdminGameConfig.KeyDelayNextQuestion,
            AdminGameConfig.DefaultDelayNextQuestion, null, 0f, 60f);

        AddOverrideFloatRow(card, AdminStrings.Get("towerWait"), AdminGameConfig.KeyTowerWaitMinutes,
            AdminGameConfig.DefaultTowerWaitMinutes, null, 0f, 1440f);

        AddOverrideFloatRow(card, AdminStrings.Get("loadingMin"), AdminGameConfig.KeyLoadingMinSeconds,
            AdminGameConfig.DefaultLoadingMinSeconds, null, 0f, 120f);
    }

    private void BuildDifficultyCard(RectTransform content)
    {
        RectTransform card = AddCard(content, AdminStrings.Get("tabTimers"));

        AddOverrideNumberRow(card, AdminStrings.Get("maxHints"), AdminGameConfig.KeyMaxHints,
            AdminGameConfig.DefaultMaxHints, null, 0f, 20f);

        AddOverrideNumberRow(card, AdminStrings.Get("maxRuneKeys"), AdminGameConfig.KeyMaxRuneKeys,
            AdminGameConfig.DefaultMaxRuneKeys, null, 0f, 99f);

        AddOverrideFloatRow(card, AdminStrings.Get("regenMinutes"), AdminGameConfig.KeyRuneRegenMinutes,
            AdminGameConfig.DefaultRuneRegenMinutes, null, 0f, 1440f);

        AddOverrideNumberRow(card, AdminStrings.Get("costNewTower"), AdminGameConfig.KeyCostNewTower,
            AdminGameConfig.DefaultCostNewTower, null, 0f, 99f);

        AddOverrideNumberRow(card, AdminStrings.Get("costRetry"), AdminGameConfig.KeyCostRechallengeTower,
            AdminGameConfig.DefaultCostRechallengeTower, null, 0f, 99f);

        AddOverrideNumberRow(card, AdminStrings.Get("costNewTower") + " (T4)", AdminGameConfig.KeyCostNewTower4,
            AdminGameConfig.DefaultCostNewTower4, null, 0f, 99f);

        AddOverrideNumberRow(card, AdminStrings.Get("costRetry") + " (T4)", AdminGameConfig.KeyCostRechallengeTower4,
            AdminGameConfig.DefaultCostRechallengeTower4, null, 0f, 99f);

        AddOverrideNumberRow(card, AdminStrings.Get("tower4Total"), AdminGameConfig.KeyTower4RequiredTotal,
            AdminGameConfig.DefaultTower4RequiredTotal, null, 0f, 999f);
    }

    private void BuildRewardCard(RectTransform content)
    {
        RectTransform card = AddCard(content, AdminStrings.Get("badges"));

        AchievementType[] types = AdminGameConfig.BadgeTypes;
        for (int i = 0; i < types.Length; i++)
        {
            AchievementType type = types[i];
            string label = AdminStrings.Get(AdminGameConfig.BadgeLabelKey(type));

            AddOverrideNumberRow(card, AdminStrings.Get("dropChance") + " - " + label,
                AdminGameConfig.DropChanceKey(type), AdminGameConfig.DefaultDropChance(type), null, 0f, 100f);

            AddOverrideNumberRow(card, AdminStrings.Get("hintDropChance") + " - " + label,
                AdminGameConfig.HintDropChanceKey(type), AdminGameConfig.DefaultHintDropChance(type), null, 0f, 100f);
        }

        AddOverrideNumberRow(card, AdminStrings.Get("hints"), AdminGameConfig.KeyHintRewardAmount,
            AdminGameConfig.DefaultHintRewardAmount, null, 0f, 20f);
    }

    private void BuildTargetCard(RectTransform content)
    {
        RectTransform card = AddCard(content, AdminStrings.Get("passTarget"));

        IReadOnlyList<AdminGameConfig.LevelEntry> levels = AdminGameConfig.Levels;
        for (int i = 0; i < levels.Count; i++)
        {
            AdminGameConfig.LevelEntry level = levels[i];

            AddOverrideNumberRow(card, AdminStrings.Get("passTarget") + " - " + level.labelEn,
                AdminGameConfig.PassTargetKey(level.stageId),
                AchievementData.GetDefaultPassTarget(level.stageId), null, 0f, 99f);

            AddOverrideNumberRow(card, AdminStrings.Get("conquerorTarget") + " - " + level.labelEn,
                AdminGameConfig.ConquerorTargetKey(level.stageId),
                AchievementData.GetDefaultConquerorTarget(level.stageId), null, 0f, 99f);
        }
    }
}
