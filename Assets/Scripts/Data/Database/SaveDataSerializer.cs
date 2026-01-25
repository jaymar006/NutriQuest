using System;
using System.IO;
using UnityEngine;

/// <summary>
/// Handles serialization/deserialization of save data.
/// Optimized for Android: Uses UTC DateTime, atomic file writes, and error recovery.
/// </summary>
public static class SaveDataSerializer
{
    private const string SAVE_FILE_NAME = "nutriquest_save.json";
    private const string BACKUP_FILE_NAME = "nutriquest_save_backup.json";
    
    // Android: Application.persistentDataPath resolves to:
    // /storage/emulated/0/Android/data/[package]/files on Android
    
    /// <summary>
    /// Serializes CloudSaveData to JSON string
    /// </summary>
    public static string Serialize(CloudSaveData saveData)
    {
        try
        {
            // Prepare data for serialization (convert Dictionary to List, DateTime to string)
            saveData.PrepareForSerialization();
            if (saveData.userData != null)
            {
                saveData.userData.PrepareForSerialization();
            }
            
            string json = JsonUtility.ToJson(saveData, true);
            return json;
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to serialize save data: {e.Message}");
            return null;
        }
    }
    
    /// <summary>
    /// Deserializes JSON string to CloudSaveData
    /// </summary>
    public static CloudSaveData Deserialize(string json)
    {
        try
        {
            if (string.IsNullOrEmpty(json))
            {
                Debug.LogWarning("Attempted to deserialize empty JSON string");
                return null;
            }
            
            CloudSaveData saveData = JsonUtility.FromJson<CloudSaveData>(json);
            return saveData;
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to deserialize save data: {e.Message}");
            return null;
        }
    }
    
    /// <summary>
    /// Gets the full path to the save file.
    /// Android: Returns path in app's private storage (no permissions needed)
    /// </summary>
    public static string GetSaveFilePath()
    {
        string path = Path.Combine(Application.persistentDataPath, SAVE_FILE_NAME);
        
        // Android: Log path for debugging (only in editor/dev builds)
        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log($"Save file path: {path}");
        #endif
        
        return path;
    }
    
    /// <summary>
    /// Gets the full path to the backup file
    /// </summary>
    public static string GetBackupFilePath()
    {
        return Path.Combine(Application.persistentDataPath, BACKUP_FILE_NAME);
    }
    
    /// <summary>
    /// Validates that the save data structure is valid
    /// </summary>
    public static bool ValidateSaveData(CloudSaveData saveData)
    {
        if (saveData == null)
        {
            Debug.LogError("Save data is null");
            return false;
        }
        
        if (saveData.userData == null)
        {
            Debug.LogError("User data is null in save data");
            return false;
        }
        
        if (string.IsNullOrEmpty(saveData.userId))
        {
            Debug.LogError("User ID is null or empty in save data");
            return false;
        }
        
        return true;
    }
    
    /// <summary>
    /// Creates a backup of the current save file
    /// </summary>
    public static bool CreateBackup()
    {
        try
        {
            string savePath = GetSaveFilePath();
            string backupPath = GetBackupFilePath();
            
            if (File.Exists(savePath))
            {
                File.Copy(savePath, backupPath, true);
                Debug.Log("Backup created successfully");
                return true;
            }
            else
            {
                Debug.LogWarning("No save file exists to backup");
                return false;
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to create backup: {e.Message}");
            return false;
        }
    }
    
    /// <summary>
    /// Restores save data from backup
    /// </summary>
    public static CloudSaveData RestoreFromBackup()
    {
        try
        {
            string backupPath = GetBackupFilePath();
            
            if (!File.Exists(backupPath))
            {
                Debug.LogWarning("No backup file exists");
                return null;
            }
            
            string json = File.ReadAllText(backupPath);
            return Deserialize(json);
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to restore from backup: {e.Message}");
            return null;
        }
    }
}
