using UnityEngine;
using Gameplay.CutsceneManager;

#if UNITY_EDITOR
using UnityEditor;
#endif

// ---------------------------------------------------------------------------
// ChallengeTowerButton
//
// Wire this to the "Challenge the Tower" button on the Main Menu.
//
// New game (intro cutscene not seen yet):
//   Main Menu -> Loading Screen -> Cutscene -> Gameplay
//   - Loading Screen + Cutscene: handled by CutsceneTrigger -> CutsceneLauncher
//     (already in your project).
//   - Cutscene -> Gameplay: handled by DialogueManager's "On Finish Load
//     Scene" field on the Cutscene scene's DialogueManager. Make sure that
//     field is set to your Gameplay scene in the Inspector.
//
// Returning player (intro cutscene already seen):
//   Main Menu -> Loading Screen -> Gameplay
//   - Handled by SceneTransitionManager.NavigateTo, cutscene skipped entirely.
//
// SETUP:
// 1. Add this component anywhere in the Main Menu scene (e.g. on the button
//    itself or a MenuController object).
// 2. Drag your intro Cutscene scene into "Intro Cutscene" (SceneAsset field).
// 3. Drag your Gameplay scene into "Gameplay Scene".
// 4. Wire the button's OnClick() -> ChallengeTowerButton.OnChallengeTowerPressed().
// 5. Confirm CutsceneLauncher and SceneTransitionManager both live on your
//    persistent boot object (DontDestroyOnLoad), each with their Loading
//    Scene assigned.
// ---------------------------------------------------------------------------
public class ChallengeTowerButton : MonoBehaviour
{
    [Header("Intro Cutscene (plays only if not seen yet)")]
    [Tooltip("Drag the intro cutscene scene here. Its own 'Use Loading Screen' " +
             "checkbox controls whether it routes through the Loading Scene.")]
    [SerializeField] private CutsceneTrigger introCutscene = new CutsceneTrigger();

    [Header("Gameplay (used when the cutscene is skipped)")]
#if UNITY_EDITOR
    [SerializeField] private SceneAsset gameplaySceneAsset;
#endif
    [SerializeField] private string gameplaySceneName;

    // Hook this up to the button's OnClick() in the Inspector.
    public void OnChallengeTowerPressed()
    {
        if (string.IsNullOrEmpty(gameplaySceneName))
        {
            Debug.LogError("[ChallengeTowerButton] Gameplay Scene is not assigned!");
            return;
        }

        // Plays the cutscene if it hasn't been seen yet (new game); otherwise
        // runs GoStraightToGameplay() immediately.
        introCutscene.PlayIfNotSeen(GoStraightToGameplay);
    }

    private void GoStraightToGameplay()
    {
        Debug.Log("[ChallengeTowerButton] Intro cutscene already seen — going straight to Gameplay.");

        if (SceneTransitionManager.Instance != null)
        {
            SceneTransitionManager.Instance.NavigateTo(gameplaySceneName, true);
        }
        else
        {
            Debug.LogWarning("[ChallengeTowerButton] SceneTransitionManager not found. Loading '" +
                              gameplaySceneName + "' directly.");
            UnityEngine.SceneManagement.SceneManager.LoadScene(gameplaySceneName);
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        introCutscene.EditorSyncSceneName();

        if (gameplaySceneAsset != null)
            gameplaySceneName = gameplaySceneAsset.name;
    }
#endif
}