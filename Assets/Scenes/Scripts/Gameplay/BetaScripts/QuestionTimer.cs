using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class QuestionTimer : MonoBehaviour
{
    public static QuestionTimer Instance { get; private set; }

    [Header("Timer Settings")]
    [Tooltip("Time in seconds per question.")]
    [SerializeField] private float timePerQuestion = 15f;

    [Header("UI")]
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private Image timerFillImage;

    [Header("Color Settings")]
    [Tooltip("Color when time is above 50%.")]
    [SerializeField] private Color normalColor = Color.green;
    [Tooltip("Color when time is between 25% and 50%.")]
    [SerializeField] private Color warningColor = Color.yellow;
    [Tooltip("Color when time is below 25%.")]
    [SerializeField] private Color dangerColor = Color.red;

    [Header("Low Time Settings")]
    [Tooltip("Play pulse animation when below this many seconds.")]
    [SerializeField] private float pulseThreshold = 5f;
    [Tooltip("Speed of the pulse animation.")]
    [SerializeField] private float pulseSpeed = 8f;
    [Tooltip("Scale of the pulse animation.")]
    [SerializeField] private float pulseScale = 1.2f;

    private float _currentTime = 0f;
    private bool _isRunning = false;
    private bool _isExpired = false;
    private Coroutine _timerCoroutine;
    private Coroutine _pulseCoroutine;
    private Vector3 _originalScale;

    private void Awake()
    {
        Instance = this;

        if (timerText != null)
            _originalScale = timerText.transform.localScale;
    }

    // Start timer for a new question //
    public void StartTimer()
    {
        _isExpired = false;
        _currentTime = timePerQuestion;

        if (_timerCoroutine != null)
            StopCoroutine(_timerCoroutine);

        if (_pulseCoroutine != null)
        {
            StopCoroutine(_pulseCoroutine);
            if (timerText != null)
                timerText.transform.localScale = _originalScale;
        }

        _timerCoroutine = StartCoroutine(TimerLoop());
    }

    // Stop timer externally when answer is selected //
    public void StopTimer()
    {
        _isRunning = false;

        if (_timerCoroutine != null)
        {
            StopCoroutine(_timerCoroutine);
            _timerCoroutine = null;
        }

        if (_pulseCoroutine != null)
        {
            StopCoroutine(_pulseCoroutine);
            _pulseCoroutine = null;

            if (timerText != null)
                timerText.transform.localScale = _originalScale;
        }
    }

    // Pause and resume for cutscenes or popups //
    public void PauseTimer() => _isRunning = false;
    public void ResumeTimer() => _isRunning = true;

    private IEnumerator TimerLoop()
    {
        _isRunning = true;

        while (_currentTime > 0f)
        {
            if (_isRunning)
            {
                _currentTime -= Time.deltaTime;
                _currentTime = Mathf.Max(_currentTime, 0f);

                UpdateUI();

                // Start pulse when low on time //
                if (_currentTime <= pulseThreshold && _pulseCoroutine == null)
                    _pulseCoroutine = StartCoroutine(PulseEffect());
            }

            yield return null;
        }

        // Time is up //
        if (!_isExpired)
        {
            _isExpired = true;
            _isRunning = false;
            OnTimerExpired();
        }
    }

    private void UpdateUI()
    {
        // Update text //
        if (timerText != null)
        {
            int seconds = Mathf.CeilToInt(_currentTime);
            timerText.text = seconds.ToString();

            // Update color based on remaining time //
            float percent = _currentTime / timePerQuestion;

            if (percent > 0.5f)
                timerText.color = normalColor;
            else if (percent > 0.25f)
                timerText.color = warningColor;
            else
                timerText.color = dangerColor;
        }

        // Update fill image if assigned //
        if (timerFillImage != null)
        {
            float percent = _currentTime / timePerQuestion;
            timerFillImage.fillAmount = percent;

            if (percent > 0.5f)
                timerFillImage.color = normalColor;
            else if (percent > 0.25f)
                timerFillImage.color = warningColor;
            else
                timerFillImage.color = dangerColor;
        }
    }

    // Pulse animation when time is low //
    private IEnumerator PulseEffect()
    {
        if (timerText == null) yield break;

        while (_currentTime > 0f && _isRunning)
        {
            float scale = 1f + (Mathf.Sin(Time.time * pulseSpeed) * (pulseScale - 1f));
            timerText.transform.localScale = _originalScale * scale;
            yield return null;
        }

        timerText.transform.localScale = _originalScale;
        _pulseCoroutine = null;
    }

    // Called when timer hits zero - auto submit wrong answer //
    private void OnTimerExpired()
    {
        Debug.Log("[QuestionTimer] Time is up! Auto submitting wrong answer.");

        StopTimer();

        // Lock answers so player cannot click //
        if (AnswerBTNFunction2.Instance != null)
        {
            // Force wrong answer by locking and registering wrong //
            if (!AnswerBTNFunction2.Instance.IsAnswerLocked)
            {
                // Play wrong SFX //
                if (QuizSFXManager.Instance != null)
                    QuizSFXManager.Instance.PlayWrong();

                // React as wrong answer //
                if (CatCompanion.Instance != null)
                    CatCompanion.Instance.ReactToAnswer(false);

                // Register as wrong answer in score //
                if (ScoreManager.Instance != null)
                    ScoreManager.Instance.RegisterAnswer(false);

                // Lock buttons so player cannot click //
                AnswerBTNFunction2.Instance.LockAllButtons();

                // Proceed to next question //
                if (QuestionGeneratorBeta.Instance != null)
                    QuestionGeneratorBeta.Instance.OnAnswerSelected();
            }
        }
    }
}