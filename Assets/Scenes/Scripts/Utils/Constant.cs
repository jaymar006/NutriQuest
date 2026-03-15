/// <summary>
/// Game constants used throughout the application.
/// Centralized location for all configurable values.
/// </summary>
public static class Constant
{
    // Game Configuration
    public const string GAME_NAME = "NutriQuest";
    public const string GAME_VERSION = "1.0.0";
    
    // Default Values
    public const int DEFAULT_MAX_STAMINA = 100;
    public const int DEFAULT_STAMINA_COST_PER_QUESTION = 10;
    public const int DEFAULT_STAMINA_REGEN_RATE = 60; // seconds per stamina point
    public const int DEFAULT_COOLDOWN_DURATION = 300; // 5 minutes in seconds
    
    // Scoring
    public const int POINTS_PER_CORRECT_ANSWER = 10;
    public const int BONUS_POINTS_PERFECT_SCORE = 50;
    public const int BONUS_POINTS_STREAK = 5;
    
    // Tower Configuration
    public const int DEFAULT_TOWER_STAMINA_COST = 10;
    public const int DEFAULT_TOWER_HINTS = 3;
    public const int MIN_REQUIRED_SCORE = 70; // Percentage to clear a tower
    
    // Question Configuration
    public const int QUESTIONS_PER_TOWER = 10;
    public const string DEFAULT_LANGUAGE = "en";
    public const string TAGALOG_LANGUAGE = "tl";
    
    // Achievement Configuration
    public const int MAX_RECENT_ATTEMPTS = 50;
    
    // Save Data
    public const string SAVE_FILE_NAME = "nutriquest_save.json";
    public const string BACKUP_FILE_NAME = "nutriquest_save_backup.json";
    public const int SAVE_VERSION = 1;
    
    // Auto-save Configuration
    public const float AUTO_SAVE_INTERVAL = 120f; // 2 minutes (optimized for Android battery)
    
    // Network Configuration
    public const float NETWORK_CHECK_INTERVAL = 5f; // Check network every 5 seconds
    public const int NETWORK_TIMEOUT_SECONDS = 10;
    
    // UI Configuration
    public const float UI_ANIMATION_DURATION = 0.3f;
    public const float NOTIFICATION_DISPLAY_TIME = 3f;
    
    // Screen Configuration (Portrait Android)
    public const int SCREEN_WIDTH = 1080;  // Portrait width
    public const int SCREEN_HEIGHT = 1920; // Portrait height
    public const float SCREEN_ASPECT_RATIO = (float)SCREEN_HEIGHT / SCREEN_WIDTH; // 16:9 in portrait (9:16)
    
    // Sound Configuration
    public const float DEFAULT_MASTER_VOLUME = 1.0f;
    public const float DEFAULT_MUSIC_VOLUME = 0.7f;
    public const float DEFAULT_SFX_VOLUME = 0.8f;
    
    // Google Play Games (Android)
    public const string LEADERBOARD_HIGH_SCORE = ""; // Set your leaderboard ID
    public const string LEADERBOARD_TOWERS_CLEARED = ""; // Set your leaderboard ID
    
    // Resource Paths
    public const string QUESTIONS_DATA_PATH = "Questions/questions";
    public const string TOWERS_DATA_PATH = "Data/tower";
    public const string ACHIEVEMENTS_DATA_PATH = "Data/achievement";
    public const string RECIPES_DATA_PATH = "Data/recipes";
    
    // Error Messages
    public const string ERROR_NO_NETWORK = "No internet connection available";
    public const string ERROR_SAVE_FAILED = "Failed to save game data";
    public const string ERROR_LOAD_FAILED = "Failed to load game data";
    public const string ERROR_CLOUD_SAVE_UNAVAILABLE = "Cloud save is not available. Please sign in to Google Play Games.";
    
    // Success Messages
    public const string SUCCESS_SAVED = "Game saved successfully";
    public const string SUCCESS_LOADED = "Game loaded successfully";
    public const string SUCCESS_CLOUD_SYNCED = "Data synced to cloud";
    
    // Validation
    public const int MIN_USERNAME_LENGTH = 3;
    public const int MAX_USERNAME_LENGTH = 20;
    public const int MIN_PASSWORD_LENGTH = 6;
    public const int MAX_PASSWORD_LENGTH = 50;
}
