using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// The runtime override layer.
///
/// Most of the game's content and tuning lives serialized inside scene assets
/// (questions in Level 1-4.unity, recipe text in Recipe 1-4.unity, timers in
/// component fields). A shipped build cannot write to those assets, so the
/// teacher/parent admin panel cannot edit them directly.
///
/// Instead this store keeps a JSON shadow document on the device. The game
/// reads every tunable through the Get* helpers here, which fall back to the
/// scene-authored default when no override exists. The admin panel writes
/// overrides; the built-in content stays untouched and is always recoverable
/// by clearing the override.
///
/// Storage notes:
///   - JsonUtility cannot serialize Dictionary, so every map is a List of
///     key/value pairs.
///   - JsonUtility cannot serialize properties or polymorphic types, so the
///     document uses plain public fields only.
///   - The file is a few kilobytes and is read/written on the main thread.
///     That is deliberate: the game is single threaded for gameplay and an
///     async read would race the first frame that asks for a value.
/// </summary>
public static class AdminContentStore
{
    public const int SchemaVersion = 2;
    public const string FileName = "admin_content.json";
    public const string TempFileName = "admin_content.json.tmp";
    public const string CorruptFileName = "admin_content.corrupt.json";
    public const int MaxAuditEntries = 500;
    public const int MaxBackups = 5;

    /// <summary>Key of the flag that lets the admin system be switched off.</summary>
    public const string KeyAdminEnabled = "admin.enabled";

    // ------------------------------------------------------------------
    // Serializable document model
    // ------------------------------------------------------------------

    [Serializable]
    public class StringEntry
    {
        public string key;
        public string value;
    }

    [Serializable]
    public class NumberEntry
    {
        public string key;
        public float value;
    }

    [Serializable]
    public class FlagEntry
    {
        public string key;
        public bool value;
    }

    /// <summary>Text replacement, keyed by the ORIGINAL text.</summary>
    [Serializable]
    public class TextEntry
    {
        public string key;
        public string value;
    }

    [Serializable]
    public class QuestionPool
    {
        public string key;
        public List<QuestionData> questions = new List<QuestionData>();
    }

    [Serializable]
    public class RecipeBlock
    {
        public string id;
        public string labelEn;
        public string labelFil;
        public string subLabelEn;
        public string subLabelFil;
        public string bodyEn;
        public string bodyFil;
    }

    [Serializable]
    public class RecipeOverride
    {
        public string id;
        public string nameEn;
        public string nameFil;
        public string descriptionEn;
        public string descriptionFil;
        public string category;
        public string unlockStageId;
        public List<RecipeBlock> blocks = new List<RecipeBlock>();
    }

    /// <summary>
    /// A piece of editable game content that is not a question or a recipe:
    /// categories, levels and rewards all use this shape.
    /// </summary>
    [Serializable]
    public class ContentEntry
    {
        public string id = "";
        public string group = "";
        public string name = "";
        public string value = "";
        public bool enabled = true;
    }

    /// <summary>One line of the audit log.</summary>
    [Serializable]
    public class AuditEntry
    {
        public string utc = "";
        public string tab = "";
        public string action = "";
        public string oldValue = "";
        public string newValue = "";
        public string detail = "";
    }

    [Serializable]
    public class Document
    {
        public int schemaVersion = SchemaVersion;
        public int contentRevision = 0;
        public string updatedUtc = "";

        public List<StringEntry> strings = new List<StringEntry>();
        public List<NumberEntry> numbers = new List<NumberEntry>();
        public List<FlagEntry> flags = new List<FlagEntry>();
        public List<TextEntry> texts = new List<TextEntry>();
        public List<QuestionPool> questionPools = new List<QuestionPool>();
        public List<RecipeOverride> recipes = new List<RecipeOverride>();
        public List<ContentEntry> content = new List<ContentEntry>();
        public List<AuditEntry> audit = new List<AuditEntry>();
    }

    // ------------------------------------------------------------------
    // State
    // ------------------------------------------------------------------

    private static Document _doc;
    private static bool _loaded;

    public static bool IsLoaded => _loaded;
    public static int ContentRevision => _doc != null ? _doc.contentRevision : 0;
    public static string UpdatedUtc => _doc != null ? _doc.updatedUtc : "";

    public static string FilePath =>
        Path.Combine(Application.persistentDataPath, FileName);

