using UnityEngine;
using UnityEngine.SceneManagement;

public class GameDataReset : MonoBehaviour
{
    public void ResetAllData()
    {
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();

        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}