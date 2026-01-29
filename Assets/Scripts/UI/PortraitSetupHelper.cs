using UnityEngine;

/// <summary>
/// Helper script to ensure portrait orientation and UI setup.
/// Attach to a GameObject in Bootstrap scene to auto-configure portrait settings.
/// </summary>
public class PortraitSetupHelper : MonoBehaviour
{
    [Header("Portrait Configuration")]
    [SerializeField] private bool forcePortraitOnStart = true;
    [SerializeField] private int targetWidth = Constant.SCREEN_WIDTH;
    [SerializeField] private int targetHeight = Constant.SCREEN_HEIGHT;
    
    private void Start()
    {
        if (forcePortraitOnStart)
        {
            ConfigurePortrait();
        }
    }
    
    /// <summary>
    /// Configures the game for portrait orientation
    /// </summary>
    public void ConfigurePortrait()
    {
        // Set screen orientation (runtime)
        Screen.orientation = ScreenOrientation.Portrait;
        Screen.autorotateToPortrait = true;
        Screen.autorotateToPortraitUpsideDown = false;
        Screen.autorotateToLandscapeLeft = false;
        Screen.autorotateToLandscapeRight = false;
        
        Debug.Log($"Portrait mode configured: {targetWidth}x{targetHeight}");
    }
    
    /// <summary>
    /// Gets the current screen dimensions
    /// </summary>
    public Vector2 GetScreenSize()
    {
        return new Vector2(Screen.width, Screen.height);
    }
    
    /// <summary>
    /// Checks if device is in portrait orientation
    /// </summary>
    public bool IsPortrait()
    {
        return Screen.width < Screen.height;
    }
}
