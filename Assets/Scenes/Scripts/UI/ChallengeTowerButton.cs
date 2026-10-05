using UnityEngine;
using UnityEngine.Events;
using Gameplay.CutsceneManager;

#if UNITY_EDITOR
using UnityEditor;
#endif

// ---------------------------------------------------------------------------
// ChallengeTowerButton
//
// Asks LevelInfoScreen.CanProceed() before doing anything. If the player
// lacks the rune keys, the press is ignored (onNotEnoughKeys fires instead).
// Otherwise the original flow runs:
//   cutscene not seen -> Loading Screen -> Cutscene -> Gameplay
//   cutscene seen     -> Loading Screen -> Gameplay
//
// NOTE: this script only GATES on keys. It does not spend them (the cost
// logic is private to LevelInfoScreen).
//
// SETUP:
// 1. Drag the tower's LevelInfoScreen into "Level Info".
// 2. Assign the intro cutscene + gameplay scene as before.
// 3. Wire the button's OnClick() -> OnChallengeTowerPressed().
// 4. (Optional) Use "On Not Enough Keys" to open a modal.
// ---------------------------------------------------------------------------
public class ChallengeTowerButton : MonoBehaviour
{
    [Header("Rune Key Check")]
    [SerializeField] private LevelInfoScreen levelInfo;
    [Tooltip("Fires when the player doesn't have enough rune keys. " +
             "The button does nothing else in that case.")]
    [SerializeField] private UnityEvent onNotEnoughKeys;

    [Header("Intro Cutscene (plays only if not seen yet)")]
    [SerializeField] private CutsceneTrigger introCutscene = new CutsceneTrigger();

    [Header("Gameplay (used when the cutscene is skipped)")]
#if UNITY_EDITOR
    [SerializeField] private SceneAsset gameplaySceneAsset;
#endif
    [SerializeField] private string gameplaySceneName;

    private bool isNavigating = false;

    private void OnEnable() => isNavigating = false;

    // Hook this up to the button's OnClick() in the Inspector.
    public void OnChallengeTowerPressed()
    {
        if (isNavigating) return;

        if (levelInfo == null)
        {
            Debug.LogError("[ChallengeTowerButton] LevelInfoScreen is not assigned!");
            return;
        }

        // Not enough keys (or RuneKeySystem missing) -> do not activate.
        if (!levelInfo.CanProceed())
        {
            onNotEnoughKeys?.Invoke();
            return;
        }

        if (string.IsNullOrEmpty(gameplaySceneName))
        {
            Debug.LogError("[ChallengeTowerButton] Gameplay Scene is not assigned!");
            return;
        }

        isNavigating = true;
        introCutscene.PlayIfNotSeen(GoStraightToGameplay);
    }

    private void GoStraightToGameplay()
    {
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