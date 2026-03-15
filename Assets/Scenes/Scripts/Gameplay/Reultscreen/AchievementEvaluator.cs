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

        // Steps Towards Success — at least 1 correct but below 70%
        if (correct >= 1 && percent < 0.70f)
            return AchievementType.StepsTowardsSuccess;

        // Got zero correct — no badge
        return AchievementType.None;
    }
}