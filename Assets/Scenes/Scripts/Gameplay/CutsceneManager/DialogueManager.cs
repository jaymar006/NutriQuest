using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;
using System.Collections;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Gameplay.CutsceneManager
{
    public class DialogueManager : MonoBehaviour
    {
        public static DialogueManager instance;

        [Header("UI Components")]
        public GameObject dialoguePanel;
        public TextMeshProUGUI nameBox;
        public TextMeshProUGUI textBox;
        public GameObject continueIndicator;

        [Header("Scroll Rect")]
        public ScrollRect textScrollRect;

        [Header("All Characters In Scene")]
        public List<CharacterVN> allCharacters = new List<CharacterVN>();

        [Header("Scene Visuals")]
        public GameObject sceneVisualsRoot;

        [Header("Black Screen")]
        public Image blackScreenImage;
        public float fadeDuration = 1f;

        [Header("Audio")]
        public AudioSource audioSource;
        public AudioSource typingSFXSource;

        [Header("Typing SFX Settings")]
        [Tooltip("Default typing sound if line has no typingSFX assigned")]
        public AudioClip defaultTypingSFX;
        [Tooltip("Play typing sound every N characters (1 = every character)")]
        public int typingSFXInterval = 2;
        [Tooltip("Stop typing SFX when player skips typing")]
        public bool stopSFXOnSkip = true;

        [Header("Name Input Screen")]
        public NameInputScreen nameInputScreen;

        [Header("Typewriter Settings")]
        public float typingSpeed = 0.04f;

        [Header("Entrance Animation")]
        [Tooltip("Slide distance used by SlideFromLeft / SlideFromRight entrance types")]
        public float slideDistance = 200f;

        [Header("Portrait Settings")]
        [Tooltip("Container holding all portraits")]
        public GameObject portraitContainer;

        [Header("Dialogue Lines")]
        public DialogueLine[] dialogueLines;

        [Header("Auto Start")]
        public bool autoStartOnAwake = true;

        [Header("Input Debounce")]
        [Tooltip("Minimum seconds between taps being accepted. Prevents rapid " +
                 "double-taps (e.g. touch + mouse firing the same instant) from " +
                 "being registered as two separate inputs.")]
        public float minTimeBetweenTaps = 0.15f;

        [Header("Anti-Spam Hold (Genshin-style)")]
        [Tooltip("Once a line's text is FULLY visible — whether it finished typing " +
                 "naturally or the player tap-skipped the typewriter — this is the " +
                 "minimum number of seconds it must stay on screen before a tap is " +
                 "allowed to advance to the next line. This is separate from " +
                 "Min Time Between Taps above: that one only stops near-simultaneous " +
                 "double-inputs, this one deliberately holds the fully-shown line so " +
                 "spam-tapping can't blow through dialogue faster than it can be read. " +
                 "Set to 0 to disable and advance immediately, same as before this feature.")]
        public float minFullTextDisplayTime = 0.3f;

        [Header("Where This Cutscene Leads")]
        [Tooltip("When true, fades to black and loads 'On Finish Load Scene' after the " +
                 "last dialogue line. THIS is what makes each cutscene scene self-contained " +
                 "— without a scene assigned below, the screen will fade to black and then " +
                 "simply stay black, since nothing else will load anything next.")]
        public bool autoAdvanceOnEnd = true;

#if UNITY_EDITOR
        [Tooltip("Drag the scene this cutscene should load once it finishes — e.g. this " +
                 "intro cutscene's value should be MainMenu/MapScene; an outro's should be " +
                 "ResultScene (or Credits, for Tower 4's outro).")]
        [SerializeField] private SceneAsset onFinishLoadSceneAsset;
#endif

        [SerializeField, HideInInspector] private string onFinishLoadScene = "";

        [Tooltip("Show the loading screen while loading the scene above? Usually OFF here " +
                 "since a loading screen typically already happened before THIS cutscene " +
                 "started.")]
        public bool onFinishUseLoadingScreen = false;

        [Tooltip("Optional: used only for debug logging, to identify which cutscene just ended.")]
        public string dialogueSceneID = "";

        // Public state
        public bool isTyping { get; private set; }
        public bool dialogueFinished { get; private set; }

        public string OnFinishLoadScene => onFinishLoadScene;

        // Private state
        private int currentIndex = 0;
        private DialogueLine currentLine = null;

        private Coroutine typingCoroutine;
        private Coroutine entranceCoroutine;
        private Coroutine blackScreenCoroutine;
        private Coroutine reactionCoroutine;

        private Coroutine portraitOutroCoroutine;

        private CharacterVN currentSpeaker;

        private bool inputBlocked = false;
        private bool previousLineWasBlackScreen = false;
        private bool waitingForNameInput = false;

        private bool isReacting = false;

        // Locks out re-entrant Advance() calls while a line transition
        // (portrait outro + next-line setup) is still in flight. Without this,
        // two fast taps could each spin up their own AdvanceWithOutro(),
        // stacking outro/entrance coroutines on top of each other.
        private bool isAdvancing = false;

        // FIX: Timestamp of the moment the CURRENT line's text became fully
        // visible — set both when TypeLine() finishes naturally and when
        // SnapToFull() completes a tap-skip. Advance() uses this alongside
        // minFullTextDisplayTime to hold a fully-shown line on screen for a
        // short beat before a tap is allowed to move past it.
        private float lineFullyShownTime = -999f;

        private float lastAcceptedTapTime = -999f;

        private RectTransform textContentRect;
        private Image currentOptionalImage;
        private AudioClip currentTypingSFX;

        private RectTransform currentPortraitRect;
        private Vector3 originalPortraitPosition;
        private Vector3 originalPortraitScale;

        // -------------------------------------------------------------------------
        // Editor
        // -------------------------------------------------------------------------
#if UNITY_EDITOR
        private void OnValidate()
        {
            if (onFinishLoadSceneAsset != null)
            {
                onFinishLoadScene = onFinishLoadSceneAsset.name;

                string assetPath = AssetDatabase.GetAssetPath(onFinishLoadSceneAsset);
                bool inBuildSettingsAndEnabled = false;

                foreach (EditorBuildSettingsScene s in EditorBuildSettings.scenes)
                {
                    if (s.path == assetPath && s.enabled)
                    {
                        inBuildSettingsAndEnabled = true;
                        break;
                    }
                }

                if (!inBuildSettingsAndEnabled)
                {
                    Debug.LogWarning("[DialogueManager] '" + onFinishLoadSceneAsset.name +
                                     "' is assigned as On Finish Load Scene on " + gameObject.name +
                                     ", but it is not enabled in File > Build Settings. It will " +
                                     "fail to load at runtime — add it there (or enable it if " +
                                     "already listed but unchecked).");
                }
            }
            else
            {
                onFinishLoadScene = "";
            }
        }
#endif

        // -------------------------------------------------------------------------
        // Unity lifecycle
        // -------------------------------------------------------------------------
        private void Awake()
        {
            if (instance == null)
                instance = this;
            else
                Destroy(gameObject);

            if (textScrollRect != null)
                textContentRect = textScrollRect.content;

            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
                if (audioSource == null)
                    audioSource = gameObject.AddComponent<AudioSource>();
            }
            audioSource.playOnAwake = false;

            if (typingSFXSource == null)
            {
                GameObject sfxObj = new GameObject("TypingSFXSource");
                sfxObj.transform.SetParent(transform);
                typingSFXSource = sfxObj.AddComponent<AudioSource>();
                typingSFXSource.playOnAwake = false;
            }

            if (blackScreenImage != null)
            {
                Color c = blackScreenImage.color;
                c.a = 0f;
                blackScreenImage.color = c;
                blackScreenImage.gameObject.SetActive(false);
            }

            if (nameInputScreen != null)
            {
                nameInputScreen.OnNameConfirmed = OnPlayerNameConfirmed;
                nameInputScreen.Hide();
            }

            InitializeSFXVolume();
        }

        private void Start()
        {
            if (autoStartOnAwake)
                StartDialogue();
        }

        private void Update()
        {
            if (dialogueFinished || inputBlocked || waitingForNameInput)
                return;

            bool advance = false;

            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
                advance = true;

            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
                advance = true;

            if (Keyboard.current != null &&
                (Keyboard.current.spaceKey.wasPressedThisFrame || Keyboard.current.enterKey.wasPressedThisFrame))
                advance = true;

            if (!advance) return;

            if (Time.unscaledTime - lastAcceptedTapTime < minTimeBetweenTaps)
                return;

            lastAcceptedTapTime = Time.unscaledTime;
            Advance();
        }

        // -------------------------------------------------------------------------
        // Volume helpers
        // -------------------------------------------------------------------------
        private void InitializeSFXVolume()
        {
            float vol = PlayerPrefs.GetFloat("SFXVolume", 1f);
            if (audioSource != null) audioSource.volume = vol;
            if (typingSFXSource != null) typingSFXSource.volume = vol;
        }

        public void SetSFXVolume(float volume)
        {
            if (audioSource != null) audioSource.volume = volume;
            if (typingSFXSource != null) typingSFXSource.volume = volume;
        }

        // -------------------------------------------------------------------------
        // Public API
        // -------------------------------------------------------------------------
        public void StartDialogue()
        {
            if (dialogueLines == null || dialogueLines.Length == 0)
            {
                Debug.LogWarning("[DialogueManager] No dialogue lines assigned.");
                return;
            }

            currentIndex = 0;
            dialogueFinished = false;
            currentLine = null;
            currentSpeaker = null;
            previousLineWasBlackScreen = false;
            waitingForNameInput = false;
            currentOptionalImage = null;
            currentTypingSFX = null;
            isReacting = false;
            isAdvancing = false;
            lineFullyShownTime = -999f;

            if (dialoguePanel != null)
                dialoguePanel.SetActive(true);

            if (continueIndicator != null)
                continueIndicator.SetActive(false);

            DimAll();
            ResetAllPopOut();
            HideAllOptionalImages();

            ShowLine(dialogueLines[currentIndex]);
        }

        // -------------------------------------------------------------------------
        // Advance
        // -------------------------------------------------------------------------
        private void Advance()
        {
            if (isTyping)
            {
                SnapToFull();
                return;
            }

            // Block re-entry while a transition is already running.
            if (isAdvancing)
                return;

            // FIX: Genshin-style anti-spam hold. Even though the text is fully
            // shown, don't let a tap advance until minFullTextDisplayTime has
            // passed since it became fully visible. This is what actually
            // stops spam-tapping from blowing through lines the instant each
            // one finishes typing — SnapToFull() above is untouched, so
            // skipping the TYPEWRITER itself is still instant on tap.
            if (Time.unscaledTime - lineFullyShownTime < minFullTextDisplayTime)
                return;

            if (currentPortraitRect != null &&
                currentLine != null &&
                (currentLine.enableReaction || currentLine.enableShake))
            {
                if (isReacting && reactionCoroutine != null)
                {
                    StopCoroutine(reactionCoroutine);
                    ResetPortraitTransform();
                    isReacting = false;
                }

                // Snapshot the target rect + base pose/scale as parameters
                // instead of letting the coroutine read mutable fields each
                // frame. If the player advances again mid-reaction, those
                // fields get reassigned to the NEXT speaker — without the
                // snapshot this coroutine would then animate the wrong portrait.
                reactionCoroutine = StartCoroutine(
                    PlayReaction(currentLine, currentPortraitRect, originalPortraitScale, originalPortraitPosition));
            }

            if (currentIndex < dialogueLines.Length && dialogueLines[currentIndex].openNameInputAfterThisLine)
            {
                OpenNameInput();
                return;
            }

            isAdvancing = true;
            StartCoroutine(AdvanceWithOutro());
        }

        private IEnumerator AdvanceWithOutro()
        {
            if (currentLine != null &&
                currentPortraitRect != null &&
                currentLine.outroType != OutroType.None)
            {
                // A reaction/shake from this same tap could still be
                // animating this exact rect — stop it before the outro starts
                // moving it, otherwise they fight over anchoredPosition/localScale.
                if (reactionCoroutine != null)
                {
                    StopCoroutine(reactionCoroutine);
                    reactionCoroutine = null;
                    isReacting = false;
                    ResetPortraitTransform();
                }

                if (portraitOutroCoroutine != null)
                    StopCoroutine(portraitOutroCoroutine);

                // Snapshot target + base position, same reasoning as PlayReaction above.
                portraitOutroCoroutine = StartCoroutine(
                    PlayPortraitOutro(currentLine, currentPortraitRect, originalPortraitPosition));
                yield return new WaitForSeconds(currentLine.outroDuration);
            }

            currentIndex++;

            if (currentIndex < dialogueLines.Length)
                ShowLine(dialogueLines[currentIndex]);
            else
                EndDialogue();

            // Release the lock once the transition has fully resolved
            // (next line shown, or dialogue ended).
            isAdvancing = false;
        }

        // -------------------------------------------------------------------------
        // ShowLine and sub-handlers
        // -------------------------------------------------------------------------
        private void ShowLine(DialogueLine line)
        {
            currentLine = line;

            // Guarantee a clean slate — stop any reaction still running
            // from the previous line before HandlePortraitDisplay() below
            // overwrites currentPortraitRect / originalPortraitScale / position.
            if (reactionCoroutine != null)
            {
                StopCoroutine(reactionCoroutine);
                reactionCoroutine = null;
            }
            isReacting = false;
            ResetPortraitTransform();

            // FIX: reset the anti-spam clock for the new line. Combined with
            // the isTyping check at the top of Advance(), this means a tap
            // can't sneak past minFullTextDisplayTime using a stale timestamp
            // left over from the PREVIOUS line.
            lineFullyShownTime = -999f;

            StartCoroutine(BlockInputForOneFrame());

            if (continueIndicator != null)
                continueIndicator.SetActive(false);

            if (dialoguePanel != null)
                dialoguePanel.SetActive(!line.hideDialoguePanel);

            HandleBlackScreen(line);
            HandleNameDisplay(line);
            HandlePortraitDisplay(line);
            HandleOptionalImageDisplay(line);
            SetupTypingSFX(line);
            PlayLineSound(line);

            if (typingCoroutine != null)
                StopCoroutine(typingCoroutine);

            typingCoroutine = StartCoroutine(TypeLine(line));
        }

        private void HandleBlackScreen(DialogueLine line)
        {
            bool currentIsBlack = line.useBlackScreen;
            bool needsFadeToBlack = currentIsBlack && !previousLineWasBlackScreen;
            bool needsFadeToClear = !currentIsBlack && previousLineWasBlackScreen;

            if (blackScreenCoroutine != null)
                StopCoroutine(blackScreenCoroutine);

            if (needsFadeToBlack)
            {
                if (sceneVisualsRoot != null)
                    sceneVisualsRoot.SetActive(false);
                blackScreenCoroutine = StartCoroutine(FadeBlackScreen(0f, 1f, fadeDuration));
            }
            else if (needsFadeToClear)
            {
                if (sceneVisualsRoot != null)
                    sceneVisualsRoot.SetActive(true);
                blackScreenCoroutine = StartCoroutine(FadeBlackScreen(1f, 0f, fadeDuration));
            }
            else if (!currentIsBlack)
            {
                if (sceneVisualsRoot != null)
                    sceneVisualsRoot.SetActive(true);
            }

            previousLineWasBlackScreen = currentIsBlack;

            if (currentIsBlack && portraitContainer != null)
                portraitContainer.SetActive(false);
        }

        private void HandleNameDisplay(DialogueLine line)
        {
            if (nameBox == null) return;

            if (line.ShouldUseCustomName())
                nameBox.text = line.customSpeakerName;
            else if (line.character != null)
                nameBox.text = line.character.characterName;
            else
                nameBox.text = "";
        }

        private void HandlePortraitDisplay(DialogueLine line)
        {
            bool hasEmotionPortrait = line.emotionPortrait != null;
            bool hasCharacter = line.character != null;

            if (hasEmotionPortrait || hasCharacter)
            {
                if (portraitContainer != null)
                    portraitContainer.SetActive(true);

                if (hasCharacter)
                {
                    if (currentSpeaker != null && currentSpeaker != line.character)
                        currentSpeaker.SetActive(false);

                    currentSpeaker = line.character;
                    DimAllExcept(currentSpeaker);
                    currentSpeaker.SetActive(true);
                    ResetAllPopOut();
                    currentSpeaker.PopOut();

                    if (hasEmotionPortrait)
                        currentSpeaker.SetPortrait(line.emotionPortrait);

                    if (entranceCoroutine != null)
                        StopCoroutine(entranceCoroutine);

                    if (line.entranceType != EntranceType.None)
                        entranceCoroutine = StartCoroutine(PlayEntrance(currentSpeaker.portraitImage, line));
                }
                else if (hasEmotionPortrait)
                {
                    if (currentSpeaker != null)
                    {
                        currentSpeaker.SetActive(false);
                        currentSpeaker = null;
                    }

                    if (allCharacters.Count > 0 && allCharacters[0] != null)
                    {
                        CharacterVN tempCharacter = allCharacters[0];
                        tempCharacter.SetPortrait(line.emotionPortrait);
                        tempCharacter.SetActive(true);
                        currentSpeaker = tempCharacter;

                        if (entranceCoroutine != null)
                            StopCoroutine(entranceCoroutine);

                        if (line.entranceType != EntranceType.None)
                            entranceCoroutine = StartCoroutine(PlayEntrance(tempCharacter.portraitImage, line));
                    }
                }

                CachePortraitReferences();
            }
            else
            {
                if (currentSpeaker != null)
                {
                    currentSpeaker.SetActive(false);
                    currentSpeaker = null;
                }

                DimAll();

                if (portraitContainer != null)
                    portraitContainer.SetActive(false);

                currentPortraitRect = null;
            }
        }

        private void CachePortraitReferences()
        {
            if (currentSpeaker != null && currentSpeaker.portraitImage != null)
            {
                currentPortraitRect = currentSpeaker.portraitImage.GetComponent<RectTransform>();
                if (currentPortraitRect != null)
                {
                    originalPortraitPosition = currentPortraitRect.anchoredPosition;
                    originalPortraitScale = currentPortraitRect.localScale;
                }
            }
            else
            {
                currentPortraitRect = null;
            }
        }

        private void ResetPortraitTransform()
        {
            if (currentPortraitRect == null) return;
            currentPortraitRect.localScale = originalPortraitScale;
            currentPortraitRect.anchoredPosition = originalPortraitPosition;
        }

        private void HandleOptionalImageDisplay(DialogueLine line)
        {
            if (currentOptionalImage != null && currentOptionalImage != line.optionalUIImage)
                currentOptionalImage.gameObject.SetActive(false);

            currentOptionalImage = line.optionalUIImage;

            if (currentOptionalImage != null)
                currentOptionalImage.gameObject.SetActive(true);
        }

        private void HideAllOptionalImages()
        {
            if (currentOptionalImage != null)
            {
                currentOptionalImage.gameObject.SetActive(false);
                currentOptionalImage = null;
            }

            if (dialogueLines == null) return;

            foreach (DialogueLine line in dialogueLines)
            {
                if (line.optionalUIImage != null)
                    line.optionalUIImage.gameObject.SetActive(false);
            }
        }

        // -------------------------------------------------------------------------
        // Audio helpers
        // -------------------------------------------------------------------------
        private void SetupTypingSFX(DialogueLine line)
        {
            currentTypingSFX = line.typingSFX != null ? line.typingSFX : defaultTypingSFX;
        }

        private void PlayTypingSFX()
        {
            if (currentTypingSFX != null && typingSFXSource != null)
                typingSFXSource.PlayOneShot(currentTypingSFX);
        }

        private void StopTypingSFX()
        {
            if (typingSFXSource != null)
                typingSFXSource.Stop();
        }

        private void PlayLineSound(DialogueLine line)
        {
            if (line.soundClip == null || audioSource == null) return;
            audioSource.PlayOneShot(line.soundClip);
        }

        // -------------------------------------------------------------------------
        // PlayReaction
        // Takes target/baseScale/basePos as parameters (snapshotted at call
        // time in Advance()) instead of reading the mutable currentPortraitRect
        // / originalPortraitScale / originalPortraitPosition fields each
        // frame. Those fields can be reassigned to a NEW speaker by
        // ShowLine() -> CachePortraitReferences() while this coroutine is
        // still running, which was the cause of a previous fast-tap glitch.
        // -------------------------------------------------------------------------
        private IEnumerator PlayReaction(DialogueLine line, RectTransform target, Vector3 baseScale, Vector2 basePos)
        {
            if (target == null) yield break;

            isReacting = true;

            if (line.enableReaction)
            {
                float elapsed = 0f;
                Vector3 startScale = target.localScale;
                Vector3 targetScale = baseScale * line.reactionScaleTarget;

                while (elapsed < line.reactionDuration * 0.5f)
                {
                    elapsed += Time.deltaTime;
                    float t = elapsed / (line.reactionDuration * 0.5f);
                    target.localScale = Vector3.Lerp(startScale, targetScale,
                        1f - Mathf.Cos(t * Mathf.PI * 0.5f));
                    yield return null;
                }
                target.localScale = targetScale;
            }

            if (line.enableShake)
            {
                float clampedShakeDuration = Mathf.Max(line.shakeDuration, 0.05f);
                float shakeElapsed = 0f;

                while (shakeElapsed < clampedShakeDuration)
                {
                    shakeElapsed += Time.deltaTime;
                    target.anchoredPosition = new Vector2(
                        basePos.x + Random.Range(-line.shakeIntensity, line.shakeIntensity),
                        basePos.y + Random.Range(-line.shakeIntensity, line.shakeIntensity));
                    yield return null;
                }

                target.anchoredPosition = basePos;
            }

            if (line.enableReaction)
            {
                float elapsed = 0f;
                Vector3 startScale = target.localScale;

                while (elapsed < line.reactionDuration * 0.5f)
                {
                    elapsed += Time.deltaTime;
                    float t = elapsed / (line.reactionDuration * 0.5f);
                    target.localScale = Vector3.Lerp(startScale, baseScale,
                        Mathf.Sin(t * Mathf.PI * 0.5f));
                    yield return null;
                }
                target.localScale = baseScale;
            }

            isReacting = false;
        }

        // -------------------------------------------------------------------------
        // Portrait outro
        // Takes target/basePos as parameters for the same reason as
        // PlayReaction above.
        // -------------------------------------------------------------------------
        private IEnumerator PlayPortraitOutro(DialogueLine line, RectTransform target, Vector2 basePos)
        {
            if (target == null || line.outroType == OutroType.None) yield break;

            float elapsed = 0f;
            Vector2 startPos = target.anchoredPosition;

            Canvas canvas = target.GetComponentInParent<Canvas>();
            float canvasWidth = canvas != null ? canvas.pixelRect.width : Screen.width;
            float slideDist = canvasWidth + target.rect.width;

            float dir = line.outroType == OutroType.SlideLeft ? -1f : 1f;
            Vector2 targetPos = new Vector2(startPos.x + slideDist * dir, startPos.y);

            while (elapsed < line.outroDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / line.outroDuration;
                float eased = t * t * (3f - 2f * t);
                target.anchoredPosition = Vector2.Lerp(startPos, targetPos, eased);
                yield return null;
            }

            target.anchoredPosition = targetPos;
            target.anchoredPosition = basePos;
        }

        // -------------------------------------------------------------------------
        // PlayEntrance
        // -------------------------------------------------------------------------
        private IEnumerator PlayEntrance(Image target, DialogueLine line)
        {
            if (target == null) yield break;

            RectTransform rect = target.GetComponent<RectTransform>();
            if (rect == null) yield break;

            if (line.customEntranceClip != null)
            {
                Animator anim = target.GetComponent<Animator>();
                if (anim == null)
                    anim = target.gameObject.AddComponent<Animator>();

                if (anim.runtimeAnimatorController != null)
                {
                    AnimatorOverrideController overrideCtrl =
                        new AnimatorOverrideController(anim.runtimeAnimatorController);
                    overrideCtrl["CustomEntrance"] = line.customEntranceClip;
                    anim.runtimeAnimatorController = overrideCtrl;
                    anim.Play("CustomEntrance", 0, 0f);
                    yield return new WaitForSeconds(line.customEntranceClip.length);
                }
                else
                {
                    Debug.LogWarning("[DialogueManager] customEntranceClip assigned but Animator has no controller. Falling back to procedural entrance.");
                }

                yield break;
            }

            float elapsed = 0f;
            Vector2 originalPos = rect.anchoredPosition;

            if (line.entranceType == EntranceType.SlideFromLeft || line.entranceType == EntranceType.SlideFromRight)
            {
                float dir = line.entranceType == EntranceType.SlideFromLeft ? -1f : 1f;
                Vector2 startPos = originalPos + new Vector2(dir * slideDistance, 0f);

                while (elapsed < line.entranceDuration)
                {
                    elapsed += Time.deltaTime;
                    float t = Mathf.SmoothStep(0f, 1f, elapsed / line.entranceDuration);
                    rect.anchoredPosition = Vector2.Lerp(startPos, originalPos, t);
                    yield return null;
                }
                rect.anchoredPosition = originalPos;
            }
            else if (line.entranceType == EntranceType.FadeIn)
            {
                Color c = target.color;
                c.a = 0f;
                target.color = c;

                while (elapsed < line.entranceDuration)
                {
                    elapsed += Time.deltaTime;
                    c.a = Mathf.SmoothStep(0f, 1f, elapsed / line.entranceDuration);
                    target.color = c;
                    yield return null;
                }

                c.a = 1f;
                target.color = c;
            }
        }

        // -------------------------------------------------------------------------
        // Typewriter
        // -------------------------------------------------------------------------
        private IEnumerator TypeLine(DialogueLine line)
        {
            isTyping = true;

            string displayText = BuildFormattedText(line);
            textBox.text = displayText;
            textBox.maxVisibleCharacters = 0;
            textBox.ForceMeshUpdate();

            RebuildTextLayout();
            ScrollToTop();

            int total = textBox.textInfo.characterCount;
            WaitForSeconds wait = new WaitForSeconds(typingSpeed);

            for (int i = 0; i <= total; i++)
            {
                textBox.maxVisibleCharacters = i;

                if (i > 0 && i % typingSFXInterval == 0)
                    PlayTypingSFX();

                if (i % 5 == 0)
                {
                    RebuildTextLayout();
                    ScrollToBottom();
                }

                yield return wait;
            }

            isTyping = false;

            // FIX: text just became fully visible naturally — start the
            // anti-spam hold clock from right now.
            lineFullyShownTime = Time.unscaledTime;

            RebuildTextLayout();
            ScrollToBottom();

            if (continueIndicator != null)
                continueIndicator.SetActive(true);
        }

        private string BuildFormattedText(DialogueLine line)
        {
            // Read language from PlayerPrefs — LocalizationManager keeps
            // "SelectedLanguage" in sync whenever the player switches language.
            bool isFilipino = PlayerPrefs.GetString("SelectedLanguage", "fil") == "fil";

            string rawText = line.dialogueText;

            if (isFilipino && !string.IsNullOrEmpty(line.filipinoText))
                rawText = line.filipinoText;

            string text = PlayerNameManager.InjectPlayerName(rawText);

            if (line.useBold && line.useItalic)
                text = "<b><i>" + text + "</i></b>";
            else if (line.useBold)
                text = "<b>" + text + "</b>";
            else if (line.useItalic)
                text = "<i>" + text + "</i>";

            return text;
        }

        private void SnapToFull()
        {
            if (typingCoroutine != null)
                StopCoroutine(typingCoroutine);

            isTyping = false;

            // FIX: same as the natural-finish case in TypeLine() — the text
            // is now fully visible (via tap-skip instead of typing out), so
            // the anti-spam hold clock starts from right now too. Without
            // this, tap-skipping the typewriter would leave the OLD (or
            // default -999) timestamp in place and let the very next tap
            // blow straight through to the next line with zero hold at all.
            lineFullyShownTime = Time.unscaledTime;

            if (stopSFXOnSkip)
                StopTypingSFX();

            textBox.text = BuildFormattedText(dialogueLines[currentIndex]);
            textBox.maxVisibleCharacters = int.MaxValue;

            RebuildTextLayout();
            ScrollToBottom();

            if (continueIndicator != null)
                continueIndicator.SetActive(true);
        }

        // -------------------------------------------------------------------------
        // End dialogue
        // -------------------------------------------------------------------------
        private void EndDialogue()
        {
            if (reactionCoroutine != null) StopCoroutine(reactionCoroutine);
            if (portraitOutroCoroutine != null) StopCoroutine(portraitOutroCoroutine);

            isReacting = false;
            isAdvancing = false; // don't leave the lock stuck true if StartDialogue() is reused
            ResetPortraitTransform();

            dialogueFinished = true;
            isTyping = false;
            currentLine = null;

            StopTypingSFX();

            if (currentSpeaker != null)
                currentSpeaker.SetActive(false);

            DimAll();
            ResetAllPopOut();
            HideAllOptionalImages();

            if (continueIndicator != null)
                continueIndicator.SetActive(false);

            // This scene now actually loads onFinishLoadScene once the
            // dialogue ends, instead of only fading to black and hiding the
            // panel with nothing happening after.
            if (autoAdvanceOnEnd)
            {
                if (blackScreenCoroutine != null)
                    StopCoroutine(blackScreenCoroutine);

                blackScreenCoroutine = StartCoroutine(FadeAndLoadNextScene());
            }
            else
            {
                if (previousLineWasBlackScreen && blackScreenImage != null)
                {
                    if (blackScreenCoroutine != null)
                        StopCoroutine(blackScreenCoroutine);

                    blackScreenCoroutine = StartCoroutine(FadeBlackScreen(1f, 0f, fadeDuration));

                    if (sceneVisualsRoot != null)
                        sceneVisualsRoot.SetActive(true);
                }

                if (portraitContainer != null)
                    portraitContainer.SetActive(true);

                if (dialoguePanel != null)
                    dialoguePanel.SetActive(false);
            }

            currentTypingSFX = null;
        }

        // -------------------------------------------------------------------------
        // Name input
        // -------------------------------------------------------------------------
        private void OpenNameInput()
        {
            if (nameInputScreen == null)
            {
                Debug.LogWarning("[DialogueManager] Name input screen missing.");
                currentIndex++;

                if (currentIndex < dialogueLines.Length)
                    ShowLine(dialogueLines[currentIndex]);
                else
                    EndDialogue();

                return;
            }

            waitingForNameInput = true;
            nameInputScreen.Show();
        }

        private void OnPlayerNameConfirmed(string confirmedName)
        {
            waitingForNameInput = false;
            currentIndex++;

            if (currentIndex < dialogueLines.Length)
                ShowLine(dialogueLines[currentIndex]);
            else
                EndDialogue();
        }

        // -------------------------------------------------------------------------
        // Black screen fade
        // -------------------------------------------------------------------------
        private IEnumerator FadeBlackScreen(float startAlpha, float endAlpha, float duration)
        {
            if (blackScreenImage == null) yield break;

            blackScreenImage.gameObject.SetActive(true);

            float elapsed = 0f;
            Color c = blackScreenImage.color;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                c.a = Mathf.Lerp(startAlpha, endAlpha, Mathf.Clamp01(elapsed / duration));
                blackScreenImage.color = c;
                yield return null;
            }

            c.a = endAlpha;
            blackScreenImage.color = c;

            if (endAlpha <= 0f)
                blackScreenImage.gameObject.SetActive(false);
        }

        // -------------------------------------------------------------------------
        // Fades to black, then actually loads onFinishLoadScene via
        // SceneTransitionManager.
        // -------------------------------------------------------------------------
        private IEnumerator FadeAndLoadNextScene()
        {
            yield return StartCoroutine(FadeBlackScreen(0f, 1f, fadeDuration));

            Debug.Log("[DialogueManager] Dialogue finished: " + dialogueSceneID);

            if (string.IsNullOrEmpty(onFinishLoadScene))
            {
                Debug.LogError("[DialogueManager] On Finish Load Scene is empty on " + gameObject.name +
                                ". The screen will stay black — drag the scene you want to load " +
                                "into the 'On Finish Load Scene Asset' field in the Inspector.");
                if (dialoguePanel != null)
                    dialoguePanel.SetActive(false);
                yield break;
            }

            if (SceneTransitionManager.Instance != null)
            {
                SceneTransitionManager.Instance.NavigateTo(onFinishLoadScene, onFinishUseLoadingScreen);
            }
            else
            {
                Debug.LogWarning("[DialogueManager] SceneTransitionManager not found. Loading '" +
                                 onFinishLoadScene + "' directly.");
                UnityEngine.SceneManagement.SceneManager.LoadScene(onFinishLoadScene);
            }
        }

        // -------------------------------------------------------------------------
        // Scroll and layout helpers
        // -------------------------------------------------------------------------
        private void RebuildTextLayout()
        {
            if (textContentRect != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(textContentRect);
            else if (textBox != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(textBox.GetComponent<RectTransform>());
        }

        private void ScrollToBottom()
        {
            if (textScrollRect != null)
                textScrollRect.normalizedPosition = new Vector2(0f, 0f);
        }

        private void ScrollToTop()
        {
            if (textScrollRect != null)
                textScrollRect.normalizedPosition = new Vector2(0f, 1f);
        }

        // -------------------------------------------------------------------------
        // Character visibility helpers
        // -------------------------------------------------------------------------
        private void DimAll()
        {
            foreach (CharacterVN c in allCharacters)
            {
                if (c != null)
                    c.SetActive(false);
            }
        }

        private void DimAllExcept(CharacterVN speaker)
        {
            foreach (CharacterVN c in allCharacters)
            {
                if (c != null)
                    c.SetActive(c == speaker);
            }
        }

        private void ResetAllPopOut()
        {
            foreach (CharacterVN c in allCharacters)
            {
                if (c != null)
                    c.ResetPopOut();
            }
        }

        // -------------------------------------------------------------------------
        // Misc helpers
        // -------------------------------------------------------------------------
        private IEnumerator BlockInputForOneFrame()
        {
            inputBlocked = true;
            yield return null;
            inputBlocked = false;
        }
    }
}