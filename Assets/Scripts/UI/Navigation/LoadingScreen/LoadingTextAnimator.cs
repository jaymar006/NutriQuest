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

        loadingText.ForceMeshUpdate();
        TMP_TextInfo textInfo = loadingText.textInfo;

        // Collect visible character indices
        var visibleIndices = new System.Collections.Generic.List<int>();
        for (int i = 0; i < textInfo.characterCount; i++)
        {
            if (textInfo.characterInfo[i].isVisible)
                visibleIndices.Add(i);
        }

        if (visibleIndices.Count == 0) yield break;

        // Store original vertex positions ONCE per cycle
        // Key: materialIndex -> original vertices snapshot
        var originalVertices = new System.Collections.Generic.Dictionary<int, Vector3[]>();
        for (int i = 0; i < textInfo.meshInfo.Length; i++)
        {
            Vector3[] verts = textInfo.meshInfo[i].vertices;
            Vector3[] copy = new Vector3[verts.Length];
            verts.CopyTo(copy, 0);
            originalVertices[i] = copy;
        }

        // Animate each letter one after another (staggered start, NOT waiting for each to finish)
        // We track progress per-letter ourselves so we can bail cleanly
        int letterCount = visibleIndices.Count;
        float[] letterTimers = new float[letterCount];
        bool[] letterDone = new bool[letterCount];
        bool[] letterStarted = new bool[letterCount];
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
                letterStarted[nextToStart] = true;
                nextToStart++;
                staggerAccumulator -= letterSpacingDelay;
            }

            // Update each active letter
            for (int li = 0; li < letterCount; li++)
            {
                if (!letterStarted[li] || letterDone[li]) continue;

                int charIndex = visibleIndices[li];

                loadingText.ForceMeshUpdate();
                textInfo = loadingText.textInfo;

                if (charIndex >= textInfo.characterCount) { letterDone[li] = true; continue; }
                if (!textInfo.characterInfo[charIndex].isVisible) { letterDone[li] = true; continue; }

                int matIndex = textInfo.characterInfo[charIndex].materialReferenceIndex;
                int vtxIndex = textInfo.characterInfo[charIndex].vertexIndex;

                letterTimers[li] += Time.deltaTime * bounceSpeed;
                float t = letterTimers[li];

                if (t >= 1f)
                {
                    // Snap back to original position
                    Vector3[] orig = originalVertices[matIndex];
                    Vector3[] dest = textInfo.meshInfo[matIndex].vertices;
                    dest[vtxIndex + 0] = orig[vtxIndex + 0];
                    dest[vtxIndex + 1] = orig[vtxIndex + 1];
                    dest[vtxIndex + 2] = orig[vtxIndex + 2];
                    dest[vtxIndex + 3] = orig[vtxIndex + 3];
                    letterDone[li] = true;
                }
                else
                {
                    float offset = Mathf.Sin(t * Mathf.PI) * bounceHeight;
                    Vector3[] orig = originalVertices[matIndex];
                    Vector3[] dest = textInfo.meshInfo[matIndex].vertices;
                    dest[vtxIndex + 0] = orig[vtxIndex + 0] + Vector3.up * offset;
                    dest[vtxIndex + 1] = orig[vtxIndex + 1] + Vector3.up * offset;
                    dest[vtxIndex + 2] = orig[vtxIndex + 2] + Vector3.up * offset;
                    dest[vtxIndex + 3] = orig[vtxIndex + 3] + Vector3.up * offset;
                }
            }

            loadingText.UpdateVertexData(TMP_VertexDataUpdateFlags.Vertices);

            // Check if all letters are done this cycle
            anyCycleRunning = false;
            for (int li = 0; li < letterCount; li++)
            {
                if (letterStarted[li] && !letterDone[li])
                {
                    anyCycleRunning = true;
                    break;
                }
                // If not all started yet, keep running
                if (!letterStarted[li])
                {
                    anyCycleRunning = true;
                    break;
                }
            }

            yield return null;
        }

        // Small pause between bounce cycles
        yield return new WaitForSeconds(0.2f);
    }
}