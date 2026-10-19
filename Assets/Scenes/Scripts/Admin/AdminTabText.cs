using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// The Text tab: change any wording that is baked into a scene.
///
/// Most of the game's words are plain TextMeshPro strings inside the scene
/// assets: the cutscene dialogue, the mascot's lines, the loading-screen
/// trivia, the help library, the recipe labels. A shipped build cannot edit
/// those assets, so the override layer stores a replacement keyed by the
/// original string, and AdminTextOverrideApplier swaps it in when the scene
/// loads.
///
/// That makes this tab the general escape hatch: pick a scene, find the
/// string, retype it. The scene picker loads the scene so the strings can be
/// discovered from the live hierarchy.
/// </summary>
public class AdminTabText : AdminTabBase
{
    private const int MaxListedStrings = 80;

    private string _selected;
    private string _search = "";

    public AdminTabText(AdminPanelRoot root) : base(root) { }

    public override string TitleKey => "tabText";

    public override void Build(RectTransform content)
    {
        BuildScenePicker(content);
        BuildSearch(content);
        BuildEditor(content);
        BuildStringList(content);
    }

    // ------------------------------------------------------------------
    // Scene picker
    // ------------------------------------------------------------------

    private void BuildScenePicker(RectTransform content)
    {
        RectTransform card = AddCard(content, AdminStrings.Get("source"));

        RectTransform row = AddRow(card);
        AddRowLabel(row, AdminStrings.Get("currentScene"));

        TMP_Text current = AdminUi.CreateText(row, "Current", SceneManager.GetActiveScene().name,
                                              AdminUi.FontSmall, TextAlignmentOptions.MidlineRight, AdminUi.Muted);
        LayoutElement currentLayout = current.gameObject.AddComponent<LayoutElement>();
        currentLayout.preferredWidth = 300f;
        currentLayout.minWidth = 200f;
        currentLayout.flexibleWidth = 0f;
    }

    private void BuildSceneButtons(RectTransform card)
    {
        string[] scenes = AdminGameConfig.TextScenes;
        for (int i = 0; i < scenes.Length; i++)
        {
            string sceneName = scenes[i];
            Button button = AdminUi.CreateButton(card, "Scene_" + sceneName, sceneName, () =>
            {
                _selected = null;
                SceneManager.LoadScene(sceneName);
            }, AdminUi.CardAlt, AdminUi.FontSmall);

            LayoutElement layout = button.gameObject.AddComponent<LayoutElement>();
            layout.preferredWidth = 280f;
            layout.minWidth = 200f;
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
            if (Root != null) Root.RefreshCurrentTab();
        });
    }

    // ------------------------------------------------------------------
    // The editor for the selected string
    // ------------------------------------------------------------------

    private void BuildEditor(RectTransform content)
    {
        if (string.IsNullOrEmpty(_selected)) return;

        RectTransform card = AddCard(content, AdminStrings.Get("newText"));

        RectTransform originalRow = AddRow(card);
        AddRowLabel(originalRow, AdminStrings.Get("originalText"));
        TMP_Text original = AdminUi.CreateText(originalRow, "Value", _selected, AdminUi.FontSmall,
                                               TextAlignmentOptions.MidlineLeft, AdminUi.Muted);
        LayoutElement originalLayout = original.gameObject.AddComponent<LayoutElement>();
        originalLayout.preferredWidth = 520f;
        originalLayout.minWidth = 320f;
        originalLayout.flexibleWidth = 1f;

        RectTransform row = AddRow(card, 220f);
        AddRowLabel(row, AdminStrings.Get("newText"));

        string current = AdminContentStore.GetText(_selected);
        TMP_InputField input = AddRowInput(row, current, true);
        input.onValueChanged.AddListener(text =>
        {
            AdminContentStore.SetText(_selected, text);
            Commit("text", "Edited a scene string");
        });

        if (AdminContentStore.HasTextOverride(_selected))
        {
            AddButtonRow(card, AdminStrings.Get("reset"), () =>
            {
                AdminContentStore.ClearText(_selected);
                Commit("text", "Restored a scene string");
                if (Root != null) Root.RefreshCurrentTab();
            }, AdminUi.CardAlt);
        }
    }

    // ------------------------------------------------------------------
    // The list of editable strings
    // ------------------------------------------------------------------

    private void BuildStringList(RectTransform content)
    {
        List<string> strings = CollectSceneStrings();

        RectTransform card = AddCard(content, AdminStrings.Get("textCount", strings.Count));

        if (strings.Count == 0)
        {
            AddButtonRow(card, AdminStrings.Get("noEditableText"), () => { }, AdminUi.CardAlt);
            BuildSceneButtons(card);
            return;
        }

        int listed = 0;
        for (int i = 0; i < strings.Count && listed < MaxListedStrings; i++)
        {
            string value = strings[i];
            if (!string.IsNullOrEmpty(_search) &&
                value.IndexOf(_search, System.StringComparison.OrdinalIgnoreCase) < 0)
            {
                continue;
            }

            listed++;

            bool selected = value == _selected;
            Button button = AdminUi.CreateButton(card, "String", value, () =>
            {
                _selected = value;
                if (Root != null) Root.RefreshCurrentTab();
            }, selected ? AdminUi.Accent : AdminUi.CardAlt, AdminUi.FontSmall);

            LayoutElement layout = button.gameObject.AddComponent<LayoutElement>();
            layout.minHeight = 64f;
            layout.preferredHeight = 64f;
        }

        BuildSceneButtons(card);
    }

    /// <summary>
    /// Collects the distinct text strings of the loaded scene, skipping the
    /// admin panel's own UI.
    ///
    /// Inactive objects are included: the game keeps a separate GameObject
    /// for the English and the Filipino copy of every string and only ever
    /// activates one of them, so the teacher has to be able to edit both.
    /// </summary>
    private static List<string> CollectSceneStrings()
    {
        List<string> result = new List<string>();
        HashSet<string> seen = new HashSet<string>();

        TMP_Text[] texts = Object.FindObjectsByType<TMP_Text>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < texts.Length; i++)
        {
            TMP_Text text = texts[i];
            if (text == null) continue;

            // Skip the admin panel's own interface.
            Transform root = text.transform.root;
            if (root != null && root.name.StartsWith("[Admin")) continue;

            string value = text.text;
            if (string.IsNullOrEmpty(value)) continue;
            if (!seen.Add(value)) continue;

            result.Add(value);
        }

        return result;
    }
}
