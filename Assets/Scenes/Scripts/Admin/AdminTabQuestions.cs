using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The Questions tab: list, search, add, edit, delete and switch questions on
/// and off.
///
/// The pool comes from AdminQuestionCatalog, which merges the teacher's
/// override over the built-in catalogue, and the game reads the same merged
/// pool through QuestionGeneratorBeta. So what this tab shows is exactly
/// what the player will be asked.
///
/// A question that is switched off stays in the pool but is never asked.
/// </summary>
public class AdminTabQuestions : AdminTabBase
{
    private string _levelKey = "Level 1";
    private int _index;
    private string _search = "";

    // The question buttons are created once and filtered in place, so typing
    // in the search box never rebuilds (and so destroys) the search field.
    private readonly List<GameObject> _listRows = new List<GameObject>();
    private readonly List<QuestionData> _listValues = new List<QuestionData>();

    public AdminTabQuestions(AdminPanelRoot root) : base(root) { }

    public override string TitleKey => "tabQuestions";

    public override void Build(RectTransform content)
    {
        BuildLevelPicker(content);
        BuildSearch(content);
        BuildList(content);
        BuildEditor(content);
    }

    // ------------------------------------------------------------------
    // Level picker
    // ------------------------------------------------------------------

    private void BuildLevelPicker(RectTransform content)
    {
        RectTransform card = AddCard(content, AdminStrings.Get("level"));

        // The towers sit side by side in a row, like tabs, rather than
        // stacked one per line.
        RectTransform row = AddRow(card, 72f);

        IReadOnlyList<AdminGameConfig.LevelEntry> levels = AdminGameConfig.Levels;
        for (int i = 0; i < levels.Count; i++)
        {
            AdminGameConfig.LevelEntry level = levels[i];
            bool selected = level.sceneName == _levelKey;

            Button button = AdminUi.CreateButton(row, "Level_" + level.sceneName, level.labelEn,
                () =>
                {
                    _levelKey = level.sceneName;
                    _index = 0;
                    if (Root != null) Root.RefreshCurrentTab();
                },
                selected ? AdminUi.Accent : AdminUi.CardAlt, AdminUi.FontBody);

            LayoutElement layout = AdminUi.EnsureLayout(button.gameObject);
            layout.preferredWidth = 220f;
            layout.minWidth = 120f;
            layout.flexibleWidth = 1f;
        }
    }

    // ------------------------------------------------------------------
    // Search
    // ------------------------------------------------------------------

    private void BuildSearch(RectTransform content)
    {
        RectTransform card = AddCard(content, AdminStrings.Get("search"));

        RectTransform row = AddRow(card);
        AddRowLabel(row, AdminStrings.Get("search"));

        TMP_InputField input = AddRowInput(row, _search);
        input.onValueChanged.AddListener(text =>
        {
            _search = text ?? "";
            // Filter in place. Rebuilding the tab here would destroy the
            // search field while it is being typed into.
            ApplyFilter();
        });
    }

    private void ApplyFilter()
    {
        for (int i = 0; i < _listRows.Count; i++)
        {
            QuestionData question = _listValues[i];
            bool match = true;
            if (!string.IsNullOrEmpty(_search) && question != null)
            {
                match = question.question != null &&
                        question.question.IndexOf(_search, StringComparison.OrdinalIgnoreCase) >= 0;
            }
            if (_listRows[i] != null) _listRows[i].SetActive(match);
        }
    }

    // ------------------------------------------------------------------
    // The list of questions
    // ------------------------------------------------------------------

    private void BuildList(RectTransform content)
    {
        List<QuestionData> pool = AdminQuestionCatalog.GetEffective(_levelKey);
        if (pool == null) pool = new List<QuestionData>();

        RectTransform card = AddCard(content, AdminStrings.Get("questionCount", pool.Count));

        _listRows.Clear();
        _listValues.Clear();

        for (int i = 0; i < pool.Count; i++)
        {
            QuestionData question = pool[i];
            if (question == null) continue;

            int position = i;
            string label = (i + 1) + ". " + Truncate(question.question, 44) +
                           (question.isEnabled ? "" : "  (off)");

            Button button = AdminUi.CreateButton(card, "Question_" + i, label, () =>
            {
                _index = position;
                if (Root != null) Root.RefreshCurrentTab();
            }, i == _index ? AdminUi.Accent : AdminUi.CardAlt, AdminUi.FontSmall);

            LayoutElement layout = AdminUi.EnsureLayout(button.gameObject);
            layout.minHeight = 64f;
            layout.preferredHeight = 64f;
            layout.preferredWidth = 300f;
            layout.minWidth = 200f;
            layout.flexibleWidth = 1f;

            _listRows.Add(button.gameObject);
            _listValues.Add(question);
        }

        ApplyFilter();

        AddButtonRow(card, AdminStrings.Get("addQuestion"), () =>
        {
            List<QuestionData> working = EnsureWorkingPool();
            if (working == null) return;

            QuestionData fresh = new QuestionData
            {
                question = "New question",
                answerA = "Answer A",
                answerB = "Answer B",
                answerC = "Answer C",
                answerD = "Answer D",
                correctAnswer = "A",
                isEnabled = true,
            };
            working.Add(fresh);
            _index = working.Count - 1;
            Commit("Questions", "Added a question to " + _levelKey);
            if (Root != null) Root.RefreshCurrentTab();
        }, AdminUi.AccentDim);
    }

