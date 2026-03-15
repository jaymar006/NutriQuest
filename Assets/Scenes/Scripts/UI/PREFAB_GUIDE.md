# Unity Prefabs Guide for NutriQuest

## What Are Prefabs?

**Prefabs** (short for "prefabricated objects") are reusable GameObject templates in Unity. Think of them as "blueprints" or "cookie cutters" that you design once and can use multiple times throughout your project.

---

## Why Use Prefabs?

### 1. **Reusability**
- Design once, use many times
- Create a button template and instantiate it for each tower/level

### 2. **Consistency**
- All instances look and behave the same
- Ensures uniform UI design across your game

### 3. **Efficiency**
- Update the prefab once, all instances update automatically
- No need to manually change each button individually

### 4. **Dynamic Creation**
- Create UI elements at runtime (when the game is running)
- Perfect for lists, grids, and dynamic content

### 5. **Organization**
- Keep your project organized
- Store reusable components in one place

---

## How Prefabs Work in NutriQuest

### Example: Tower Button Prefab

In the **MainMenu** scene, you need to display multiple towers (levels). Instead of manually creating a button for each tower in the Unity Editor, you:

1. **Create the prefab once:**
   - Design one button with the right size, style, and components
   - Save it as `TowerButtonPrefab` in `Assets/Prefabs/UI/`

2. **Use it in code:**
   - `MainMenuUI.cs` loads all towers from your data
   - For each tower, it **instantiates** (creates a copy of) the prefab
   - Sets the button's text to the tower name
   - Adds it to the grid

3. **Result:**
   - You get 10 tower buttons automatically (one for each tower)
   - All buttons look identical
   - If you change the prefab, all buttons update

---

## Code Example

Here's how `MainMenuUI.cs` uses the prefab:

```csharp
// The prefab reference (assigned in Unity Inspector)
[SerializeField] private GameObject towerButtonPrefab;

// Create a button for each tower
private void CreateTowerButton(Tower tower)
{
    // Instantiate (create a copy of) the prefab
    GameObject buttonObj = Instantiate(towerButtonPrefab, towerGrid.transform);
    
    // Customize this specific instance
    TextMeshProUGUI buttonText = buttonObj.GetComponentInChildren<TextMeshProUGUI>();
    buttonText.text = tower.towerName; // Set unique text
    
    // Add click handler
    button.onClick.AddListener(() => OnTowerButtonClicked(tower.towerId));
}
```

---

## Prefabs Used in NutriQuest

### 1. **TowerButtonPrefab**
- **Location:** `Assets/Prefabs/UI/TowerButtonPrefab.prefab`
- **Purpose:** Buttons for selecting towers/levels in MainMenu
- **Used by:** `MainMenuUI.cs`
- **How:** Dynamically created for each tower in the game

### 2. **QuestionCard Prefab** (Future)
- **Purpose:** Display quiz questions during gameplay
- **How:** Created for each question in a quiz

### 3. **RecipeCard Prefab** (Future)
- **Purpose:** Display recipe cards in the Recipes screen
- **How:** Created for each unlocked recipe

### 4. **Achievement Prefab** (Future)
- **Purpose:** Display achievement badges
- **How:** Created for each achievement

### 5. **NavBar Prefab** (Optional)
- **Purpose:** Reusable navigation bar for multiple scenes
- **How:** Can be added to any scene that needs navigation

---

## How to Create a Prefab

### Step-by-Step: Creating TowerButtonPrefab

1. **Create the GameObject:**
   - In Unity Editor, right-click Hierarchy → UI → Button - TextMeshPro
   - Name it: "TowerButton"

2. **Design it:**
   - Set size: 400x200
   - Set text: "Tower Name" (placeholder)
   - Add any images, colors, or styling you want
   - Add any custom components if needed

