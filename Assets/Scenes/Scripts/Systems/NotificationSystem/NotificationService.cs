using System;
using System.Collections.Generic;
using UnityEngine;

// =============================================================================
// NotificationService  NutriQuest
//
// Universal "new / unseen" indicator store. Any feature can use it:
//
//     NotificationService.Raise("recipe/Tower1_Stage1");   // show the bubble
//     NotificationService.Clear("recipe/Tower1_Stage1");   // hide it
//     NotificationService.IsActive("shop/new_skin");
//     NotificationService.CountActive("recipe/");          // everything under a prefix
//
// State is saved in PlayerPrefs, so it survives scene changes and restarts.
// It is a static class: nothing to place in a scene, nothing to initialise.
//
// ID RULES
//   - Use a "category/name" style so parent badges can group by prefix,
//     e.g. "recipe/Tower1_Stage1", "shop/skin_cat", "update/v1.2".
//   - Do not use the '|' character in an ID.
//
// PlayerPrefs keys written: Notif_Active_<id>, Notif_Triggered_<id>, Notif_Index
// =============================================================================
public static class NotificationService
{
    private const string ACTIVE_PREFIX = "Notif_Active_";
    private const string TRIGGERED_PREFIX = "Notif_Triggered_";
    private const string INDEX_KEY = "Notif_Index";
    private const char SEPARATOR = '|';

    /// <summary>Fires with the ID whose state changed ("" when many changed).</summary>
    public static event Action<string> OnChanged;

    public static bool IsActive(string id)
    {
        return !string.IsNullOrEmpty(id) && PlayerPrefs.GetInt(ACTIVE_PREFIX + id, 0) == 1;
    }

    public static void Raise(string id)
    {
        if (string.IsNullOrEmpty(id) || IsActive(id)) return;
        Register(id);
        PlayerPrefs.SetInt(ACTIVE_PREFIX + id, 1);
        PlayerPrefs.Save();
        OnChanged?.Invoke(id);
    }

    public static void Clear(string id)
    {
        if (!IsActive(id)) return;
        PlayerPrefs.SetInt(ACTIVE_PREFIX + id, 0);
        PlayerPrefs.Save();
        OnChanged?.Invoke(id);
    }

    /// <summary>Clears every notification whose ID starts with prefix ("" = all).</summary>
    public static void ClearAll(string prefix = "")
    {
        foreach (string id in GetIndex())
            if (id.StartsWith(prefix, StringComparison.Ordinal))
                PlayerPrefs.SetInt(ACTIVE_PREFIX + id, 0);
        PlayerPrefs.Save();
        OnChanged?.Invoke("");
    }

    public static int CountActive(string prefix = "")
    {
        int count = 0;
        foreach (string id in GetIndex())
            if (id.StartsWith(prefix, StringComparison.Ordinal) && IsActive(id))
                count++;
        return count;
    }

    public static bool AnyActive(string prefix = "") => CountActive(prefix) > 0;

    /// <summary>
    /// Ties a notification to an on/off flag (e.g. "recipe unlocked").
    /// Raises ONCE when the flag turns on. If the flag later turns off
    /// (progress reset), the notification re-arms for the next time.
    /// </summary>
    public static void SyncWithFlag(string id, bool flagOn)
    {
        if (string.IsNullOrEmpty(id)) return;

        string triggeredKey = TRIGGERED_PREFIX + id;
        bool triggered = PlayerPrefs.GetInt(triggeredKey, 0) == 1;

        if (flagOn && !triggered)
        {
            PlayerPrefs.SetInt(triggeredKey, 1);
            Raise(id);
            PlayerPrefs.Save();
        }
        else if (!flagOn && triggered)
        {
            PlayerPrefs.DeleteKey(triggeredKey);
            Clear(id);
            PlayerPrefs.Save();
        }
    }

    // --- internal: PlayerPrefs cannot list keys, so we keep our own index -----
    private static string[] GetIndex()
    {
        string raw = PlayerPrefs.GetString(INDEX_KEY, "");
        return raw.Split(new[] { SEPARATOR }, StringSplitOptions.RemoveEmptyEntries);
    }

    private static void Register(string id)
    {
        List<string> ids = new List<string>(GetIndex());
        if (ids.Contains(id)) return;
        ids.Add(id);
        PlayerPrefs.SetString(INDEX_KEY, string.Join(SEPARATOR.ToString(), ids));
    }
}
