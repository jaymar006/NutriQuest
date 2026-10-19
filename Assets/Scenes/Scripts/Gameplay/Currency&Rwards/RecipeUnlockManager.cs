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

        [Tooltip("GameObject shown while locked (e.g. lock icon/overlay). Turned off once unlocked.")]
        public GameObject lockOverlay;
    }

    [Header("Recipe Entries")]
    [SerializeField] private List<RecipeEntry> recipes = new List<RecipeEntry>();

    private const string FIRST_CLEAR_PREFIX = ProgressKeys.FirstClear;

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
        for (int i = 0; i < recipes.Count; i++)
        {
            RecipeEntry recipe = recipes[i];
            if (recipe == null || recipe.recipeButton == null) continue;

            bool unlocked = IsRecipeUnlocked(EffectiveStageId(i, recipe.requiredStageID));

            recipe.recipeButton.interactable = unlocked;

            if (recipe.lockOverlay != null)
            {
                recipe.lockOverlay.SetActive(!unlocked);
            }

            Debug.Log($"[RecipeUnlockManager] '{recipe.recipeName}' " +
                      $"(stageID='{EffectiveStageId(i, recipe.requiredStageID)}') -> {(unlocked ? "UNLOCKED" : "LOCKED")}");
        }
    }

    /// <summary>
    /// The stage a recipe unlocks after. The teacher can change this from the
    /// admin panel; the Inspector value is the built-in default.
    ///
    /// The entries in this component line up with AdminGameConfig.Recipes by
    /// index, which is what lets the override find its recipe.
    /// </summary>
    private static string EffectiveStageId(int index, string inspectorStageId)
    {
        if (index >= 0 && index < AdminGameConfig.Recipes.Count)
        {
            AdminContentStore.RecipeOverride recipe =
                AdminContentStore.GetRecipe(AdminGameConfig.Recipes[index].id);
            if (recipe != null && !string.IsNullOrEmpty(recipe.unlockStageId))
            {
                return recipe.unlockStageId;
            }
        }
        return inspectorStageId;
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