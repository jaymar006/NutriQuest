using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class CutsceneController : MonoBehaviour
{
    [Header("Dialogue")]
    [Tooltip("Assign the DialogueManager in this cutscene scene.")]
    [SerializeField] private DialogueManager dialogueManager;

    [Header("Recipe Unlock UI")]
    [Tooltip("Optional panel shown when recipe unlocks.")]
    [SerializeField] private GameObject recipeUnlockPanel;
    [Tooltip("How long to show the unlock panel before continuing.")]
    [SerializeField] private float recipeUnlockDisplayTime = 2.5f;

    [Header("Transition Settings")]
    [SerializeField] private bool useLoadingScreen = false;

    private string _stageID;
    private string _nextScene;

    private const string FIRST_CLEAR_PREFIX = "FirstClear_";

    private void Start()
    {
        // Read data passed from StageCompleteHandler //
        _stageID = CutsceneData.GetStageID();
        _nextScene = CutsceneData.GetNextScene();

        Debug.Log("[CutsceneController] Stage: " + _stageID + " Next: " + _nextScene);

        if (recipeUnlockPanel != null)
            recipeUnlockPanel.SetActive(false);

        // Hook into DialogueManager finish event //
        StartCoroutine(WaitForDialogueToFinish());
    }

    // Poll until dialogue is done then trigger unlock + transition //
    private IEnumerator WaitForDialogueToFinish()
    {
        // Wait for DialogueManager to exist and finish //
        while (dialogueManager == null)
        {
            Debug.LogWarning("[CutsceneController] DialogueManager not found — retrying...");
            yield return null;
        }

        // Wait until dialogue reports finished //
        yield return new WaitUntil(() => dialogueManager.IsDialogueFinished);

        Debug.Log("[CutsceneController] Dialogue finished — unlocking recipe.");

        // Unlock recipe //
        UnlockRecipe();

        // Show unlock panel if assigned //
        if (recipeUnlockPanel != null)
        {
            recipeUnlockPanel.SetActive(true);
            yield return new WaitForSeconds(recipeUnlockDisplayTime);
            recipeUnlockPanel.SetActive(false);
        }

        // Transition to result screen //
        TransitionToResult();
    }

    private void UnlockRecipe()
    {
        // Save first clear //
        PlayerPrefs.SetInt(FIRST_CLEAR_PREFIX + _stageID, 1);
        PlayerPrefs.Save();

        // Refresh recipe manager if it exists //
        if (RecipeUnlockManager.Instance != null)
            RecipeUnlockManager.Instance.RefreshUnlockStates();

        Debug.Log("[CutsceneController] Recipe unlocked for: " + _stageID);
    }

    private void TransitionToResult()
    {
        if (string.IsNullOrEmpty(_nextScene))
        {
            Debug.LogError("[CutsceneController] Next scene name is empty!");
            return;
        }

        Debug.Log("[CutsceneController] Transitioning to: " + _nextScene);

        if (SceneTransitionManager.Instance != null)
            SceneTransitionManager.Instance.NavigateTo(_nextScene, useLoadingScreen);
        else
            UnityEngine.SceneManagement.SceneManager.LoadScene(_nextScene);
    }
}