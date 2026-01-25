using System;
using System.IO;
using UnityEngine;

public class LocalSaveManager
{
    private CloudSaveData _currentSaveData;
    private bool _isDirty; // Track if data has been modified
    
    public CloudSaveData CurrentSaveData => _currentSaveData;
    public bool HasSaveData => _currentSaveData != null;
    public bool IsDirty => _isDirty;
    
    /// <summary>
    /// Loads save data from disk
    /// </summary>
    public bool LoadSaveData()
    {
        try
        {
            string savePath = SaveDataSerializer.GetSaveFilePath();
            
            if (!File.Exists(savePath))
            {
                Debug.Log("No save file found, creating new save data");
                _currentSaveData = CreateNewSaveData();
                _isDirty = true;
                return true;
            }
            
            string json = File.ReadAllText(savePath);
            _currentSaveData = SaveDataSerializer.Deserialize(json);
            
            if (_currentSaveData == null)
            {
                Debug.LogWarning("Failed to deserialize save data, attempting backup restore");
                _currentSaveData = SaveDataSerializer.RestoreFromBackup();
                
                if (_currentSaveData == null)
                {
                    Debug.LogWarning("Backup restore failed, creating new save data");
                    _currentSaveData = CreateNewSaveData();
                }
            }
            
            if (!SaveDataSerializer.ValidateSaveData(_currentSaveData))
            {
                Debug.LogWarning("Save data validation failed, creating new save data");
                _currentSaveData = CreateNewSaveData();
            }
            
            _isDirty = false;
            Debug.Log("Save data loaded successfully");
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to load save data: {e.Message}");
            _currentSaveData = CreateNewSaveData();
            _isDirty = true;
            return false;
        }
    }
    
    /// <summary>
    /// Saves current save data to disk
    /// </summary>
    public bool SaveSaveData(bool createBackup = true)
    {
        if (_currentSaveData == null)
        {
            Debug.LogError("Cannot save: No save data exists");
            return false;
        }
        
        try
        {
            // Create backup before saving
            if (createBackup)
            {
                SaveDataSerializer.CreateBackup();
            }
            
            // Update save timestamp
            _currentSaveData.UpdateSaveTime();
            
            // Serialize and save
            string json = SaveDataSerializer.Serialize(_currentSaveData);
            
            if (string.IsNullOrEmpty(json))
            {
                Debug.LogError("Failed to serialize save data");
                return false;
            }
            
            string savePath = SaveDataSerializer.GetSaveFilePath();
            
            // Android optimization: Ensure directory exists
            string directory = Path.GetDirectoryName(savePath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            
            // Android: Use atomic write (write to temp, then rename) to prevent corruption
            string tempPath = savePath + ".tmp";
            File.WriteAllText(tempPath, json);
            
            // Atomic move on Android
            if (File.Exists(savePath))
            {
                File.Delete(savePath);
            }
            File.Move(tempPath, savePath);
            
            _isDirty = false;
            Debug.Log($"Save data saved successfully to: {savePath}");
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to save save data: {e.Message}");
            return false;
        }
    }
    
    /// <summary>
    /// Creates a new save data instance
    /// </summary>
    public CloudSaveData CreateNewSaveData()
    {
        // Android: Use device unique identifier (persists across app reinstalls on Android)
        string deviceId = SystemInfo.deviceUniqueIdentifier;
        
        // Fallback for Android devices that don't provide unique ID
        if (string.IsNullOrEmpty(deviceId) || deviceId == "00000000-0000-0000-0000-000000000000")
        {
            // Use Android ID as fallback (requires AndroidManifest permission)
            deviceId = SystemInfo.deviceModel + "_" + SystemInfo.deviceName;
            Debug.LogWarning("Using fallback device ID for Android");
        }
        
        User newUser = new User
        {
            userId = deviceId,
            username = "Player",
            password = "", // Password should be set during registration
            currentTower = 0,
            highestScore = 0,
            staminaPoints = 100,
            maxStamina = 100,
            soundEnabled = true
        };
        
        CloudSaveData newSaveData = new CloudSaveData(newUser);
        _currentSaveData = newSaveData;
        _isDirty = true;
        
        return newSaveData;
    }
    
    /// <summary>
    /// Sets the current save data and marks as dirty
    /// </summary>
    public void SetSaveData(CloudSaveData saveData)
    {
        _currentSaveData = saveData;
        _isDirty = true;
    }
    
    /// <summary>
    /// Marks save data as modified (dirty)
    /// </summary>
    public void MarkDirty()
    {
        _isDirty = true;
    }
    
    /// <summary>
    /// Deletes the save file
    /// </summary>
    public bool DeleteSaveData()
    {
        try
        {
            string savePath = SaveDataSerializer.GetSaveFilePath();
            
            if (File.Exists(savePath))
            {
                File.Delete(savePath);
                Debug.Log("Save file deleted");
            }
            
            _currentSaveData = null;
            _isDirty = false;
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to delete save data: {e.Message}");
            return false;
        }
    }
    
    /// <summary>
    /// Checks if a save file exists
    /// </summary>
    public bool SaveFileExists()
    {
        string savePath = SaveDataSerializer.GetSaveFilePath();
        return File.Exists(savePath);
    }
    
    /// <summary>
    /// Gets the file size of the save file in bytes
    /// </summary>
    public long GetSaveFileSize()
    {
        try
        {
            string savePath = SaveDataSerializer.GetSaveFilePath();
            
            if (File.Exists(savePath))
            {
                FileInfo fileInfo = new FileInfo(savePath);
                return fileInfo.Length;
            }
            
            return 0;
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to get save file size: {e.Message}");
            return 0;
        }
    }
}
