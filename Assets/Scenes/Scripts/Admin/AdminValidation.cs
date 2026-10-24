using System.Collections.Generic;

/// <summary>
/// The input rules the admin panel enforces before it accepts a change.
///
/// These are deliberately small and shared, so the Questions tab and the
/// Recipes tab cannot drift apart on what "valid" means.
/// </summary>
public static class AdminValidation
{
    /// <summary>
    /// Checks a question. Returns an empty string when the question is fine,
    /// otherwise a short reason that the tab shows to the teacher.
    /// </summary>
    public static string ValidateQuestion(QuestionData question, List<QuestionData> all)
    {
        if (question == null) return "The question is missing.";

        if (string.IsNullOrEmpty(question.question) || question.question.Trim().Length == 0)
        {
            return "The question text cannot be empty.";
        }

        if (string.IsNullOrEmpty(question.answerA) || string.IsNullOrEmpty(question.answerB) ||
            string.IsNullOrEmpty(question.answerC) || string.IsNullOrEmpty(question.answerD))
        {
            return "All four answers are needed.";
        }

        string correct = (question.correctAnswer ?? "").Trim();
        if (correct.Length != 1 || "ABCD".IndexOf(correct.ToUpperInvariant()) < 0)
        {
            return "Pick exactly one correct answer.";
        }

        if (all != null)
        {
            for (int i = 0; i < all.Count; i++)
            {
                QuestionData other = all[i];
                if (other == null || ReferenceEquals(other, question)) continue;
                if (string.IsNullOrEmpty(other.question)) continue;
                if (other.question.Trim().Equals(question.question.Trim(),
                        System.StringComparison.OrdinalIgnoreCase))
                {
                    return "Another question already has that text.";
                }
            }
        }

        return "";
    }

    /// <summary>
    /// Checks a recipe. Returns an empty string when the recipe is fine.
    /// </summary>
    public static string ValidateRecipe(AdminContentStore.RecipeOverride recipe)
    {
        if (recipe == null) return "The recipe is missing.";

        if (string.IsNullOrEmpty(recipe.nameEn) || recipe.nameEn.Trim().Length == 0)
        {
            return "The recipe needs a name.";
        }

        bool hasIngredients = recipe.blocks != null &&
            recipe.blocks.Exists(b => b != null && !string.IsNullOrEmpty(b.bodyEn) && b.bodyEn.Trim().Length > 0);
        bool hasSteps = recipe.blocks != null &&
            recipe.blocks.Exists(b => b != null && !string.IsNullOrEmpty(b.bodyEn) && b.bodyEn.Trim().Length > 0);

        if (!hasIngredients && !hasSteps)
        {
            return "The recipe needs at least some ingredients or steps.";
        }

        return "";
    }
}
