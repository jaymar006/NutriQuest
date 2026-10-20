using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Applies the teacher's text overrides to every TextMeshPro string in the
/// scene that is currently loaded.
///
/// This is what makes scene-authored text editable without touching the scene
/// assets. The override is keyed by the ORIGINAL string, so the applier walks
/// the scene, reads each text, and swaps it when the original matches an
/// override. Because the English and Filipino copies of a string are
/// different originals, one override table covers both languages.
///
/// The applier is spawned automatically and follows every scene load, so no
/// scene needs a component for this to work.
///
/// It runs a couple of frames late on purpose: scripts such as
/// TitleScreenManager write their text in Start(), and applying the overrides
/// before that would be undone by the script.
/// </summary>
public class AdminTextOverrideApplier : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Spawn()
    {
        if (FindAnyObjectByType<AdminTextOverrideApplier>() != null) return;

        GameObject go = new GameObject("[AdminTextOverrideApplier]");
        DontDestroyOnLoad(go);
        go.hideFlags = HideFlags.HideInHierarchy;
        go.AddComponent<AdminTextOverrideApplier>();
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
        StartCoroutine(ApplyNextFrame());
    }

    private IEnumerator Start()
    {
        yield return ApplyNextFrame();
    }

    private static IEnumerator ApplyNextFrame()
    {
        // Let every Start() in the scene finish writing its text first.
        yield return null;
        yield return null;
        Apply();
    }

    /// <summary>Swaps every overridden string in the loaded scenes.</summary>
    public static int Apply()
    {
        if (!AdminContentStore.IsLoaded) return 0;

        int changed = 0;

        // The generic text overrides first, then the recipe pass, so a recipe
        // the teacher edited through the Recipes tab wins over a generic
        // string edit.
        if (AdminContentStore.GetTextOverrides().Count > 0)
        {
            // Inactive objects have to be included. The game keeps a separate
            // GameObject for the English and the Filipino copy of every string
            // and only ever activates one of them, so a search that skipped
            // inactive objects would only ever reach the current language.
            TMP_Text[] texts = FindObjectsByType<TMP_Text>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);

            for (int i = 0; i < texts.Length; i++)
            {
                TMP_Text text = texts[i];
                if (text == null) continue;

                // Never rewrite the admin panel's own interface.
                Transform root = text.transform.root;
                if (root != null && root.name.StartsWith("[Admin")) continue;

                string original = text.text;
                if (string.IsNullOrEmpty(original)) continue;

                string replacement = AdminContentStore.GetText(original);
                if (replacement != original)
                {
                    text.text = replacement;
                    changed++;
                }
            }
        }

        changed += ApplyRecipes();

        return changed;
    }

    // ------------------------------------------------------------------
    // Recipes
    // ------------------------------------------------------------------

    /// <summary>
    /// Writes the merged recipe text into the recipe scene.
    ///
    /// The recipe scenes hold their wording as plain TextMeshPro strings, so
    /// the scene text stays the built-in default and the teacher's change
    /// comes from the catalogue. Each recipe block is written into the object
    /// with the same name: "P1" holds the ingredients, "PSF1" the second
    /// ingredient block of the stir fry recipe, and "Steps" the method.
    ///
    /// Both language copies are written, each from its own half of the
    /// merged recipe, so the teacher can change either language.
    /// </summary>
    private static int ApplyRecipes()
    {
        string sceneName = SceneManager.GetActiveScene().name;

        AdminGameConfig.RecipeEntry entry = null;
        for (int i = 0; i < AdminGameConfig.Recipes.Count; i++)
        {
            if (AdminGameConfig.Recipes[i].sceneName == sceneName)
            {
                entry = AdminGameConfig.Recipes[i];
                break;
            }
        }
        if (entry == null) return 0;

        // Only when the teacher has actually changed the recipe. Otherwise the
        // scene text, which is the built-in content, stays as it is.
        if (!AdminRecipeCatalog.HasOverride(entry.id)) return 0;

        AdminContentStore.RecipeOverride recipe = AdminRecipeCatalog.GetMerged(entry.id);
        if (recipe == null || recipe.blocks == null) return 0;

        int changed = 0;
        for (int i = 0; i < recipe.blocks.Count; i++)
        {
            AdminContentStore.RecipeBlock block = recipe.blocks[i];
            if (block == null || string.IsNullOrEmpty(block.id)) continue;

            for (int language = 0; language < 2; language++)
            {
                bool filipino = language == 1;
                TMP_Text target = FindTextByName(block.id, filipino);
                if (target == null) continue;

                string text = filipino ? block.bodyFil : block.bodyEn;
                if (string.IsNullOrEmpty(text)) continue;

                if (target.text != text)
                {
                    target.text = text;
                    changed++;
                }
            }
        }

        return changed;
    }

    /// <summary>Finds a named text object for one language.</summary>
    private static TMP_Text FindTextByName(string objectName, bool filipino)
    {
        TMP_Text[] texts = FindObjectsByType<TMP_Text>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < texts.Length; i++)
        {
            if (texts[i] == null) continue;
            if (texts[i].gameObject.name != objectName) continue;
            if (IsFilipino(texts[i]) == filipino) return texts[i];
        }
        return null;
    }

    /// <summary>
    /// Which language a text belongs to. The language is tagged on a group
    /// higher up the hierarchy, so the ancestors are walked until one is found.
    /// </summary>
    private static bool IsFilipino(TMP_Text text)
    {
        Transform current = text.transform;
        for (int i = 0; i < 12 && current != null; i++)
        {
            LocalizedElement element = current.GetComponent<LocalizedElement>();
            if (element != null) return element.Language == ContentLanguage.Filipino;
            current = current.parent;
        }
        return LocalizationManager.Instance != null && LocalizationManager.Instance.IsFilipino;
    }
}
