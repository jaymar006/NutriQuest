using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using Gameplay.CutsceneManager;

public class LevelInfoScreen : MonoBehaviour
{
    [Header("Score Display")]
    [SerializeField] private TMP_Text highScoreText;

    [Header("Badge Display")]
    [Tooltip("Assign in order: Genius, Conqueror, Challenger, Steps")]
    [SerializeField] private List<Image> badgeImages = new List<Image>();

    [Header("Stage Settings")]
    [SerializeField] private string stageID = "Stage_1";
    [Tooltip("Tower index: 0 = Tower1, 1 = Tower2, 2 = Tower3, 3 = Tower4")]
    [SerializeField] private int towerIndex = 0;

    [Header("UI")]
    [SerializeField] private Button challengeButton;
    [SerializeField] private TMP_Text runeKeyWarningText;

    [Header("Navigation")]
    [SerializeField] private SceneNavigationSystem sceneNavigation;

    [Header("Intro Cutscene")]
    [Tooltip("Drag the intro cutscene scene here. Leave empty if this tower has no intro cutscene. " +
             "No separate CutsceneManager GameObject needed — this field handles it directly.")]
    [SerializeField] private CutsceneTrigger introCutscene = new CutsceneTrigger();

    [Header("Cost Display")]
    [Tooltip("FIX: Must include BOTH language copies of the rune cost text for this " +
             "tower — e.g. the RuneCostM1 under TOWERMODALS ENGLISH *and* the one " +
             "under TOWERMODALS TAGALOG. They're separate GameObjects toggled by " +
             "LocalizedGroupToggle, not one shared object, so both need to be dragged " +
             "in here or the language that isn't currently active will show a stale " +
             "leftover number the moment LocalizedGroupToggle switches to it.")]
    [SerializeField] private List<TMP_Text> costDisplayTexts = new List<TMP_Text>();

    [Header("Warning Animation")]
    [Tooltip("Assign the SquishSquashManager on the WARNING TEXT object only.")]
    [SerializeField] private SquishSquashManager warningShakeAnimation;
    [Tooltip("Auto-hide warning text after this many seconds. 0 = never hide.")]
    [SerializeField] private float warningAutoDismiss = 2f;

    [Header("Not Enough Keys")]
    [Tooltip("Fires when the player taps Challenge without enough rune keys. " +
             "Drag a GameObject in (e.g. a 'Not Enough Keys' modal) and pick " +
             "GameObject -> SetActive(bool) from the dropdown to open it — " +
             "same drag-and-drop pattern as TapCounterTrigger.")]
    [SerializeField] private UnityEvent onNotEnoughKeys;

    private const string HIGH_SCORE_PREFIX = "HighScore_";
    private const string BADGE_PREFIX = "Badge_";
    private const string ATTEMPT_PREFIX = "Attempts_";

    private readonly Color unlockedColor = Color.white;
    private readonly Color lockedColor = new Color(0.4f, 0.4f, 0.4f, 1f);

    private const int COST_NEW_TOWER = 3;
    private const int COST_RECHALLENGE_TOWER = 2;
    private const int COST_NEW_TOWER4 = 4;
    private const int COST_RECHALLENGE_TOWER4 = 3;

    private int currentCost = 1;
    private Coroutine _warningDismissCoroutine;

    // FIX: Guards against a second click/navigation firing while we're already
    // mid-transition. Without this, OnKeysChanged (fired by SpendKey) can
    // re-enable challengeButton before the scene transition finishes, letting
    // a fast double-tap call NavigateToScene() twice — the second call reaches
    // SceneTransitionManager.NavigateTo() while it's still transitioning from
    // the first, and gets silently dropped with a console warning.
    private bool isNavigating = false;

#if UNITY_EDITOR
    // Keeps introCutscene's stored scene name in sync with whatever
    // SceneAsset is dragged into the Inspector.
    private void OnValidate()
    {
        introCutscene?.EditorSyncSceneName();
    }
#endif