    private static string Truncate(string value, int max)
    {
        if (string.IsNullOrEmpty(value)) return "(empty)";
        value = value.Replace("\n", " ");
        return value.Length <= max ? value : value.Substring(0, max) + "...";
    }

    // ------------------------------------------------------------------
    // The editor for the selected question
    // ------------------------------------------------------------------

    private void BuildEditor(RectTransform content)
    {
        List<QuestionData> pool = AdminQuestionCatalog.GetEffective(_levelKey);
        if (pool == null || pool.Count == 0) return;

        if (_index < 0) _index = 0;
        if (_index >= pool.Count) _index = pool.Count - 1;

        QuestionData question = pool[_index];
        RectTransform card = AddCard(content, AdminStrings.Get("questionCount", pool.Count));

        BuildNavigator(card, pool);

        TMP_Text warning = AdminUi.CreateText(card, "Warning", "", AdminUi.FontSmall,
                                              TextAlignmentOptions.TopLeft, AdminUi.Danger);
        AdminUi.SetLayout(warning.gameObject, 40f, 40f);

        AddOverrideQuestionField(card, AdminStrings.Get("questionText"), "question", question, false);
        AddOverrideQuestionField(card, AdminStrings.Get("questionText") + " (FIL)", "questionFilipino", question, false);
        AddOverrideQuestionField(card, AdminStrings.Get("answerA"), "answerA", question, false);
        AddOverrideQuestionField(card, AdminStrings.Get("answerA") + " (FIL)", "answerAFilipino", question, false);
        AddOverrideQuestionField(card, AdminStrings.Get("answerB"), "answerB", question, false);
        AddOverrideQuestionField(card, AdminStrings.Get("answerB") + " (FIL)", "answerBFilipino", question, false);
        AddOverrideQuestionField(card, AdminStrings.Get("answerC"), "answerC", question, false);
        AddOverrideQuestionField(card, AdminStrings.Get("answerC") + " (FIL)", "answerCFilipino", question, false);
        AddOverrideQuestionField(card, AdminStrings.Get("answerD"), "answerD", question, false);
        AddOverrideQuestionField(card, AdminStrings.Get("answerD") + " (FIL)", "answerDFilipino", question, false);
        AddOverrideQuestionField(card, AdminStrings.Get("category"), "category", question, false);
        AddOverrideQuestionField(card, AdminStrings.Get("explanation"), "explanation", question, true);

        AddOverrideNumberField(card, AdminStrings.Get("difficulty"), question);
        AddOverrideEnabledField(card, question);

        BuildAnswerSelector(card, question);

        AddButtonRow(card, AdminStrings.Get("removeQuestion"), () =>
        {
            if (Root == null) return;
            int position = _index;
            Root.Confirm(AdminStrings.Get("removeQuestion"), AdminStrings.Get("confirmDeleteQuestion"),
                AdminStrings.Get("delete"), () =>
                {
                    List<QuestionData> working = EnsureWorkingPool();
                    if (working == null || working.Count <= 1) return;
                    working.RemoveAt(position);
                    if (_index >= working.Count) _index = working.Count - 1;
                    Commit("Questions", "Deleted a question from " + _levelKey);
                    Toast(AdminStrings.Get("reset"));
                    if (Root != null) Root.RefreshCurrentTab();
                });
        }, AdminUi.Danger);

        AddButtonRow(card, AdminStrings.Get("restoreQuestions"), () =>
        {
            AdminQuestionCatalog.ClearOverride(_levelKey);
            _index = 0;
            Commit("Questions", "Restored the built-in questions for " + _levelKey);
            if (Root != null) Root.RefreshCurrentTab();
        }, AdminUi.CardAlt);
    }

    private void BuildNavigator(RectTransform card, List<QuestionData> pool)
    {
        RectTransform row = AddRow(card);

        Button previous = AdminUi.CreateButton(row, "Previous", "<", () =>
        {
            _index = Mathf.Max(0, _index - 1);
            if (Root != null) Root.RefreshCurrentTab();
        }, AdminUi.CardAlt, AdminUi.FontHeading);
        LayoutElement previousLayout = AdminUi.EnsureLayout(previous.gameObject);
        previousLayout.preferredWidth = 110f;
        previousLayout.minWidth = 110f;
        previousLayout.flexibleWidth = 0f;

        TMP_Text label = AdminUi.CreateText(row, "Label", (_index + 1) + " / " + pool.Count,
                                            AdminUi.FontHeading, TextAlignmentOptions.Center, AdminUi.Text);
        LayoutElement labelLayout = AdminUi.EnsureLayout(label.gameObject);
        labelLayout.preferredWidth = 200f;
        labelLayout.minWidth = 160f;
        labelLayout.flexibleWidth = 1f;

        Button next = AdminUi.CreateButton(row, "Next", ">", () =>
        {
            _index = Mathf.Min(pool.Count - 1, _index + 1);
            if (Root != null) Root.RefreshCurrentTab();
        }, AdminUi.CardAlt, AdminUi.FontHeading);
        LayoutElement nextLayout = AdminUi.EnsureLayout(next.gameObject);
        nextLayout.preferredWidth = 110f;
        nextLayout.minWidth = 110f;
        nextLayout.flexibleWidth = 0f;
    }

