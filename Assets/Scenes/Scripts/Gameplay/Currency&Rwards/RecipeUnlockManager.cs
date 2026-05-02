using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class RecipeUnlockManager : MonoBehaviour
{
    public static RecipeUnlockManager Instance { get; private set; }

    [System.Serializable]
    public class RecipeEntry
    {
        [Tooltip("Display name - shown in debug logs")]
        public string recipeName;

        [Tooltip("Must match the stageID used by ResultScreenManager (e.g. Tower1_Stage1)")]
        public string requiredStageID;

        public Button recipeButton;
    }

    [Header("Recipe Entries")]
    [SerializeField] private List<RecipeEntry> recipes = new List<RecipeEntry>();

    private const string FIRST_CLEAR_PREFIX = "FirstClear_";

    private readonly Color unlockedColor = Color.white;
    private readonly Color lockedColor = new Color(0.2f, 0.2f, 0.2f, 0.3f);

    private void Awake()
    {
        // Singleton - survive scene loads if placed on a persistent object,
        // or let each scene create its own instance if placed on a scene object.
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        RefreshUnlockStates();
    }

    // Called by ResultScreenManager after saving FirstClear_ to PlayerPrefs.
    // Safe to call from anywhere - just re-reads PlayerPrefs and updates buttons.
    public void RefreshUnlockStates()
    {
        foreach (RecipeEntry recipe in recipes)
        {
            if (recipe == null || recipe.recipeButton == null) continue;

            bool unlocked = IsRecipeUnlocked(recipe.requiredStageID);

            recipe.recipeButton.interactable = unlocked;
            recipe.recipeButton.image.color = unlocked ? unlockedColor : lockedColor;

            Debug.Log($"[RecipeUnlockManager] '{recipe.recipeName}' " +
                      $"(stageID='{recipe.requiredStageID}') -> {(unlocked ? "UNLOCKED" : "LOCKED")}");
        }
    }

    // A recipe is unlocked when its required stage has been cleared at least once.
    // Empty stageID means "always unlocked" (useful for starter recipes).
    private bool IsRecipeUnlocked(string stageID)
    {
        if (string.IsNullOrEmpty(stageID)) return true;
        int value = PlayerPrefs.GetInt(FIRST_CLEAR_PREFIX + stageID, 0);
        Debug.Log($"[RecipeUnlockManager] PlayerPrefs['{FIRST_CLEAR_PREFIX}{stageID}'] = {value}");
        return value == 1;
    }
}