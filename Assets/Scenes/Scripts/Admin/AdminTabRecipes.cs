using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The Recipes tab: list, add, edit and delete recipes.
///
/// The built-in recipes were extracted into
/// Assets/Resources/Data/recipes.json (see Tools/build_recipe_catalog.ps1).
/// The panel edits an override; AdminRecipeCatalog merges the override over
/// the built-in catalogue and AdminTextOverrideApplier writes the merged
/// text back into the recipe scene, so the game always shows what this tab
/// shows.
///
/// Each block of a recipe is a named area of the recipe scene: "P1" holds
/// the ingredients, "PSF1" the second ingredient block of the stir fry
/// recipe, and "Steps" the method. The teacher edits those as text, which is
/// exactly how they appear in the game.
/// </summary>
public class AdminTabRecipes : AdminTabBase
{
    private string _recipeId = "recipe_1";

    public AdminTabRecipes(AdminPanelRoot root) : base(root) { }

    public override string TitleKey => "tabRecipes";

    public override void Build(RectTransform content)
    {
        BuildRecipePicker(content);
        BuildEditor(content);
    }

    // ------------------------------------------------------------------
    // The list of recipes
    // ------------------------------------------------------------------

    private void BuildRecipePicker(RectTransform content)
    {
        RectTransform card = AddCard(content, AdminStrings.Get("recipe"));

        // The recipes sit side by side in a row, like tabs.
        RectTransform row = AddRow(card, 72f);

        List<string> ids = KnownRecipeIds();
        for (int i = 0; i < ids.Count; i++)
        {
            string id = ids[i];
            AdminContentStore.RecipeOverride recipe = AdminRecipeCatalog.GetMerged(id);
            string name = recipe == null || string.IsNullOrEmpty(recipe.nameEn) ? id : recipe.nameEn;
            bool selected = id == _recipeId;

            Button button = AdminUi.CreateButton(row, "Recipe_" + id, name, () =>
            {
                _recipeId = id;
                if (Root != null) Root.RefreshCurrentTab();
            }, selected ? AdminUi.Accent : AdminUi.CardAlt, AdminUi.FontSmall);

            LayoutElement layout = AdminUi.EnsureLayout(button.gameObject);
            layout.preferredWidth = 240f;
            layout.minWidth = 140f;
            layout.flexibleWidth = 1f;
        }

        AddButtonRow(card, AdminStrings.Get("add"), () =>
        {
            AdminContentStore.RecipeOverride fresh = new AdminContentStore.RecipeOverride
            {
                id = NextRecipeId(ids),
                nameEn = "New recipe",
                nameFil = "Bagong resipe",
                descriptionEn = "",
                descriptionFil = "",
                category = "",
                unlockStageId = "",
                blocks = new List<AdminContentStore.RecipeBlock>
                {
                    new AdminContentStore.RecipeBlock { id = "P1", labelEn = "Ingredients", bodyEn = "", bodyFil = "" },
                    new AdminContentStore.RecipeBlock { id = "Steps", labelEn = "Instructions", bodyEn = "", bodyFil = "" },
                }
            };
            AdminContentStore.SetRecipe(fresh);
            _recipeId = fresh.id;
            Commit("Recipes", "Added the recipe " + fresh.id);
            if (Root != null) Root.RefreshCurrentTab();
        }, AdminUi.AccentDim);
    }

    /// <summary>The built-in recipe ids plus any the teacher has added.</summary>
    private static List<string> KnownRecipeIds()
    {
        List<string> ids = new List<string>();
        for (int i = 0; i < AdminGameConfig.Recipes.Count; i++)
        {
            string id = AdminGameConfig.Recipes[i].id;
            if (!ids.Contains(id)) ids.Add(id);
        }
        List<AdminContentStore.RecipeOverride> overrides = AdminContentStore.GetRecipes();
        for (int i = 0; i < overrides.Count; i++)
        {
            if (overrides[i] == null) continue;
            if (!ids.Contains(overrides[i].id)) ids.Add(overrides[i].id);
        }
        return ids;
    }

    private static string NextRecipeId(List<string> existing)
    {
        int n = existing.Count + 1;
        while (true)
        {
            string id = "recipe_" + n;
            if (!existing.Contains(id)) return id;
            n++;
        }
    }

    // ------------------------------------------------------------------
    // The editor
    // ------------------------------------------------------------------

