using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class SceneNavigationSystem : MonoBehaviour
{
#if UNITY_EDITOR
    [SerializeField] private SceneAsset targetSceneAsset;
#endif
    [SerializeField] private string targetSceneName;

    [Header("Loading Scene")]
    [SerializeField] private string loadingSceneName = "LoadingScene";

    public void Navigate()
    {
        if (SceneTransitionManager.Instance == null)
        {
            Debug.LogError("SceneTransitionManager not found in scene.");
            return;
        }

        // Save the real destination
        LoadingTargetScene.TargetScene = targetSceneName;

        // Go to loading scene first
        SceneTransitionManager.Instance.NavigateTo(loadingSceneName);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (targetSceneAsset != null)
            targetSceneName = targetSceneAsset.name;
    }
#endif
}