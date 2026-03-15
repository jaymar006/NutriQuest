using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// System for managing questions, shuffling, and question selection.
/// Works with DataManager to provide questions to GameManager.
/// </summary>
public class QuestionManager : MonoBehaviour
{
    private static QuestionManager _instance;
    public static QuestionManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<QuestionManager>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("QuestionManager");
                    _instance = go.AddComponent<QuestionManager>();
                    DontDestroyOnLoad(go);
                }
            }
            return _instance;
        }
    }
    
    private bool _isInitialized = false;
    
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
        Debug.Log("QuestionManager initialized");
    }
    
    /// <summary>
    /// Gets questions for a tower, optionally shuffled
    /// </summary>
    public List<Question> GetQuestionsForTower(int towerId, bool shuffle = true, int count = -1)
    {
        List<Question> questions = DataManager.Instance?.GetQuestionsForTower(towerId);
        
        if (questions == null || questions.Count == 0)
        {
            Debug.LogWarning($"No questions found for tower {towerId}");
            return new List<Question>();
        }
        
        // Shuffle questions if requested
        if (shuffle)
        {
            questions = ShuffleQuestions(questions);
        }
        
        // Limit count if specified
        if (count > 0 && count < questions.Count)
        {
            questions = questions.Take(count).ToList();
        }
        
        return questions;
    }
    
    /// <summary>
    /// Shuffles a list of questions using Fisher-Yates algorithm
    /// </summary>
    public List<Question> ShuffleQuestions(List<Question> questions)
    {
        if (questions == null) return new List<Question>();
        
        List<Question> shuffled = new List<Question>(questions);
        
        for (int i = shuffled.Count - 1; i > 0; i--)
        {
            int randomIndex = Random.Range(0, i + 1);
            Question temp = shuffled[i];
            shuffled[i] = shuffled[randomIndex];
            shuffled[randomIndex] = temp;
        }
        
        return shuffled;
    }
    
    /// <summary>
    /// Gets a random question from a tower
    /// </summary>
    public Question GetRandomQuestion(int towerId)
    {
        List<Question> questions = GetQuestionsForTower(towerId, shuffle: true, count: 1);
        return questions.Count > 0 ? questions[0] : null;
    }
    
    /// <summary>
    /// Gets questions filtered by language
    /// </summary>
    public List<Question> GetQuestionsByLanguage(int towerId, string language)
    {
        List<Question> questions = DataManager.Instance?.GetQuestionsForTower(towerId);
        
        if (questions == null) return new List<Question>();
        
        return questions.Where(q => q.language == language).ToList();
    }
    
    /// <summary>
    /// Validates a question answer
    /// </summary>
    public bool ValidateAnswer(Question question, string selectedAnswer)
    {
        if (question == null) return false;
        
        return question.IsCorrect(selectedAnswer);
    }
    
    /// <summary>
    /// Gets total question count for a tower
    /// </summary>
    public int GetQuestionCount(int towerId)
    {
        List<Question> questions = DataManager.Instance?.GetQuestionsForTower(towerId);
        return questions?.Count ?? 0;
    }
}