    private void BuildEditor(RectTransform content)
    {
        AdminContentStore.RecipeOverride recipe = AdminRecipeCatalog.GetMerged(_recipeId);
        if (recipe == null) return;

        RectTransform card = AddCard(content, recipe.nameEn);

        AddTextField(card, AdminStrings.Get("recipe") + " (EN)", recipe.nameEn, value =>
        {
            AdminContentStore.RecipeOverride working = EnsureWorkingRecipe();
            if (working != null) working.nameEn = value;
        });

        AddTextField(card, AdminStrings.Get("recipe") + " (FIL)", recipe.nameFil, value =>
        {
            AdminContentStore.RecipeOverride working = EnsureWorkingRecipe();
            if (working != null) working.nameFil = value;
        });

        AddTextField(card, AdminStrings.Get("description") + " (EN)", recipe.descriptionEn, value =>
        {
            AdminContentStore.RecipeOverride working = EnsureWorkingRecipe();
            if (working != null) working.descriptionEn = value;
        });

        AddTextField(card, AdminStrings.Get("description") + " (FIL)", recipe.descriptionFil, value =>
        {
            AdminContentStore.RecipeOverride working = EnsureWorkingRecipe();
            if (working != null) working.descriptionFil = value;
        });

        AddTextField(card, AdminStrings.Get("category"), recipe.category, value =>
        {
            AdminContentStore.RecipeOverride working = EnsureWorkingRecipe();
            if (working != null) working.category = value;
        });

        AddTextField(card, AdminStrings.Get("unlockStage"), recipe.unlockStageId, value =>
        {
            AdminContentStore.RecipeOverride working = EnsureWorkingRecipe();
            if (working != null) working.unlockStageId = value;
        });

        // ---- The ingredient and step blocks ----
        if (recipe.blocks != null)
        {
            for (int i = 0; i < recipe.blocks.Count; i++)
            {
                AdminContentStore.RecipeBlock block = recipe.blocks[i];
                if (block == null) continue;

                int index = i;
                string title = string.IsNullOrEmpty(block.subLabelEn) ? block.labelEn : block.subLabelEn;
                if (string.IsNullOrEmpty(title)) title = block.id;

                RectTransform blockCard = AddCard(content, title);

                AddTextField(blockCard, AdminStrings.Get("ingredients") + " (EN)", block.bodyEn, value =>
                {
                    AdminContentStore.RecipeOverride working = EnsureWorkingRecipe();
                    if (working != null && index < working.blocks.Count) working.blocks[index].bodyEn = value;
                }, true);

                AddTextField(blockCard, AdminStrings.Get("ingredients") + " (FIL)", block.bodyFil, value =>
                {
                    AdminContentStore.RecipeOverride working = EnsureWorkingRecipe();
                    if (working != null && index < working.blocks.Count) working.blocks[index].bodyFil = value;
                }, true);
            }
        }

        AddButtonRow(card, AdminStrings.Get("delete"), () =>
        {
            if (Root == null) return;
            string deletedId = _recipeId;
            Root.Confirm(AdminStrings.Get("delete"), AdminStrings.Get("confirmDeleteRecipe"),
                AdminStrings.Get("delete"), () =>
                {
                    AdminContentStore.RemoveRecipe(deletedId);
                    _recipeId = "recipe_1";
                    Commit("Recipes", "Deleted the recipe " + deletedId);
                    Toast(AdminStrings.Get("reset"));
                    if (Root != null) Root.RefreshCurrentTab();
                });
        }, AdminUi.Danger);

        AddButtonRow(card, AdminStrings.Get("restoreRecipe"), () =>
        {
            AdminRecipeCatalog.ClearOverride(_recipeId);
            Commit("Recipes", "Restored the built-in recipe " + _recipeId);
            if (Root != null) Root.RefreshCurrentTab();
        }, AdminUi.CardAlt);
    }

    /// <summary>A single line text field bound to one property of the recipe.</summary>
    private void AddTextField(RectTransform card, string label, string value, Action<string> onChanged,
                               bool multiline = false)
    {
        RectTransform row = AddRow(card, multiline ? 220f : 72f);
        AddRowLabel(row, label);

        TMP_InputField input = AddRowInput(row, value ?? "", multiline, 520f);

        string last = null;
        input.onValueChanged.AddListener(text =>
        {
            if (last == text) return;
            last = text;
            onChanged(text);

            AdminContentStore.RecipeOverride working = AdminContentStore.GetRecipe(_recipeId);
            if (working == null) return;
            string problem = AdminValidation.ValidateRecipe(working);
            if (problem.Length > 0) Toast(problem);
        });
    }

    /// <summary>
    /// Returns the override, creating it from the built-in recipe the first
    /// time anything is changed.
    /// </summary>
    private AdminContentStore.RecipeOverride EnsureWorkingRecipe()
    {
        if (AdminContentStore.GetRecipe(_recipeId) == null)
        {
            AdminRecipeCatalog.SeedOverride(_recipeId);
        }
        return AdminContentStore.GetRecipe(_recipeId);
    }
}
