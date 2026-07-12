using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Gameplay.CutsceneManager
{
    // -------------------------------------------------------------------------
    // CutsceneTrigger
    //
    // Plain, embeddable field — NOT a MonoBehaviour. No separate GameObject or
    // component needed. Drop this in as a [SerializeField] field on whatever
    // script needs to trigger a cutscene, drag the cutscene scene onto it in
    // the Inspector, done.
    //
    // FIX: Play() now routes through CutsceneLauncher instead of
    // SceneTransitionManager. CutsceneLauncher has its own independent fade
    // and its own "busy" flag, completely separate from
    // SceneTransitionManager.isTransitioning — so a cutscene launch can never
    // be silently dropped because of unrelated transition state elsewhere in
    // the game (which was the cause of the intro cutscene being ignored).
    //
    // USAGE (in the owning MonoBehaviour):
    //
    //   [SerializeField] private CutsceneTrigger introCutscene = new CutsceneTrigger();
    //
    //   private void OnValidate() => introCutscene.EditorSyncSceneName();
    //
    //   introCutscene.PlayIfNotSeen(() => /* go straight to gameplay/results */);
    // -------------------------------------------------------------------------
    [System.Serializable]
    public class CutsceneTrigger
    {
#if UNITY_EDITOR
        [Tooltip("Drag the cutscene scene here (it must also be added to Build Settings).")]
        public SceneAsset sceneAsset;
#endif

        [SerializeField, HideInInspector] private string sceneName;

        [Tooltip("If true, routes through CutsceneLauncher's assigned Loading Scene before " +
                 "showing this cutscene. If false, fades straight to the cutscene scene.")]
        public bool useLoadingScreen = true;

        private const string SEEN_PREFIX = "CutsceneSeen_";

        public string SceneName => sceneName;

        // True only when a scene has actually been assigned. Lets callers
        // treat "no cutscene configured for this tower" as a normal, silent
        // no-op instead of a null-reference risk.
        public bool HasScene => !string.IsNullOrEmpty(sceneName);

#if UNITY_EDITOR
        // Call from the owning MonoBehaviour's OnValidate() so sceneName stays
        // in sync with whatever SceneAsset is dragged in. Needed because this
        // class isn't a MonoBehaviour itself, so it has no OnValidate of its own.
        public void EditorSyncSceneName()
        {
            sceneName = sceneAsset != null ? sceneAsset.name : "";
        }
#endif

        public bool ShouldPlay()
        {
            if (!HasScene) return false;
            return PlayerPrefs.GetInt(SEEN_PREFIX + sceneName, 0) == 0;
        }

        // Marks this cutscene as seen (immediately, before the scene loads,
        // so a crash or force-quit mid-cutscene doesn't cause a replay),
        // then launches it via CutsceneLauncher.
        public void Play()
        {
            if (!HasScene)
            {
                Debug.LogWarning("[CutsceneTrigger] Play() called but no scene is assigned.");
                return;
            }

            PlayerPrefs.SetInt(SEEN_PREFIX + sceneName, 1);
            PlayerPrefs.Save();

            Debug.Log("[CutsceneTrigger] Playing cutscene: " + sceneName);

            // FIX: CutsceneLauncher instead of SceneTransitionManager. See
            // class header for why — this can no longer be silently ignored
            // due to unrelated transition state elsewhere in the game.
            if (CutsceneLauncher.Instance != null)
            {
                CutsceneLauncher.Instance.LaunchCutscene(sceneName, useLoadingScreen);
            }
            else
            {
                Debug.LogWarning("[CutsceneTrigger] CutsceneLauncher not found in scene. Loading '" +
                                 sceneName + "' directly with no fade. Add a CutsceneLauncher to " +
                                 "your persistent boot/manager GameObject to fix this.");
                UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName);
            }
        }

        // Convenience for the common case: play if unseen (and a scene is
        // actually assigned), otherwise run whatever should happen when
        // there's nothing to play (e.g. go straight to gameplay, or results).
        public void PlayIfNotSeen(System.Action ifAlreadySeen)
        {
            if (HasScene && ShouldPlay())
                Play();
            else
                ifAlreadySeen?.Invoke();
        }

        // Editor/debug helper — resets this specific cutscene so it plays again.
        public void ResetSeen()
        {
            if (!HasScene) return;
            PlayerPrefs.DeleteKey(SEEN_PREFIX + sceneName);
            PlayerPrefs.Save();
            Debug.Log("[CutsceneTrigger] Reset seen flag for: " + sceneName);
        }
    }
}