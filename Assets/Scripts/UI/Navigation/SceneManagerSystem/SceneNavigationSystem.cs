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

    [Header("Loading Screen")]
    [Tooltip("ON = go through loading screen first. OFF = fade directly to target scene. Default is OFF.")]
    [SerializeField] private bool useLoadingScreen = false;

    public void Navigate()
    {
        if (SceneTransitionManager.Instance == null)
        {
            Debug.LogError("[SceneNavigationSystem] SceneTransitionManager not found!");
            return;
        }

        if (string.IsNullOrEmpty(targetSceneName))
        {
            Debug.LogError("[SceneNavigationSystem] Target scene name is empty!");
            return;
        }

        SceneTransitionManager.Instance.NavigateTo(targetSceneName, useLoadingScreen);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (targetSceneAsset != null)
            targetSceneName = targetSceneAsset.name;
    }
#endif
}