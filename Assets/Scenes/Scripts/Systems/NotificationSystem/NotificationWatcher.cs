using System;
using System.Collections.Generic;
using UnityEngine;

// =============================================================================
// NotificationWatcher  NutriQuest
//
// OPTIONAL helper. Put it on an object that is ALWAYS active in a scene (e.g.
// your Main Menu manager) and list every PlayerPrefs flag that should raise a
// notification. It checks them when the scene starts.
//
// WHY: a NotificationBadge only checks its watched flag while its own object is
// active. If a recipe button lives inside a closed panel, a parent badge (e.g.
// on the Recipes tab) would not know about the new unlock. The watcher fixes
// that, because it does not depend on any panel being open.
//
// Entries here and a "Watch PlayerPrefs Key" on a badge can safely overlap.
// =============================================================================
public class NotificationWatcher : MonoBehaviour
{
    [Serializable]
    public class Entry
    {
        [Tooltip("e.g. recipe/Tower1_Stage1")]
        public string notificationID;
        [Tooltip("PlayerPrefs int key, e.g. FirstClear_Tower1_Stage1")]
        public string watchPlayerPrefsKey;
        public int watchValue = 1;
    }

    [SerializeField] private List<Entry> entries = new List<Entry>();

    private void OnEnable() => Sync();

    public void Sync()
    {
        foreach (Entry e in entries)
        {
            if (e == null || string.IsNullOrEmpty(e.notificationID) ||
                string.IsNullOrEmpty(e.watchPlayerPrefsKey)) continue;

            NotificationService.SyncWithFlag(e.notificationID,
                PlayerPrefs.GetInt(e.watchPlayerPrefsKey, 0) == e.watchValue);
        }
    }
}
