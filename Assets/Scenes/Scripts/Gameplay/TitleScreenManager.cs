using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;
using System.Collections;
using Gameplay.CutsceneManager;

// ---------------------------------------------------------------------------
// TitleScreenManager
//
// Attach this to a GameObject in your Title/MainMenu scene.
//
// Handles two states automatically based on PlayerPrefs:
//
//   FRESH INSTALL  (no PlayerName saved)
//     - Hides the welcome bar
//     - Shows the tap prompt heading as "START THE QUEST" (newPlayerPromptText)
//     - Shows "Touch to Continue" prompt, idle-fading
//     - On tap:
//         - If the language has not been chosen yet (PlayerPrefs "LanguageSelected"
//           not set), navigates to languageSelectSceneName so the player can pick
//           a language first. That scene is responsible for setting
//           PlayerPrefs "LanguageSelected" = 1 once a choice is made and then
//           returning here (or continuing on to whatever comes next).
//         - Once the language has been selected, opens NameInputScreen for
//           first-time setup as before.
//     - After name is confirmed:
//         - If a newPlayerIntroCutscene is assigned, plays it (once per save)
//           and that cutscene scene's own DialogueManager knows where to go next.
//         - If none is assigned, navigates directly to newPlayerNextScene as
//           before (use this if newPlayerNextScene is just MapScene with no
//           VN dialogue in it).
//
//   RETURNING PLAYER  (PlayerName exists)
//     - Shows "Welcome Back : <Name>" at the top
//     - Shows the tap prompt heading as "CONTINUE" (returningPlayerPromptText)
//     - Shows "Touch to Continue" prompt, idle-fading
//     - On tap -> navigates directly to MapScene
//
// All scene names and UI references are assigned in the Inspector.
// No hardcoded strings except the PlayerPrefs key fallback "Player".
// ---------------------------------------------------------------------------
public class TitleScreenManager : MonoBehaviour
{
    // -------------------------------------------------------------------------
    // Inspector — Navigation
    // -------------------------------------------------------------------------
    [Header("Navigation")]
    [Tooltip("Scene to load for returning players (your world map / level select scene).")]
    [SerializeField] private string mapSceneName = "MapScene";

    [Tooltip("Use the loading screen when transitioning to the map? Default OFF for title screen.")]
    [SerializeField] private bool useLoadingScreenForMap = false;

    [Tooltip("Scene to load after name input for brand new players (can be an intro cutscene scene or MapScene).")]
    [SerializeField] private string newPlayerNextScene = "MapScene";

    [Tooltip("Use the loading screen for the new-player transition? Only used when " +
             "no newPlayerIntroCutscene is assigned, since the cutscene trigger handles its own fades.")]
    [SerializeField] private bool useLoadingScreenForNewPlayer = false;

    [Header("New Player Cutscene Routing")]
    [Tooltip("Drag the new-player intro cutscene scene here. If assigned, the intro plays " +
             "through it (once per save) and that cutscene scene's own DialogueManager knows " +
             "to load MapScene next. If left empty, newPlayerNextScene is loaded directly instead. " +
             "No separate CutsceneManager GameObject needed.")]
    [SerializeField] private CutsceneTrigger newPlayerIntroCutscene = new CutsceneTrigger();

    [Header("Language Selection (First Time)")]
    [Tooltip("Scene to load so a brand new player can choose their language, before name input. " +
             "That scene should set PlayerPrefs \"LanguageSelected\" = 1 once a choice is made.")]
    [SerializeField] private string languageSelectSceneName = "LanguageSelectScene";

    [Tooltip("Use the loading screen when transitioning to the language select scene?")]
    [SerializeField] private bool useLoadingScreenForLanguageSelect = false;

    // -------------------------------------------------------------------------
    // Inspector — Always-visible UI
    // -------------------------------------------------------------------------
    [Header("Always Visible")]
    [Tooltip("The 'Touch to Continue' text shown in both states.")]
    [SerializeField] private TMP_Text touchToContinueText;

    [Tooltip("CanvasGroup on the same object as touchToContinueText, used for the idle fade loop. " +
             "Add a CanvasGroup component to that object and drag it here.")]
    [SerializeField] private CanvasGroup touchToContinueCanvasGroup;

    [Tooltip("Optional pulse/bounce animator on the Touch to Continue text. Can run alongside the fade.")]
    [SerializeField] private SquishSquashManager touchToContinuePulse;

    [Tooltip("Version label at the bottom left. Text is set automatically from Application.version.")]
    [SerializeField] private TMP_Text versionText;

    [Tooltip("Quit / power button. Always visible.")]
    [SerializeField] private Button quitButton;

    // -------------------------------------------------------------------------
    // Inspector — Idle fade animation
    // -------------------------------------------------------------------------
    [Header("Touch To Continue — Idle Fade")]
    [Tooltip("Seconds for one fade-out or fade-in half of the loop.")]
    [SerializeField] private float fadeDuration = 1f;

