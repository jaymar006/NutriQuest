using UnityEngine;

public class RandomLoadingTrivia : MonoBehaviour
{
    [SerializeField] private Transform triviaParent;
    [SerializeField] private GameObject loadingObject;

    [Header("Trivia Settings")]
    [SerializeField] private float tapCooldown = 0.4f; // Prevent flicker on rapid taps

    private GameObject[] triviaObjects;
    private int lastShownIndex = -1;
    private float lastTapTime = -999f;

    private void Awake()
    {
        if (triviaParent == null)
        {
            Debug.LogError("[RandomLoadingTrivia] Trivia Parent is not assigned!");
            return;
        }

        int count = triviaParent.childCount;
        triviaObjects = new GameObject[count];
        for (int i = 0; i < count; i++)
            triviaObjects[i] = triviaParent.GetChild(i).gameObject;
    }

    private void OnEnable()
    {
        lastShownIndex = -1;
        ShowRandomTrivia();
    }

    public void ShowRandomTrivia()
    {
        if (triviaObjects == null || triviaObjects.Length == 0)
            return;

        // Enforce cooldown to prevent flicker
        if (Time.time - lastTapTime < tapCooldown)
            return;

        lastTapTime = Time.time;

        if (loadingObject != null)
            loadingObject.SetActive(true);

        // Hide all trivia
        foreach (GameObject trivia in triviaObjects)
            trivia.SetActive(false);

        // Pick a random index that isn't the same as last time
        int randomIndex;
        if (triviaObjects.Length == 1)
        {
            randomIndex = 0;
        }
        else
        {
            do { randomIndex = Random.Range(0, triviaObjects.Length); }
            while (randomIndex == lastShownIndex);
        }

        lastShownIndex = randomIndex;
        triviaObjects[randomIndex].SetActive(true);
    }
}