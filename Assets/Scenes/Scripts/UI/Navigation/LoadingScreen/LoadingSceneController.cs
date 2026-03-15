using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class LoadingSceneController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private LoadingTextAnimator loadingAnimator;
    [SerializeField] private RandomLoadingTrivia triviaSystem;

    [Header("Loading Settings")]
    [SerializeField] private float minimumLoadTime = 10.5f;

    [Header("Sound Effects")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip tapSoundEffect;

    private void Start()
    {
        if (loadingAnimator == null)
            Debug.LogError("[LoadingSceneController] loadingAnimator not assigned in Inspector!");

        if (triviaSystem == null)
            Debug.LogError("[LoadingSceneController] triviaSystem not assigned in Inspector!");

        if (audioSource == null)
            Debug.LogWarning("[LoadingSceneController] AudioSource not assigned — tap sound won't play.");

        if (tapSoundEffect == null)
            Debug.LogWarning("[LoadingSceneController] Tap sound effect not assigned in Inspector!");

        StartCoroutine(LoadingRoutine());
    }

    private IEnumerator LoadingRoutine()
    {
        while (string.IsNullOrEmpty(LoadingTargetScene.GetTarget()))
            yield return null;

        Debug.Log("[LoadingSceneController] Target scene ready: " + LoadingTargetScene.GetTarget());

        if (triviaSystem != null)
            triviaSystem.ShowRandomTrivia();

        float elapsed = 0f;
        while (elapsed < minimumLoadTime)
        {
            if (IsTapDetected())
            {
                // Play tap sound
                PlayTapSound();

                // Cycle trivia
                if (triviaSystem != null)
                    triviaSystem.ShowRandomTrivia();
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        if (loadingAnimator != null)
            loadingAnimator.SetLoadingComplete();

        yield return new WaitForSeconds(0.3f);

        Debug.Log("[LoadingSceneController] Auto-proceeding to target scene.");

        if (SceneTransitionManager.Instance != null)
            SceneTransitionManager.Instance.PlayerTappedToContinue = true;
        else
            Debug.LogError("[LoadingSceneController] SceneTransitionManager not found!");
    }

    private void PlayTapSound()
    {
        if (audioSource != null && tapSoundEffect != null)
            audioSource.PlayOneShot(tapSoundEffect);
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