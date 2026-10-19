public enum AchievementType
{
    None,
    GeniusOfTheTower,
    ConquerorOfTheTower,
    ChallengerOfTheTower,
    StepsTowardsSuccess
}

public static class AchievementData
{
    // Tower 3 has higher thresholds than the rest.
    private const int DEFAULT_PASS_TARGET = 7;
    private const int DEFAULT_PASS_TARGET_TOWER_3 = 8;
    private const int DEFAULT_CONQUEROR_TARGET = 8;
    private const int DEFAULT_CONQUEROR_TARGET_TOWER_3 = 9;

    /// <summary>
    /// The built-in number of correct answers needed to pass a tower. The
    /// admin panel shows this as the value a changed row falls back to.
    /// </summary>
    public static int GetDefaultPassTarget(string stageID)
    {
        if (stageID == "Stage_3") return DEFAULT_PASS_TARGET_TOWER_3;
        return DEFAULT_PASS_TARGET;
    }

    /// <summary>
    /// The built-in number of correct answers needed for the Conqueror badge.
    /// </summary>
    public static int GetDefaultConquerorTarget(string stageID)
    {
        if (stageID == "Stage_3") return DEFAULT_CONQUEROR_TARGET_TOWER_3;
        return DEFAULT_CONQUEROR_TARGET;
    }

    /// <summary>
    /// How many correct answers are needed to pass a tower. The teacher can
    /// change this per stage from the admin panel.
    /// </summary>
    public static int GetPassTarget(string stageID)
    {
        return AdminContentStore.GetInt(
            AdminGameConfig.PassTargetKey(stageID), GetDefaultPassTarget(stageID));
    }

    /// <summary>
    /// How many correct answers are needed for the Conqueror badge. The
    /// teacher can change this per stage from the admin panel.
    /// </summary>
    public static int GetConquerorTarget(string stageID)
    {
        return AdminContentStore.GetInt(
            AdminGameConfig.ConquerorTargetKey(stageID), GetDefaultConquerorTarget(stageID));
    }
}
