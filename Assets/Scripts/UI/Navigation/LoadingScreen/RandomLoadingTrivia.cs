using UnityEngine;

public class RandomLoadingTrivia : MonoBehaviour
{
    [Header("Parent Object That Holds All Trivia")]
    [SerializeField] private GameObject triviaParent;

    [Header("Loading Screen Object")]
    [SerializeField] private GameObject loadingObject;

    private GameObject[] triviaObjects;

    private void Awake()
    {
        InitializeTrivia();
    }

    private void OnEnable()
    {
        ShowRandomTrivia();
    }

    private void InitializeTrivia()
    {
        if (triviaParent == null)
        {
            Debug.LogError("Trivia Parent is not assigned!");
            return;
        }

        int count = triviaParent.transform.childCount;
        triviaObjects = new GameObject[count];

        for (int i = 0; i < count; i++)
        {
            triviaObjects[i] = triviaParent.transform.GetChild(i).gameObject;
        }
    }

    public void ShowRandomTrivia()
    {
        if (triviaObjects == null || triviaObjects.Length == 0)
            return;

        if (loadingObject != null)
            loadingObject.SetActive(true);

        // Turn off all trivia
        foreach (GameObject trivia in triviaObjects)
        {
            trivia.SetActive(false);
        }

        // Activate random trivia
        int randomIndex = Random.Range(0, triviaObjects.Length);
        triviaObjects[randomIndex].SetActive(true);
    }
}