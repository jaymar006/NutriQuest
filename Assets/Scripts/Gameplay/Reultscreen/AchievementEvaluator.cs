public static class AchievementEvaluator
{
    public static AchievementType Evaluate(int correct, int total, bool isFirstAttempt)
    {
        if (total == 0) return AchievementType.None;

        float percent = (float)correct / total;

        // Genius — exactly 100% on first attempt only
        if (percent >= 1.00f && isFirstAttempt)
            return AchievementType.GeniusOfTheTower;

        // Conqueror — 80% to 99%
        if (percent >= 0.80f && percent < 1.00f)
            return AchievementType.ConquerorOfTheTower;

        // Challenger — 70% to 79%
        if (percent >= 0.70f && percent < 0.80f)
            return AchievementType.ChallengerOfTheTower;

        // Steps Towards Success — 60% to 69%
        if (percent >= 0.60f && percent < 0.70f)
            return AchievementType.StepsTowardsSuccess;

        return AchievementType.None;
    }
}