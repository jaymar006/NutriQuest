using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

// =============================================================================
// BootstrapManager — NutriQuest
//
// Spawns CutsceneManager, SceneTransitionManager, and UniversalMusicManager
// as persistent singletons, then loads the Title scene.
//
// SETUP
//   1. Create a scene called "Bootstrap", set it as index 0 in
//      File > Build Settings (drag it to the top of the list).
//   2. Create an empty GameObject called "Bootstrap" in that scene.
//   3. Attach this script to it.
//   4. Drag your CutsceneManager, SceneTransitionManager, and
//      UniversalMusicManager prefabs into their matching slots.
//   5. Set "Title Scene Name" to the exact name of your title scene.
//
// WHY THE ONE-FRAME DELAY
//   SceneTransitionManager.Awake() sets the fade canvas to alpha = 1
//   (fully black) and its Start() fades it back to 0. If we call
//   SceneManager.LoadScene() on the same frame as Spawn(), Start()
//   never runs on the Bootstrap scene, leaving the canvas permanently
//   black when the Title scene loads. Waiting one frame lets Start()
//   execute so the fade initializes correctly before we switch scenes.
// =============================================================================
public class BootstrapManager : MonoBehaviour
{
    [Tooltip("Drag your SceneTransitionManager prefab here")]
    [SerializeField] private GameObject sceneTransitionManagerPrefab;

    [Tooltip("Drag your UniversalMusicManager prefab here")]
    [SerializeField] private GameObject universalMusicManagerPrefab;

    [Tooltip("Drag your CutsceneManager prefab here")]
    [SerializeField] private GameObject cutsceneManagerPrefab;

    [Tooltip("Exact name of your Title scene in Build Settings")]
    [SerializeField] private string titleSceneName = "TitleScene";

    private void Start()
    {
        // Spawn order matters:
        // SceneTransitionManager first so everything else can call NavigateTo()
        // UniversalMusicManager second so BGM is ready on the title screen
        // CutsceneManager last — depends on SceneTransitionManager for its fade
        Spawn(sceneTransitionManagerPrefab, "SceneTransitionManager");
        Spawn(universalMusicManagerPrefab, "UniversalMusicManager");
        Spawn(cutsceneManagerPrefab, "CutsceneManager");

        // Wait one frame so each manager's Start() runs (especially
        // SceneTransitionManager.Start() which fades the canvas from
        // alpha 1 to 0) before we switch to the Title scene.
        StartCoroutine(LoadTitleAfterOneFrame());
    }

    private IEnumerator LoadTitleAfterOneFrame()
    {
        yield return null; // let all Start() methods finish

        if (string.IsNullOrEmpty(titleSceneName))
        {
            Debug.LogError("[BootstrapManager] Title Scene Name is empty! " +
                           "Assign it in the Inspector.");
            yield break;
        }

        Debug.Log("[BootstrapManager] All managers spawned. Loading: " + titleSceneName);
        SceneManager.LoadScene(titleSceneName);
    }

    private void Spawn(GameObject prefab, string managerName)
    {
        if (prefab != null)
        {
            Instantiate(prefab);
            Debug.Log("[BootstrapManager] Spawned: " + managerName);
        }
        else
        {
            Debug.LogWarning("[BootstrapManager] " + managerName + " prefab not assigned! " +
                             managerName + ".Instance will be null until a scene " +
                             "containing it loads.");
        }
    }
}