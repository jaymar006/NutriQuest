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

    private void OnEnable()
    {
        loadingComplete = false;

        if (tapToContinueText != null)
            tapToContinueText.SetActive(false);

        StartCoroutine(AnimateLoadingText());
    }

    public void SetLoadingComplete()
    {
        loadingComplete = true;

        if (tapToContinueText != null)
            tapToContinueText.SetActive(true);
    }

    private IEnumerator AnimateLoadingText()
    {
        while (!loadingComplete)
        {
            yield return BounceLetters();
        }
    }

    private IEnumerator BounceLetters()
    {
        loadingText.ForceMeshUpdate();
        TMP_TextInfo textInfo = loadingText.textInfo;

        for (int i = 0; i < textInfo.characterCount; i++)
        {
            if (!textInfo.characterInfo[i].isVisible)
                continue;

            StartCoroutine(BounceLetter(i));
            yield return new WaitForSeconds(letterSpacingDelay);
        }
    }

    private IEnumerator BounceLetter(int index)
    {
        loadingText.ForceMeshUpdate();
        TMP_TextInfo textInfo = loadingText.textInfo;

        if (index >= textInfo.characterCount)
            yield break;

        int materialIndex = textInfo.characterInfo[index].materialReferenceIndex;
        int vertexIndex = textInfo.characterInfo[index].vertexIndex;

        Vector3[] sourceVertices = textInfo.meshInfo[materialIndex].vertices;
        Vector3[] copiedVertices = new Vector3[sourceVertices.Length];
        sourceVertices.CopyTo(copiedVertices, 0);

        float time = 0f;

        while (time < 1f && !loadingComplete)
        {
            time += Time.deltaTime * bounceSpeed;
            float offset = Mathf.Sin(time * Mathf.PI) * bounceHeight;

            Vector3[] destinationVertices = textInfo.meshInfo[materialIndex].vertices;

            destinationVertices[vertexIndex + 0] = copiedVertices[vertexIndex + 0] + Vector3.up * offset;
            destinationVertices[vertexIndex + 1] = copiedVertices[vertexIndex + 1] + Vector3.up * offset;
            destinationVertices[vertexIndex + 2] = copiedVertices[vertexIndex + 2] + Vector3.up * offset;
            destinationVertices[vertexIndex + 3] = copiedVertices[vertexIndex + 3] + Vector3.up * offset;

            loadingText.UpdateVertexData(TMP_VertexDataUpdateFlags.Vertices);

            yield return null;
        }
    }
}