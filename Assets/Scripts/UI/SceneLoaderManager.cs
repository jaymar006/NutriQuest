using UnityEngine;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class SceneLoaderManager : MonoBehaviour
{
    [Header("Scene Reference (Drag Scene Here)")]
#if UNITY_EDITOR
    [SerializeField] private SceneAsset sceneAsset;
#endif
    [SerializeField] private string sceneName;

    [Header("Loading Settings")]
    [SerializeField] private float loadDelay = 0.2f;

    private bool isLoading = false;

    public void LoadScene()
    {
        if (isLoading) return;

        if (!string.IsNullOrEmpty(sceneName))
        {
            isLoading = true;
            Invoke(nameof(LoadSceneAfterDelay), loadDelay);
        }
        else
        {
            Debug.LogWarning("Scene name is empty!");
        }
    }

    private void LoadSceneAfterDelay()
    {
        SceneManager.LoadScene(sceneName);
    }

    public void LoadScene(int sceneIndex)
    {
        if (isLoading) return;

        isLoading = true;
        Invoke(nameof(LoadSceneByIndexAfterDelay), loadDelay);

        void LoadSceneByIndexAfterDelay()
        {
            Scene scene = (Scene)sceneIndex;
            SceneManager.LoadScene(scene.ToString());
        }
    }

    public void LoadNextScene()
    {
        if (isLoading) return;

        isLoading = true;
        Invoke(nameof(LoadNextAfterDelay), loadDelay);
    }

    private void LoadNextAfterDelay()
    {
        int current = SceneManager.GetActiveScene().buildIndex;
        SceneManager.LoadScene(current + 1);
    }

    public void LoadPreviousScene()
    {
        if (isLoading) return;

        isLoading = true;
        Invoke(nameof(LoadPreviousAfterDelay), loadDelay);
    }

    private void LoadPreviousAfterDelay()
    {
        int current = SceneManager.GetActiveScene().buildIndex;
        SceneManager.LoadScene(current - 1);
    }

    public void QuitGame()
    {
        if (isLoading) return;

        isLoading = true;
        Invoke(nameof(QuitAfterDelay), loadDelay);
    }

    private void QuitAfterDelay()
    {
        Application.Quit();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (sceneAsset != null)
        {
            sceneName = sceneAsset.name;
        }
    }
#endif

    public enum Scene
    {
        Bootstrap,
        Library,
        Recipes,
        TriviaSection,
        Achievements,
        MainMenu,
        LoadingScreen
    }
}
