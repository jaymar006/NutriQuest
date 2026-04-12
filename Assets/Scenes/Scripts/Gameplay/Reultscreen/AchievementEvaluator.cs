using UnityEngine;

public static class AchievementEvaluator
{
    public static AchievementType Evaluate(int correct, int total)
    {
        if (total == 0) return AchievementType.None;

        float percent = (float)correct / total;

        if (percent >= 1.00f)
            return AchievementType.GeniusOfTheTower;

        if (percent >= 0.80f)
            return AchievementType.ConquerorOfTheTower;

        if (percent >= 0.70f && percent < 0.80f)
            return AchievementType.ChallengerOfTheTower;

        if (correct >= 1 && percent < 0.70f)
            return AchievementType.StepsTowardsSuccess;

        return AchievementType.None;
    }
}