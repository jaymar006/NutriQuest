using System.Collections.Generic;
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
        List<Question> source = DataManager.Instance?.GetQuestionsForTower(towerId);
        
        if (source == null || source.Count == 0)
        {
            Debug.LogWarning($"No questions found for tower {towerId}");
            return new List<Question>();
        }

        // Shuffle (copies) if requested, otherwise copy manually so callers
        // never mutate DataManager's cached list in place.
        List<Question> working;
        if (shuffle)
        {
            working = ShuffleQuestions(source);
        }
        else if (count <= 0 || count >= source.Count)
        {
            working = new List<Question>(source.Count);
            for (int i = 0; i < source.Count; i++)
                working.Add(source[i]);
            return working;
        }
        else
        {
            working = source;
        }
        
        // Limit count if specified (manual take, no LINQ).
        int take = (count > 0 && count < working.Count) ? count : working.Count;
        if (shuffle && take == working.Count)
            return working;

        List<Question> result = new List<Question>(take);
        for (int i = 0; i < take; i++)
            result.Add(working[i]);
        
        return result;
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
    /// Gets questions filtered by language (single manual pass, pre-sized, no LINQ)
    /// </summary>
    public List<Question> GetQuestionsByLanguage(int towerId, string language)
    {
        List<Question> questions = DataManager.Instance?.GetQuestionsForTower(towerId);
        
        if (questions == null) return new List<Question>();
        
        List<Question> result = new List<Question>(questions.Count);
        for (int i = 0; i < questions.Count; i++)
        {
            Question q = questions[i];
            if (q != null && q.language == language)
                result.Add(q);
        }
        return result;
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
    /// Gets total question count for a tower (cached count, no list allocation)
    /// </summary>
    public int GetQuestionCount(int towerId)
    {
        if (DataManager.Instance != null)
            return DataManager.Instance.GetQuestionCountForTower(towerId);
        return 0;
    }
}
