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

    private bool isLoading;

    private void Awake()
    {
        isLoading = false;
    }

    // -----------------------------
    // LOAD BY NAME
    // -----------------------------
    public void LoadScene()
    {
        if (isLoading)
        {
            Debug.Log("Scene is already loading.");
            return;
        }

        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogError("Scene name is empty!");
            return;
        }

        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogError("Scene not found in Build Settings: " + sceneName);
            return;
        }

        isLoading = true;
        Invoke(nameof(LoadSceneAfterDelay), loadDelay);
    }

    private void LoadSceneAfterDelay()
    {
        SceneManager.LoadScene(sceneName);
    }

    // -----------------------------
    // LOAD BY BUILD INDEX
    // -----------------------------
    public void LoadSceneByIndex(int index)
    {
        if (isLoading) return;

        if (index < 0 || index >= SceneManager.sceneCountInBuildSettings)
        {
            Debug.LogError("Invalid scene index: " + index);
            return;
        }

        isLoading = true;
        Invoke(nameof(LoadSceneByIndexAfterDelay), loadDelay);

        void LoadSceneByIndexAfterDelay()
        {
            SceneManager.LoadScene(index);
        }
    }

    // -----------------------------
    // LOAD NEXT / PREVIOUS
    // -----------------------------
    public void LoadNextScene()
    {
        if (isLoading) return;

        int current = SceneManager.GetActiveScene().buildIndex;
        int next = current + 1;

        if (next >= SceneManager.sceneCountInBuildSettings)
        {
            Debug.LogWarning("No next scene available.");
            return;
        }

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

        int current = SceneManager.GetActiveScene().buildIndex;
        int previous = current - 1;

        if (previous < 0)
        {
            Debug.LogWarning("No previous scene available.");
            return;
        }

        isLoading = true;
        Invoke(nameof(LoadPreviousAfterDelay), loadDelay);
    }

    private void LoadPreviousAfterDelay()
    {
        int current = SceneManager.GetActiveScene().buildIndex;
        SceneManager.LoadScene(current - 1);
    }

    // -----------------------------
    // QUIT
    // -----------------------------
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
}