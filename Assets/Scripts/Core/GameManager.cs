using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Main game manager that handles game state, current session, and gameplay flow.
/// </summary>
public class GameManager : MonoBehaviour
{
    private static GameManager _instance;
    public static GameManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<GameManager>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("GameManager");
                    _instance = go.AddComponent<GameManager>();
                    DontDestroyOnLoad(go);
                }
            }
            return _instance;
        }
    }
    
    private bool _isInitialized = false;
    private GameState _currentState = GameState.MainMenu;
    
    // Current session data
    private Attempt _currentAttempt;
    private int _currentTowerId = -1;
    private List<Question> _currentQuestions;
    private int _currentQuestionIndex = 0;
    private int _currentScore = 0;
    private int _correctAnswers = 0;
    
    // Events
    public event Action<GameState> OnGameStateChanged;
    public event Action<Attempt> OnAttemptStarted;
    public event Action<Attempt> OnAttemptCompleted;
    public event Action<Question> OnQuestionChanged;
    public event Action<int, int> OnScoreUpdated; // currentScore, correctAnswers
    
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
    
    public void Initialize()
    {
        if (_isInitialized) return;
        
        _isInitialized = true;
        SetGameState(GameState.MainMenu);
        
        Debug.Log("GameManager initialized");
    }
    
    /// <summary>
    /// Starts a new quiz attempt for a tower
    /// </summary>
    public bool StartAttempt(int towerId)
    {
        if (_currentState != GameState.MainMenu && _currentState != GameState.TowerSelection)
        {
            Debug.LogWarning("Cannot start attempt - game is not in correct state");
            return false;
        }
        
        // Check if user has enough stamina using StaminaSystem
        Tower tower = DataManager.Instance?.GetTower(towerId);
        int staminaCost = tower?.staminaCost ?? Constant.DEFAULT_TOWER_STAMINA_COST;
        
        if (StaminaSystem.Instance != null && !StaminaSystem.Instance.HasEnoughStamina(staminaCost))
        {
            Debug.LogWarning("Not enough stamina to start attempt");
            return false;
        }
        
        // Check cooldown using CooldownSystem
        if (CooldownSystem.Instance != null && !CooldownSystem.Instance.IsTowerAvailable(towerId))
        {
            Debug.LogWarning($"Tower {towerId} is on cooldown");
            return false;
        }
        
        // Use stamina
        if (StaminaSystem.Instance != null)
        {
            StaminaSystem.Instance.UseStamina(staminaCost);
        }
        
        // Load questions for this tower using QuestionManager
        _currentQuestions = QuestionManager.Instance?.GetQuestionsForTower(towerId, shuffle: true);
        if (_currentQuestions == null || _currentQuestions.Count == 0)
        {
            Debug.LogError($"No questions found for tower {towerId}");
            return false;
        }
        
        // Get save data for user ID
        CloudSaveData saveData = DatabaseManager.Instance?.GetSaveData();
        
        // Create new attempt
        _currentAttempt = new Attempt
        {
            attemptId = UnityEngine.Random.Range(1000, 9999),
            userId = saveData?.userId ?? "",
            towerId = towerId,
            score = 0,
            cleared = false,
            perfectScore = false
        };
        
        _currentTowerId = towerId;
        _currentQuestionIndex = 0;
        _currentScore = 0;
        _correctAnswers = 0;
        
        SetGameState(GameState.Gameplay);
        OnAttemptStarted?.Invoke(_currentAttempt);
        
        // Load first question
        LoadCurrentQuestion();
        
        Debug.Log($"Started attempt for tower {towerId}");
        return true;
    }
    
    /// <summary>
    /// Submits an answer for the current question
    /// </summary>
    public bool SubmitAnswer(string selectedAnswer)
    {
        if (_currentState != GameState.Gameplay || _currentQuestions == null || _currentQuestionIndex >= _currentQuestions.Count)
        {
            return false;
        }
        
        Question currentQuestion = _currentQuestions[_currentQuestionIndex];
        bool isCorrect = currentQuestion.IsCorrect(selectedAnswer);
        
        int pointsEarned = 0;
        if (isCorrect)
        {
            pointsEarned = Constant.POINTS_PER_CORRECT_ANSWER;
            _correctAnswers++;
            _currentScore += pointsEarned;
        }
        
        // Update attempt
        _currentAttempt.score = _currentScore;
        
        OnScoreUpdated?.Invoke(_currentScore, _correctAnswers);
        
        // Move to next question or complete attempt
        _currentQuestionIndex++;
        
        if (_currentQuestionIndex >= _currentQuestions.Count)
        {
            CompleteAttempt();
        }
        else
        {
            LoadCurrentQuestion();
        }
        
        return isCorrect;
    }
    
    /// <summary>
    /// Loads the current question
    /// </summary>
    private void LoadCurrentQuestion()
    {
        if (_currentQuestions == null || _currentQuestionIndex >= _currentQuestions.Count)
        {
            return;
        }
        
        Question question = _currentQuestions[_currentQuestionIndex];
        OnQuestionChanged?.Invoke(question);
    }
    
    /// <summary>
    /// Completes the current attempt
    /// </summary>
    public void CompleteAttempt()
    {
        if (_currentAttempt == null) return;
        
        _currentAttempt.dateAttempted = DateTime.UtcNow;
        
        // Calculate if cleared (70% or higher)
        float accuracy = _currentQuestions.Count > 0 ? (float)_correctAnswers / _currentQuestions.Count : 0f;
        _currentAttempt.cleared = accuracy >= (Constant.MIN_REQUIRED_SCORE / 100f);
        _currentAttempt.perfectScore = _correctAnswers == _currentQuestions.Count;
        
        // Update user data
        CloudSaveData saveData = DatabaseManager.Instance?.GetSaveData();
        if (saveData?.userData != null)
        {
            // Update highest score
            if (_currentAttempt.score > saveData.userData.highestScore)
            {
                saveData.userData.highestScore = _currentAttempt.score;
            }
            
            // Update current tower progress
            if (_currentAttempt.cleared && _currentTowerId > saveData.userData.currentTower)
            {
                saveData.userData.currentTower = _currentTowerId;
            }
        }
        
        // Save attempt to database
        if (saveData != null)
        {
            saveData.recentAttempts.Add(_currentAttempt);
            
            // Keep only recent attempts
            if (saveData.recentAttempts.Count > Constant.MAX_RECENT_ATTEMPTS)
            {
                saveData.recentAttempts.RemoveAt(0);
            }
            
            DatabaseManager.Instance?.MarkDataDirty();
        }
        
        // Handle tower completion
        if (_currentAttempt.cleared && TowerSystem.Instance != null)
        {
            TowerSystem.Instance.CompleteTower(_currentTowerId);
        }
        
        // Check achievements
        if (AchievementSystem.Instance != null)
        {
            AchievementSystem.Instance.CheckAchievements();
        }
        
        // Check recipe unlocks
        if (RecipeSystem.Instance != null)
        {
            RecipeSystem.Instance.CheckRecipeUnlocks();
        }
        
        SetGameState(GameState.Results);
        OnAttemptCompleted?.Invoke(_currentAttempt);
        
        Debug.Log($"Attempt completed - Score: {_currentAttempt.score}, Cleared: {_currentAttempt.cleared}");
    }
    
    /// <summary>
    /// Gets the current question
    /// </summary>
    public Question GetCurrentQuestion()
    {
        if (_currentQuestions == null || _currentQuestionIndex >= _currentQuestions.Count)
        {
            return null;
        }
        return _currentQuestions[_currentQuestionIndex];
    }
    
    /// <summary>
    /// Gets the current attempt
    /// </summary>
    public Attempt GetCurrentAttempt()
    {
        return _currentAttempt;
    }
    
    /// <summary>
    /// Gets the current game state
    /// </summary>
    public GameState GetGameState()
    {
        return _currentState;
    }
    
    /// <summary>
    /// Sets the game state
    /// </summary>
    public void SetGameState(GameState newState)
    {
        if (_currentState == newState) return;
        
        GameState previousState = _currentState;
        _currentState = newState;
        
        OnGameStateChanged?.Invoke(newState);
        Debug.Log($"Game state changed: {previousState} -> {newState}");
    }
    
    /// <summary>
    /// Resets the current session
    /// </summary>
    public void ResetSession()
    {
        _currentAttempt = null;
        _currentTowerId = -1;
        _currentQuestions = null;
        _currentQuestionIndex = 0;
        _currentScore = 0;
        _correctAnswers = 0;
    }
}

public enum GameState
{
    MainMenu,
    TowerSelection,
    Gameplay,
    Results,
    Profile,
    Recipes,
    Settings
}
