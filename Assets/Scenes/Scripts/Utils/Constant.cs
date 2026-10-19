/// <summary>
/// Game constants used throughout the application.
///
/// Only values that something actually reads live here. Several constants
/// that used to sit in this file (stamina costs, point bonuses, error and
/// success message strings, the old "tl" language code) were never used by
/// any system and were removed, so that the live values are easy to find.
///
/// The teacher's runtime settings (timers, difficulty, rewards, content) are
/// NOT here: they live in the admin panel's override layer, see
/// AdminContentStore and AdminGameConfig.
/// </summary>
public static class Constant
{
    // Game Configuration
    public const string GAME_NAME = "NutriQuest";
    public const string GAME_VERSION = "1.0.0";

    // Default Values
    public const int DEFAULT_MAX_STAMINA = 100;

    // Network Configuration
    public const float NETWORK_CHECK_INTERVAL = 5f; // Check network every 5 seconds
    public const int NETWORK_TIMEOUT_SECONDS = 10;

    // Screen Configuration (Portrait Android)
    public const int SCREEN_WIDTH = 1080;  // Portrait width
    public const int SCREEN_HEIGHT = 1920; // Portrait height
    public const float SCREEN_ASPECT_RATIO = (float)SCREEN_HEIGHT / SCREEN_WIDTH; // 16:9 in portrait (9:16)

    // Save Data
    public const string SAVE_FILE_NAME = "nutriquest_save.json";
    public const string BACKUP_FILE_NAME = "nutriquest_save_backup.json";
    public const int SAVE_VERSION = 1;

    // Resource Paths
    public const string QUESTIONS_DATA_PATH = "Questions/questions";
    public const string TOWERS_DATA_PATH = "Data/tower";
    public const string ACHIEVEMENTS_DATA_PATH = "Data/achievement";
    public const string RECIPES_DATA_PATH = "Data/recipes";

    // Validation
    public const int MIN_USERNAME_LENGTH = 3;
    public const int MAX_USERNAME_LENGTH = 20;
    public const int MIN_PASSWORD_LENGTH = 6;
    public const int MAX_PASSWORD_LENGTH = 50;
}
