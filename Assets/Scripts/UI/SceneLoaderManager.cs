using UnityEngine;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class SceneLoaderManager : MonoBehaviour
{
    [Header("Scene Reference")]
#if UNITY_EDITOR
    [SerializeField] private SceneAsset sceneAsset;
#endif
    [SerializeField] private string sceneName;

    public void LoadScene()
    {
        if (TransitionManager.Instance == null)
        {
            Debug.LogError("No TransitionManager in scene.");
            return;
        }

        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogError("SceneLoaderManager: Scene name is empty.");
            return;
        }

        TransitionManager.Instance.LoadScene(sceneName);
    }

    public void LoadNextScene()
    {
        int current = SceneManager.GetActiveScene().buildIndex;
        TransitionManager.Instance.LoadScene(current + 1);
    }

    public void LoadPreviousScene()
    {
        int current = SceneManager.GetActiveScene().buildIndex;
        TransitionManager.Instance.LoadScene(current - 1);
    }

    public void QuitGame()
    {
        Application.Quit();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (sceneAsset != null)
            sceneName = sceneAsset.name;
    }
#endif
}