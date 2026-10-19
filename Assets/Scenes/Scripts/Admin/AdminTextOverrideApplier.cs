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
        if (AdminContentStore.GetTextOverrides().Count == 0) return 0;

        // Inactive objects have to be included. The game keeps a separate
        // GameObject for the English and the Filipino copy of every string and
        // only ever activates one of them, so a search that skipped inactive
        // objects would only ever reach the current language.
        TMP_Text[] texts = FindObjectsByType<TMP_Text>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);
        int changed = 0;

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

        return changed;
    }
}