    [Tooltip("Lowest alpha reached during the fade-out.")]
    [SerializeField, Range(0f, 1f)] private float fadeMinAlpha = 0.25f;

    [Tooltip("Highest alpha reached during the fade-in.")]
    [SerializeField, Range(0f, 1f)] private float fadeMaxAlpha = 1f;

    // -------------------------------------------------------------------------
    // Inspector — Returning player UI
    // -------------------------------------------------------------------------
    [Header("Returning Player UI")]
    [Tooltip("Root object for the top welcome bar. Shown only for returning players.")]
    [SerializeField] private GameObject welcomeBarRoot;

    [Tooltip("'Welcome Back : <Name>' label inside the welcome bar.")]
    [SerializeField] private TMP_Text welcomeNameText;

    // -------------------------------------------------------------------------
    // Inspector — Tap prompt heading (shown in both states, wording differs)
    // -------------------------------------------------------------------------
    [Header("Tap Prompt Heading")]
    [Tooltip("Heading shown above Touch to Continue. Text changes depending on new-install vs returning player.")]
    [SerializeField] private TMP_Text startTheQuestLabel;

    [Tooltip("Heading text for a brand new install.")]
    [SerializeField] private string newPlayerPromptText = "START THE QUEST";

    [Tooltip("Heading text for a returning player.")]
    [SerializeField] private string returningPlayerPromptText = "CONTINUE";

    // -------------------------------------------------------------------------
    // Inspector — First-time name input
    // -------------------------------------------------------------------------
    [Header("Name Input (First Time)")]
    [Tooltip("The NameInputScreen component. Shown only on fresh install when player taps.")]
    [SerializeField] private NameInputScreen nameInputScreen;

    // -------------------------------------------------------------------------
    // Inspector — Tap sound
    // -------------------------------------------------------------------------
    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip tapSound;

    // -------------------------------------------------------------------------
    // Private state
    // -------------------------------------------------------------------------
    private bool isReturningPlayer = false;
    private bool inputLocked = false;   // prevents double-tap during transition
    private bool waitingForName = false; // true while NameInputScreen is open

    private Coroutine fadeLoopCoroutine;

    private const string LanguageSelectedKey = "LanguageSelected";

    // -------------------------------------------------------------------------
    // Unity lifecycle
    // -------------------------------------------------------------------------

#if UNITY_EDITOR
    private void OnValidate()
    {
        newPlayerIntroCutscene?.EditorSyncSceneName();
    }
#endif

    private void Start()
    {
        // Version label
        if (versionText != null)
            versionText.text = "Version " + Application.version;

        // Quit button
        if (quitButton != null)
            quitButton.onClick.AddListener(OnQuitPressed);

        // Determine state
        isReturningPlayer = PlayerNameManager.Instance != null
            ? PlayerNameManager.Instance.HasPlayerName()
            : PlayerPrefs.HasKey("PlayerName");

        // Build UI for the detected state
        if (isReturningPlayer)
            SetupReturningPlayerUI();
        else
            SetupFreshInstallUI();

        // Hook name input callback
        if (nameInputScreen != null)
            nameInputScreen.OnNameConfirmed += OnNameConfirmed;

        // Start tap prompt pulse if assigned
        if (touchToContinuePulse != null)
            touchToContinuePulse.PlaySquashAndStretch();

        // FIX: idle fade loop for the touch-to-continue prompt, the classic
        // "breathing" tap prompt seen in mobile games. Runs continuously while
        // the player hasn't tapped yet; pauses itself once input locks.
        if (touchToContinueCanvasGroup != null)
            fadeLoopCoroutine = StartCoroutine(TouchToContinueFadeLoop());
    }

    private void OnDestroy()
    {
        if (nameInputScreen != null)
            nameInputScreen.OnNameConfirmed -= OnNameConfirmed;

        if (quitButton != null)
            quitButton.onClick.RemoveListener(OnQuitPressed);

        if (fadeLoopCoroutine != null)
            StopCoroutine(fadeLoopCoroutine);
    }

