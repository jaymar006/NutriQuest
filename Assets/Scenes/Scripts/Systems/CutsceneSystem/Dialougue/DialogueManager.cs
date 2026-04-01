using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class DialogueManager : MonoBehaviour
{
    [System.Serializable]
    public class CharacterData
    {
        public string characterName;
        public DialogueCharacterAnimator characterAnimator;
    }

    [System.Serializable]
    public class DialogueLine
    {
        [Tooltip("Must match a character name exactly.")]
        public string speakerName;

        [TextArea(2, 5)]
        [Tooltip("Supports rich text: <b>bold</b> <color=red>color</color> <size=40>size</size>")]
        public string dialogueText;

        [Tooltip("Sprite to swap on speaker for this line. Leave empty to keep current.")]
        public Sprite characterSprite;

        [Header("Line Animations")]
        [Tooltip("Zoom in on the speaking character's sprite.")]
        public bool zoomCharacter = false;
        [Tooltip("Zoom in on the entire scene/camera.")]
        public bool zoomCamera = false;
        [Tooltip("Shake the speaking character.")]
        public bool shakeCharacter = false;
        [Tooltip("Shake the entire scene/camera.")]
        public bool shakeCamera = false;

        [Header("Text Entrance Animation")]
        public TextEntrance textEntrance = TextEntrance.FadeIn;
    }

    public enum TextEntrance
    {
        None,
        FadeIn,
        SlideFromLeft,
        SlideFromRight,
        PopScale,
        BounceIn
    }

    [Header("Characters")]
    [SerializeField] private List<CharacterData> characters = new List<CharacterData>();

    [Header("Dialogue Lines")]
    [SerializeField] private List<DialogueLine> dialogueLines = new List<DialogueLine>();

    [Header("UI")]
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private TMP_Text speakerNameText;
    [SerializeField] private TMP_Text dialogueBodyText;
    [SerializeField] private GameObject tapToContinueIndicator;
    [SerializeField] private RectTransform dialogueBoxRect;

    [Header("Typewriter Settings")]
    [SerializeField] private float typewriterSpeed = 0.04f;

    [Header("Transition Settings")]
    [SerializeField] private string nextSceneName;
    [SerializeField] private bool useLoadingScreen = false;
    [SerializeField] private bool autoStart = true;

    private int _currentLineIndex = 0;
    private bool _isTyping = false;
    private bool _dialogueFinished = false;
    public bool IsDialogueFinished => _dialogueFinished;
    private Coroutine _typewriterCoroutine;
    private Coroutine _textEntranceCoroutine;
    private string _currentSpeaker = "";
  


    private void Start()
    {
        if (tapToContinueIndicator != null)
            tapToContinueIndicator.SetActive(false);

        if (autoStart)
            StartDialogue();
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0) ||
            (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began))
        {
            OnScreenTapped();
        }
    }

    public void StartDialogue()
    {
        _currentLineIndex = 0;
        _dialogueFinished = false;

        if (dialoguePanel != null)
            dialoguePanel.SetActive(true);

        ShowLine(_currentLineIndex);
    }

    private void OnScreenTapped()
    {
        if (_dialogueFinished) return;

        if (_isTyping)
        {
            SkipTypewriter();
            return;
        }

        _currentLineIndex++;

        if (_currentLineIndex >= dialogueLines.Count)
        {
            FinishDialogue();
            return;
        }

        ShowLine(_currentLineIndex);
    }

    private void ShowLine(int index)
    {
        if (index < 0 || index >= dialogueLines.Count) return;

        DialogueLine line = dialogueLines[index];

        // Update speaker name //
        if (speakerNameText != null)
            speakerNameText.text = line.speakerName;

        // Update character visuals //
        UpdateCharacterVisuals(line);

        // Play line animations //
        PlayLineAnimations(line);

        // Play voice intro SFX //
        if (DialogueAudioManager.Instance != null)
            DialogueAudioManager.Instance.PlayVoiceIntro(line.speakerName);

        // Hide tap indicator while typing //
        if (tapToContinueIndicator != null)
            tapToContinueIndicator.SetActive(false);

        // Play text entrance animation //
        if (_textEntranceCoroutine != null)
            StopCoroutine(_textEntranceCoroutine);

        _textEntranceCoroutine = StartCoroutine(
            PlayTextEntrance(line.textEntrance, line));
    }

    // Highlight speaker, dim others, swap sprite //
    private void UpdateCharacterVisuals(DialogueLine line)
    {
        foreach (CharacterData character in characters)
        {
            if (character.characterAnimator == null) continue;

            bool isSpeaking = character.characterName == line.speakerName;
            character.characterAnimator.SetHighlight(isSpeaking);

            if (isSpeaking && line.characterSprite != null)
                character.characterAnimator.SetSprite(line.characterSprite);
        }
    }

    // Trigger per-line animations //
    private void PlayLineAnimations(DialogueLine line)
    {
        DialogueCharacterAnimator speaker = GetCharacterAnimator(line.speakerName);

        if (speaker != null)
        {
            if (line.zoomCharacter) speaker.PlayZoomIn();
            else speaker.PlayZoomOut();

            if (line.shakeCharacter) speaker.PlayShake();
        }

        if (DialogueCameraAnimator.Instance != null)
        {
            if (line.zoomCamera) DialogueCameraAnimator.Instance.PlayZoomIn();
            else DialogueCameraAnimator.Instance.PlayZoomOut();

            if (line.shakeCamera) DialogueCameraAnimator.Instance.PlayShake();
        }
    }

    // Text entrance animation before typewriter starts //
    private IEnumerator PlayTextEntrance(TextEntrance entrance, DialogueLine line)
    {
        if (dialogueBodyText == null) yield break;

        dialogueBodyText.alpha = 0f;
        dialogueBodyText.text = "";

        CanvasGroup cg = dialogueBodyText.GetComponent<CanvasGroup>();
        if (cg == null) cg = dialogueBodyText.gameObject.AddComponent<CanvasGroup>();

        Vector3 originalPos = dialogueBoxRect != null
            ? dialogueBoxRect.localPosition
            : Vector3.zero;

        float duration = 0.25f;
        float time = 0f;

        switch (entrance)
        {
            case TextEntrance.FadeIn:
                cg.alpha = 0f;
                while (time < duration)
                {
                    time += Time.deltaTime;
                    cg.alpha = Mathf.Lerp(0f, 1f, time / duration);
                    yield return null;
                }
                cg.alpha = 1f;
                break;

            case TextEntrance.SlideFromLeft:
                if (dialogueBoxRect != null)
                {
                    dialogueBoxRect.localPosition = originalPos + new Vector3(-300f, 0f, 0f);
                    cg.alpha = 1f;
                    while (time < duration)
                    {
                        time += Time.deltaTime;
                        float t = Mathf.SmoothStep(0f, 1f, time / duration);
                        dialogueBoxRect.localPosition = Vector3.Lerp(
                            originalPos + new Vector3(-300f, 0f, 0f), originalPos, t);
                        yield return null;
                    }
                    dialogueBoxRect.localPosition = originalPos;
                }
                break;

            case TextEntrance.SlideFromRight:
                if (dialogueBoxRect != null)
                {
                    dialogueBoxRect.localPosition = originalPos + new Vector3(300f, 0f, 0f);
                    cg.alpha = 1f;
                    while (time < duration)
                    {
                        time += Time.deltaTime;
                        float t = Mathf.SmoothStep(0f, 1f, time / duration);
                        dialogueBoxRect.localPosition = Vector3.Lerp(
                            originalPos + new Vector3(300f, 0f, 0f), originalPos, t);
                        yield return null;
                    }
                    dialogueBoxRect.localPosition = originalPos;
                }
                break;

            case TextEntrance.PopScale:
                if (dialogueBoxRect != null)
                {
                    Vector3 originalScale = dialogueBoxRect.localScale;
                    dialogueBoxRect.localScale = Vector3.zero;
                    cg.alpha = 1f;
                    while (time < duration)
                    {
                        time += Time.deltaTime;
                        float t = Mathf.SmoothStep(0f, 1f, time / duration);
                        dialogueBoxRect.localScale = Vector3.Lerp(
                            Vector3.zero, originalScale, t);
                        yield return null;
                    }
                    dialogueBoxRect.localScale = originalScale;
                }
                break;

            case TextEntrance.BounceIn:
                if (dialogueBoxRect != null)
                {
                    Vector3 originalScale = dialogueBoxRect.localScale;
                    dialogueBoxRect.localScale = Vector3.zero;
                    cg.alpha = 1f;
                    while (time < duration * 1.3f)
                    {
                        time += Time.deltaTime;
                        float t = time / duration;
                        float bounce = Mathf.Sin(t * Mathf.PI) * 0.3f;
                        float scale = Mathf.Lerp(0f, 1f, t) + bounce;
                        dialogueBoxRect.localScale = originalScale * Mathf.Clamp(scale, 0f, 1.4f);
                        yield return null;
                    }
                    dialogueBoxRect.localScale = originalScale;
                }
                break;

            default:
                cg.alpha = 1f;
                break;
        }

        dialogueBodyText.alpha = 1f;

        // Start typewriter after entrance //
        if (_typewriterCoroutine != null)
            StopCoroutine(_typewriterCoroutine);

        _typewriterCoroutine = StartCoroutine(TypewriterEffect(line));
    }

    // Typewriter with rich text support and per-character SFX //
    private IEnumerator TypewriterEffect(DialogueLine line)
    {
        _isTyping = true;
        _currentSpeaker = line.speakerName;

        dialogueBodyText.text = "";

        // TMP supports rich text natively — just feed the text directly //
        string fullText = line.dialogueText;
        int charIndex = 0;

        // Use TMP's maxVisibleCharacters for rich text typewriter //
        dialogueBodyText.text = fullText;
        dialogueBodyText.maxVisibleCharacters = 0;

        int totalChars = dialogueBodyText.textInfo.characterCount;

        while (charIndex < totalChars)
        {
            charIndex++;
            dialogueBodyText.maxVisibleCharacters = charIndex;

            // Play typing SFX //
            if (DialogueAudioManager.Instance != null)
                DialogueAudioManager.Instance.PlayTypingSound(line.speakerName, charIndex);

            yield return new WaitForSeconds(typewriterSpeed);
        }

        dialogueBodyText.maxVisibleCharacters = totalChars;
        _isTyping = false;
        _typewriterCoroutine = null;

        if (tapToContinueIndicator != null)
            tapToContinueIndicator.SetActive(true);
    }

    // Skip typewriter — show all text immediately //
    private void SkipTypewriter()
    {
        if (_typewriterCoroutine != null)
        {
            StopCoroutine(_typewriterCoroutine);
            _typewriterCoroutine = null;
        }

        if (dialogueBodyText != null)
            dialogueBodyText.maxVisibleCharacters = int.MaxValue;

        if (DialogueAudioManager.Instance != null)
            DialogueAudioManager.Instance.StopTypingSound();

        _isTyping = false;

        if (tapToContinueIndicator != null)
            tapToContinueIndicator.SetActive(true);
    }

    private void FinishDialogue()
    {
        _dialogueFinished = true;

        if (tapToContinueIndicator != null)
            tapToContinueIndicator.SetActive(false);

        if (dialoguePanel != null)
            dialoguePanel.SetActive(false);

        Debug.Log("[DialogueManager] Dialogue finished.");

        if (!string.IsNullOrEmpty(nextSceneName))
        {
            if (SceneTransitionManager.Instance != null)
                SceneTransitionManager.Instance.NavigateTo(nextSceneName, useLoadingScreen);
            else
                UnityEngine.SceneManagement.SceneManager.LoadScene(nextSceneName);
        }
    }

    public void SkipAllDialogue() => FinishDialogue();

    private DialogueCharacterAnimator GetCharacterAnimator(string name)
    {
        foreach (CharacterData c in characters)
        {
            if (c.characterName == name)
                return c.characterAnimator;
        }
        return null;
    }

    
}