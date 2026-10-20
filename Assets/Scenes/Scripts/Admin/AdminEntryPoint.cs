using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Adds the entry point to the admin panel: a version label in the Settings
/// panel that opens the panel when tapped.
///
/// The entry point is injected at runtime rather than authored into the scene
/// so that the feature needs no scene editing. The injected button is a
/// child of the Settings panel, so it appears and disappears with the panel
/// exactly like the rest of that window.
///
/// The version label on the title screen is made tappable as well, because
/// "tap the version" is the gesture the teacher is told to use and the title
/// screen is where they see the version first.
/// </summary>
public class AdminEntryPoint : MonoBehaviour
{
    private const string ButtonName = "AdminVersionButton";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        if (FindAnyObjectByType<AdminEntryPoint>() != null) return;

        GameObject go = new GameObject("[AdminEntryPoint]");
        DontDestroyOnLoad(go);
        AdminEntryPoint entry = go.AddComponent<AdminEntryPoint>();
        entry.StartCoroutine(InstallNextFrame());
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        StartCoroutine(InstallNextFrame());
    }

    private static IEnumerator InstallNextFrame()
    {
        // Let the scene finish loading and its scripts finish writing text.
        yield return null;
        yield return null;

        InstallIntoSettingsPanel();
        MakeTitleScreenVersionTappable();
    }

    // ------------------------------------------------------------------
    // Settings panel
    // ------------------------------------------------------------------

    private static void InstallIntoSettingsPanel()
    {
        RectTransform panel = FindSettingsPanel();
        if (panel == null)
        {
            // Most scenes have no settings panel at all, so this is not
            // worth warning about. Only the main menu is expected to have
            // one, so that is the only case worth a warning.
            if (SceneManager.GetActiveScene().name == "MainMenu")
            {
                Debug.LogWarning("[AdminEntryPoint] The MainMenu settings panel was not found, " +
                                 "so the admin entry point was not added.");
            }
            return;
        }

        // The main menu has the version label authored in the scene as a real
        // GameObject (AdminVersionButton), so the teacher can see and move it
        // in the Editor. Only inject one when a scene has no authored button.
        if (panel.GetComponentInChildren<AdminVersionButton>(true) != null)
        {
            Debug.Log("[AdminEntryPoint] The settings panel already has an AdminVersionButton; " +
                      "not injecting another one.");
            return;
        }

        if (panel.Find(ButtonName) != null) return;

        // The version label is parented to the panel's "Settings" heading so
        // it always sits directly beside the heading word. The main menu's
        // nav bar occupies the top-right corner of the tab area, so anchoring
        // the label to the panel's top-right would put it underneath the bar.
        RectTransform heading = FindHeading(panel);
        Transform parent = heading != null ? (Transform)heading : panel;

        RectTransform buttonRect = AdminUi.CreateRect(parent, ButtonName);
        if (heading != null)
        {
            // Immediately to the right of the heading word.
            buttonRect.anchorMin = new Vector2(1f, 0.5f);
            buttonRect.anchorMax = new Vector2(1f, 0.5f);
            buttonRect.pivot = new Vector2(0f, 0.5f);
            buttonRect.anchoredPosition = new Vector2(20f, 0f);
            buttonRect.sizeDelta = new Vector2(420f, 50f);
        }
        else
        {
            // Fallback: bottom-left of the panel.
            buttonRect.anchorMin = new Vector2(0f, 0f);
            buttonRect.anchorMax = new Vector2(0f, 0f);
            buttonRect.pivot = new Vector2(0f, 0f);
            buttonRect.anchoredPosition = new Vector2(30f, 24f);
            buttonRect.sizeDelta = new Vector2(420f, 50f);
        }

        Image background = buttonRect.gameObject.AddComponent<Image>();
        background.color = new Color(1f, 1f, 1f, 0f);
        background.raycastTarget = true;

        Button button = buttonRect.gameObject.AddComponent<Button>();
        button.targetGraphic = background;

        TMP_Text label = AdminUi.CreateText(buttonRect, "Label", AdminVersionService.Label,
                                            AdminUi.FontSmall, TextAlignmentOptions.MidlineLeft,
                                            new Color(0.68f, 0.73f, 0.80f, 1f));
        AdminUi.Stretch(label.rectTransform, 4f, 4f);
        label.raycastTarget = false;

        button.onClick.AddListener(() => AdminPanelRoot.Open());

        Debug.Log("[AdminEntryPoint] Admin entry point added to the settings panel in scene '" +
                  SceneManager.GetActiveScene().name + "' (parent: " + parent.name + ").");
    }

    /// <summary>
    /// Finds the "Settings" heading inside the settings panel. The heading
    /// GameObject is called "SettingsTXT".
    /// </summary>
    private static RectTransform FindHeading(RectTransform panel)
    {
        RectTransform[] children = panel.GetComponentsInChildren<RectTransform>(true);
        for (int i = 0; i < children.Length; i++)
        {
            if (children[i] != null && children[i].gameObject.name == "SettingsTXT")
            {
                return children[i];
            }
        }
        return null;
    }

    /// <summary>
    /// Finds the settings panel in the loaded scene.
    ///
    /// In the main menu the settings screen is one of three tabs swapped by
    /// TabsManager, so the panel and the nav button have different names:
    /// the panel is "SettingsWindow" and the button that selects it is
    /// "Settings". Two strategies are used so the entry point keeps working
    /// if the panel is ever renamed.
    ///
    /// Inactive objects have to be included: the settings tab is inactive
    /// until the player selects it, so a search that skipped inactive objects
    /// would never find it.
    /// </summary>
    private static RectTransform FindSettingsPanel()
    {
        RectTransform[] all = Object.FindObjectsByType<RectTransform>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);

        // 1. By the panel's own name.
        for (int i = 0; i < all.Length; i++)
        {
            if (all[i] != null && all[i].gameObject.name == "SettingsWindow") return all[i];
        }

        // 2. By the panel's heading. "SettingsTXT" is the title inside the
        //    settings tab and its parent is the panel itself.
        for (int i = 0; i < all.Length; i++)
        {
            if (all[i] == null || all[i].gameObject.name != "SettingsTXT") continue;
            Transform parent = all[i].parent;
            RectTransform parentRect = parent as RectTransform;
            if (parentRect != null) return parentRect;
        }

        // 3. A tab named "Settings" that owns a "SettingsTXT" child.
        for (int i = 0; i < all.Length; i++)
        {
            if (all[i] == null || all[i].gameObject.name != "Settings") continue;
            if (all[i].Find("SettingsTXT") != null) return all[i];
        }

        return null;
    }

    // ------------------------------------------------------------------
    // Title screen
    // ------------------------------------------------------------------

    /// <summary>Makes the title screen's version label open the admin panel.</summary>
    private static void MakeTitleScreenVersionTappable()
    {
        if (SceneManager.GetActiveScene().name != "TitleScreen") return;

        TMP_Text[] texts = Object.FindObjectsByType<TMP_Text>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < texts.Length; i++)
        {
            TMP_Text text = texts[i];
            if (text == null || string.IsNullOrEmpty(text.text)) continue;
            if (!text.text.StartsWith("Version")) continue;
            if (text.GetComponent<AdminVersionButton>() != null) continue;

            // AdminVersionButton counts the taps, so the title screen uses the
            // same hidden gesture as the settings label.
            text.gameObject.AddComponent<AdminVersionButton>();
        }
    }
}
