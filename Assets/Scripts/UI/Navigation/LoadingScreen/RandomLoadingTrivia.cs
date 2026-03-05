using UnityEngine;

public class RandomLoadingTrivia : MonoBehaviour
{
    [SerializeField] private Transform triviaParent;
    [SerializeField] private GameObject loadingObject;

    private GameObject[] triviaObjects;

    private void Awake()
    {
        if (triviaParent == null)
        {
            Debug.LogError("Trivia Parent is not assigned!");
            return;
        }

        int count = triviaParent.childCount;
        triviaObjects = new GameObject[count];

        for (int i = 0; i < count; i++)
        {
            triviaObjects[i] = triviaParent.GetChild(i).gameObject;
        }
    }

    private void OnEnable()
    {
        ShowRandomTrivia();
    }

    public void ShowRandomTrivia()
    {
        if (triviaObjects == null || triviaObjects.Length == 0)
            return;

        if (loadingObject != null)
            loadingObject.SetActive(true);

        foreach (GameObject trivia in triviaObjects)
            trivia.SetActive(false);

        int randomIndex = Random.Range(0, triviaObjects.Length);
        triviaObjects[randomIndex].SetActive(true);
    }
}