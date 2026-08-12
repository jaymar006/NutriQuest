using UnityEngine;
using UnityEngine.UI;
using System.Collections;

// ---------------------------------------------------------------------------
// CreditsManager
//
// Attach this to a GameObject in your Credits scene.
//
// Classic movie-style auto-scroll:
//   - creditsContent starts positioned BELOW the visible viewport (set this
//     up in the Inspector/Scene view) and scrolls upward at scrollSpeed
//     until it has fully cleared the TOP of the viewport.
//   - When the scroll finishes, automatically navigates to nextSceneName.
//
// First-time vs repeat viewing (PlayerPrefs "CreditsSeen"):
//   - FIRST time the player reaches this scene: the roll cannot be skipped.
//     skipButtonRoot stays hidden the whole time; the scene only advances
//     once the scroll naturally finishes.
//   - SECOND time and onward: skipButtonRoot is shown, and tapping it stops
//     the scroll early and navigates immediately. Letting it finish
//     naturally still works too.
//
// Requires a RectTransform "viewport" (the masked/visible area) to measure
// how far the content needs to travel — it does not need a ScrollRect
// component, a simple RectMask2D or Mask on the viewport works fine.
// ---------------------------------------------------------------------------
public class CreditsManager : MonoBehaviour
{
    // -------------------------------------------------------------------------
    // Inspector — Scroll setup
    // -------------------------------------------------------------------------
    [Header("Scroll Setup")]
    [Tooltip("The RectTransform holding all the credit names/text. Position this " +
             "in the Scene view BELOW the viewport before pressing Play — that's " +
             "the roll's starting point.")]
    [SerializeField] private RectTransform creditsContent;

    [Tooltip("The visible/masked area the credits scroll through. Used only to " +
             "measure scroll distance, so the roll ends once content fully " +
             "clears the top of this rect.")]
    [SerializeField] private RectTransform viewport;

    [Tooltip("Scroll speed in pixels per second (unscaled time).")]
    [SerializeField] private float scrollSpeed = 60f;

    [Tooltip("Extra pixels to scroll past the point content fully clears the " +
             "viewport top, so the very last line doesn't feel like it cuts off abruptly.")]
    [SerializeField] private float extraTailDistance = 100f;

    // -------------------------------------------------------------------------
    // Inspector — Navigation
    // -------------------------------------------------------------------------
    [Header("Navigation")]
    [Tooltip("Scene to load once the credits finish (or are skipped).")]
    [SerializeField] private string nextSceneName = "MainMenu";

    [Tooltip("Use the loading screen for the transition out of Credits?")]
    [SerializeField] private bool useLoadingScreenOnExit = false;

    // -------------------------------------------------------------------------
    // Inspector — Skip button (repeat viewings only)
    // -------------------------------------------------------------------------
    [Header("Skip Button (hidden on first viewing)")]
    [Tooltip("Root object for the skip button. Only shown from the SECOND viewing onward.")]
    [SerializeField] private GameObject skipButtonRoot;

    [Tooltip("The actual Button component. Tapping it stops the roll and navigates immediately.")]
    [SerializeField] private Button skipButton;

    // -------------------------------------------------------------------------
    // Inspector — Audio
    // -------------------------------------------------------------------------
    [Header("Audio")]
    [Tooltip("Optional music/audio source for the credits scene. Left alone by " +
             "this script other than being stopped on exit, if assigned.")]
    [SerializeField] private AudioSource creditsAudioSource;

    // -------------------------------------------------------------------------
    // Private state
    // -------------------------------------------------------------------------
    private const string CreditsSeenKey = "CreditsSeen";

    private bool hasSeenCreditsBefore = false;
    private bool hasFinished = false;
    private Coroutine scrollCoroutine;

    // -------------------------------------------------------------------------
    // Unity lifecycle
    // -------------------------------------------------------------------------
    private void Start()
    {
        hasSeenCreditsBefore = PlayerPrefs.GetInt(CreditsSeenKey, 0) == 1;

        // FIX-equivalent design: skip is only ever available from the second
        // viewing onward. First-time players watch the full roll.
        if (skipButtonRoot != null)
            skipButtonRoot.SetActive(hasSeenCreditsBefore);

        if (hasSeenCreditsBefore && skipButton != null)
            skipButton.onClick.AddListener(OnSkipPressed);

        if (creditsContent == null || viewport == null)
        {
            Debug.LogWarning("[CreditsManager] creditsContent or viewport not assigned. " +
                              "Skipping straight to " + nextSceneName + ".");
            FinishCredits();
            return;
        }

        scrollCoroutine = StartCoroutine(ScrollCredits());
    }

    private void OnDestroy()
    {
        if (skipButton != null)
            skipButton.onClick.RemoveListener(OnSkipPressed);
    }

    // -------------------------------------------------------------------------
    // Scrolling
    // -------------------------------------------------------------------------
    private IEnumerator ScrollCredits()
    {
        float startY = creditsContent.anchoredPosition.y;

        // Distance needed for the content to fully clear the top of the
        // viewport: viewport height (to cross the visible area) + content
        // height (so the last line actually exits, not just enters) + a
        // small tail buffer so the ending doesn't feel abrupt.
        float totalDistance = viewport.rect.height + creditsContent.rect.height + extraTailDistance;
        float endY = startY + totalDistance;

        while (creditsContent.anchoredPosition.y < endY)
        {
            float newY = creditsContent.anchoredPosition.y + scrollSpeed * Time.unscaledDeltaTime;
            creditsContent.anchoredPosition = new Vector2(creditsContent.anchoredPosition.x, newY);
            yield return null;
        }

        FinishCredits();
    }

    // -------------------------------------------------------------------------
    // Skip / finish
    // -------------------------------------------------------------------------
    private void OnSkipPressed()
    {
        // Only reachable when skipButtonRoot is active, i.e. only on repeat
        // viewings — first-time viewers never see this button at all.
        if (scrollCoroutine != null)
            StopCoroutine(scrollCoroutine);

        FinishCredits();
    }

    private void FinishCredits()
    {
        if (hasFinished) return; // guards against skip + natural-finish double-firing
        hasFinished = true;

        PlayerPrefs.SetInt(CreditsSeenKey, 1);
        PlayerPrefs.Save();

        if (creditsAudioSource != null)
            creditsAudioSource.Stop();

        NavigateTo(nextSceneName, useLoadingScreenOnExit);
    }

    // -------------------------------------------------------------------------
    // Navigation
    // -------------------------------------------------------------------------
    private void NavigateTo(string sceneName, bool withLoadingScreen)
    {
        if (SceneTransitionManager.Instance != null)
        {
            SceneTransitionManager.Instance.NavigateTo(sceneName, withLoadingScreen);
        }
        else
        {
            Debug.LogWarning("[CreditsManager] SceneTransitionManager not found. Loading scene directly.");
            UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName);
        }
    }
}