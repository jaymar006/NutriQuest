public static class ResultData
{
    private const string KEY_CORRECT = "Result_Correct";
    private const string KEY_WRONG = "Result_Wrong";
    private const string KEY_TOTAL = "Result_Total";
    private const string KEY_STAGE_ID = "Result_StageID";
    private const string KEY_TOWER_INDEX = "Result_TowerIndex";

    public static void Save(int correct, int wrong, int total,
        string stageID, int towerIndex)
    {
        UnityEngine.PlayerPrefs.SetInt(KEY_CORRECT, correct);
        UnityEngine.PlayerPrefs.SetInt(KEY_WRONG, wrong);
        UnityEngine.PlayerPrefs.SetInt(KEY_TOTAL, total);
        UnityEngine.PlayerPrefs.SetString(KEY_STAGE_ID, stageID);
        UnityEngine.PlayerPrefs.SetInt(KEY_TOWER_INDEX, towerIndex);
        UnityEngine.PlayerPrefs.Save();
    }

    public static int GetCorrect() => UnityEngine.PlayerPrefs.GetInt(KEY_CORRECT, 0);
    public static int GetWrong() => UnityEngine.PlayerPrefs.GetInt(KEY_WRONG, 0);
    public static int GetTotal() => UnityEngine.PlayerPrefs.GetInt(KEY_TOTAL, 10);
    public static string GetStageID() => UnityEngine.PlayerPrefs.GetString(KEY_STAGE_ID, "");
    public static int GetTowerIndex() => UnityEngine.PlayerPrefs.GetInt(KEY_TOWER_INDEX, 0);
}