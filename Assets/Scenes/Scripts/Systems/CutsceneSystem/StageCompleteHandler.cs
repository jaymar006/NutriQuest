using UnityEngine;

public class StageCompleteHandler : MonoBehaviour
{
    [Header("Stage Settings")]
    [SerializeField] private string stageID = "Stage_1";

    [Header("Scene Names")]
    [SerializeField] private string cutsceneSceneName = "Cutscene_Stage1";
    [SerializeField] private string resultSceneName = "ResultScene";

    [SerializeField] private bool useLoadingScreen = false;

    private const string FIRST_CLEAR_PREFIX = "FirstClear_";

    public void OnStageComplete()
    {
        if (string.IsNullOrEmpty(stageID))
        {
            Debug.LogError("[StageCompleteHandler] stageID is not set!");
            return;
        }

        bool isFirstClear = PlayerPrefs.GetInt(FIRST_CLEAR_PREFIX + stageID, 0) == 0;

        if (isFirstClear)
        {
            Debug.Log("[StageCompleteHandler] First clear! Loading cutscene for: " + stageID);
            CutsceneData.Set(stageID, resultSceneName);

            if (SceneTransitionManager.Instance != null)
                SceneTransitionManager.Instance.NavigateTo(cutsceneSceneName, useLoadingScreen);
            else
                UnityEngine.SceneManagement.SceneManager.LoadScene(cutsceneSceneName);
        }
        else
        {
            Debug.Log("[StageCompleteHandler] Not first clear — going directly to result screen.");
            if (SceneTransitionManager.Instance != null)
                SceneTransitionManager.Instance.NavigateTo(resultSceneName, useLoadingScreen);
            else
                UnityEngine.SceneManagement.SceneManager.LoadScene(resultSceneName);
        }
    }
}