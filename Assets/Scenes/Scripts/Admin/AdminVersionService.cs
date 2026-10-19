using UnityEngine;

/// <summary>
/// The version the game shows to the player.
///
/// There are two different things here and they must not be confused:
///   BuildVersion    - Application.version, baked into the APK by Unity.
///                    The admin panel can read it but never change it.
///   DisplayVersion  - the string shown on the title screen and in Settings.
///                    The teacher can override this so a classroom build can
///                    be labelled, for example "School Edition 2026".
///
/// ContentRevision counts how many times the teacher has saved a change to
/// the admin configuration. It is the honest replacement for "version
/// control" at runtime: there is no source control on a device, but the
/// content does have a revision number that teachers can quote.
/// </summary>
public static class AdminVersionService
{
    /// <summary>The real build version. Read only.</summary>
    public static string BuildVersion => Application.version;

    /// <summary>The version string shown to the player. Overridable.</summary>
    public static string DisplayVersion =>
        AdminContentStore.GetString(AdminGameConfig.KeyDisplayVersion, Application.version);

    public static void SetDisplayVersion(string value)
    {
        AdminContentStore.SetString(AdminGameConfig.KeyDisplayVersion, value ?? Application.version);
    }

    public static bool IsDisplayVersionOverridden =>
        AdminContentStore.HasOverride(AdminGameConfig.KeyDisplayVersion);

    /// <summary>How many times the admin configuration has been saved.</summary>
    public static int ContentRevision => AdminContentStore.ContentRevision;

    /// <summary>The label used on the title screen and in Settings.</summary>
    public static string Label
    {
        get
        {
            string version = DisplayVersion;
            if (ContentRevision > 0) return "Version " + version + " (rev " + ContentRevision + ")";
            return "Version " + version;
        }
    }
}
