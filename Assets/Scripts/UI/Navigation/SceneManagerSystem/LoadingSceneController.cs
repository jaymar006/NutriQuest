using UnityEngine;

public class LoadingSceneController : MonoBehaviour
{
    private void Start()
    {
        SceneTransitionManager.Instance.LoadPendingScene();
    }
}