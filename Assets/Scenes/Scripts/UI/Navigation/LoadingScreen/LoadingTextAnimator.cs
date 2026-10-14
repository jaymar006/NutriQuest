using UnityEngine;
using TMPro;
using System.Collections;

public class LoadingTextAnimator : MonoBehaviour
{
    [Header("Text References")]
    [SerializeField] private TMP_Text loadingText;
    [SerializeField] private GameObject tapToContinueText;

    [Header("Animation Settings")]
    [SerializeField] private float bounceHeight = 8f;
    [SerializeField] private float bounceSpeed = 2f;
    [SerializeField] private float letterSpacingDelay = 0.05f;

    private bool loadingComplete = false;
    private Coroutine animationCoroutine;

    // Reused across bounce cycles to avoid per-cycle GC (List/Dictionary/array allocs) //
    private readonly System.Collections.Generic.List<int> _visibleIndices = new System.Collections.Generic.List<int>();
    private readonly System.Collections.Generic.Dictionary<int, Vector3[]> _originalVertices = new System.Collections.Generic.Dictionary<int, Vector3[]>();
    private float[] _letterTimers = System.Array.Empty<float>();
    private bool[] _letterDone = System.Array.Empty<bool>();
    private bool[] _letterStarted = System.Array.Empty<bool>();
    private static readonly WaitForSeconds _cyclePause = new WaitForSeconds(0.2f);

    private void OnEnable()
    {
        loadingComplete = false;

        if (tapToContinueText != null)
            tapToContinueText.SetActive(false);

        if (loadingText != null)
        {
            loadingText.gameObject.SetActive(true);

            if (animationCoroutine != null)
                StopCoroutine(animationCoroutine);

            animationCoroutine = StartCoroutine(AnimateLoadingText());
        }
        else
        {
            Debug.LogError("[LoadingTextAnimator] loadingText TMP_Text is not assigned in the Inspector!");
        }
    }

    public void SetLoadingComplete()
    {
        Debug.Log("[LoadingTextAnimator] SetLoadingComplete called.");
        loadingComplete = true;
    }

    private IEnumerator AnimateLoadingText()
    {
        Debug.Log("[LoadingTextAnimator] Animation loop started.");

        // Keep running full bounce cycles until loading is done
        while (!loadingComplete)
        {
            yield return RunBounceCycle();
        }

        Debug.Log("[LoadingTextAnimator] Exited bounce loop. Hiding loading text.");

        // Hide loading text cleanly
        if (loadingText != null)
            loadingText.gameObject.SetActive(false);

        yield return new WaitForSeconds(0.15f);

        // Show tap to continue
        if (tapToContinueText != null)
            tapToContinueText.SetActive(true);

        Debug.Log("[LoadingTextAnimator] Tap to Continue is now visible.");
    }

