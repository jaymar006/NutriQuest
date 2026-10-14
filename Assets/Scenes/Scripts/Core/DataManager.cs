using System;
using System.Collections.Generic;
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

    // Zero-alloc lookup caches built once after LoadGameData.
    private Dictionary<int, List<Question>> _questionsByTower = new Dictionary<int, List<Question>>();
    private Dictionary<int, Question> _questionById = new Dictionary<int, Question>();
    private Dictionary<int, Tower> _towerById = new Dictionary<int, Tower>();
    private Dictionary<int, Achievement> _achievementById = new Dictionary<int, Achievement>();
    private Dictionary<string, Recipe> _recipeById = new Dictionary<string, Recipe>();
    private static readonly List<Question> _emptyQuestions = new List<Question>();
    
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

        BuildCaches();
        
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

    /// <summary>
    /// Builds dictionary caches once after LoadGameData (manual loops, no LINQ).
    /// </summary>
    private void BuildCaches()
    {
        _questionsByTower.Clear();
        _questionById.Clear();
        _towerById.Clear();
        _achievementById.Clear();
        _recipeById.Clear();

        if (_questions != null)
        {
            for (int i = 0; i < _questions.Count; i++)
            {
                Question q = _questions[i];
                if (q == null) continue;
                if (!_questionById.ContainsKey(q.questionId))
                    _questionById.Add(q.questionId, q);

                List<Question> list;
                if (!_questionsByTower.TryGetValue(q.towerId, out list) || list == null)
                {
                    list = new List<Question>();
                    _questionsByTower[q.towerId] = list;
                }
                list.Add(q);
            }
        }

        if (_towers != null)
        {
            for (int i = 0; i < _towers.Count; i++)
            {
                Tower t = _towers[i];
                if (t == null) continue;
                if (!_towerById.ContainsKey(t.towerId))
                    _towerById.Add(t.towerId, t);
            }
        }

        if (_achievements != null)
        {
            for (int i = 0; i < _achievements.Count; i++)
            {
                Achievement a = _achievements[i];
                if (a == null) continue;
                if (!_achievementById.ContainsKey(a.achievementId))
                    _achievementById.Add(a.achievementId, a);
            }
        }

        if (_recipes != null)
        {
            for (int i = 0; i < _recipes.Count; i++)
            {
                Recipe r = _recipes[i];
                if (r == null || string.IsNullOrEmpty(r.id)) continue;
                if (!_recipeById.ContainsKey(r.id))
                    _recipeById.Add(r.id, r);
            }
        }
    }
    
    public List<Question> GetQuestionsForTower(int towerId)
    {
        if (_questions == null) return _emptyQuestions;
        List<Question> cached;
        if (_questionsByTower != null && _questionsByTower.TryGetValue(towerId, out cached) && cached != null)
            return cached;
        return _emptyQuestions;
    }

    /// <summary>
    /// Cached count without allocating a list.
    /// </summary>
    public int GetQuestionCountForTower(int towerId)
    {
        if (_questionsByTower == null) return 0;
        List<Question> cached;
        if (_questionsByTower.TryGetValue(towerId, out cached) && cached != null)
            return cached.Count;
        return 0;
    }
    
    public Question GetQuestion(int questionId)
    {
        if (_questions == null) return null;
        Question cached;
        if (_questionById != null && _questionById.TryGetValue(questionId, out cached))
            return cached;
        // Manual fallback loop (no LINQ).
        for (int i = 0; i < _questions.Count; i++)
        {
            if (_questions[i] != null && _questions[i].questionId == questionId)
                return _questions[i];
        }
        return null;
    }
    
    public List<Tower> GetAllTowers()
    {
        return _towers ?? new List<Tower>();
    }
    
    public Tower GetTower(int towerId)
    {
        if (_towers == null) return null;
        Tower cached;
        if (_towerById != null && _towerById.TryGetValue(towerId, out cached))
            return cached;
        for (int i = 0; i < _towers.Count; i++)
        {
            if (_towers[i] != null && _towers[i].towerId == towerId)
                return _towers[i];
        }
        return null;
    }
    
    public List<Achievement> GetAllAchievements()
    {
        return _achievements ?? new List<Achievement>();
    }
    
    public Achievement GetAchievement(int achievementId)
    {
        if (_achievements == null) return null;
        Achievement cached;
        if (_achievementById != null && _achievementById.TryGetValue(achievementId, out cached))
            return cached;
        for (int i = 0; i < _achievements.Count; i++)
        {
            if (_achievements[i] != null && _achievements[i].achievementId == achievementId)
                return _achievements[i];
        }
        return null;
    }
    
    public List<Recipe> GetAllRecipes()
    {
        return _recipes ?? new List<Recipe>();
    }
    
    public Recipe GetRecipe(string recipeId)
    {
        if (_recipes == null || string.IsNullOrEmpty(recipeId)) return null;
        Recipe cached;
        if (_recipeById != null && _recipeById.TryGetValue(recipeId, out cached))
            return cached;
        for (int i = 0; i < _recipes.Count; i++)
        {
            if (_recipes[i] != null && _recipes[i].id == recipeId)
                return _recipes[i];
        }
        return null;
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
