public static class LoadingTargetScene
{
    private static string targetScene;

    public static void SetTarget(string sceneName)
    {
        targetScene = sceneName;
    }

    public static string GetTarget()
    {
        return targetScene;
    }

    public static void Clear()
    {
        targetScene = null;
    }
}