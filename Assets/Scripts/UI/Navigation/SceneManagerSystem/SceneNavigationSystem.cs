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

    [Header("Loading Settings")]
    [SerializeField] private bool useLoadingScreen = true;
    [SerializeField] private string loadingSceneName = "LoadingScene";

    public void Navigate()
    {
        if (SceneTransitionManager.Instance == null)
        {
            Debug.LogError("SceneTransitionManager not found in scene.");
            return;
        }

        if (string.IsNullOrEmpty(targetSceneName))
        {
            Debug.LogError("Target scene name is empty.");
            return;
        }

        if (useLoadingScreen)
        {
            LoadingTargetScene.SetTarget(targetSceneName);
            SceneTransitionManager.Instance.NavigateTo(loadingSceneName);
        }
        else
        {
            SceneTransitionManager.Instance.NavigateTo(targetSceneName);
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (targetSceneAsset != null)
            targetSceneName = targetSceneAsset.name;
    }
#endif
}