    // Runs ONE full bounce cycle across all letters sequentially, then waits for the wave to finish
    private IEnumerator RunBounceCycle()
    {
        if (loadingText == null) yield break;

        // Single mesh sync per cycle; cached textInfo reused for the whole wave //
        loadingText.ForceMeshUpdate();
        TMP_TextInfo textInfo = loadingText.textInfo;

        // Collect visible character indices (reuse list, no alloc) //
        _visibleIndices.Clear();
        for (int i = 0; i < textInfo.characterCount; i++)
        {
            if (textInfo.characterInfo[i].isVisible)
                _visibleIndices.Add(i);
        }

        if (_visibleIndices.Count == 0) yield break;

        // Store original vertex positions ONCE per cycle (reuse dictionary + arrays) //
        // Key: materialIndex -> original vertices snapshot
        for (int i = 0; i < textInfo.meshInfo.Length; i++)
        {
            Vector3[] verts = textInfo.meshInfo[i].vertices;
            if (!_originalVertices.TryGetValue(i, out Vector3[] copy) || copy.Length != verts.Length)
            {
                copy = new Vector3[verts.Length];
                _originalVertices[i] = copy;
            }
            verts.CopyTo(copy, 0);
        }

        // Animate each letter one after another (staggered start, NOT waiting for each to finish)
        // We track progress per-letter ourselves so we can bail cleanly
        // Reuse timer arrays; grow only when the letter count grows //
        int letterCount = _visibleIndices.Count;
        EnsureLetterCapacity(letterCount);
        System.Array.Clear(_letterTimers, 0, letterCount);
        System.Array.Clear(_letterDone, 0, letterCount);
        System.Array.Clear(_letterStarted, 0, letterCount);
        float staggerAccumulator = 0f;
        int nextToStart = 0;

        bool anyCycleRunning = true;

        while (anyCycleRunning)
        {
            if (loadingComplete) yield break;

            // Tick stagger to decide when to start next letter
            staggerAccumulator += Time.deltaTime;
            while (nextToStart < letterCount && staggerAccumulator >= letterSpacingDelay)
            {
                _letterStarted[nextToStart] = true;
                nextToStart++;
                staggerAccumulator -= letterSpacingDelay;
            }

            bool anyMoved = false;

            // Update each active letter (no ForceMeshUpdate here; textInfo cached above) //
            for (int li = 0; li < letterCount; li++)
            {
                if (!_letterStarted[li] || _letterDone[li]) continue;

                int charIndex = _visibleIndices[li];

                if (charIndex >= textInfo.characterCount) { _letterDone[li] = true; continue; }
                if (!textInfo.characterInfo[charIndex].isVisible) { _letterDone[li] = true; continue; }

                int matIndex = textInfo.characterInfo[charIndex].materialReferenceIndex;
                int vtxIndex = textInfo.characterInfo[charIndex].vertexIndex;

                _letterTimers[li] += Time.deltaTime * bounceSpeed;
                float t = _letterTimers[li];

                if (t >= 1f)
                {
                    // Snap back to original position
                    Vector3[] orig = _originalVertices[matIndex];
                    Vector3[] dest = textInfo.meshInfo[matIndex].vertices;
                    dest[vtxIndex + 0] = orig[vtxIndex + 0];
                    dest[vtxIndex + 1] = orig[vtxIndex + 1];
                    dest[vtxIndex + 2] = orig[vtxIndex + 2];
                    dest[vtxIndex + 3] = orig[vtxIndex + 3];
                    _letterDone[li] = true;
                    anyMoved = true;
                }
                else
                {
                    float offset = Mathf.Sin(t * Mathf.PI) * bounceHeight;
                    Vector3[] orig = _originalVertices[matIndex];
                    Vector3[] dest = textInfo.meshInfo[matIndex].vertices;
                    dest[vtxIndex + 0] = orig[vtxIndex + 0] + Vector3.up * offset;
                    dest[vtxIndex + 1] = orig[vtxIndex + 1] + Vector3.up * offset;
                    dest[vtxIndex + 2] = orig[vtxIndex + 2] + Vector3.up * offset;
                    dest[vtxIndex + 3] = orig[vtxIndex + 3] + Vector3.up * offset;
                    anyMoved = true;
                }
            }

            // Only push vertex data when at least one letter actually moved //
            if (anyMoved)
                loadingText.UpdateVertexData(TMP_VertexDataUpdateFlags.Vertices);

            // Check if all letters are done this cycle
            anyCycleRunning = false;
            for (int li = 0; li < letterCount; li++)
            {
                if (_letterStarted[li] && !_letterDone[li])
                {
                    anyCycleRunning = true;
                    break;
                }
                // If not all started yet, keep running
                if (!_letterStarted[li])
                {
                    anyCycleRunning = true;
                    break;
                }
            }

            yield return null;
        }

        // Small pause between bounce cycles (cached yield, no alloc) //
        yield return _cyclePause;
    }

    private void EnsureLetterCapacity(int letterCount)
    {
        if (_letterTimers.Length < letterCount)
        {
            _letterTimers = new float[letterCount];
            _letterDone = new bool[letterCount];
            _letterStarted = new bool[letterCount];
        }
    }
}