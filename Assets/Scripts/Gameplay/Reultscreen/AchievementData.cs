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
    // Tower 3 has higher thresholds than the rest
    public static int GetPassTarget(string stageID)
    {
        if (stageID == "Stage_3") return 8;
        return 7;
    }

    public static int GetConquerorTarget(string stageID)
    {
        if (stageID == "Stage_3") return 9;
        return 8;
    }
}