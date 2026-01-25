using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class Recipe
{
    public string id;
    public string name;
    public string description;
    public string imagePath;
    public List<Ingredient> ingredients;
    public List<string> instructions;
    public int difficulty; // 1-5 scale
    public int prepTimeMinutes;
    public int cookTimeMinutes;
    public int servings;
    public List<string> categories; // e.g., "Breakfast", "Vegetarian", "High Protein"
    public int pointsRequired; // Points needed to unlock
    public bool isUnlocked;
    public string unlockDateString; // DateTime as string for JSON serialization
    
    // Non-serialized property for runtime use
    [NonSerialized] private DateTime? _unlockDate;
    
    public DateTime unlockDate
    {
        get
        {
            if (!_unlockDate.HasValue && !string.IsNullOrEmpty(unlockDateString))
            {
                if (DateTime.TryParse(unlockDateString, out DateTime parsed))
                {
                    _unlockDate = parsed;
                }
            }
            return _unlockDate ?? default(DateTime);
        }
        set
        {
            _unlockDate = value;
            unlockDateString = value.ToString("O");
        }
    }
    
    public Recipe()
    {
        ingredients = new List<Ingredient>();
        instructions = new List<string>();
        categories = new List<string>();
    }
}

[Serializable]
public class Ingredient
{
    public string name;
    public float amount;
    public string unit; // e.g., "cups", "tbsp", "g", "pieces"
    
    public Ingredient(string name, float amount, string unit)
    {
        this.name = name;
        this.amount = amount;
        this.unit = unit;
    }
}
