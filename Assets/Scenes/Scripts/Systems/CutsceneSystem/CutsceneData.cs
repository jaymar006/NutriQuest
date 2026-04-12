public static class CutsceneData
{
    private static string _stageID = "";
    private static string _nextScene = "";

    public static void Set(string stageID, string nextScene)
    {
        _stageID = stageID ?? "";
        _nextScene = nextScene ?? "";
    }

    public static string GetStageID() => _stageID;
    public static string GetNextScene() => _nextScene;
}