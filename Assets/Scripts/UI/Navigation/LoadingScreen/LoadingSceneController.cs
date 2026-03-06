using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class LoadingSceneController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private LoadingTextAnimator loadingAnimator;

    private void Start()
    {
        if (loadingAnimator == null)
            Debug.LogError("[LoadingSceneController] loadingAnimator not assigned in Inspector!");

        StartCoroutine(WaitForLoadThenTap());
    }

    private IEnumerator WaitForLoadThenTap()
    {
        // Wait until SceneTransitionManager has set the target scene
        // (it sets it right after loading this scene)
        while (string.IsNullOrEmpty(LoadingTargetScene.GetTarget()))
            yield return null;

        Debug.Log("[LoadingSceneController] Target scene ready: " + LoadingTargetScene.GetTarget());

        // Simulate a minimum loading delay so the animation plays
        // (target scene is loaded by SceneTransitionManager, not here)
        yield return new WaitForSeconds(1.5f);

        // Tell animator loading is done — hide text, show tap to continue
        if (loadingAnimator != null)
            loadingAnimator.SetLoadingComplete();

        Debug.Log("[LoadingSceneController] Waiting for tap...");

        // Wait for player tap
        bool tapped = false;
        while (!tapped)
        {
            if (IsTapDetected())
                tapped = true;

            yield return null;
        }

        Debug.Log("[LoadingSceneController] Tap detected — notifying SceneTransitionManager.");

        // Tell SceneTransitionManager the player is ready
        // It takes over from here: fade out > load target > fade in
        if (SceneTransitionManager.Instance != null)
            SceneTransitionManager.Instance.PlayerTappedToContinue = true;
        else
            Debug.LogError("[LoadingSceneController] SceneTransitionManager not found!");
    }

    private bool IsTapDetected()
    {
        if (Touchscreen.current != null &&
            Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
            return true;

        if (Mouse.current != null &&
            Mouse.current.leftButton.wasPressedThisFrame)
            return true;

        return false;
    }
}