    public static string TempFilePath =>
        Path.Combine(Application.persistentDataPath, TempFileName);

    public static string BackupFolder =>
        Path.Combine(Application.persistentDataPath, "admin_backups");

    public static string ExportFolder =>
        Path.Combine(Application.persistentDataPath, "admin_exports");

    // ------------------------------------------------------------------
    // Lifecycle
    // ------------------------------------------------------------------

    public static void Load()
    {
        if (_loaded) return;
        _loaded = true;

        Document loaded = null;
        string path = FilePath;

        try
        {
            if (File.Exists(path))
            {
                string json = File.ReadAllText(path);
                if (!string.IsNullOrEmpty(json))
                {
                    loaded = JsonUtility.FromJson<Document>(json);
                    if (loaded == null)
                    {
                        QuarantineCorruptFile(path, "the file did not contain a readable admin document");
                    }
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning("[AdminContentStore] Could not read the admin config, falling back to defaults: " + e.Message);
            QuarantineCorruptFile(path, e.Message);
            loaded = null;
        }

        _doc = loaded ?? new Document();
        Normalize(_doc);
        Migrate(_doc);
    }

    /// <summary>
    /// Moves a file that could not be parsed out of the way, so the game can
    /// start from the built-in defaults without the bad file being silently
    /// overwritten by the next save.
    /// </summary>
    private static void QuarantineCorruptFile(string path, string reason)
    {
        try
        {
            if (!File.Exists(path)) return;
            string target = Path.Combine(Application.persistentDataPath, CorruptFileName);
            if (File.Exists(target)) File.Delete(target);
            File.Move(path, target);
            Debug.LogWarning("[AdminContentStore] The admin config could not be read (" + reason +
                             "). It was moved to " + CorruptFileName +
                             " and the built-in defaults are in use.");
        }
        catch (Exception e)
        {
            Debug.LogWarning("[AdminContentStore] Could not move the unreadable admin config aside: " + e.Message);
        }
    }

    /// <summary>Makes sure every list exists.</summary>
    private static void Normalize(Document d)
    {
        if (d.strings == null) d.strings = new List<StringEntry>();
        if (d.numbers == null) d.numbers = new List<NumberEntry>();
        if (d.flags == null) d.flags = new List<FlagEntry>();
        if (d.texts == null) d.texts = new List<TextEntry>();
        if (d.questionPools == null) d.questionPools = new List<QuestionPool>();
        if (d.recipes == null) d.recipes = new List<RecipeOverride>();
        if (d.content == null) d.content = new List<ContentEntry>();
        if (d.audit == null) d.audit = new List<AuditEntry>();
    }

    /// <summary>
    /// Brings an older document up to the current schema. Every step is
    /// additive, so a document written by any shipped version keeps its data.
    /// </summary>
    private static void Migrate(Document d)
    {
        if (d.schemaVersion == SchemaVersion) return;

        if (d.schemaVersion < 2)
        {
            // v1 -> v2: the audit log gained a tab and an old/new value, the
            // recipe gained a description and a category, and a new list of
            // editable content (categories, levels, rewards) was added. Old
            // entries keep their action and detail and simply have empty new
            // fields.
            for (int i = 0; i < d.audit.Count; i++)
            {
                AuditEntry e = d.audit[i];
                if (e == null) continue;
                if (e.tab == null) e.tab = "";
                if (e.oldValue == null) e.oldValue = "";
                if (e.newValue == null) e.newValue = "";
            }
        }

        d.schemaVersion = SchemaVersion;
    }

    // ------------------------------------------------------------------
    // Saving
    // ------------------------------------------------------------------

    /// <summary>
    /// Writes the document to disk. The file is written to a temp path first
    /// and then moved over the target, so a crash can never leave a partially
    /// written file behind. Bumps the content revision by default.
    /// </summary>
    public static void Save(bool bumpRevision = true)
    {
        Load();
        if (_doc == null) return;

        if (bumpRevision)
        {
            _doc.contentRevision++;
        }
        _doc.updatedUtc = DateTime.UtcNow.ToString("o");
        _doc.schemaVersion = SchemaVersion;

        string path = FilePath;
        string temp = TempFilePath;

        try
        {
            string dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);

            string json = JsonUtility.ToJson(_doc, true);

            // 1. write the new file beside the target
            File.WriteAllText(temp, json);

            // 2. keep a copy of what we are about to replace
            Backup(path);

            // 3. move it over the target in one step
            if (File.Exists(path)) File.Delete(path);
            File.Move(temp, path);
        }
        catch (Exception e)
        {
            Debug.LogError("[AdminContentStore] Failed to save the admin config: " + e.Message);
            try { if (File.Exists(temp)) File.Delete(temp); } catch { }
        }
    }

    /// <summary>Keeps a copy of the file we are about to replace.</summary>
    private static void Backup(string path)
    {
        try
        {
            if (!File.Exists(path)) return;
            string folder = BackupFolder;
            if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

            // shift the existing backups up by one and drop the oldest
            for (int i = MaxBackups - 1; i >= 1; i--)
            {
                string from = Path.Combine(folder, "admin_content." + i + ".json");
                string to = Path.Combine(folder, "admin_content." + (i + 1) + ".json");
                if (!File.Exists(from)) continue;
                // File.Move has no overwrite overload in the .NET Standard 2.1
                // profile that Unity uses, so the target is removed first.
                if (File.Exists(to)) File.Delete(to);
                File.Move(from, to);
            }

            File.Copy(path, Path.Combine(folder, "admin_content.1.json"), true);
        }
        catch (Exception e)
        {
            Debug.LogWarning("[AdminContentStore] Could not write a backup: " + e.Message);
        }
    }

    /// <summary>Lists the backup files, newest first.</summary>
    public static List<string> GetBackups()
    {
        List<string> result = new List<string>();
        try
        {
            string folder = BackupFolder;
            if (!Directory.Exists(folder)) return result;
            for (int i = 1; i <= MaxBackups; i++)
            {
                string path = Path.Combine(folder, "admin_content." + i + ".json");
                if (File.Exists(path)) result.Add(path);
            }
        }
        catch { }
        return result;
    }

    /// <summary>Restores a backup file over the live config.</summary>
    public static bool RestoreBackup(string backupPath)
    {
        try
        {
            if (!File.Exists(backupPath)) return false;
            File.Copy(backupPath, FilePath, true);
            _loaded = false;
            _doc = null;
            Load();
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError("[AdminContentStore] Could not restore the backup: " + e.Message);
            return false;
        }
    }

    /// <summary>Throws away every override and returns the game to its built-in state.</summary>
    public static void ResetAll()
    {
        Load();
        _doc = new Document();
        Save(bumpRevision: true);
    }

    // ------------------------------------------------------------------
    // String values
    // ------------------------------------------------------------------

    public static string GetString(string key, string fallback)
    {
        Load();
        if (_doc == null) return fallback;
        for (int i = 0; i < _doc.strings.Count; i++)
        {
            if (_doc.strings[i].key == key) return _doc.strings[i].value;
        }
        return fallback;
    }

    public static void SetString(string key, string value)
    {
        Load();
        for (int i = 0; i < _doc.strings.Count; i++)
        {
            if (_doc.strings[i].key == key)
            {
                _doc.strings[i].value = value;
                return;
            }
        }
        _doc.strings.Add(new StringEntry { key = key, value = value });
    }

    // ------------------------------------------------------------------
    // Numeric values
    // ------------------------------------------------------------------

    public static float GetFloat(string key, float fallback)
    {
        Load();
        if (_doc == null) return fallback;
        for (int i = 0; i < _doc.numbers.Count; i++)
        {
            if (_doc.numbers[i].key == key) return _doc.numbers[i].value;
        }
        return fallback;
    }

    public static int GetInt(string key, int fallback)
    {
        return Mathf.RoundToInt(GetFloat(key, fallback));
    }

    public static void SetFloat(string key, float value)
    {
        Load();
        for (int i = 0; i < _doc.numbers.Count; i++)
        {
            if (_doc.numbers[i].key == key)
            {
                _doc.numbers[i].value = value;
                return;
            }
        }
        _doc.numbers.Add(new NumberEntry { key = key, value = value });
    }

    public static void SetInt(string key, int value)
    {
        SetFloat(key, value);
    }

    // ------------------------------------------------------------------
    // Boolean values
    // ------------------------------------------------------------------

    public static bool GetBool(string key, bool fallback)
    {
        Load();
        if (_doc == null) return fallback;
        for (int i = 0; i < _doc.flags.Count; i++)
        {
            if (_doc.flags[i].key == key) return _doc.flags[i].value;
        }
        return fallback;
    }

    public static void SetBool(string key, bool value)
    {
        Load();
        for (int i = 0; i < _doc.flags.Count; i++)
        {
            if (_doc.flags[i].key == key)
            {
                _doc.flags[i].value = value;
                return;
            }
        }
        _doc.flags.Add(new FlagEntry { key = key, value = value });
    }

    // ------------------------------------------------------------------
    // The admin system can be switched off entirely
    // ------------------------------------------------------------------

    /// <summary>
    /// False when the teacher has turned the admin system off, for example
    /// before shipping a build to players. The entry point checks this and
    /// refuses to open the panel.
    /// </summary>
    public static bool IsAdminEnabled => GetBool(KeyAdminEnabled, true);

    public static void SetAdminEnabled(bool enabled)
    {
        SetBool(KeyAdminEnabled, enabled);
    }

    // ------------------------------------------------------------------
    // Override presence and clearing
    // ------------------------------------------------------------------

    /// <summary>True when the teacher has explicitly set this key.</summary>
    public static bool HasOverride(string key)
    {
        Load();
        if (_doc == null) return false;
        for (int i = 0; i < _doc.numbers.Count; i++)
            if (_doc.numbers[i].key == key) return true;
        for (int i = 0; i < _doc.flags.Count; i++)
            if (_doc.flags[i].key == key) return true;
        for (int i = 0; i < _doc.strings.Count; i++)
            if (_doc.strings[i].key == key) return true;
        return false;
    }

    /// <summary>Removes an override so the built-in default applies again.</summary>
    public static void ClearKey(string key)
    {
        Load();
        if (_doc == null) return;
        _doc.numbers.RemoveAll(e => e.key == key);
        _doc.flags.RemoveAll(e => e.key == key);
        _doc.strings.RemoveAll(e => e.key == key);
    }

    /// <summary>Removes every override whose key starts with the given prefix.</summary>
    public static int ClearPrefix(string prefix)
    {
        Load();
        if (_doc == null || string.IsNullOrEmpty(prefix)) return 0;
        int before = _doc.numbers.Count + _doc.flags.Count + _doc.strings.Count;
        _doc.numbers.RemoveAll(e => e.key != null && e.key.StartsWith(prefix));
        _doc.flags.RemoveAll(e => e.key != null && e.key.StartsWith(prefix));
        _doc.strings.RemoveAll(e => e.key != null && e.key.StartsWith(prefix));
        return before - (_doc.numbers.Count + _doc.flags.Count + _doc.strings.Count);
    }

    // ------------------------------------------------------------------
    // Text replacement (keyed by the original text)
    // ------------------------------------------------------------------

    public static string GetText(string original)
    {
        Load();
        if (_doc == null || string.IsNullOrEmpty(original)) return original;
        for (int i = 0; i < _doc.texts.Count; i++)
        {
            if (_doc.texts[i].key == original) return _doc.texts[i].value;
        }

        // Fall back to a whitespace-insensitive match. A TextMeshPro string
        // read at runtime can differ from the one that was serialised (a
        // trailing newline, for example), and the override has to still land.
        string trimmed = original.Trim();
        for (int i = 0; i < _doc.texts.Count; i++)
        {
            if (_doc.texts[i].key != null && _doc.texts[i].key.Trim() == trimmed)
            {
                return _doc.texts[i].value;
            }
        }
        return original;
    }

    public static bool HasTextOverride(string original)
    {
        Load();
        if (_doc == null || string.IsNullOrEmpty(original)) return false;
        for (int i = 0; i < _doc.texts.Count; i++)
        {
            if (_doc.texts[i].key == original) return true;
        }
        return false;
    }

    public static void SetText(string original, string replacement)
    {
        Load();
        if (string.IsNullOrEmpty(original)) return;
        if (replacement == original)
        {
            ClearText(original);
            return;
        }
        for (int i = 0; i < _doc.texts.Count; i++)
        {
            if (_doc.texts[i].key == original)
            {
                _doc.texts[i].value = replacement;
                return;
            }
        }
        _doc.texts.Add(new TextEntry { key = original, value = replacement });
    }

    /// <summary>
    /// Given the text that is currently on screen, returns the ORIGINAL string
    /// that an override replaced. The scene already shows the replaced text
    /// after the applier has run, so without this the second edit of the
    /// same string would be keyed off the replacement and stop matching the
    /// scene. Returns the input when nothing matches.
    /// </summary>
    public static string GetOriginalText(string current)
    {
        Load();
        if (_doc == null || string.IsNullOrEmpty(current)) return current;
        for (int i = 0; i < _doc.texts.Count; i++)
        {
            if (_doc.texts[i].value == current) return _doc.texts[i].key;
        }
        return current;
    }

    public static void ClearText(string original)
    {
        Load();
        if (_doc == null) return;
        _doc.texts.RemoveAll(e => e.key == original);
    }

    /// <summary>Removes every text override, so all scene text goes back to default.</summary>
    public static int ClearAllText()
    {
        Load();
        if (_doc == null) return 0;
        int count = _doc.texts.Count;
        _doc.texts.Clear();
        return count;
    }

    /// <summary>All text replacements currently in effect, for the admin UI.</summary>
    public static List<TextEntry> GetTextOverrides()
    {
        Load();
        return _doc != null ? new List<TextEntry>(_doc.texts) : new List<TextEntry>();
    }

    // ------------------------------------------------------------------
    // Question pools
    // ------------------------------------------------------------------

    public static List<QuestionData> GetQuestionPool(string key)
    {
        Load();
        if (_doc == null) return null;
        for (int i = 0; i < _doc.questionPools.Count; i++)
        {
            if (_doc.questionPools[i].key == key) return _doc.questionPools[i].questions;
        }
        return null;
    }

    public static bool HasQuestionPool(string key)
    {
        return GetQuestionPool(key) != null;
    }

    public static void SetQuestionPool(string key, List<QuestionData> questions)
    {
        Load();
        for (int i = 0; i < _doc.questionPools.Count; i++)
        {
            if (_doc.questionPools[i].key == key)
            {
                _doc.questionPools[i].questions = questions ?? new List<QuestionData>();
                return;
            }
        }
        _doc.questionPools.Add(new QuestionPool { key = key, questions = questions ?? new List<QuestionData>() });
    }

    public static void ClearQuestionPool(string key)
    {
        Load();
        if (_doc == null) return;
        _doc.questionPools.RemoveAll(p => p.key == key);
    }

    // ------------------------------------------------------------------
    // Recipes
    // ------------------------------------------------------------------

    public static RecipeOverride GetRecipe(string id)
    {
        Load();
        if (_doc == null) return null;
        for (int i = 0; i < _doc.recipes.Count; i++)
        {
            if (_doc.recipes[i].id == id) return _doc.recipes[i];
        }
        return null;
    }

    public static void SetRecipe(RecipeOverride recipe)
    {
        Load();
        if (recipe == null) return;
        for (int i = 0; i < _doc.recipes.Count; i++)
        {
            if (_doc.recipes[i].id == recipe.id)
            {
                _doc.recipes[i] = recipe;
                return;
            }
        }
        _doc.recipes.Add(recipe);
    }

    /// <summary>Removes a recipe override so the built-in recipe applies again.</summary>
    public static void RemoveRecipe(string id)
    {
        Load();
        if (_doc == null) return;
        _doc.recipes.RemoveAll(r => r.id == id);
    }

    public static List<RecipeOverride> GetRecipes()
    {
        Load();
        return _doc != null ? new List<RecipeOverride>(_doc.recipes) : new List<RecipeOverride>();
    }

    // ------------------------------------------------------------------
    // Editable content (categories, levels, rewards)
    // ------------------------------------------------------------------

    public static List<ContentEntry> GetContent(string group)
    {
        Load();
        List<ContentEntry> result = new List<ContentEntry>();
        if (_doc == null) return result;
        for (int i = 0; i < _doc.content.Count; i++)
        {
            if (_doc.content[i] != null && _doc.content[i].group == group) result.Add(_doc.content[i]);
        }
        return result;
    }

    public static ContentEntry GetContentEntry(string group, string id)
    {
        Load();
        if (_doc == null) return null;
        for (int i = 0; i < _doc.content.Count; i++)
        {
            if (_doc.content[i] != null && _doc.content[i].group == group && _doc.content[i].id == id)
            {
                return _doc.content[i];
            }
        }
        return null;
    }

    public static void SetContentEntry(ContentEntry entry)
    {
        Load();
        if (entry == null) return;
        for (int i = 0; i < _doc.content.Count; i++)
        {
            if (_doc.content[i] != null && _doc.content[i].group == entry.group && _doc.content[i].id == entry.id)
            {
                _doc.content[i] = entry;
                return;
            }
        }
        _doc.content.Add(entry);
    }

    public static void RemoveContentEntry(string group, string id)
    {
        Load();
        if (_doc == null) return;
        _doc.content.RemoveAll(c => c != null && c.group == group && c.id == id);
    }

    // ------------------------------------------------------------------
    // Audit log
    // ------------------------------------------------------------------

    /// <summary>Appends a line to the audit log without a tab or old/new value.</summary>
    public static void AddAudit(string action, string detail)
    {
        AddAudit("", action, "", "", detail);
    }

    /// <summary>
    /// Appends a line to the audit log, recording which tab made the change
    /// and what the value was before and after.
    /// </summary>
    public static void AddAudit(string tab, string action, string oldValue, string newValue, string detail)
    {
        Load();
        if (_doc == null) return;
        _doc.audit.Add(new AuditEntry
        {
            utc = DateTime.UtcNow.ToString("o"),
            tab = tab ?? "",
            action = action ?? "",
            oldValue = oldValue ?? "",
            newValue = newValue ?? "",
            detail = detail ?? ""
        });
        while (_doc.audit.Count > MaxAuditEntries)
        {
            _doc.audit.RemoveAt(0);
        }
    }

    public static List<AuditEntry> GetAudit()
    {
        Load();
        return _doc != null ? new List<AuditEntry>(_doc.audit) : new List<AuditEntry>();
    }

    /// <summary>Empties the audit log. The log itself is not an override.</summary>
    public static void ClearAudit()
    {
        Load();
        if (_doc == null) return;
        _doc.audit.Clear();
        Save(bumpRevision: false);
    }

    // ------------------------------------------------------------------
    // Export / import
    // ------------------------------------------------------------------

    public static string ExportJson()
    {
        Load();
        return JsonUtility.ToJson(_doc, true);
    }

    /// <summary>
    /// The live document. Used to build a patch, which copies the parts it
    /// needs. Callers must not modify what they get back.
    /// </summary>
    public static Document ExportDocument()
    {
        Load();
        return _doc;
    }

    /// <summary>
    /// Writes the current configuration to a file in the export folder and
    /// returns its path, so it can be copied off the device.
    /// </summary>
    public static string ExportToFile(string fileName = null)
    {
        try
        {
            string folder = ExportFolder;
            if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

            string name = string.IsNullOrEmpty(fileName)
                ? "nutriquest_admin_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".json"
                : fileName;

            string path = Path.Combine(folder, name);
            File.WriteAllText(path, ExportJson());
            Debug.Log("[AdminContentStore] Exported the admin config to " + path);
            return path;
        }
        catch (Exception e)
        {
            Debug.LogError("[AdminContentStore] Could not export the admin config: " + e.Message);
            return null;
        }
    }

    /// <summary>Lists the exported configuration files, newest first.</summary>
    public static List<string> GetExports()
    {
        List<string> result = new List<string>();
        try
        {
            string folder = ExportFolder;
            if (!Directory.Exists(folder)) return result;
            string[] files = Directory.GetFiles(folder, "*.json");
            Array.Sort(files, (a, b) => File.GetLastWriteTimeUtc(b).CompareTo(File.GetLastWriteTimeUtc(a)));
            result.AddRange(files);
        }
        catch { }
        return result;
    }

    /// <summary>
    /// Merges an exported document over the current one. Unknown keys are kept,
    /// matching entries are replaced. Returns false if the payload is not a
    /// valid admin document.
    /// </summary>
    public static bool ImportJson(string json, out string error)
    {
        error = "";
        if (string.IsNullOrWhiteSpace(json))
        {
            error = "Empty configuration.";
            return false;
        }

        Document incoming;
        try
        {
            incoming = JsonUtility.FromJson<Document>(json);
        }
        catch (Exception e)
        {
            error = "Not a valid admin configuration: " + e.Message;
            return false;
        }

        if (incoming == null)
        {
            error = "Not a valid admin configuration.";
            return false;
        }

        Normalize(incoming);
        Load();

        // Merge by key so a partial export never wipes unrelated settings.
        MergeStringEntries(_doc.strings, incoming.strings);
        MergeNumberEntries(_doc.numbers, incoming.numbers);
        MergeFlagEntries(_doc.flags, incoming.flags);
        MergeTextEntries(_doc.texts, incoming.texts);
        MergeQuestionPools(_doc.questionPools, incoming.questionPools);
        MergeRecipes(_doc.recipes, incoming.recipes);
        MergeContentEntries(_doc.content, incoming.content);

        return true;
    }

    /// <summary>
    /// Merges a document over the current one. Used by the patch system,
    /// which already has a parsed document in hand.
    /// </summary>
    public static bool ImportDocument(Document incoming, out string error)
    {
        error = "";
        if (incoming == null)
        {
            error = "Not a valid admin configuration.";
            return false;
        }

        Normalize(incoming);
        Load();

        MergeStringEntries(_doc.strings, incoming.strings);
        MergeNumberEntries(_doc.numbers, incoming.numbers);
        MergeFlagEntries(_doc.flags, incoming.flags);
        MergeTextEntries(_doc.texts, incoming.texts);
        MergeQuestionPools(_doc.questionPools, incoming.questionPools);
        MergeRecipes(_doc.recipes, incoming.recipes);
        MergeContentEntries(_doc.content, incoming.content);

        return true;
    }

    private static void MergeContentEntries(List<ContentEntry> into, List<ContentEntry> from)
    {
        if (from == null) return;
        for (int i = 0; i < from.Count; i++)
        {
            ContentEntry s = from[i];
            if (s == null || string.IsNullOrEmpty(s.id)) continue;
            ContentEntry existing = into.Find(e => e != null && e.group == s.group && e.id == s.id);
            if (existing != null) into[into.IndexOf(existing)] = s;
            else into.Add(s);
        }
    }

    private static void MergeStringEntries(List<StringEntry> into, List<StringEntry> from)
    {
        if (from == null) return;
        for (int i = 0; i < from.Count; i++)
        {
            StringEntry s = from[i];
            if (s == null || string.IsNullOrEmpty(s.key)) continue;
            StringEntry existing = into.Find(e => e.key == s.key);
            if (existing != null) existing.value = s.value;
            else into.Add(new StringEntry { key = s.key, value = s.value });
        }
    }

    private static void MergeNumberEntries(List<NumberEntry> into, List<NumberEntry> from)
    {
        if (from == null) return;
        for (int i = 0; i < from.Count; i++)
        {
            NumberEntry s = from[i];
            if (s == null || string.IsNullOrEmpty(s.key)) continue;
            NumberEntry existing = into.Find(e => e.key == s.key);
            if (existing != null) existing.value = s.value;
            else into.Add(new NumberEntry { key = s.key, value = s.value });
        }
    }

    private static void MergeFlagEntries(List<FlagEntry> into, List<FlagEntry> from)
    {
        if (from == null) return;
        for (int i = 0; i < from.Count; i++)
        {
            FlagEntry s = from[i];
            if (s == null || string.IsNullOrEmpty(s.key)) continue;
            FlagEntry existing = into.Find(e => e.key == s.key);
            if (existing != null) existing.value = s.value;
            else into.Add(new FlagEntry { key = s.key, value = s.value });
        }
    }

    private static void MergeTextEntries(List<TextEntry> into, List<TextEntry> from)
    {
        if (from == null) return;
        for (int i = 0; i < from.Count; i++)
        {
            TextEntry s = from[i];
            if (s == null || string.IsNullOrEmpty(s.key)) continue;
            TextEntry existing = into.Find(e => e.key == s.key);
            if (existing != null) existing.value = s.value;
            else into.Add(new TextEntry { key = s.key, value = s.value });
        }
    }

    private static void MergeQuestionPools(List<QuestionPool> into, List<QuestionPool> from)
    {
        if (from == null) return;
        for (int i = 0; i < from.Count; i++)
        {
            QuestionPool s = from[i];
            if (s == null || string.IsNullOrEmpty(s.key)) continue;
            QuestionPool existing = into.Find(p => p.key == s.key);
            if (existing != null) existing.questions = s.questions;
            else into.Add(new QuestionPool { key = s.key, questions = s.questions });
        }
    }

    private static void MergeRecipes(List<RecipeOverride> into, List<RecipeOverride> from)
    {
        if (from == null) return;
        for (int i = 0; i < from.Count; i++)
        {
            RecipeOverride s = from[i];
            if (s == null || string.IsNullOrEmpty(s.id)) continue;
            RecipeOverride existing = into.Find(r => r.id == s.id);
            if (existing != null) existing.blocks = s.blocks;
            else into.Add(s);
        }
    }
}
