using UnityEngine;

namespace UI.Navigation
{
    public class QuitButton : MonoBehaviour
    {
        public void QuitGame()
        {
            // This works only in an actual Android build, not in the Unity Editor
            Application.Quit();
        }
    }
}