using System;
using System.Collections;
using UnityEngine;

public class SquishSquashManager : MonoBehaviour
{
    [Header("Squash and Stretch Core")]
    [SerializeField] private Transform transformToAffect;
    [SerializeField] private SquashStretchAxis axisToAffect = SquashStretchAxis.Y;
    [SerializeField, Range(0, 1f)] private float animationDuration = 0.25f;
    [SerializeField] private bool canBeOverwritten;
    [SerializeField] private bool playOnStart;
    [SerializeField] private bool playsEveryTime = true;
    [SerializeField, Range(0, 100f)] private float chanceToPlay = 100f;

    [Flags]
    public enum SquashStretchAxis
    {
        None = 0,
        X = 1,
        Y = 2,
        Z = 4
    }

    [Header("Animation Settings")]
    [SerializeField] private float initialScale = 1f;
    [SerializeField] private float maximumScale = 1.3f;
    [SerializeField] private bool resetToInitialScaleAfterAnimation = true;
    [SerializeField] private bool reverseAnimationCurveAfterPlaying;

    private bool _isReversed;

    [SerializeField]
    private AnimationCurve squashAndStretchCurve = new AnimationCurve(
        new Keyframe(0f, 0f),
        new Keyframe(0.25f, 1f),
        new Keyframe(1f, 0f)
    );

    [Header("Looping Settings")]
    [SerializeField] private bool looping;
    [SerializeField] private float loopingDelay = 0.5f;

    [Header("Shake Animation")]
    [Tooltip("Enable or disable the shake animation.")]
    [SerializeField] private bool enableShake = false;
    [Tooltip("Play shake on Start if enabled.")]
    [SerializeField] private bool shakeOnStart = false;
    [Tooltip("How far the object moves during shake.")]
    [SerializeField] private float shakeStrength = 10f;
    [Tooltip("How long the shake lasts.")]
    [SerializeField, Range(0f, 2f)] private float shakeDuration = 0.3f;
    [Tooltip("How many shakes per second.")]
    [SerializeField] private float shakeFrequency = 20f;
    [Tooltip("If true, shake loops until stopped manually.")]
    [SerializeField] private bool shakeLooping = false;
    [Tooltip("Delay between shake loops.")]
    [SerializeField] private float shakeLoopDelay = 0.5f;
    [Tooltip("Direction the shake moves.")]
    [SerializeField] private ShakeDirection shakeDirection = ShakeDirection.Horizontal;
    [Tooltip("If true, shake fades out smoothly toward the end.")]
    [SerializeField] private bool shakeFadeOut = true;

    public enum ShakeDirection
    {
        Horizontal,
        Vertical,
        Both,
        Diagonal_UpRight,
        Diagonal_UpLeft
    }

    private Coroutine _squashAndStretchCoroutine;
    private Coroutine _shakeCoroutine;
    private WaitForSeconds _loopingDelayWaitForSeconds;
    private WaitForSeconds _shakeLoopDelayWaitForSeconds;
    private Vector3 _initialScaleVector;
    private Vector3 _initialPositionVector;

    private bool affectX => (axisToAffect & SquashStretchAxis.X) != 0;
    private bool affectY => (axisToAffect & SquashStretchAxis.Y) != 0;
    private bool affectZ => (axisToAffect & SquashStretchAxis.Z) != 0;

    private static event Action _squashAndStretchAllObjectsLikeThis;
    private static event Action _shakeAllObjectsLikeThis;

    private void Awake()
    {
        if (transformToAffect == null)
            transformToAffect = transform;

        _initialScaleVector = transformToAffect.localScale;
        _loopingDelayWaitForSeconds = new WaitForSeconds(loopingDelay);
        _shakeLoopDelayWaitForSeconds = new WaitForSeconds(shakeLoopDelay);
    }

    private void Start()
    {
        // Wait one frame for layout to fully settle before capturing position //
        StartCoroutine(CaptureInitialPosition());
    }

    // Capture position after layout fully settles //
    private IEnumerator CaptureInitialPosition()
    {
        yield return null;
        yield return null;

        _initialPositionVector = transformToAffect.localPosition;

        if (playOnStart)
            CheckForAndStartCoroutine();

        if (enableShake && shakeOnStart)
            PlayShake();
    }

    // Static callers //
    public static void SquashAndStretchAllObjectsLikeThis()
    {
        _squashAndStretchAllObjectsLikeThis?.Invoke();
    }

    public static void ShakeAllObjectsLikeThis()
    {
        _shakeAllObjectsLikeThis?.Invoke();
    }

    private void OnEnable()
    {
        _squashAndStretchAllObjectsLikeThis += PlaySquashAndStretch;
        _shakeAllObjectsLikeThis += PlayShake;
    }

    private void OnDisable()
    {
        if (_squashAndStretchCoroutine != null)
            StopCoroutine(_squashAndStretchCoroutine);

        if (_shakeCoroutine != null)
        {
            StopCoroutine(_shakeCoroutine);
            transformToAffect.localPosition = _initialPositionVector;
        }

        _squashAndStretchAllObjectsLikeThis -= PlaySquashAndStretch;
        _shakeAllObjectsLikeThis -= PlayShake;
    }

    // Squash and Stretch //
    public void PlaySquashAndStretch()
    {
        if (looping && !canBeOverwritten)
            return;

        CheckForAndStartCoroutine();
    }

