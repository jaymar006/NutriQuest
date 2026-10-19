using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The built-in recipe catalogue.
///
/// The recipe text used to be hardcoded as bilingual TextMeshPro strings
/// inside Recipe 1-4.unity, which meant the teacher/parent admin panel had
/// nothing to edit. The content was extracted into
/// Assets/Resources/Data/recipes.json (see Tools/build_recipe_catalog.ps1),
/// which revives the previously empty Data/recipes.json file.
///
/// The panel edits an override; this class merges that override over the
/// built-in catalogue so the effective recipe is always well defined.
///
/// The scene text is still the source of truth for what is displayed. The
/// override is applied by AdminTextOverrideApplier, which swaps any
/// TextMeshPro string that matches a catalogue entry. That keeps the scenes
/// untouched while still letting the teacher change the wording.
/// </summary>
public static class AdminRecipeCatalog
{
    [Serializable]
    private class CatalogDocument
    {
        public int schemaVersion = 1;
        public List<AdminContentStore.RecipeOverride> recipes = new List<AdminContentStore.RecipeOverride>();
    }

    private static List<AdminContentStore.RecipeOverride> _builtIn;
    private static bool _loaded;

    public static bool IsLoaded => _loaded;

    /// <summary>The built-in recipe for an id, or null when it is unknown.</summary>
    public static AdminContentStore.RecipeOverride GetBuiltIn(string id)
    {
        Load();
        if (_builtIn == null) return null;
        for (int i = 0; i < _builtIn.Count; i++)
        {
            if (_builtIn[i] != null && _builtIn[i].id == id) return _builtIn[i];
        }
        return null;
    }

    public static List<AdminContentStore.RecipeOverride> GetAllBuiltIn()
    {
        Load();
        return _builtIn != null ? new List<AdminContentStore.RecipeOverride>(_builtIn) : new List<AdminContentStore.RecipeOverride>();
    }

    /// <summary>
    /// The recipe as the game should show it: the teacher's override when one
    /// exists, otherwise the built-in catalogue entry.
    /// </summary>
    public static AdminContentStore.RecipeOverride GetEffective(string id)
    {
        AdminContentStore.RecipeOverride overrideRecipe = AdminContentStore.GetRecipe(id);
        if (overrideRecipe != null) return overrideRecipe;
        return GetBuiltIn(id);
    }

    /// <summary>
    /// Creates an override seeded from the built-in recipe so the teacher can
    /// edit it. Returns the override that now exists.
    /// </summary>
    public static AdminContentStore.RecipeOverride SeedOverride(string id)
    {
        AdminContentStore.RecipeOverride builtIn = GetBuiltIn(id);
        if (builtIn == null) return null;

        AdminContentStore.RecipeOverride seeded = new AdminContentStore.RecipeOverride
        {
            id = builtIn.id,
            nameEn = builtIn.nameEn,
            nameFil = builtIn.nameFil,
            unlockStageId = builtIn.unlockStageId,
            blocks = new List<AdminContentStore.RecipeBlock>()
        };

        if (builtIn.blocks != null)
        {
            for (int i = 0; i < builtIn.blocks.Count; i++)
            {
                AdminContentStore.RecipeBlock b = builtIn.blocks[i];
                if (b == null) continue;
                seeded.blocks.Add(new AdminContentStore.RecipeBlock
                {
                    id = b.id,
                    labelEn = b.labelEn,
                    labelFil = b.labelFil,
                    subLabelEn = b.subLabelEn,
                    subLabelFil = b.subLabelFil,
                    bodyEn = b.bodyEn,
                    bodyFil = b.bodyFil,
                });
            }
        }

        AdminContentStore.SetRecipe(seeded);
        return seeded;
    }

    /// <summary>Drops the override so the built-in recipe shows again.</summary>
    public static void ClearOverride(string id)
    {
        AdminContentStore.RemoveRecipe(id);
    }

    private static void Load()
    {
        if (_loaded) return;
        _loaded = true;

        List<AdminContentStore.RecipeOverride> parsed = new List<AdminContentStore.RecipeOverride>();

        try
        {
            TextAsset asset = Resources.Load<TextAsset>(Constant.RECIPES_DATA_PATH);
            if (asset == null)
            {
                Debug.LogWarning("[AdminRecipeCatalog] Resources/" + Constant.RECIPES_DATA_PATH +
                                 ".json not found. Recipes cannot be edited.");
            }
            else if (!string.IsNullOrWhiteSpace(asset.text))
            {
                CatalogDocument doc = JsonUtility.FromJson<CatalogDocument>(asset.text);
                if (doc != null && doc.recipes != null) parsed.AddRange(doc.recipes);
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning("[AdminRecipeCatalog] Could not parse the recipe catalogue: " + e.Message);
        }

        _builtIn = parsed;
    }
}
