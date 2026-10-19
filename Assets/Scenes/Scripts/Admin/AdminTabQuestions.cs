using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The Questions tab: edit the questions a student sees.
///
/// The built-in pool for each level was extracted into
/// Assets/Resources/Data/questions.json. The panel edits an override pool;
/// QuestionGeneratorBeta reads the override when one exists and the built-in
/// pool otherwise, so the built-in content is never lost.
///
/// Only one question is shown at a time. With twenty questions per level in
/// two languages that is around eleven fields, which is far more readable as
/// a single form than as a wall of inputs.
/// </summary>
public class AdminTabQuestions : AdminTabBase
{
    private string _levelKey = "Level 1";
    private int _index;

    public AdminTabQuestions(AdminPanelRoot root) : base(root) { }

    public override string TitleKey => "tabQuestions";

    public override void Build(RectTransform content)
    {
        BuildLevelPicker(content);
        BuildQuestionEditor(content);
    }

    // ------------------------------------------------------------------
    // Level picker
    // ------------------------------------------------------------------

    private void BuildLevelPicker(RectTransform content)
    {
        RectTransform card = AddCard(content, AdminStrings.Get("level"));

        IReadOnlyList<AdminGameConfig.LevelEntry> levels = AdminGameConfig.Levels;
        for (int i = 0; i < levels.Count; i++)
        {
            AdminGameConfig.LevelEntry level = levels[i];
            bool selected = level.sceneName == _levelKey;

            Button button = AdminUi.CreateButton(card, "Level_" + level.sceneName, level.labelEn,
                () =>
                {
                    _levelKey = level.sceneName;
                    _index = 0;
                    if (Root != null) Root.RefreshCurrentTab();
                },
                selected ? AdminUi.Accent : AdminUi.CardAlt, AdminUi.FontBody);

            LayoutElement layout = button.gameObject.AddComponent<LayoutElement>();
            layout.preferredWidth = 220f;
            layout.minWidth = 160f;
            layout.flexibleWidth = 1f;
        }
    }

    // ------------------------------------------------------------------
    // Question editor
    // ------------------------------------------------------------------

    private void BuildQuestionEditor(RectTransform content)
    {
        List<QuestionData> pool = AdminQuestionCatalog.GetEffective(_levelKey);

        if (pool == null)
        {
            RectTransform card = AddCard(content, AdminStrings.Get("questions"));
            AddButtonRow(card, AdminStrings.Get("restoreQuestions"), () =>
            {
                AdminQuestionCatalog.SeedOverride(_levelKey);
                Commit("questions", "Restored the built-in questions for " + _levelKey);
                if (Root != null) Root.RefreshCurrentTab();
            });
            return;
        }

        if (_index < 0) _index = 0;
        if (_index >= pool.Count) _index = Mathf.Max(0, pool.Count - 1);

        RectTransform card = AddCard(content, AdminStrings.Get("questionCount", pool.Count));

        BuildNavigator(card, pool);
        BuildFields(card, pool);
        BuildAnswerSelector(card, pool);
        BuildPoolActions(card, pool);
    }

    private void BuildNavigator(RectTransform card, List<QuestionData> pool)
    {
        RectTransform row = AddRow(card);

        Button previous = AdminUi.CreateButton(row, "Previous", "<", () =>
        {
            _index = Mathf.Max(0, _index - 1);
            if (Root != null) Root.RefreshCurrentTab();
        }, AdminUi.CardAlt, AdminUi.FontHeading);
        LayoutElement previousLayout = previous.gameObject.AddComponent<LayoutElement>();
        previousLayout.preferredWidth = 110f;
        previousLayout.minWidth = 110f;
        previousLayout.flexibleWidth = 0f;

        TMP_Text label = AdminUi.CreateText(row, "Label", (_index + 1) + " / " + pool.Count,
                                            AdminUi.FontHeading, TextAlignmentOptions.Center, AdminUi.Text);
        LayoutElement labelLayout = label.gameObject.AddComponent<LayoutElement>();
        labelLayout.preferredWidth = 200f;
        labelLayout.minWidth = 160f;
        labelLayout.flexibleWidth = 1f;

        Button next = AdminUi.CreateButton(row, "Next", ">", () =>
        {
            _index = Mathf.Min(pool.Count - 1, _index + 1);
            if (Root != null) Root.RefreshCurrentTab();
        }, AdminUi.CardAlt, AdminUi.FontHeading);
        LayoutElement nextLayout = next.gameObject.AddComponent<LayoutElement>();
        nextLayout.preferredWidth = 110f;
        nextLayout.minWidth = 110f;
        nextLayout.flexibleWidth = 0f;
    }

    private void BuildFields(RectTransform card, List<QuestionData> pool)
    {
        QuestionData question = pool[_index];

        AddOverrideQuestionField(card, AdminStrings.Get("questionText"), "question", question);
        AddOverrideQuestionField(card, AdminStrings.Get("questionText") + " (FIL)", "questionFilipino", question);
        AddOverrideQuestionField(card, AdminStrings.Get("answerA"), "answerA", question);
        AddOverrideQuestionField(card, AdminStrings.Get("answerA") + " (FIL)", "answerAFilipino", question);
        AddOverrideQuestionField(card, AdminStrings.Get("answerB"), "answerB", question);
        AddOverrideQuestionField(card, AdminStrings.Get("answerB") + " (FIL)", "answerBFilipino", question);
        AddOverrideQuestionField(card, AdminStrings.Get("answerC"), "answerC", question);
        AddOverrideQuestionField(card, AdminStrings.Get("answerC") + " (FIL)", "answerCFilipino", question);
        AddOverrideQuestionField(card, AdminStrings.Get("answerD"), "answerD", question);
        AddOverrideQuestionField(card, AdminStrings.Get("answerD") + " (FIL)", "answerDFilipino", question);
    }

