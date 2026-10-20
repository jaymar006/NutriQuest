using System;
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
/// content does have a revision number that teachers can quote, and the date
/// of the last change is shown next to it.
/// </summary>
public static class AdminVersionService
{
    /// <summary>The real build version. Read only.</summary>
    public static string BuildVersion => Application.version;

    /// <summary>The version string shown to the player. Overridable.</summary>
    public static string DisplayVersion =>
        AdminContentStore.GetString(AdminGameConfig.KeyDisplayVersion, Application.version);

    /// <summary>Raised when the displayed version changes, so labels can refresh.</summary>
    public static event Action OnDisplayVersionChanged;

    public static void SetDisplayVersion(string value)
    {
        AdminContentStore.SetString(AdminGameConfig.KeyDisplayVersion, value ?? Application.version);
        OnDisplayVersionChanged?.Invoke();
    }

    public static bool IsDisplayVersionOverridden =>
        AdminContentStore.HasOverride(AdminGameConfig.KeyDisplayVersion);

    /// <summary>How many times the admin configuration has been saved.</summary>
    public static int ContentRevision => AdminContentStore.ContentRevision;

    /// <summary>The short label, for example "v1.2.0 | content 14".</summary>
    public static string Label
    {
        get
        {
            string version = DisplayVersion;
            if (string.IsNullOrEmpty(version)) version = BuildVersion;
            if (!version.StartsWith("v")) version = "v" + version;
            return version + " | content " + ContentRevision;
        }
    }

    /// <summary>
    /// The label with the date of the last content change underneath it, for
    /// example "v1.2.0 | content 14" and "2026-10-21 03:12".
    /// </summary>
    public static string LabelWithDate
    {
        get
        {
            string label = Label;
            string date = LastChangedShort;
            return string.IsNullOrEmpty(date) ? label : label + "\n" + date;
        }
    }

    /// <summary>When the admin content was last changed, in local time.</summary>
    public static string LastChangedShort
    {
        get
        {
            string utc = AdminContentStore.UpdatedUtc;
            if (string.IsNullOrEmpty(utc)) return "";
            if (!DateTime.TryParse(utc, out DateTime parsed)) return "";
            return parsed.ToLocalTime().ToString("yyyy-MM-dd HH:mm");
        }
    }
}
