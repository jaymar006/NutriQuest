using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The built-in question catalogue.
///
/// The questions used to be serialized inside Level 1-4.unity as the
/// questionPool list on the QuestionGeneratorBeta component. A shipped build
/// cannot write to those assets, so the teacher/parent admin panel edits an
/// override pool instead.
///
/// The built-in pool was extracted into Assets/Resources/Data/questions.json
/// (see Tools/build_question_catalog.ps1). This class loads it and merges the
/// teacher's override over it.
///
/// QuestionGeneratorBeta reads the effective pool through
/// AdminQuestionCatalog.GetEffective so the game uses the teacher's version
/// when one exists and the built-in one otherwise.
/// </summary>
public static class AdminQuestionCatalog
{
    public const string ResourcePath = "Data/questions";

    [Serializable]
    private class PoolEntry
    {
        public string levelKey;
        public string stageId;
        public List<QuestionData> questions = new List<QuestionData>();
    }

    [Serializable]
    private class CatalogDocument
    {
        public int schemaVersion = 1;
        public List<PoolEntry> pools = new List<PoolEntry>();
    }

    private static Dictionary<string, List<QuestionData>> _builtIn;
    private static bool _loaded;

    public static bool IsLoaded => _loaded;

    /// <summary>The built-in pool for a level, or null when the level is unknown.</summary>
    public static List<QuestionData> GetBuiltIn(string levelKey)
    {
        Load();
        if (_builtIn == null) return null;
        List<QuestionData> pool;
        return _builtIn.TryGetValue(levelKey, out pool) ? pool : null;
    }

    /// <summary>
    /// The pool the game should use: the teacher's override when one exists,
    /// otherwise the built-in pool.
    /// </summary>
    public static List<QuestionData> GetEffective(string levelKey)
    {
        List<QuestionData> overridden = AdminContentStore.GetQuestionPool(levelKey);
        if (overridden != null) return overridden;
        return GetBuiltIn(levelKey);
    }

    /// <summary>Creates an override seeded from the built-in pool.</summary>
    public static List<QuestionData> SeedOverride(string levelKey)
    {
        List<QuestionData> builtIn = GetBuiltIn(levelKey);
        if (builtIn == null) return null;

        List<QuestionData> seeded = new List<QuestionData>();
        for (int i = 0; i < builtIn.Count; i++)
        {
            QuestionData src = builtIn[i];
            if (src == null) continue;
            seeded.Add(new QuestionData
            {
                question = src.question,
                answerA = src.answerA,
                answerB = src.answerB,
                answerC = src.answerC,
                answerD = src.answerD,
                correctAnswer = src.correctAnswer,
                questionFilipino = src.questionFilipino,
                answerAFilipino = src.answerAFilipino,
                answerBFilipino = src.answerBFilipino,
                answerCFilipino = src.answerCFilipino,
                answerDFilipino = src.answerDFilipino,
            });
        }

        AdminContentStore.SetQuestionPool(levelKey, seeded);
        return seeded;
    }

    /// <summary>Removes the override so the built-in pool is used again.</summary>
    public static void ClearOverride(string levelKey)
    {
        AdminContentStore.ClearQuestionPool(levelKey);
    }

    private static void Load()
    {
        if (_loaded) return;
        _loaded = true;

        Dictionary<string, List<QuestionData>> map = new Dictionary<string, List<QuestionData>>();

        try
        {
            TextAsset asset = Resources.Load<TextAsset>(ResourcePath);
            if (asset == null)
            {
                Debug.LogWarning("[AdminQuestionCatalog] Resources/" + ResourcePath +
                                 ".json not found. Questions cannot be edited.");
            }
            else if (!string.IsNullOrWhiteSpace(asset.text))
            {
                CatalogDocument doc = JsonUtility.FromJson<CatalogDocument>(asset.text);
                if (doc != null && doc.pools != null)
                {
                    for (int i = 0; i < doc.pools.Count; i++)
                    {
                        PoolEntry pool = doc.pools[i];
                        if (pool == null || string.IsNullOrEmpty(pool.levelKey)) continue;
                        map[pool.levelKey] = pool.questions ?? new List<QuestionData>();
                    }
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning("[AdminQuestionCatalog] Could not parse the question catalogue: " + e.Message);
        }

        _builtIn = map;
    }
}