    private void OnEnable()
    {
        // FIX: Reset the navigation guard every time this screen opens, so a
        // stale "true" from a previous visit can't block a legitimate click.
        isNavigating = false;

        // FIX: Subscribe to key change events so the button updates the moment keys change,
        //      even when this modal is already open
        RuneKeySystem.OnKeysChanged += OnKeysChanged;

        // FIX: costDisplayTexts contains BOTH the English and Tagalog copies
        // of RuneCostM1 for this tower (they're separate GameObjects under
        // TOWERMODALS ENGLISH / TOWERMODALS TAGALOG, toggled by
        // LocalizedGroupToggle). RefreshDisplay() below already writes to
        // both regardless of which one is currently active/visible, so
        // whichever LocalizedGroupToggle shows next already has the right
        // number. This subscription additionally catches the case where the
        // player switches language WHILE this modal is already open, so the
        // numbers can't go stale mid-session either.
        LocalizationManager.OnLanguageChanged += RefreshDisplay;

        RefreshDisplay();
    }

    private void OnDisable()
    {
        // FIX: Always unsubscribe to avoid ghost callbacks after the modal closes
        RuneKeySystem.OnKeysChanged -= OnKeysChanged;
        LocalizationManager.OnLanguageChanged -= RefreshDisplay;
    }

    private void Start()
    {
        if (challengeButton != null)
        {
            challengeButton.onClick.RemoveAllListeners();
            challengeButton.onClick.AddListener(OnChallengeButtonClicked);
        }

        if (runeKeyWarningText != null)
            runeKeyWarningText.gameObject.SetActive(false);
    }

    // FIX: Called every time RuneKeySystem fires OnKeysChanged
    private void OnKeysChanged()
    {
        currentCost = CalculateCost();

        // FIX: Don't let a key-count change re-enable the challenge button
        // while we're already navigating away from this screen. Without this
        // guard, SpendKey() reducing the player's key count fires this event
        // mid-navigation, UpdateButtonState() re-enables the button (if the
        // player still has enough keys for another attempt), and a second
        // tap can slip through and call NavigateToScene() again.
        if (!isNavigating)
            UpdateButtonState();

        DisplayCost();
    }

    // Called when challenge button is clicked
    private void OnChallengeButtonClicked()
    {
        // FIX: Hard guard at the top — if we're already navigating, ignore
        // any further clicks entirely, regardless of button state.
        if (isNavigating)
            return;

        if (RuneKeySystem.Instance == null)
        {
            Debug.LogError("[LevelInfoScreen] RuneKeySystem not found!");
            return;
        }

        currentCost = CalculateCost();

        // FIX: The button is no longer disabled when keys are insufficient
        // (see UpdateButtonState) — it stays tappable so this check can run
        // and open whatever GameObject is wired into onNotEnoughKeys instead
        // of the tap silently doing nothing.
        if (!RuneKeySystem.Instance.HasEnoughKeys(currentCost))
        {
            onNotEnoughKeys?.Invoke();
            return;
        }

        bool spent = RuneKeySystem.Instance.SpendKey(currentCost);

        if (spent)
        {
            Debug.Log("[LevelInfoScreen] Spent " + currentCost + " key(s). Proceeding to level...");
            HideWarning();

            // FIX: Lock navigation and disable the button immediately, before
            // OnKeysChanged (fired by SpendKey above) has a chance to
            // re-enable it. This is what actually stops the double-tap from
            // reaching SceneTransitionManager.NavigateTo() a second time.
            isNavigating = true;

            if (challengeButton != null)
                challengeButton.interactable = false;

            if (warningShakeAnimation != null)
                warningShakeAnimation.PlaySquashAndStretch();

            NavigateToScene();
        }
        else
        {
            // Safety fallback — should not normally happen since HasEnoughKeys
            // was already checked above. Could only fire from a genuine race
            // (e.g. keys spent by something else between the check and here).
            Debug.LogWarning("[LevelInfoScreen] SpendKey failed unexpectedly. Refreshing button state.");
            UpdateButtonState();
        }
    }

    private void NavigateToScene()
    {
        if (sceneNavigation == null)
        {
            Debug.LogError("[LevelInfoScreen] SceneNavigationSystem not assigned!");
            return;
        }

        // FIX: introCutscene is a plain embedded field now — no separate
        // GameObject/component to find or null-check for existence. If a
        // scene is assigned and unseen, it plays (and that cutscene scene's
        // own DialogueManager already knows to load gameplay next). If not,
        // go straight to gameplay.
        introCutscene.PlayIfNotSeen(() => sceneNavigation.Navigate());
    }

