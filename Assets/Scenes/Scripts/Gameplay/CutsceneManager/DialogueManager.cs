using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;
using System.Collections;
using System.Collections.Generic;

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

        [Header("Name Input Screen")]
        public NameInputScreen nameInputScreen;

        [Header("Typewriter Settings")]
        public float typingSpeed = 0.04f;

        [Header("Entrance Animation")]
        public float slideDistance = 200f;
        public float animationDuration = 0.3f;

        [Header("Dialogue Lines")]
        public DialogueLine[] dialogueLines;

        [Header("Auto Start")]
        public bool autoStartOnAwake = true;

        [Header("Portrait Settings")]
        [Tooltip("Container holding all portraits")]
        public GameObject portraitContainer;

        public bool isTyping { get; private set; }
        public bool dialogueFinished { get; private set; }

        private int currentIndex = 0;

        private Coroutine typingCoroutine;
        private Coroutine entranceCoroutine;
        private Coroutine blackScreenCoroutine;

        private CharacterVN currentSpeaker;

        private bool inputBlocked = false;
        private bool previousLineWasBlackScreen = false;
        private bool waitingForNameInput = false;

        private RectTransform textContentRect;

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
        }

        private void Start()
        {
            if (autoStartOnAwake)
                StartDialogue();
        }

        private void Update()
        {
            if (dialogueFinished)
                return;

            if (inputBlocked)
                return;

            if (waitingForNameInput)
                return;

            bool advance = false;

            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                advance = true;
            }

            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
            {
                advance = true;
            }

            if (Keyboard.current != null)
            {
                if (Keyboard.current.spaceKey.wasPressedThisFrame || Keyboard.current.enterKey.wasPressedThisFrame)
                {
                    advance = true;
                }
            }

            if (advance)
                Advance();
        }

        public void StartDialogue()
        {
            if (dialogueLines == null || dialogueLines.Length == 0)
            {
                Debug.LogWarning("[DialogueManager] No dialogue lines assigned.");
                return;
            }

            currentIndex = 0;
            dialogueFinished = false;
            currentSpeaker = null;
            previousLineWasBlackScreen = false;
            waitingForNameInput = false;

            if (dialoguePanel != null)
                dialoguePanel.SetActive(true);

            if (continueIndicator != null)
                continueIndicator.SetActive(false);

            DimAll();
            ResetAllPopOut();

            ShowLine(dialogueLines[currentIndex]);
        }

        private void Advance()
        {
            if (isTyping)
            {
                SnapToFull();
                return;
            }

            if (currentIndex < dialogueLines.Length && dialogueLines[currentIndex].openNameInputAfterThisLine)
            {
                OpenNameInput();
                return;
            }

            currentIndex++;

            if (currentIndex < dialogueLines.Length)
            {
                ShowLine(dialogueLines[currentIndex]);
            }
            else
            {
                EndDialogue();
            }
        }

        private string BuildFormattedText(DialogueLine line)
        {
            string text = PlayerNameManager.InjectPlayerName(line.dialogueText);

            if (line.useBold && line.useItalic)
            {
                text = "<b><i>" + text + "</i></b>";
            }
            else if (line.useBold)
            {
                text = "<b>" + text + "</b>";
            }
            else if (line.useItalic)
            {
                text = "<i>" + text + "</i>";
            }

            return text;
        }

        private void ShowLine(DialogueLine line)
        {
            StartCoroutine(BlockInputForOneFrame());

            if (continueIndicator != null)
                continueIndicator.SetActive(false);

            if (dialoguePanel != null)
            {
                dialoguePanel.SetActive(!line.hideDialoguePanel);
            }

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

            HandleNameDisplay(line);
            HandlePortraitDisplay(line);

            if (currentIsBlack)
            {
                if (portraitContainer != null)
                    portraitContainer.SetActive(false);
            }

            PlayLineSound(line);

            if (typingCoroutine != null)
                StopCoroutine(typingCoroutine);

            typingCoroutine = StartCoroutine(TypeLine(line));
        }

        private void HandleNameDisplay(DialogueLine line)
        {
            if (line.ShouldUseCustomName())
            {
                nameBox.text = line.customSpeakerName;
            }
            else if (line.character != null)
            {
                nameBox.text = line.character.characterName;
            }
            else
            {
                nameBox.text = "";
            }
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
                    {
                        currentSpeaker.SetActive(false);
                    }

                    currentSpeaker = line.character;
                    DimAllExcept(currentSpeaker);
                    currentSpeaker.SetActive(true);
                    ResetAllPopOut();
                    currentSpeaker.PopOut();

                    if (hasEmotionPortrait)
                    {
                        currentSpeaker.SetPortrait(line.emotionPortrait);
                    }

                    if (entranceCoroutine != null)
                    {
                        StopCoroutine(entranceCoroutine);
                    }

                    if (line.entranceType != EntranceType.None)
                    {
                        entranceCoroutine = StartCoroutine(PlayEntrance(currentSpeaker.portraitImage, line.entranceType));
                    }
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
                        {
                            StopCoroutine(entranceCoroutine);
                        }

                        if (line.entranceType != EntranceType.None)
                        {
                            entranceCoroutine = StartCoroutine(PlayEntrance(tempCharacter.portraitImage, line.entranceType));
                        }
                    }
                }
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
            }
        }

        private void OpenNameInput()
        {
            if (nameInputScreen == null)
            {
                Debug.LogWarning("[DialogueManager] Name input screen missing.");
                currentIndex++;

                if (currentIndex < dialogueLines.Length)
                {
                    ShowLine(dialogueLines[currentIndex]);
                }
                else
                {
                    EndDialogue();
                }

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
            {
                ShowLine(dialogueLines[currentIndex]);
            }
            else
            {
                EndDialogue();
            }
        }

        private void PlayLineSound(DialogueLine line)
        {
            if (line.soundClip == null)
                return;

            if (audioSource == null)
                return;

            audioSource.PlayOneShot(line.soundClip);
        }

        private IEnumerator FadeBlackScreen(float startAlpha, float endAlpha, float duration)
        {
            if (blackScreenImage == null)
                yield break;

            blackScreenImage.gameObject.SetActive(true);

            float elapsed = 0f;
            Color c = blackScreenImage.color;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                c.a = Mathf.Lerp(startAlpha, endAlpha, t);
                blackScreenImage.color = c;
                yield return null;
            }

            c.a = endAlpha;
            blackScreenImage.color = c;

            if (endAlpha <= 0f)
            {
                blackScreenImage.gameObject.SetActive(false);
            }
        }

        private IEnumerator BlockInputForOneFrame()
        {
            inputBlocked = true;
            yield return null;
            inputBlocked = false;
        }

        private void SnapToFull()
        {
            if (typingCoroutine != null)
                StopCoroutine(typingCoroutine);

            isTyping = false;

            string displayText = BuildFormattedText(dialogueLines[currentIndex]);
            textBox.text = displayText;
            textBox.maxVisibleCharacters = int.MaxValue;

            RebuildTextLayout();
            ScrollToBottom();

            if (continueIndicator != null)
                continueIndicator.SetActive(true);
        }

        private void EndDialogue()
        {
            dialogueFinished = true;
            isTyping = false;

            if (previousLineWasBlackScreen && blackScreenImage != null)
            {
                if (blackScreenCoroutine != null)
                {
                    StopCoroutine(blackScreenCoroutine);
                }

                blackScreenCoroutine = StartCoroutine(FadeBlackScreen(1f, 0f, fadeDuration));

                if (sceneVisualsRoot != null)
                    sceneVisualsRoot.SetActive(true);
            }

            if (currentSpeaker != null)
            {
                currentSpeaker.SetActive(false);
            }

            DimAll();
            ResetAllPopOut();

            if (portraitContainer != null)
                portraitContainer.SetActive(true);

            if (continueIndicator != null)
                continueIndicator.SetActive(false);

            if (dialoguePanel != null)
                dialoguePanel.SetActive(false);
        }

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

                if (i % 5 == 0)
                {
                    RebuildTextLayout();
                    ScrollToBottom();
                }

                yield return wait;
            }

            isTyping = false;
            RebuildTextLayout();
            ScrollToBottom();

            if (continueIndicator != null)
                continueIndicator.SetActive(true);
        }

        private void RebuildTextLayout()
        {
            if (textContentRect != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(textContentRect);
            }
            else if (textBox != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(textBox.GetComponent<RectTransform>());
            }
        }

        private void ScrollToBottom()
        {
            if (textScrollRect != null)
            {
                textScrollRect.normalizedPosition = new Vector2(0f, 0f);
            }
        }

        private void ScrollToTop()
        {
            if (textScrollRect != null)
            {
                textScrollRect.normalizedPosition = new Vector2(0f, 1f);
            }
        }

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

        private IEnumerator PlayEntrance(Image target, EntranceType type)
        {
            if (target == null)
                yield break;

            RectTransform rect = target.GetComponent<RectTransform>();

            if (rect == null)
                yield break;

            float elapsed = 0f;
            Vector2 originalPos = rect.anchoredPosition;

            if (type == EntranceType.SlideFromLeft || type == EntranceType.SlideFromRight)
            {
                float dir = type == EntranceType.SlideFromLeft ? -1f : 1f;
                Vector2 startPos = originalPos + new Vector2(dir * slideDistance, 0f);

                while (elapsed < animationDuration)
                {
                    elapsed += Time.deltaTime;
                    float t = Mathf.SmoothStep(0f, 1f, elapsed / animationDuration);
                    rect.anchoredPosition = Vector2.Lerp(startPos, originalPos, t);
                    yield return null;
                }

                rect.anchoredPosition = originalPos;
            }
            else if (type == EntranceType.FadeIn)
            {
                Color c = target.color;
                c.a = 0f;
                target.color = c;

                while (elapsed < animationDuration)
                {
                    elapsed += Time.deltaTime;
                    float t = Mathf.SmoothStep(0f, 1f, elapsed / animationDuration);
                    c.a = Mathf.Lerp(0f, 1f, t);
                    target.color = c;
                    yield return null;
                }

                c.a = 1f;
                target.color = c;
            }
        }
    }
}