3. **Save as Prefab:**
   - Drag the GameObject from Hierarchy to `Assets/Prefabs/UI/` folder
   - Unity will create a `.prefab` file
   - Rename it to "TowerButtonPrefab"
   - **Delete the original from Hierarchy** (the prefab is now in the Project window)

4. **Use in Code:**
   - In `MainMenuUI` component, drag the prefab to the "Tower Button Prefab" field
   - The script will use it to create buttons dynamically

---

## Prefab vs Regular GameObject

| **Regular GameObject** | **Prefab** |
|------------------------|------------|
| Exists only in one scene | Can be used in multiple scenes |
| Must be manually duplicated | Can be instantiated in code |
| Changes affect only that instance | Changes affect all instances |
| Good for unique objects | Good for repeated objects |

---

## Prefab Workflow

### When to Use Prefabs:

✅ **Use Prefabs for:**
- Buttons that appear multiple times (tower buttons, menu items)
- Cards/panels that repeat (question cards, recipe cards)
- UI elements created dynamically at runtime
- Objects you want to reuse across scenes

❌ **Don't Use Prefabs for:**
- Unique one-time objects (main menu background)
- Scene-specific elements that won't be reused
- Simple static UI (single text label, single button)

---

## Prefab Best Practices

### 1. **Organize by Type**
```
Assets/Prefabs/
  ├── UI/
  │   ├── TowerButtonPrefab.prefab
  │   ├── QuestionCardPrefab.prefab
  │   └── RecipeCardPrefab.prefab
  └── Game/
      └── TowerNodePrefab.prefab
```

### 2. **Name Clearly**
- Use descriptive names: `TowerButtonPrefab` not `Button1`
- Include "Prefab" in the name to avoid confusion

### 3. **Design for Flexibility**
- Use placeholder text that will be replaced in code
- Make components accessible (use `GetComponent` or `GetComponentInChildren`)
- Keep styling consistent

### 4. **Test Before Using**
- Test the prefab in a scene first
- Make sure all components work
- Then save as prefab and use in code

---

## Common Prefab Operations

### Instantiate (Create at Runtime)
```csharp
GameObject instance = Instantiate(prefab, parentTransform);
```

### Modify Instance
```csharp
// Each instance can be customized
instance.GetComponent<TextMeshProUGUI>().text = "Custom Text";
instance.GetComponent<Image>().color = Color.red;
```

### Destroy Instance
```csharp
Destroy(instance); // Remove when no longer needed
```

---

## Troubleshooting

### Issue: Prefab not showing in Inspector
- **Solution:** Make sure you dragged the prefab from Project window, not Hierarchy

### Issue: Changes to prefab not updating instances
- **Solution:** Click "Apply" button in Inspector when editing prefab
- Or use "Open Prefab" to edit the prefab directly

### Issue: Instances not updating when prefab changes
- **Solution:** Check if instances are "Overrides" (yellow text in Inspector)
- Click "Revert" to use prefab values, or "Apply All" to push changes

### Issue: Prefab reference is missing/null
- **Solution:** 
  1. Make sure prefab exists in `Assets/Prefabs/UI/`
  2. Assign it in the Inspector to the script's prefab field
  3. Check the file path is correct

---

## Summary

**Prefabs = Reusable Templates**

- Create once, use many times
- Perfect for dynamic UI (buttons, cards, lists)
- Update once, all instances update
- Essential for scalable game development

In NutriQuest, prefabs are used primarily for:
- Tower selection buttons (MainMenu)
- Question cards (Gameplay - future)
- Recipe cards (Recipes screen - future)
- Achievement badges (Profile - future)

---

## Quick Reference

**Create Prefab:**
1. Design GameObject in scene
2. Drag to `Assets/Prefabs/UI/` folder
3. Delete from Hierarchy

**Use Prefab:**
1. Assign prefab to script field in Inspector
2. Use `Instantiate(prefab)` in code
3. Customize each instance as needed

**Update Prefab:**
1. Select prefab in Project window
2. Edit in Inspector
3. Click "Apply" to save changes
