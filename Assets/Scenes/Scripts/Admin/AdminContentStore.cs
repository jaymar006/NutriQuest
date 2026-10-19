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
    public const int SchemaVersion = 1;
    public const string FileName = "admin_content.json";
    public const int MaxAuditEntries = 200;

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
        public string unlockStageId;
        public List<RecipeBlock> blocks = new List<RecipeBlock>();
    }

    [Serializable]
    public class AuditEntry
    {
        public string utc;
        public string action;
        public string detail;
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

    // ------------------------------------------------------------------
    // Lifecycle
    // ------------------------------------------------------------------

    public static void Load()
    {
        if (_loaded) return;
        _loaded = true;

        Document loaded = null;
        try
        {
            string path = FilePath;
            if (File.Exists(path))
            {
                string json = File.ReadAllText(path);
                if (!string.IsNullOrEmpty(json))
                {
                    loaded = JsonUtility.FromJson<Document>(json);
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning("[AdminContentStore] Could not read admin config, starting fresh: " + e.Message);
            loaded = null;
        }

        _doc = loaded ?? new Document();
        Normalize(_doc);
    }

    /// <summary>Makes sure every list exists and the schema version matches.</summary>
    private static void Normalize(Document d)
    {
        if (d.strings == null) d.strings = new List<StringEntry>();
        if (d.numbers == null) d.numbers = new List<NumberEntry>();
        if (d.flags == null) d.flags = new List<FlagEntry>();
        if (d.texts == null) d.texts = new List<TextEntry>();
        if (d.questionPools == null) d.questionPools = new List<QuestionPool>();
        if (d.recipes == null) d.recipes = new List<RecipeOverride>();
        if (d.audit == null) d.audit = new List<AuditEntry>();

        if (d.schemaVersion != SchemaVersion)
        {
            Debug.LogWarning("[AdminContentStore] Config schema " + d.schemaVersion +
                             " differs from " + SchemaVersion + "; unknown fields are ignored.");
            d.schemaVersion = SchemaVersion;
        }
    }

    /// <summary>Writes the document to disk. Bumps the content revision by default.</summary>
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

        try
        {
            string path = FilePath;
            string dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);
            File.WriteAllText(path, JsonUtility.ToJson(_doc, true));
        }
        catch (Exception e)
        {
            Debug.LogError("[AdminContentStore] Failed to save admin config: " + e.Message);
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

    public static void ClearText(string original)
    {
        Load();
        if (_doc == null) return;
        _doc.texts.RemoveAll(e => e.key == original);
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

    public static void ClearRecipes()
    {
        Load();
        if (_doc == null) return;
        _doc.recipes.Clear();
    }

    // ------------------------------------------------------------------
    // Audit log
    // ------------------------------------------------------------------

    public static void AddAudit(string action, string detail)
    {
        Load();
        if (_doc == null) return;
        _doc.audit.Add(new AuditEntry
        {
            utc = DateTime.UtcNow.ToString("o"),
            action = action ?? "",
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

    // ------------------------------------------------------------------
    // Export / import
    // ------------------------------------------------------------------

    public static string ExportJson()
    {
        Load();
        return JsonUtility.ToJson(_doc, true);
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

        return true;
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
