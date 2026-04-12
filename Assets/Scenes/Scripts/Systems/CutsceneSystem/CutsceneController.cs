using UnityEngine;
using System.Collections;

public class CutsceneController : MonoBehaviour
{
    [Header("Dialogue")]
    [SerializeField] private DialogueManager dialogueManager;

    [Header("Recipe Unlock UI")]
    [SerializeField] private GameObject recipeUnlockPanel;
    [SerializeField] private float recipeUnlockDisplayTime = 2.5f;

    [Header("Transition Settings")]
    [SerializeField] private bool useLoadingScreen = false;

    private string _stageID;
    private string _nextScene;
    private const string FIRST_CLEAR_PREFIX = "FirstClear_";

    private void Start()
    {
        _stageID = CutsceneData.GetStageID();
        _nextScene = CutsceneData.GetNextScene();

        if (string.IsNullOrEmpty(_stageID))
            Debug.LogWarning("[CutsceneController] StageID is empty!");

        if (recipeUnlockPanel != null)
            recipeUnlockPanel.SetActive(false);

        StartCoroutine(WaitForDialogueToFinish());
    }

    private IEnumerator WaitForDialogueToFinish()
    {
        // Wait for DialogueManager reference
        if (dialogueManager == null)
        {
            Debug.LogWarning("[CutsceneController] DialogueManager not assigned. Trying to find it...");
            dialogueManager = FindObjectOfType<DialogueManager>();
        }

        if (dialogueManager == null)
        {
            Debug.LogError("[CutsceneController] DialogueManager not found!");
            yield break;
        }

        // Wait until dialogue finishes
        yield return new WaitUntil(() => dialogueManager.IsDialogueFinished);

        Debug.Log("[CutsceneController] Dialogue finished — unlocking recipe.");
        UnlockRecipe();

        // Show unlock panel if assigned
        if (recipeUnlockPanel != null)
        {
            recipeUnlockPanel.SetActive(true);
            yield return new WaitForSeconds(recipeUnlockDisplayTime);
            recipeUnlockPanel.SetActive(false);
        }

        TransitionToResult();
    }

    private void UnlockRecipe()
    {
        PlayerPrefs.SetInt(FIRST_CLEAR_PREFIX + _stageID, 1);
        PlayerPrefs.Save();

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