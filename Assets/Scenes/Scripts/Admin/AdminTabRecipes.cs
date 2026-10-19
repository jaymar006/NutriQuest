using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The Recipes tab: edit a recipe's wording, its ingredients and which tower
/// unlocks it.
///
/// The built-in recipes were extracted into Assets/Resources/Data/recipes.json
/// (see Tools/build_recipe_catalog.ps1). The panel edits an override; the
/// scene still shows the original text, and AdminTextOverrideApplier swaps it
/// for the teacher's version the next time the scene loads.
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

    private void BuildRecipePicker(RectTransform content)
    {
        RectTransform card = AddCard(content, AdminStrings.Get("recipe"));

        IReadOnlyList<AdminGameConfig.RecipeEntry> recipes = AdminGameConfig.Recipes;
        for (int i = 0; i < recipes.Count; i++)
        {
            AdminGameConfig.RecipeEntry recipe = recipes[i];
            bool selected = recipe.id == _recipeId;

            Button button = AdminUi.CreateButton(card, "Recipe_" + recipe.id, recipe.nameEn, () =>
            {
                _recipeId = recipe.id;
                if (Root != null) Root.RefreshCurrentTab();
            }, selected ? AdminUi.Accent : AdminUi.CardAlt, AdminUi.FontSmall);

            LayoutElement layout = button.gameObject.AddComponent<LayoutElement>();
            layout.preferredWidth = 320f;
            layout.minWidth = 220f;
            layout.flexibleWidth = 1f;
        }
    }

    private void BuildEditor(RectTransform content)
    {
        AdminContentStore.RecipeOverride effective = AdminRecipeCatalog.GetEffective(_recipeId);
        if (effective == null)
        {
            RectTransform card = AddCard(content, AdminStrings.Get("recipes"));
            AddButtonRow(card, AdminStrings.Get("restoreRecipe"), () =>
            {
                AdminRecipeCatalog.SeedOverride(_recipeId);
                Commit("recipes", "Restored the built-in recipe " + _recipeId);
                if (Root != null) Root.RefreshCurrentTab();
            });
            return;
        }

        bool overridden = AdminContentStore.GetRecipe(_recipeId) != null;

        // ---- Names and unlock rule ----
        RectTransform card = AddCard(content, effective.nameEn);

        AddRecipeField(card, AdminStrings.Get("recipe") + " (EN)", effective.nameEn, value =>
        {
            AdminContentStore.RecipeOverride working = EnsureWorkingRecipe();
            if (working != null) working.nameEn = value;
        });

        AddRecipeField(card, AdminStrings.Get("recipe") + " (FIL)", effective.nameFil, value =>
        {
            AdminContentStore.RecipeOverride working = EnsureWorkingRecipe();
            if (working != null) working.nameFil = value;
        });

        AddRecipeField(card, AdminStrings.Get("unlockStage"), effective.unlockStageId, value =>
        {
            AdminContentStore.RecipeOverride working = EnsureWorkingRecipe();
            if (working != null) working.unlockStageId = value;
        });

        // ---- Blocks: the ingredients and the instructions ----
        if (effective.blocks != null)
        {
            for (int i = 0; i < effective.blocks.Count; i++)
            {
                AdminContentStore.RecipeBlock block = effective.blocks[i];
                if (block == null) continue;

                string title = string.IsNullOrEmpty(block.subLabelEn) ? block.labelEn : block.subLabelEn;
                RectTransform blockCard = AddCard(content, title);

                AddRecipeField(blockCard, AdminStrings.Get("sectionText") + " (EN)", block.bodyEn, value =>
                {
                    AdminContentStore.RecipeOverride working = EnsureWorkingRecipe();
                    if (working != null && i < working.blocks.Count) working.blocks[i].bodyEn = value;
                }, true);

                AddRecipeField(blockCard, AdminStrings.Get("sectionText") + " (FIL)", block.bodyFil, value =>
                {
                    AdminContentStore.RecipeOverride working = EnsureWorkingRecipe();
                    if (working != null && i < working.blocks.Count) working.blocks[i].bodyFil = value;
                }, true);
            }
        }

        if (overridden)
        {
            AddButtonRow(card, AdminStrings.Get("restoreRecipe"), () =>
            {
                AdminRecipeCatalog.ClearOverride(_recipeId);
                Commit("recipes", "Restored the built-in recipe " + _recipeId);
                if (Root != null) Root.RefreshCurrentTab();
            }, AdminUi.CardAlt);
        }
    }

    private void AddRecipeField(RectTransform card, string label, string value, System.Action<string> onChanged,
                                bool multiline = false)
    {
        RectTransform row = AddRow(card, multiline ? 220f : 72f);
        AddRowLabel(row, label);

        TMP_InputField input = AddRowInput(row, value ?? "", multiline);
        input.onValueChanged.AddListener(text =>
        {
            onChanged(text);
            Commit("recipes", "Edited " + label + " of " + _recipeId);
        });
    }

    /// <summary>Returns the override, creating it from the built-in recipe on first edit.</summary>
    private AdminContentStore.RecipeOverride EnsureWorkingRecipe()
    {
        if (AdminContentStore.GetRecipe(_recipeId) == null)
        {
            AdminRecipeCatalog.SeedOverride(_recipeId);
        }
        return AdminContentStore.GetRecipe(_recipeId);
    }
}