    public void RefreshDisplay()
    {
        currentCost = CalculateCost();
        DisplayHighScore();
        DisplayBadges();
        DisplayCost();
        UpdateButtonState();
    }

    // FIX: isNavigating is now the ONLY thing that disables the button.
    // Insufficient keys used to disable it outright (challengeButton.interactable
    // = hasEnough), which meant tapping while short on keys did nothing at
    // all. The button now stays tappable so OnChallengeButtonClicked() can
    // detect the insufficient-keys case itself and fire onNotEnoughKeys.
    private void UpdateButtonState()
    {
        if (challengeButton == null) return;

        challengeButton.interactable = !isNavigating;

        bool hasEnough = RuneKeySystem.Instance != null && RuneKeySystem.Instance.HasEnoughKeys(currentCost);
        Debug.Log($"[LevelInfoScreen] Has enough keys: {hasEnough} (need {currentCost} key(s), have {(RuneKeySystem.Instance != null ? RuneKeySystem.Instance.CurrentKeys : 0)})");
    }

    private int CalculateCost()
    {
        bool hasAttempted = PlayerPrefs.GetInt(ATTEMPT_PREFIX + stageID, 0) > 0;

        if (towerIndex == 3)
            return hasAttempted ? COST_RECHALLENGE_TOWER4 : COST_NEW_TOWER4;
        else
            return hasAttempted ? COST_RECHALLENGE_TOWER : COST_NEW_TOWER;
    }

    private void ShowWarning(string message)
    {
        if (runeKeyWarningText != null)
        {
            runeKeyWarningText.gameObject.SetActive(true);
            runeKeyWarningText.text = message;
        }

        if (warningAutoDismiss > 0f)
        {
            if (_warningDismissCoroutine != null)
                StopCoroutine(_warningDismissCoroutine);
            _warningDismissCoroutine = StartCoroutine(AutoDismissWarning());
        }
    }

    private void HideWarning()
    {
        if (_warningDismissCoroutine != null)
        {
            StopCoroutine(_warningDismissCoroutine);
            _warningDismissCoroutine = null;
        }

        if (runeKeyWarningText != null)
            runeKeyWarningText.gameObject.SetActive(false);
    }

    private IEnumerator AutoDismissWarning()
    {
        yield return new WaitForSeconds(warningAutoDismiss);
        HideWarning();
    }

    private void DisplayCost()
    {
        // FIX: Loops over EVERY entry, active or not — this is what makes
        // wiring both language copies into costDisplayTexts work. An
        // inactive TMP_Text still accepts .text writes; it just won't be
        // visible until LocalizedGroupToggle activates its GameObject.
        foreach (TMP_Text text in costDisplayTexts)
        {
            if (text != null)
                text.text = currentCost.ToString();
        }
    }

    private void DisplayHighScore()
    {
        int highScore = PlayerPrefs.GetInt(HIGH_SCORE_PREFIX + stageID, 0);
        if (highScoreText != null)
            highScoreText.text = highScore.ToString();
    }

    private void DisplayBadges()
    {
        AchievementType[] allTypes = new AchievementType[]
        {
            AchievementType.GeniusOfTheTower,
            AchievementType.ConquerorOfTheTower,
            AchievementType.ChallengerOfTheTower,
            AchievementType.StepsTowardsSuccess
        };

        for (int i = 0; i < badgeImages.Count; i++)
        {
            if (badgeImages[i] == null) continue;
            bool earned = IsBadgeEarned(allTypes[i]);
            badgeImages[i].color = earned ? unlockedColor : lockedColor;
        }
    }

    private bool IsBadgeEarned(AchievementType type)
    {
        string key = BADGE_PREFIX + stageID + "_" + type.ToString();
        return PlayerPrefs.GetInt(key, 0) == 1;
    }

    public bool CanProceed()
    {
        if (RuneKeySystem.Instance == null) return false;
        currentCost = CalculateCost();
        return RuneKeySystem.Instance.HasEnoughKeys(currentCost);
    }
}