    /// <summary>
    /// A text field for one property of the selected question. The pool is
    /// copied into an override the first time the teacher edits anything.
    /// </summary>
    private void AddOverrideQuestionField(RectTransform card, string label, string field,
                                          QuestionData question, bool multiline)
    {
        RectTransform row = AddRow(card, multiline ? 170f : 72f);
        AddRowLabel(row, label);

        string value = ReadField(question, field);
        TMP_InputField input = AddRowInput(row, value, multiline, 520f);

        string last = null;
        input.onValueChanged.AddListener(text =>
        {
            if (last == text) return;
            last = text;

            List<QuestionData> working = EnsureWorkingPool();
            if (working == null || _index >= working.Count) return;
            WriteField(working[_index], field, text);

            string problem = AdminValidation.ValidateQuestion(working[_index], working);
            if (problem.Length > 0)
            {
                Toast(problem);
            }
            Commit("Questions", "Edited " + field + " of question " + (_index + 1) + " in " + _levelKey);
        });
    }

    /// <summary>A number field for the difficulty of the selected question.</summary>
    private void AddOverrideNumberField(RectTransform card, string label, QuestionData question)
    {
        RectTransform row = AddRow(card);
        AddRowLabel(row, label);

        int value = question == null ? 0 : question.difficulty;

        TMP_InputField input = AddRowInput(row, value.ToString());
        input.contentType = TMP_InputField.ContentType.IntegerNumber;

        int last = int.MinValue;
        input.onValueChanged.AddListener(text =>
        {
            int parsed;
            if (!int.TryParse(text, out parsed)) return;
            if (last == parsed) return;
            last = parsed;

            List<QuestionData> working = EnsureWorkingPool();
            if (working == null || _index >= working.Count) return;
            working[_index].difficulty = Mathf.Clamp(parsed, 0, 5);
            Commit("Questions", "Set the difficulty of question " + (_index + 1) + " in " + _levelKey);
        });
    }

    /// <summary>Switches the selected question on or off.</summary>
    private void AddOverrideEnabledField(RectTransform card, QuestionData question)
    {
        RectTransform row = AddRow(card);
        AddRowLabel(row, AdminStrings.Get("enable"));

        bool enabled = question == null || question.isEnabled;
        Toggle toggle = AdminUi.CreateToggle(row, "Enabled", enabled);

        toggle.onValueChanged.AddListener(value =>
        {
            List<QuestionData> working = EnsureWorkingPool();
            if (working == null || _index >= working.Count) return;
            working[_index].isEnabled = value;
            Commit("Questions", (value ? "Enabled" : "Disabled") + " question " + (_index + 1) + " in " + _levelKey);
            if (Root != null) Root.RefreshCurrentTab();
        });
    }

    private void BuildAnswerSelector(RectTransform card, QuestionData question)
    {
        RectTransform row = AddRow(card);
        AddRowLabel(row, AdminStrings.Get("correctAnswer"));

        string current = question == null || question.correctAnswer == null ? "A" : question.correctAnswer;

        for (int i = 0; i < 4; i++)
        {
            string letter = ((char)('A' + i)).ToString();
            bool selected = string.Equals(current.Trim(), letter, StringComparison.OrdinalIgnoreCase);

            Button button = AdminUi.CreateButton(row, "Answer" + letter, letter, () =>
            {
                List<QuestionData> working = EnsureWorkingPool();
                if (working == null || _index >= working.Count) return;
                working[_index].correctAnswer = letter;
                Commit("Questions", "Set the correct answer of question " + (_index + 1) + " in " + _levelKey);
                if (Root != null) Root.RefreshCurrentTab();
            }, selected ? AdminUi.Accent : AdminUi.CardAlt, AdminUi.FontBody);

            LayoutElement layout = AdminUi.EnsureLayout(button.gameObject);
            layout.preferredWidth = 110f;
            layout.minWidth = 110f;
            layout.flexibleWidth = 0f;
        }
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
        if (question == null) return "";
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
            case "category": return question.category ?? "";
            case "explanation": return question.explanation ?? "";
        }
        return "";
    }

    private static void WriteField(QuestionData question, string field, string value)
    {
        if (question == null) return;
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
            case "category": question.category = value; break;
            case "explanation": question.explanation = value; break;
        }
    }
}
