using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using System.Collections;

public class LoadingSceneController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private LoadingTextAnimator loadingAnimator;

    private AsyncOperation loadingOperation;

    private bool loadingReady = false;
    private bool waitingForTap = false;

    private void Start()
    {
        StartCoroutine(LoadSceneAsync());
    }

    private IEnumerator LoadSceneAsync()
    {
        string targetScene = LoadingTargetScene.GetTarget();

        if (string.IsNullOrEmpty(targetScene))
        {
            Debug.LogError("Target scene is empty!");
            yield break;
        }

        loadingOperation = SceneManager.LoadSceneAsync(targetScene);
        loadingOperation.allowSceneActivation = false;

        // Wait until loading reaches 90%
        while (loadingOperation.progress < 0.9f)
        {
            yield return null;
        }

        loadingReady = true;
        waitingForTap = true;

        if (loadingAnimator != null)
            loadingAnimator.SetLoadingComplete();

        // Wait for valid tap AFTER ready
        while (waitingForTap)
        {
            if (IsTapDetected())
            {
                waitingForTap = false;
            }

            yield return null;
        }

        loadingOperation.allowSceneActivation = true;

        LoadingTargetScene.Clear();
    }

    private bool IsTapDetected()
    {
        // Mobile / Touch
        if (Touchscreen.current != null &&
            Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
            return true;

        // Mouse (Editor testing)
        if (Mouse.current != null &&
            Mouse.current.leftButton.wasPressedThisFrame)
            return true;

        return false;
    }
}