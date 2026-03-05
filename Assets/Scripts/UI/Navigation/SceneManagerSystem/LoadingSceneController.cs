using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class LoadingSceneController : MonoBehaviour
{
    [SerializeField] private LoadingTextAnimator loadingAnimator;

    private AsyncOperation operation;

    void Start()
    {
        StartCoroutine(LoadSceneAsync());
    }

    IEnumerator LoadSceneAsync()
    {
        string targetScene = LoadingTargetScene.TargetScene;

        operation = SceneManager.LoadSceneAsync(targetScene);
        operation.allowSceneActivation = false;

        while (operation.progress < 0.9f)
        {
            yield return null;
        }

        loadingAnimator.SetLoadingComplete();

        while (!Input.GetMouseButtonDown(0))
        {
            yield return null;
        }

        operation.allowSceneActivation = true;
    }
}