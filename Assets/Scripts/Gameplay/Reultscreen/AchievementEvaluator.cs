public static class AchievementEvaluator
{
    public static AchievementType Evaluate(int correct, int total, string stageID)
    {
        int passTarget = AchievementData.GetPassTarget(stageID);
        int conquerorTarget = AchievementData.GetConquerorTarget(stageID);

        // Genius — perfect score every time, no longer one time only
        if (correct == total)
            return AchievementType.GeniusOfTheTower;

        // Steps Towards Success — less than 2 correct
        if (correct < 2)
            return AchievementType.StepsTowardsSuccess;

        // Failed — below pass target but not Steps
        if (correct < passTarget)
            return AchievementType.None;

        // Conqueror — passed with high score
        if (correct >= conquerorTarget)
            return AchievementType.ConquerorOfTheTower;

        // Challenger — passed but below conqueror target
        return AchievementType.ChallengerOfTheTower;
    }
}