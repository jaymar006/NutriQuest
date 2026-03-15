using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Manages loading and accessing game data (questions, towers, achievements, recipes).
/// </summary>
public class DataManager : MonoBehaviour
{
    private static DataManager _instance;
    public static DataManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<DataManager>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("DataManager");
                    _instance = go.AddComponent<DataManager>();
                    DontDestroyOnLoad(go);
                }
            }
            return _instance;
        }
    }
    
    private bool _isInitialized = false;
    
    // Loaded data
    private List<Question> _questions;
    private List<Tower> _towers;
    private List<Achievement> _achievements;
    private List<Recipe> _recipes;
    
    // Events
    public System.Action OnDataLoaded;
    
    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
        }
    }
    
    /// <summary>
    /// Loads all game data from Resources
    /// </summary>
    public void LoadGameData()
    {
        if (_isInitialized)
        {
            Debug.LogWarning("Data already loaded");
            return;
        }
        
        Debug.Log("Loading game data...");
        
        // Load questions
        LoadQuestions();
        
        // Load towers
        LoadTowers();
        
        // Load achievements
        LoadAchievements();
        
        // Load recipes
        LoadRecipes();
        
        _isInitialized = true;
        OnDataLoaded?.Invoke();
        
        Debug.Log($"Data loaded - Questions: {_questions?.Count ?? 0}, Towers: {_towers?.Count ?? 0}");
    }
    
    /// <summary>
    /// Loads questions from JSON file
    /// </summary>
    private void LoadQuestions()
    {
        _questions = new List<Question>();
        
        TextAsset questionsFile = Resources.Load<TextAsset>(Constant.QUESTIONS_DATA_PATH);
        if (questionsFile != null)
        {
            try
            {
                QuestionsData data = JsonUtility.FromJson<QuestionsData>(questionsFile.text);
                if (data != null && data.questions != null)
                {
                    _questions = data.questions;
                    Debug.Log($"Loaded {_questions.Count} questions");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to parse questions JSON: {e.Message}");
            }
        }
        else
        {
            Debug.LogWarning("Questions JSON file not found in Resources");
        }
    }
    
    /// <summary>
    /// Loads towers from JSON file
    /// </summary>
    private void LoadTowers()
    {
        _towers = new List<Tower>();
        
        TextAsset towersFile = Resources.Load<TextAsset>(Constant.TOWERS_DATA_PATH);
        if (towersFile != null)
        {
            try
            {
                TowersData data = JsonUtility.FromJson<TowersData>(towersFile.text);
                if (data != null && data.towers != null)
                {
                    _towers = data.towers;
                    Debug.Log($"Loaded {_towers.Count} towers");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to parse towers JSON: {e.Message}");
            }
        }
        else
        {
            Debug.LogWarning("Towers JSON file not found in Resources");
        }
    }
    
    /// <summary>
    /// Loads achievements from JSON file
    /// </summary>
    private void LoadAchievements()
    {
        _achievements = new List<Achievement>();
        
        TextAsset achievementsFile = Resources.Load<TextAsset>(Constant.ACHIEVEMENTS_DATA_PATH);
        if (achievementsFile != null)
        {
            try
            {
                AchievementsData data = JsonUtility.FromJson<AchievementsData>(achievementsFile.text);
                if (data != null && data.achievements != null)
                {
                    _achievements = data.achievements;
                    Debug.Log($"Loaded {_achievements.Count} achievements");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to parse achievements JSON: {e.Message}");
            }
        }
        else
        {
            Debug.LogWarning("Achievements JSON file not found in Resources");
        }
    }
    
    /// <summary>
    /// Loads recipes from JSON file
    /// </summary>
    private void LoadRecipes()
    {
        _recipes = new List<Recipe>();
        
        TextAsset recipesFile = Resources.Load<TextAsset>(Constant.RECIPES_DATA_PATH);
        if (recipesFile != null)
        {
            try
            {
                RecipesData data = JsonUtility.FromJson<RecipesData>(recipesFile.text);
                if (data != null && data.recipes != null)
                {
                    _recipes = data.recipes;
                    Debug.Log($"Loaded {_recipes.Count} recipes");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to parse recipes JSON: {e.Message}");
            }
        }
        else
        {
            Debug.LogWarning("Recipes JSON file not found in Resources");
        }
    }
    
    // Getters
    
    public List<Question> GetAllQuestions()
    {
        return _questions ?? new List<Question>();
    }
    
    public List<Question> GetQuestionsForTower(int towerId)
    {
        if (_questions == null) return new List<Question>();
        return _questions.Where(q => q.towerId == towerId).ToList();
    }
    
    public Question GetQuestion(int questionId)
    {
        if (_questions == null) return null;
        return _questions.FirstOrDefault(q => q.questionId == questionId);
    }
    
    public List<Tower> GetAllTowers()
    {
        return _towers ?? new List<Tower>();
    }
    
    public Tower GetTower(int towerId)
    {
        if (_towers == null) return null;
        return _towers.FirstOrDefault(t => t.towerId == towerId);
    }
    
    public List<Achievement> GetAllAchievements()
    {
        return _achievements ?? new List<Achievement>();
    }
    
    public Achievement GetAchievement(int achievementId)
    {
        if (_achievements == null) return null;
        return _achievements.FirstOrDefault(a => a.achievementId == achievementId);
    }
    
    public List<Recipe> GetAllRecipes()
    {
        return _recipes ?? new List<Recipe>();
    }
    
    public Recipe GetRecipe(string recipeId)
    {
        if (_recipes == null) return null;
        return _recipes.FirstOrDefault(r => r.id == recipeId);
    }
    
    public bool IsDataLoaded()
    {
        return _isInitialized;
    }
}

// JSON Data Wrapper Classes
[System.Serializable]
public class QuestionsData
{
    public List<Question> questions;
}

[System.Serializable]
public class TowersData
{
    public List<Tower> towers;
}

[System.Serializable]
public class AchievementsData
{
    public List<Achievement> achievements;
}

[System.Serializable]
public class RecipesData
{
    public List<Recipe> recipes;
}
