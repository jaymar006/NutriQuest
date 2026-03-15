using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// System for managing stamina regeneration and usage.
/// Integrates with DatabaseManager and GameManager.
/// </summary>
public class StaminaSystem : MonoBehaviour
{
    private static StaminaSystem _instance;
    public static StaminaSystem Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<StaminaSystem>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("StaminaSystem");
                    _instance = go.AddComponent<StaminaSystem>();
                    DontDestroyOnLoad(go);
                }
            }
            return _instance;
        }
    }
    
    private bool _isInitialized = false;
    private float _lastRegenCheckTime = 0f;
    private float _regenCheckInterval = 1f; // Check every second
    
    // Events
    public event Action<int, int> OnStaminaChanged; // current, max
    public event Action OnStaminaFull;
    public event Action OnStaminaDepleted;
    
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
        _lastRegenCheckTime = Time.time;
        Debug.Log("StaminaSystem initialized");
    }
    
    private void Update()
    {
        if (!_isInitialized) return;
        
        // Check for stamina regeneration periodically
        if (Time.time - _lastRegenCheckTime >= _regenCheckInterval)
        {
            RegenerateStamina();
            _lastRegenCheckTime = Time.time;
        }
    }
    
    /// <summary>
    /// Regenerates stamina based on time passed
    /// </summary>
    public void RegenerateStamina()
    {
        CloudSaveData saveData = DatabaseManager.Instance?.GetSaveData();
        if (saveData?.stamina == null) return;
        
        int previousStamina = saveData.stamina.currentStamina;
        saveData.stamina.RegenerateStamina();
        int currentStamina = saveData.stamina.currentStamina;
        
        if (currentStamina != previousStamina)
        {
            OnStaminaChanged?.Invoke(currentStamina, saveData.stamina.maxStamina);
            DatabaseManager.Instance?.MarkDataDirty();
            
            if (currentStamina >= saveData.stamina.maxStamina && previousStamina < saveData.stamina.maxStamina)
            {
                OnStaminaFull?.Invoke();
            }
        }
    }
    
    /// <summary>
    /// Uses stamina and returns true if successful
    /// </summary>
    public bool UseStamina(int amount)
    {
        CloudSaveData saveData = DatabaseManager.Instance?.GetSaveData();
        if (saveData?.stamina == null) return false;
        
        if (saveData.stamina.currentStamina < amount)
        {
            return false;
        }
        
        saveData.stamina.currentStamina -= amount;
        // Note: Stamina model doesn't have lastStaminaUseTime per ERD, only lastRegenTime
        
        OnStaminaChanged?.Invoke(saveData.stamina.currentStamina, saveData.stamina.maxStamina);
        DatabaseManager.Instance?.MarkDataDirty();
        
        if (saveData.stamina.currentStamina <= 0)
        {
            OnStaminaDepleted?.Invoke();
        }
        
        return true;
    }
    
    /// <summary>
    /// Gets current stamina
    /// </summary>
    public int GetCurrentStamina()
    {
        CloudSaveData saveData = DatabaseManager.Instance?.GetSaveData();
        return saveData?.stamina?.currentStamina ?? 0;
    }
    
    /// <summary>
    /// Gets max stamina
    /// </summary>
    public int GetMaxStamina()
    {
        CloudSaveData saveData = DatabaseManager.Instance?.GetSaveData();
        return saveData?.stamina?.maxStamina ?? Constant.DEFAULT_MAX_STAMINA;
    }
    
    /// <summary>
    /// Checks if user has enough stamina for an action
    /// </summary>
    public bool HasEnoughStamina(int requiredAmount)
    {
        return GetCurrentStamina() >= requiredAmount;
    }
    
    /// <summary>
    /// Gets time until next stamina regeneration
    /// </summary>
    public TimeSpan GetTimeUntilNextRegen()
    {
        CloudSaveData saveData = DatabaseManager.Instance?.GetSaveData();
        if (saveData?.stamina == null) return TimeSpan.Zero;
        
        return saveData.stamina.GetTimeUntilNextRegen();
    }
    
    /// <summary>
    /// Gets stamina percentage (0-1)
    /// </summary>
    public float GetStaminaPercentage()
    {
        CloudSaveData saveData = DatabaseManager.Instance?.GetSaveData();
        if (saveData?.stamina == null) return 0f;
        
        return saveData.stamina.GetStaminaPercentage();
    }
}
