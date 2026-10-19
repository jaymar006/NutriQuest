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
            Debug.LogWarning("[AdminEntryPoint] No settings panel found in this scene.");
            return;
        }
        if (panel.Find(ButtonName) != null) return;

        RectTransform buttonRect = AdminUi.CreateRect(panel, ButtonName);
        // The settings panel is hand positioned: the title is top-left, the
        // sound sliders are in the middle and the language dropdown and the
        // decorative bar fill the bottom. The top-right corner is the only
        // free space, so the version label goes there.
        buttonRect.anchorMin = new Vector2(1f, 1f);
        buttonRect.anchorMax = new Vector2(1f, 1f);
        buttonRect.pivot = new Vector2(1f, 1f);
        buttonRect.anchoredPosition = new Vector2(-30f, -24f);
        buttonRect.sizeDelta = new Vector2(430f, 62f);

        Image background = buttonRect.gameObject.AddComponent<Image>();
        background.color = new Color(1f, 1f, 1f, 0f);
        background.raycastTarget = true;

        Button button = buttonRect.gameObject.AddComponent<Button>();
        button.targetGraphic = background;

        TMP_Text label = AdminUi.CreateText(buttonRect, "Label", AdminVersionService.Label,
                                            AdminUi.FontSmall, TextAlignmentOptions.MidlineRight,
                                            new Color(0.72f, 0.76f, 0.82f, 1f));
        AdminUi.Stretch(label.rectTransform, 4f, 4f);
        label.raycastTarget = false;

        button.onClick.AddListener(() => AdminPanelRoot.Open());

        Debug.Log("[AdminEntryPoint] Admin panel entry point added to the Settings panel.");
    }

    /// <summary>
    /// Finds the Settings panel. The main menu has both a "Settings" nav
    /// button and a "SettingsWindow" panel; the panel is the one that owns a
    /// "SettingsTXT" title.
    ///
    /// Inactive objects have to be included: the settings panel is inactive
    /// when the scene loads and is only switched on when the player opens it,
    /// so a search that skipped inactive objects would never find it.
    /// </summary>
    private static RectTransform FindSettingsPanel()
    {
        RectTransform fallback = null;

        RectTransform[] all = Object.FindObjectsByType<RectTransform>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < all.Length; i++)
        {
            RectTransform rect = all[i];
            if (rect == null) continue;

            string name = rect.gameObject.name;
            if (name == "SettingsWindow") return rect;

            if (name == "Settings")
            {
                if (rect.Find("SettingsTXT") != null) return rect;
                fallback = rect;
            }
        }

        return fallback;
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
            if (text.GetComponent<Button>() != null) continue;

            Image background = text.gameObject.AddComponent<Image>();
            background.color = new Color(1f, 1f, 1f, 0f);
            background.raycastTarget = true;

            Button button = text.gameObject.AddComponent<Button>();
            button.targetGraphic = background;
            button.onClick.AddListener(() => AdminPanelRoot.Open());
        }
    }
}