    private void CheckForAndStartCoroutine()
    {
        if (axisToAffect == SquashStretchAxis.None)
        {
            Debug.Log("Axis to affect is set to None.", gameObject);
            return;
        }

        if (_squashAndStretchCoroutine != null)
        {
            StopCoroutine(_squashAndStretchCoroutine);
            if (playsEveryTime && resetToInitialScaleAfterAnimation)
                transformToAffect.localScale = _initialScaleVector;
        }

        _squashAndStretchCoroutine = StartCoroutine(SquashAndStretchEffect());
    }

    private IEnumerator SquashAndStretchEffect()
    {
        do
        {
            if (!playsEveryTime)
            {
                float random = UnityEngine.Random.Range(0, 100f);
                if (random > chanceToPlay)
                {
                    yield return null;
                    continue;
                }
            }

            if (reverseAnimationCurveAfterPlaying)
                _isReversed = !_isReversed;

            float elapsedTime = 0;
            Vector3 originalScale = _initialScaleVector;
            Vector3 modifiedScale = originalScale;

            while (elapsedTime < animationDuration)
            {
                elapsedTime += Time.deltaTime;

                float curvePosition = _isReversed
                    ? 1 - (elapsedTime / animationDuration)
                    : elapsedTime / animationDuration;

                float curveValue = squashAndStretchCurve.Evaluate(curvePosition);
                float remappedValue = initialScale + (curveValue * (maximumScale - initialScale));

                float minimumThreshold = 0.0001f;
                if (Mathf.Abs(remappedValue) < minimumThreshold)
                    remappedValue = minimumThreshold;

                modifiedScale.x = affectX
                    ? originalScale.x * remappedValue
                    : originalScale.x / remappedValue;

                modifiedScale.y = affectY
                    ? originalScale.y * remappedValue
                    : originalScale.y / remappedValue;

                modifiedScale.z = affectZ
                    ? originalScale.z * remappedValue
                    : originalScale.z / remappedValue;

                transformToAffect.localScale = modifiedScale;

                yield return null;
            }

            if (resetToInitialScaleAfterAnimation)
                transformToAffect.localScale = originalScale;

            if (looping)
                yield return _loopingDelayWaitForSeconds;

        } while (looping);
    }

    // Shake Animation //
    public void PlayShake()
    {
        if (!enableShake) return;

        if (_shakeCoroutine != null)
        {
            StopCoroutine(_shakeCoroutine);
            transformToAffect.localPosition = _initialPositionVector;
        }

        _shakeCoroutine = StartCoroutine(ShakeEffect());
    }

    public void StopShake()
    {
        if (_shakeCoroutine != null)
        {
            StopCoroutine(_shakeCoroutine);
            _shakeCoroutine = null;
        }

        transformToAffect.localPosition = _initialPositionVector;
    }

    // Get the shake direction vector based on selected direction //
    private Vector3 GetShakeDirectionVector(float value)
    {
        switch (shakeDirection)
        {
            case ShakeDirection.Horizontal:
                return new Vector3(value, 0f, 0f);

            case ShakeDirection.Vertical:
                return new Vector3(0f, value, 0f);

            case ShakeDirection.Both:
                return new Vector3(value, value, 0f);

            case ShakeDirection.Diagonal_UpRight:
                return new Vector3(value, value * 0.5f, 0f);

            case ShakeDirection.Diagonal_UpLeft:
                return new Vector3(-value, value * 0.5f, 0f);

            default:
                return new Vector3(value, 0f, 0f);
        }
    }

    private IEnumerator ShakeEffect()
    {
        do
        {
            float elapsedTime = 0f;

            while (elapsedTime < shakeDuration)
            {
                elapsedTime += Time.deltaTime;

                // Fade out strength toward end if enabled //
                float fadeMultiplier = shakeFadeOut
                    ? 1f - (elapsedTime / shakeDuration)
                    : 1f;

                float sineValue = Mathf.Sin(elapsedTime * shakeFrequency)
                    * shakeStrength
                    * fadeMultiplier;

                transformToAffect.localPosition = _initialPositionVector
                    + GetShakeDirectionVector(sineValue);

                yield return null;
            }

            // Snap back to original position //
            transformToAffect.localPosition = _initialPositionVector;

            if (shakeLooping)
                yield return _shakeLoopDelayWaitForSeconds;

        } while (shakeLooping);

        _shakeCoroutine = null;
    }

    public void SetLooping(bool shouldLoop)
    {
        looping = shouldLoop;
    }

    public void SetShakeLooping(bool shouldLoop)
    {
        shakeLooping = shouldLoop;
    }

    public void SetShakeEnabled(bool enabled)
    {
        enableShake = enabled;
    }

    // Enable or disable squash and stretch //
    public void SetSquashStretchEnabled(bool enabled)
    {
        playOnStart = enabled;

        if (!enabled)
        {
            if (_squashAndStretchCoroutine != null)
            {
                StopCoroutine(_squashAndStretchCoroutine);
                _squashAndStretchCoroutine = null;
            }

            transformToAffect.localScale = _initialScaleVector;
        }
    }

    // Toggle squash and stretch on/off //
    public void ToggleSquashStretch()
    {
        SetSquashStretchEnabled(_squashAndStretchCoroutine == null);
    }

    // Toggle looping on/off and start/stop accordingly //
    public void ToggleSquashStretchLooping()
    {
        looping = !looping;

        if (looping)
            CheckForAndStartCoroutine();
        else
        {
            if (_squashAndStretchCoroutine != null)
            {
                StopCoroutine(_squashAndStretchCoroutine);
                _squashAndStretchCoroutine = null;
            }

            transformToAffect.localScale = _initialScaleVector;
        }
    }
}