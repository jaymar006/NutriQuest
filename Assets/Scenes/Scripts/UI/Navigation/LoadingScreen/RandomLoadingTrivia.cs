using UnityEngine;

public class RandomLoadingTrivia : MonoBehaviour
{
    [Header("Language Folders")]
    [SerializeField] private Transform englishTriviaParent;
    [SerializeField] private Transform filipinoTriviaParent;

    [SerializeField] private GameObject loadingObject;
    [Header("Trivia Settings")]
    [SerializeField] private float tapCooldown = 0.4f; // Prevent flicker on rapid taps

    private GameObject[] allTriviaObjects;   // every trivia object, both languages — used to hide all
    private GameObject[] currentTriviaObjects; // only the active language's set — used to pick from
    private int lastShownIndex = -1;
    private float lastTapTime = -999f;

    private void Awake()
    {
        if (englishTriviaParent == null || filipinoTriviaParent == null)
        {
            Debug.LogError("[RandomLoadingTrivia] English/Filipino trivia parent not assigned!");
            return;
        }

        allTriviaObjects = CombineChildren(englishTriviaParent, filipinoTriviaParent);
        BuildCurrentLanguageSet();
    }

    private void OnEnable()
    {
        LocalizationManager.OnLanguageChanged += HandleLanguageChanged;
        lastShownIndex = -1;
        ShowRandomTrivia();
    }

    private void OnDisable()
    {
        LocalizationManager.OnLanguageChanged -= HandleLanguageChanged;
    }

    private void HandleLanguageChanged()
    {
        BuildCurrentLanguageSet();
        lastShownIndex = -1;
        lastTapTime = -999f; // bypass cooldown so the swap isn't blocked
        ShowRandomTrivia();
    }

    private void BuildCurrentLanguageSet()
    {
        bool wantFilipino = LocalizationManager.Instance != null && LocalizationManager.Instance.IsFilipino;
        Transform activeParent = wantFilipino ? filipinoTriviaParent : englishTriviaParent;

        currentTriviaObjects = new GameObject[activeParent.childCount];
        for (int i = 0; i < activeParent.childCount; i++)
            currentTriviaObjects[i] = activeParent.GetChild(i).gameObject;
    }

    private GameObject[] CombineChildren(Transform a, Transform b)
    {
        var combined = new GameObject[a.childCount + b.childCount];
        int idx = 0;
        for (int i = 0; i < a.childCount; i++) combined[idx++] = a.GetChild(i).gameObject;
        for (int i = 0; i < b.childCount; i++) combined[idx++] = b.GetChild(i).gameObject;
        return combined;
    }

    public void ShowRandomTrivia()
    {
        if (currentTriviaObjects == null || currentTriviaObjects.Length == 0)
            return;

        if (Time.time - lastTapTime < tapCooldown)
            return;
        lastTapTime = Time.time;

        if (loadingObject != null)
            loadingObject.SetActive(true);

        // Hide ALL trivia (both languages)
        foreach (GameObject trivia in allTriviaObjects)
            trivia.SetActive(false);

        int randomIndex;
        if (currentTriviaObjects.Length == 1)
        {
            randomIndex = 0;
        }
        else
        {
            do { randomIndex = Random.Range(0, currentTriviaObjects.Length); }
            while (randomIndex == lastShownIndex);
        }

        lastShownIndex = randomIndex;
        currentTriviaObjects[randomIndex].SetActive(true);
    }
}