    private void Update()
    {
        if (inputLocked || waitingForName) return;

        // Detect tap (touch or mouse click or Enter key)
        bool tapped = false;

        if (Touchscreen.current != null &&
            Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
            tapped = true;

        if (Mouse.current != null &&
            Mouse.current.leftButton.wasPressedThisFrame)
            tapped = true;

        if (Keyboard.current != null &&
            (Keyboard.current.enterKey.wasPressedThisFrame ||
             Keyboard.current.spaceKey.wasPressedThisFrame))
            tapped = true;

        if (tapped)
            OnScreenTapped();
    }

    // -------------------------------------------------------------------------
    // UI setup
    // -------------------------------------------------------------------------

    private void SetupFreshInstallUI()
    {
        // Hide returning-player-only element
        SetActive(welcomeBarRoot, false);

        // FIX: previously this label was force-hidden on fresh install, so
        // new players never saw any heading above Touch to Continue. It's
        // now shown in both states, with wording set per state below.
        if (startTheQuestLabel != null)
        {
            startTheQuestLabel.gameObject.SetActive(true);
            startTheQuestLabel.text = newPlayerPromptText;
        }

        // Touch to Continue is already visible by default in the prefab
        if (touchToContinueText != null)
            touchToContinueText.gameObject.SetActive(true);
    }

    private void SetupReturningPlayerUI()
    {
        string playerName = GetPlayerName();

        // Welcome bar
        SetActive(welcomeBarRoot, true);
        if (welcomeNameText != null)
            welcomeNameText.text = "Welcome Back " + playerName;

        // Tap prompt heading, returning-player wording
        if (startTheQuestLabel != null)
        {
            startTheQuestLabel.gameObject.SetActive(true);
            startTheQuestLabel.text = returningPlayerPromptText;
        }
    }

    // -------------------------------------------------------------------------
    // Idle fade loop
    // -------------------------------------------------------------------------
    private IEnumerator TouchToContinueFadeLoop()
    {
        touchToContinueCanvasGroup.alpha = fadeMaxAlpha;

        while (true)
        {
            // Fade out
            yield return FadeCanvasGroup(fadeMaxAlpha, fadeMinAlpha, fadeDuration);
            // Fade back in
            yield return FadeCanvasGroup(fadeMinAlpha, fadeMaxAlpha, fadeDuration);
        }
    }

    private IEnumerator FadeCanvasGroup(float from, float to, float duration)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            // Pause the animation without breaking the loop while a
            // transition is in progress (tap registered / name input open),
            // so it doesn't visibly fight the scene fade or sit mid-fade
            // behind the name input screen.
            if (inputLocked || waitingForName)
            {
                yield return null;
                continue;
            }

            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            touchToContinueCanvasGroup.alpha = Mathf.Lerp(from, to, t);
            yield return null;
        }

        touchToContinueCanvasGroup.alpha = to;
    }

    // -------------------------------------------------------------------------
    // Tap handling
    // -------------------------------------------------------------------------

    private void OnScreenTapped()
    {
        PlayTapSound();

        if (isReturningPlayer)
        {
            // Returning player — go straight to map
            inputLocked = true;
            NavigateTo(mapSceneName, useLoadingScreenForMap);
        }
        else
        {
            // Fresh install — choose a language first, then open name input.
            if (!HasSelectedLanguage())
            {
                inputLocked = true;
                NavigateTo(languageSelectSceneName, useLoadingScreenForLanguageSelect);
                return;
            }

            waitingForName = true;
            if (nameInputScreen != null)
                nameInputScreen.Show();
            else
            {
                // No name input screen assigned — skip to next scene with default name
                Debug.LogWarning("[TitleScreenManager] NameInputScreen not assigned. Skipping to next scene.");
                inputLocked = true;
                ProceedToNewPlayerNextScene();
            }
        }
    }

    // Called by NameInputScreen.OnNameConfirmed after the player enters their name
    private void OnNameConfirmed(string confirmedName)
    {
        waitingForName = false;
        inputLocked = true;

        Debug.Log("[TitleScreenManager] Name confirmed: " + confirmedName + ". Proceeding...");

        ProceedToNewPlayerNextScene();
    }

    // newPlayerIntroCutscene is a plain embedded field — no separate
    // GameObject/component to wire up. If a scene is assigned and unseen, it
    // plays (once per save) — that cutscene scene's own DialogueManager
    // already knows to load MapScene next. If none is assigned, falls back
    // to loading newPlayerNextScene directly.
    private void ProceedToNewPlayerNextScene()
    {
        newPlayerIntroCutscene.PlayIfNotSeen(() =>
        {
            NavigateTo(newPlayerNextScene, useLoadingScreenForNewPlayer);
        });
    }

    private void OnQuitPressed()
    {
        Debug.Log("[TitleScreenManager] Quit pressed.");
        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
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
            // Fallback if SceneTransitionManager hasn't been created yet
            Debug.LogWarning("[TitleScreenManager] SceneTransitionManager not found. Loading scene directly.");
            UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName);
        }
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private string GetPlayerName()
    {
        if (PlayerNameManager.Instance != null)
            return PlayerNameManager.Instance.GetPlayerName();

        return PlayerPrefs.GetString("PlayerName", "Player");
    }

    // Whether the player has already picked a language.
    // The language select scene is expected to call:
    //     PlayerPrefs.SetInt("LanguageSelected", 1);
    //     PlayerPrefs.Save();
    // once the player confirms a language.
    private bool HasSelectedLanguage()
    {
        return PlayerPrefs.GetInt(LanguageSelectedKey, 0) == 1;
    }

    private void PlayTapSound()
    {
        if (audioSource != null && tapSound != null)
            audioSource.PlayOneShot(tapSound);
    }

    private void SetActive(GameObject obj, bool active)
    {
        if (obj != null)
            obj.SetActive(active);
    }
}