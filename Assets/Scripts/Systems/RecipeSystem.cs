using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// System for managing recipe unlocking and access.
/// Recipes are unlocked based on user points/progress.
/// </summary>
public class RecipeSystem : MonoBehaviour
{
    private static RecipeSystem _instance;
    public static RecipeSystem Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<RecipeSystem>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("RecipeSystem");
                    _instance = go.AddComponent<RecipeSystem>();
                    DontDestroyOnLoad(go);
                }
            }
            return _instance;
        }
    }
    
    private bool _isInitialized = false;
    
    // Events
    public event Action<string> OnRecipeUnlocked; // recipeId
    
    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
            Initialize();
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
        }
    }
    
    private void Initialize()
    {
        _isInitialized = true;
        Debug.Log("RecipeSystem initialized");
    }
    
    /// <summary>
    /// Checks and unlocks recipes based on user progress
    /// </summary>
    public void CheckRecipeUnlocks()
    {
        CloudSaveData saveData = DatabaseManager.Instance?.GetSaveData();
        if (saveData?.userData == null) return;
        
        List<Recipe> allRecipes = DataManager.Instance?.GetAllRecipes();
        if (allRecipes == null) return;
        
        int userScore = saveData.userData.highestScore;
        bool dataChanged = false;
        
        foreach (var recipe in allRecipes)
        {
            if (!recipe.isUnlocked && userScore >= recipe.pointsRequired)
            {
                UnlockRecipe(recipe.id);
                dataChanged = true;
            }
        }
        
        if (dataChanged)
        {
            DatabaseManager.Instance?.MarkDataDirty();
        }
    }
    
    /// <summary>
    /// Unlocks a recipe
    /// </summary>
    public void UnlockRecipe(string recipeId)
    {
        Recipe recipe = DataManager.Instance?.GetRecipe(recipeId);
        if (recipe == null)
        {
            Debug.LogError($"Recipe {recipeId} not found");
            return;
        }
        
        if (recipe.isUnlocked) return;
        
        recipe.isUnlocked = true;
        recipe.unlockDate = System.DateTime.UtcNow;
        
        OnRecipeUnlocked?.Invoke(recipeId);
        DatabaseManager.Instance?.MarkDataDirty();
        
        Debug.Log($"Recipe unlocked: {recipe.name}");
    }
    
    /// <summary>
    /// Gets all unlocked recipes
    /// </summary>
    public List<Recipe> GetUnlockedRecipes()
    {
        List<Recipe> allRecipes = DataManager.Instance?.GetAllRecipes();
        if (allRecipes == null) return new List<Recipe>();
        
        return allRecipes.Where(r => r.isUnlocked).ToList();
    }
    
    /// <summary>
    /// Gets recipes by category
    /// </summary>
    public List<Recipe> GetRecipesByCategory(string category)
    {
        List<Recipe> unlocked = GetUnlockedRecipes();
        return unlocked.Where(r => r.categories != null && r.categories.Contains(category)).ToList();
    }
    
    /// <summary>
    /// Gets recipe by ID
    /// </summary>
    public Recipe GetRecipe(string recipeId)
    {
        return DataManager.Instance?.GetRecipe(recipeId);
    }
    
    /// <summary>
    /// Checks if a recipe is unlocked
    /// </summary>
    public bool IsRecipeUnlocked(string recipeId)
    {
        Recipe recipe = GetRecipe(recipeId);
        return recipe != null && recipe.isUnlocked;
    }
}
