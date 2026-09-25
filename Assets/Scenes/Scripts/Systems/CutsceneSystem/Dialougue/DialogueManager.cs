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
        public string speakerName;
        [TextArea(2, 8)]
        public string dialogueText;
        public Sprite characterSprite;

        [Header("Line Animations")]
        public bool zoomCharacter = false;
        public bool zoomCamera = false;
        public bool shakeCharacter = false;
        public bool shakeCamera = false;

        public TextEntrance textEntrance = TextEntrance.FadeIn;
    }

    public enum TextEntrance { None, FadeIn, SlideFromLeft, SlideFromRight, PopScale, BounceIn }

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

    [Header("Transition")]
    [SerializeField] private string nextSceneName;
    [SerializeField] private bool useLoadingScreen = false;
    [SerializeField] private bool autoStart = true;

    private int _currentLineIndex = 0;
    private bool _isTyping = false;
    private bool _dialogueFinished = false;

    public bool IsDialogueFinished => _dialogueFinished;

    private Coroutine _typewriterCoroutine;
    private Coroutine _textEntranceCoroutine;

    private void Start()
    {
        if (tapToContinueIndicator != null)
            tapToContinueIndicator.SetActive(false);

        if (autoStart)
            StartDialogue();
    }

    private void Update()
    {
        if (_dialogueFinished) return;

        if (Input.GetMouseButtonDown(0) || (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began))
        {
            OnScreenTapped();
        }
    }

    public void StartDialogue()
    {
        _currentLineIndex = 0;
        _dialogueFinished = false;
        if (dialoguePanel != null) dialoguePanel.SetActive(true);
        ShowLine(0);
    }

    private void OnScreenTapped()
    {
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

        if (speakerNameText != null)
            speakerNameText.text = line.speakerName;

        UpdateCharacterVisuals(line);
        PlayLineAnimations(line);

        if (DialogueAudioManager.Instance != null)
            DialogueAudioManager.Instance.PlayVoiceIntro(line.speakerName);

        if (tapToContinueIndicator != null)
            tapToContinueIndicator.SetActive(false);

        if (_textEntranceCoroutine != null)
            StopCoroutine(_textEntranceCoroutine);

        _textEntranceCoroutine = StartCoroutine(PlayTextEntrance(line));
    }

    private void UpdateCharacterVisuals(DialogueLine line)
    {
        foreach (CharacterData c in characters)
        {
            if (c.characterAnimator == null) continue;
            bool isSpeaking = c.characterName == line.speakerName;
            c.characterAnimator.SetHighlight(isSpeaking);
            if (isSpeaking && line.characterSprite != null)
                c.characterAnimator.SetSprite(line.characterSprite);
        }
    }

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

    private IEnumerator PlayTextEntrance(DialogueLine line)
    {
        if (dialogueBodyText == null) yield break;

        // Reset text
        dialogueBodyText.text = line.dialogueText;
        dialogueBodyText.maxVisibleCharacters = 0;

        // Ensure CanvasGroup exists
        CanvasGroup cg = dialogueBodyText.GetComponent<CanvasGroup>()
            ?? dialogueBodyText.gameObject.AddComponent<CanvasGroup>();

        // Play entrance animation (simplified + safer)
        switch (line.textEntrance)
        {
            case TextEntrance.FadeIn:
                yield return StartCoroutine(FadeIn(cg));
                break;
            case TextEntrance.SlideFromLeft:
            case TextEntrance.SlideFromRight:
            case TextEntrance.PopScale:
            case TextEntrance.BounceIn:
                // You can expand these later if needed. For now keep simple fade as fallback
                yield return StartCoroutine(FadeIn(cg));
                break;
            default:
                cg.alpha = 1f;
                break;
        }

        // Start typewriter
        if (_typewriterCoroutine != null) StopCoroutine(_typewriterCoroutine);
        _typewriterCoroutine = StartCoroutine(TypewriterEffect(line));
    }

    private IEnumerator FadeIn(CanvasGroup cg)
    {
        float time = 0f;
        cg.alpha = 0f;
        while (time < 0.25f)
        {
            time += Time.deltaTime;
            cg.alpha = Mathf.Lerp(0f, 1f, time / 0.25f);
            yield return null;
        }
        cg.alpha = 1f;
    }

    private IEnumerator TypewriterEffect(DialogueLine line)
    {
        _isTyping = true;
        dialogueBodyText.maxVisibleCharacters = 0;

        int totalChars = dialogueBodyText.textInfo.characterCount;
        int charIndex = 0;

        while (charIndex < totalChars)
        {
            charIndex++;
            dialogueBodyText.maxVisibleCharacters = charIndex;

            if (DialogueAudioManager.Instance != null)
                DialogueAudioManager.Instance.PlayTypingSound(line.speakerName, charIndex);

            yield return new WaitForSeconds(typewriterSpeed);
        }

        _isTyping = false;
        if (tapToContinueIndicator != null)
            tapToContinueIndicator.SetActive(true);
    }

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
        if (tapToContinueIndicator != null) tapToContinueIndicator.SetActive(false);
        if (dialoguePanel != null) dialoguePanel.SetActive(false);

        if (!string.IsNullOrEmpty(nextSceneName))
        {
            if (SceneTransitionManager.Instance != null)
                SceneTransitionManager.Instance.NavigateTo(nextSceneName, useLoadingScreen);
            else
                UnityEngine.SceneManagement.SceneManager.LoadScene(nextSceneName);
        }
    }

    private DialogueCharacterAnimator GetCharacterAnimator(string name)
    {
        return characters.Find(c => c.characterName == name)?.characterAnimator;
    }

    public void SkipAllDialogue() => FinishDialogue();
}