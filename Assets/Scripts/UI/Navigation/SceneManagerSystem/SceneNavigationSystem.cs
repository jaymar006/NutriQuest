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

    public void Navigate()
    {
        if (SceneTransitionManager.Instance == null)
        {
            Debug.LogError("SceneTransitionManager not found in scene.");
            return;
        }

        SceneTransitionManager.Instance.NavigateTo(targetSceneName);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (targetSceneAsset != null)
            targetSceneName = targetSceneAsset.name;
    }
#endif
}