    /// <summary>
    /// Adds a text field for one property of the selected question. The pool
    /// is copied into an override the first time the teacher edits anything.
    /// </summary>
    private void AddOverrideQuestionField(RectTransform card, string label, string field, QuestionData question)
    {
        RectTransform row = AddRow(card, 170f);
        AddRowLabel(row, label);

        string value = ReadField(question, field);
        TMP_InputField input = AddRowInput(row, value, true);

        input.onValueChanged.AddListener(text =>
        {
            List<QuestionData> working = EnsureWorkingPool();
            if (working == null || _index >= working.Count) return;
            WriteField(working[_index], field, text);
            Commit("questions", "Edited " + field + " of question " + (_index + 1) + " in " + _levelKey);
        });
    }

    private void BuildAnswerSelector(RectTransform card, List<QuestionData> pool)
    {
        RectTransform row = AddRow(card);
        AddRowLabel(row, AdminStrings.Get("correctAnswer"));

        string current = pool[_index].correctAnswer ?? "A";

        for (int i = 0; i < 4; i++)
        {
            string letter = ((char)('A' + i)).ToString();
            bool selected = string.Equals(current.Trim(), letter, System.StringComparison.OrdinalIgnoreCase);

            Button button = AdminUi.CreateButton(row, "Answer" + letter, letter, () =>
            {
                List<QuestionData> working = EnsureWorkingPool();
                if (working == null || _index >= working.Count) return;
                working[_index].correctAnswer = letter;
                Commit("questions", "Set the correct answer of question " + (_index + 1) + " in " + _levelKey);
                if (Root != null) Root.RefreshCurrentTab();
            }, selected ? AdminUi.Accent : AdminUi.CardAlt, AdminUi.FontBody);

            LayoutElement layout = button.gameObject.AddComponent<LayoutElement>();
            layout.preferredWidth = 110f;
            layout.minWidth = 110f;
            layout.flexibleWidth = 0f;
        }
    }

    private void BuildPoolActions(RectTransform card, List<QuestionData> pool)
    {
        AddButtonRow(card, AdminStrings.Get("addQuestion"), () =>
        {
            List<QuestionData> working = EnsureWorkingPool();
            if (working == null) return;
            working.Add(new QuestionData
            {
                question = "New question",
                questionFilipino = "Bagong tanong",
                answerA = "A",
                answerB = "B",
                answerC = "C",
                answerD = "D",
                correctAnswer = "A",
            });
            _index = working.Count - 1;
            Commit("questions", "Added a question to " + _levelKey);
            if (Root != null) Root.RefreshCurrentTab();
        }, AdminUi.AccentDim);

        AddButtonRow(card, AdminStrings.Get("removeQuestion"), () =>
        {
            List<QuestionData> working = EnsureWorkingPool();
            if (working == null || working.Count <= 1) return;
            working.RemoveAt(_index);
            if (_index >= working.Count) _index = working.Count - 1;
            Commit("questions", "Removed a question from " + _levelKey);
            if (Root != null) Root.RefreshCurrentTab();
        }, AdminUi.Danger);

        AddButtonRow(card, AdminStrings.Get("restoreQuestions"), () =>
        {
            AdminQuestionCatalog.ClearOverride(_levelKey);
            _index = 0;
            Commit("questions", "Restored the built-in questions for " + _levelKey);
            if (Root != null) Root.RefreshCurrentTab();
        }, AdminUi.CardAlt);
    }

    /// <summary>
    /// Returns the pool the teacher is editing, creating the override from
    /// the built-in pool the first time anything is changed.
    /// </summary>
    private List<QuestionData> EnsureWorkingPool()
    {
        if (!AdminContentStore.HasQuestionPool(_levelKey))
        {
            AdminQuestionCatalog.SeedOverride(_levelKey);
        }
        return AdminContentStore.GetQuestionPool(_levelKey);
    }

    private static string ReadField(QuestionData question, string field)
    {
        switch (field)
        {
            case "question": return question.question ?? "";
            case "questionFilipino": return question.questionFilipino ?? "";
            case "answerA": return question.answerA ?? "";
            case "answerAFilipino": return question.answerAFilipino ?? "";
            case "answerB": return question.answerB ?? "";
            case "answerBFilipino": return question.answerBFilipino ?? "";
            case "answerC": return question.answerC ?? "";
            case "answerCFilipino": return question.answerCFilipino ?? "";
            case "answerD": return question.answerD ?? "";
            case "answerDFilipino": return question.answerDFilipino ?? "";
        }
        return "";
    }

    private static void WriteField(QuestionData question, string field, string value)
    {
        switch (field)
        {
            case "question": question.question = value; break;
            case "questionFilipino": question.questionFilipino = value; break;
            case "answerA": question.answerA = value; break;
            case "answerAFilipino": question.answerAFilipino = value; break;
            case "answerB": question.answerB = value; break;
            case "answerBFilipino": question.answerBFilipino = value; break;
            case "answerC": question.answerC = value; break;
            case "answerCFilipino": question.answerCFilipino = value; break;
            case "answerD": question.answerD = value; break;
            case "answerDFilipino": question.answerDFilipino = value; break;
        }
    }
}
