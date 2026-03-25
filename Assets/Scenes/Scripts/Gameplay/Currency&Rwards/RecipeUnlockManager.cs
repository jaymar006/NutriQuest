using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class RecipeUnlockManager : MonoBehaviour
{
    [System.Serializable]
    public class RecipeEntry
    {
        public string recipeName;
        public string requiredStageID;
        public Button recipeButton;
    }

    [Header("Recipe Entries")]
    [SerializeField] private List<RecipeEntry> recipes = new List<RecipeEntry>();

    private const string FIRST_CLEAR_PREFIX = "FirstClear_";
    private readonly Color unlockedColor = Color.white;
    private readonly Color lockedColor = new Color(0.4f, 0.4f, 0.4f, 1f);

    private void Start()
    {
        RefreshUnlockStates();
    }

    public void RefreshUnlockStates()
    {
        foreach (RecipeEntry recipe in recipes)
        {
            if (recipe.recipeButton == null) continue;

            bool unlocked = IsRecipeUnlocked(recipe.requiredStageID);

            recipe.recipeButton.interactable = unlocked;
            recipe.recipeButton.image.color = unlocked ? unlockedColor : lockedColor;

            Debug.Log("[RecipeUnlockManager] " + recipe.recipeName +
                " is " + (unlocked ? "UNLOCKED" : "LOCKED"));
        }
    }

    private bool IsRecipeUnlocked(string stageID)
    {
        if (string.IsNullOrEmpty(stageID)) return true;
        return PlayerPrefs.GetInt(FIRST_CLEAR_PREFIX + stageID, 0) == 1;
    }

    public static RecipeUnlockManager Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
    }

}