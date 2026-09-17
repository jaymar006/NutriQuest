using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class RecipeUnlockManager : MonoBehaviour
{
    public static RecipeUnlockManager Instance { get; private set; }

    [System.Serializable]
    public class RecipeEntry
    {
        [Header("Identification")]
        [Tooltip("Display name - shown in debug logs")]
        public string recipeName;
        [Tooltip("Must match the stageID used by ResultScreenManager (e.g. Tower1_Stage1)")]
        public string requiredStageID;

        [Header("Unlocked State")]
        [Tooltip("The recipe button shown when unlocked (icon + name). Its entire " +
                 "GameObject is hidden while locked, replaced by the hint panel below.")]
        public Button recipeButton;

        [Header("Locked State")]
        [Tooltip("The hint panel/scroll shown instead, while this recipe is locked. " +
                 "Should be a sibling of recipeButton occupying the same slot.")]
        public GameObject lockedHintRoot;

        [Tooltip("The container GameObject holding the hint's TMP_Text as a CHILD " +
                 "(rather than pointing directly at the text component). This way you " +
                 "can freely restructure or swap the child text object — different " +
                 "font, different prefab, whatever — without ever having to re-wire " +
                 "this reference again. The script finds the TMP_Text inside it automatically.")]
        public Transform lockedHintTextContainer;

        [Tooltip("The hint message shown while locked, e.g. 'Finish tower 1\\nTo unlock.' " +
                 "The word 'Hint' is added automatically above this on a line of its own, " +
                 "matching the scroll design — just write the two lines below it here.")]
        [TextArea(2, 4)]
        public string unlockHintMessage = "Finish tower 1\nTo unlock.";
    }

    [Header("Recipe Entries")]
    [SerializeField] private List<RecipeEntry> recipes = new List<RecipeEntry>();

    private const string FIRST_CLEAR_PREFIX = "FirstClear_";

    private void Awake()
    {
        // Singleton - survive scene loads if placed on a persistent object,
        // or let each scene create its own instance if placed on a scene object.
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        RefreshUnlockStates();
    }

    // Called by ResultScreenManager after saving FirstClear_ to PlayerPrefs.
    // Safe to call from anywhere - just re-reads PlayerPrefs and updates the UI.
    public void RefreshUnlockStates()
    {
        foreach (RecipeEntry recipe in recipes)
        {
            if (recipe == null) continue;

            bool unlocked = IsRecipeUnlocked(recipe.requiredStageID);

            // FIX: locked recipes now hide the button entirely instead of just
            // dimming/disabling it, and show the hint panel in its place.
            if (recipe.recipeButton != null)
                recipe.recipeButton.gameObject.SetActive(unlocked);

            if (recipe.lockedHintRoot != null)
                recipe.lockedHintRoot.SetActive(!unlocked);

            if (!unlocked && recipe.lockedHintTextContainer != null)
            {
                // FIX: looked up fresh each refresh instead of a cached direct
                // reference, so whatever TMP_Text currently lives inside the
                // container gets the message — including a font/prefab you
                // swapped in after this was last wired up. GetComponentInChildren
                // with "true" also finds it even if the container itself is
                // still inactive at this point in the loop.
                TMP_Text hintText = recipe.lockedHintTextContainer.GetComponentInChildren<TMP_Text>(true);
                if (hintText != null)
                    hintText.text = "Hint\n" + recipe.unlockHintMessage;
                else
                    Debug.LogWarning($"[RecipeUnlockManager] No TMP_Text found inside lockedHintTextContainer for '{recipe.recipeName}'.");
            }

            Debug.Log($"[RecipeUnlockManager] '{recipe.recipeName}' " +
                      $"(stageID='{recipe.requiredStageID}') -> {(unlocked ? "UNLOCKED" : "LOCKED")}");
        }
    }

    // A recipe is unlocked when its required stage has been cleared at least once.
    // Empty stageID means "always unlocked" (useful for starter recipes).
    private bool IsRecipeUnlocked(string stageID)
    {
        if (string.IsNullOrEmpty(stageID)) return true;
        int value = PlayerPrefs.GetInt(FIRST_CLEAR_PREFIX + stageID, 0);
        Debug.Log($"[RecipeUnlockManager] PlayerPrefs['{FIRST_CLEAR_PREFIX}{stageID}'] = {value}");
        return value == 1;
    }
}