using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    // Enum list of all your scenes
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

    // Load using enum index (for Buttons)
    public void LoadScene(int sceneIndex)
    {
        Scene scene = (Scene)sceneIndex;
        SceneManager.LoadScene(scene.ToString());
    }

    // Load using scene name (optional)
    public void LoadSceneByName(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }

    // Load next scene in Build Settings
    public void LoadNextScene()
    {
        int current = SceneManager.GetActiveScene().buildIndex;
        SceneManager.LoadScene(current + 1);
    }

    // Load previous scene
    public void LoadPreviousScene()
    {
        int current = SceneManager.GetActiveScene().buildIndex;
        SceneManager.LoadScene(current - 1);
    }

    // Quit game
    public void QuitGame()
    {
        Application.Quit();
    }
}
