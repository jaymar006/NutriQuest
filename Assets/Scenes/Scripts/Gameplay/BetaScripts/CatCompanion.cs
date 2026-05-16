using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using System.Text;

public class CatCompanion : MonoBehaviour
{
    public static CatCompanion Instance { get; private set; }

    [Header("Cat Sprite")]
    [SerializeField] private Image catImage;
    [SerializeField] private Sprite idleSprite;
    [SerializeField] private Sprite correctSprite;
    [SerializeField] private Sprite wrongSprite;
    [SerializeField] private Sprite hintSprite;
    [SerializeField] private Sprite completionSprite;

    [Header("Speech Bubble")]
    [SerializeField] private GameObject speechBubble;
    [SerializeField] private TMP_Text speechText;

    [Header("Idle Lines")]
    [SerializeField] private List<string> idleLines = new List<string>();

    [Header("Correct Answer Lines")]
    [SerializeField] private List<string> correctLines = new List<string>();

    [Header("Wrong Answer Lines")]
    [SerializeField] private List<string> wrongLines = new List<string>();

    [Header("Hint Lines")]
    [SerializeField] private List<string> hintLines = new List<string>();

    [Header("Completion Lines")]
    [SerializeField] private List<string> completionLines = new List<string>();

    [Header("Timing")]
    [SerializeField] private float reactionDelay = 0.5f;
    [SerializeField] private float typewriterSpeed = 0.04f;

    private Coroutine typewriterCoroutine;
    private Coroutine reactRoutineCoroutine;

    private int lastIdleIndex = -1;
    private int lastCorrectIndex = -1;
    private int lastWrongIndex = -1;
    private int lastHintIndex = -1;
    private int lastCompletionIndex = -1;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        ShowIdle();
    }

    public void ShowIdle()
    {
        if (reactRoutineCoroutine != null)
        {
            StopCoroutine(reactRoutineCoroutine);
            reactRoutineCoroutine = null;
        }

        SetSprite(idleSprite);

        string line = GetRandomLine(idleLines, ref lastIdleIndex);
        if (!string.IsNullOrEmpty(line))
            ShowSpeech(line);
        else
            Debug.LogWarning("[CatCompanion] Idle Lines list is empty!");
    }

    public void ReactToAnswer(bool isCorrect)
    {
        reactRoutineCoroutine = StartCoroutine(ReactRoutine(isCorrect));
    }

    public void ShowHint()
    {
        if (reactRoutineCoroutine != null)
        {
            StopCoroutine(reactRoutineCoroutine);
            reactRoutineCoroutine = null;
        }

        SetSprite(hintSprite);

        string line = GetRandomLine(hintLines, ref lastHintIndex);
        if (!string.IsNullOrEmpty(line))
            ShowSpeech(line);
        else
            Debug.LogWarning("[CatCompanion] Hint Lines list is empty!");
    }

    public void ShowCompletion()
    {
        if (reactRoutineCoroutine != null)
        {
            StopCoroutine(reactRoutineCoroutine);
            reactRoutineCoroutine = null;
        }

        if (typewriterCoroutine != null)
        {
            StopCoroutine(typewriterCoroutine);
            typewriterCoroutine = null;
        }

        SetSprite(completionSprite != null ? completionSprite : idleSprite);

        string line = GetRandomLine(completionLines, ref lastCompletionIndex);
        if (!string.IsNullOrEmpty(line))
            ShowSpeech(line);
        else
            Debug.LogWarning("[CatCompanion] Completion Lines list is empty! Add lines in Inspector.");
    }

    private IEnumerator ReactRoutine(bool isCorrect)
    {
        yield return new WaitForSeconds(reactionDelay);

        if (isCorrect)
        {
            SetSprite(correctSprite);
            string line = GetRandomLine(correctLines, ref lastCorrectIndex);
            if (!string.IsNullOrEmpty(line))
                ShowSpeech(line);
            else
                Debug.LogWarning("[CatCompanion] Correct Lines list is empty!");
        }
        else
        {
            SetSprite(wrongSprite);
            string line = GetRandomLine(wrongLines, ref lastWrongIndex);
            if (!string.IsNullOrEmpty(line))
                ShowSpeech(line);
            else
                Debug.LogWarning("[CatCompanion] Wrong Lines list is empty!");
        }
    }

    private void SetSprite(Sprite sprite)
    {
        if (catImage != null && sprite != null)
            catImage.sprite = sprite;
    }

    private void ShowSpeech(string line)
    {
        if (speechBubble != null)
            speechBubble.SetActive(true);

        if (typewriterCoroutine != null)
            StopCoroutine(typewriterCoroutine);

        if (speechText != null)
            typewriterCoroutine = StartCoroutine(TypewriterEffect(line));
    }

    // FIX: Use maxVisibleCharacters instead of string concatenation.
    // The old approach rebuilt the string every frame causing memory allocations
    // and garbage collection spikes which caused the lag.
    private IEnumerator TypewriterEffect(string line)
    {
        speechText.text = line;
        speechText.maxVisibleCharacters = 0;
        speechText.ForceMeshUpdate();

        int totalChars = speechText.textInfo.characterCount;
        int visibleCount = 0;

        WaitForSeconds wait = new WaitForSeconds(typewriterSpeed);

        while (visibleCount <= totalChars)
        {
            speechText.maxVisibleCharacters = visibleCount;
            visibleCount++;
            yield return wait;
        }
    }

    private string GetRandomLine(List<string> lines, ref int lastIndex)
    {
        if (lines == null || lines.Count == 0) return "";
        if (lines.Count == 1) return lines[0];

        int index;
        int attempts = 0;
        int maxAttempts = lines.Count * 2;

        do
        {
            index = Random.Range(0, lines.Count);
            attempts++;
            if (attempts >= maxAttempts) break;
        }
        while (index == lastIndex);

        lastIndex = index;
        return lines[index];
    }
}