using System.Collections;
using UnityEngine;

/// <summary>
/// Shows the correct star object (1, 2 or 3 stars) based on the player's score.
/// Attach to the "Stars" object, then drag Star1, Star2 and Star3 into the slots.
/// </summary>
public class StarRatingDisplay : MonoBehaviour
{
    [Header("Star Objects (drag from Hierarchy)")]
    [SerializeField] private GameObject star1;      // the "1 star" image
    [SerializeField] private GameObject star2;      // the "2 stars" image
    [SerializeField] private GameObject star3;      // the "3 stars" image
    [Tooltip("Optional: shown when the player earns 0 stars")]
    [SerializeField] private GameObject star0;

    [Header("Score Needed (% correct answers)")]
    [Range(0, 100)][SerializeField] private int oneStarPercent = 40;
    [Range(0, 100)][SerializeField] private int twoStarPercent = 70;
    [Range(0, 100)][SerializeField] private int threeStarPercent = 90;

    [Header("Options")]
    [Tooltip("Automatically read the score from ResultData when the scene starts")]
    [SerializeField] private bool autoShowOnStart = true;
    [Tooltip("Small pop-in animation when the stars appear")]
    [SerializeField] private bool playPopAnimation = true;
    [SerializeField] private float popDuration = 0.35f;

    [Header("Testing (Inspector only)")]
    [SerializeField] private int testCorrect = 5;
    [SerializeField] private int testTotal = 10;

    /// <summary>The number of stars the player earned (0-3).</summary>
    public int CurrentStars { get; private set; }

    private void Start()
    {
        if (autoShowOnStart)
            ShowFromResultData();
    }

    /// <summary>Reads the saved score from ResultData and shows the stars.</summary>
    public void ShowFromResultData()
    {
        ShowStars(ResultData.GetCorrect(), ResultData.GetTotal());
    }

    /// <summary>Calculates the stars from a score and shows them.</summary>
    public void ShowStars(int correct, int total)
    {
        SetStars(CalculateStars(correct, total));
    }

    /// <summary>Returns 0-3 stars for the given score.</summary>
    public int CalculateStars(int correct, int total)
    {
        if (total <= 0) return 0;

        float percent = (float)correct / total * 100f;

        if (percent >= threeStarPercent) return 3;
        if (percent >= twoStarPercent) return 2;
        if (percent >= oneStarPercent) return 1;
        return 0;
    }

    /// <summary>Shows a specific star count (0-3) directly.</summary>
    public void SetStars(int stars)
    {
        CurrentStars = Mathf.Clamp(stars, 0, 3);

        SetActive(star0, CurrentStars == 0);
        SetActive(star1, CurrentStars == 1);
        SetActive(star2, CurrentStars == 2);
        SetActive(star3, CurrentStars == 3);

        if (playPopAnimation && Application.isPlaying)
        {
            GameObject active = GetActiveStar();
            if (active != null)
            {
                StopAllCoroutines();
                StartCoroutine(PopAnimation(active.transform));
            }
        }
    }

    private GameObject GetActiveStar()
    {
        switch (CurrentStars)
        {
            case 0: return star0;
            case 1: return star1;
            case 2: return star2;
            default: return star3;
        }
    }

    private void SetActive(GameObject obj, bool value)
    {
        if (obj != null) obj.SetActive(value);
    }

    private IEnumerator PopAnimation(Transform target)
    {
        Vector3 finalScale = Vector3.one;
        float time = 0f;
        target.localScale = Vector3.zero;

        while (time < popDuration)
        {
            time += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(time / popDuration);

            // Overshoot slightly for a bouncy feel
            float s = 1f + 1.70158f * Mathf.Pow(t - 1f, 3) + 1.70158f * Mathf.Pow(t - 1f, 2);
            target.localScale = finalScale * s;
            yield return null;
        }

        target.localScale = finalScale;
    }

    // Right-click the component header in the Inspector to test without playing the whole game
    [ContextMenu("Test With Inspector Values")]
    private void TestInInspector()
    {
        ShowStars(testCorrect, testTotal);
    }
}