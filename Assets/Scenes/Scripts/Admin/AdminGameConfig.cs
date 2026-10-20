using System.Collections.Generic;

/// <summary>
/// The single source of truth for the game's structure and for every tunable
/// key used by the admin panel.
///
/// WHY THIS EXISTS
/// The tower/stage identifiers used to be duplicated in eight places:
/// TowerUnlockManager, ScoreManager, StageCompleteHandler, LevelInfoScreen,
/// ProfileProgressDisplay, DevCheatTool, ResultScreenManager and
/// AchievementData. They had already drifted apart, which is what caused
/// ProfileProgressDisplay to read "Unlocked_Tower1" while the game writes
/// "Unlocked_Tower_1" (the profile's unlocked-tower count was always wrong),
/// and what caused the FirstClear_Stage_1 versus FirstClear_Tower1_Stage1
/// mismatch that stopped recipes from unlocking.
///
/// Both the game and the admin panel now resolve identifiers through here.
///
/// NOTE ON IDENTIFIERS
///   stageId   "Stage_1"  - the progress identifier used by PlayerPrefs
///   towerName "Tower_1"  - the display/unlock identifier used by PlayerPrefs
///   sceneName "Level 1"  - the Unity scene name (contains a space)
///
/// A RECIPE may key off a different compound id ("Tower1_Stage1"). Always copy
/// the literal from the Inspector field of the component that reads it.
/// </summary>
public static class AdminGameConfig
{
    public class LevelEntry
    {
        public string stageId;
        public string towerName;
        public string sceneName;
        public string labelEn;
        public string labelFil;

        public override string ToString() => labelEn;
    }

    public class RecipeEntry
    {
        public string id;
        public string sceneName;
        public string unlockStageId;
        public string nameEn;
        public string nameFil;

        public override string ToString() => nameEn;
    }

    // ------------------------------------------------------------------
    // Tunable keys. The game reads through these, the panel writes through
    // these, so they can never drift apart.
    // ------------------------------------------------------------------

    // Timing
    public const string KeyQuestionSeconds = "timer.questionSeconds";
    public const string KeyQuestionsPerGame = "timer.questionsPerGame";
    public const string KeyDelayNextQuestion = "timer.delayNextQuestion";
    public const string KeyTowerWaitMinutes = "tower.waitMinutes";
    public const string KeyTower4RequiredTotal = "tower.tower4RequiredTotal";
    public const string KeyLoadingMinSeconds = "ui.loadingMinSeconds";

    // Difficulty
    public const string KeyMaxHints = "difficulty.maxHints";
    public const string KeyMaxRuneKeys = "difficulty.maxRuneKeys";
    public const string KeyRuneRegenMinutes = "difficulty.runeRegenMinutes";
    public const string KeyCostNewTower = "difficulty.costNewTower";
    public const string KeyCostRechallengeTower = "difficulty.costRechallengeTower";
    public const string KeyCostNewTower4 = "difficulty.costNewTower4";
    public const string KeyCostRechallengeTower4 = "difficulty.costRechallengeTower4";
    public const string KeyPassTargetPrefix = "difficulty.passTarget.";
    public const string KeyConquerorTargetPrefix = "difficulty.conquerorTarget.";

    // Rewards
    public const string KeyDropChancePrefix = "rewards.dropChance.";
    public const string KeyHintDropChancePrefix = "rewards.hintDropChance.";
    public const string KeyHintRewardAmount = "rewards.hintRewardAmount";

    // Content
    public const string KeyDisplayVersion = "version.display";

    /// <summary>Key of the question-pool override for a level.</summary>
    public static string QuestionPoolKey(LevelEntry level) => "questions." + level.sceneName;

    public static string PassTargetKey(string stageId) => KeyPassTargetPrefix + stageId;
    public static string ConquerorTargetKey(string stageId) => KeyConquerorTargetPrefix + stageId;
    public static string DropChanceKey(AchievementType type) => KeyDropChancePrefix + type;
    public static string HintDropChanceKey(AchievementType type) => KeyHintDropChancePrefix + type;

    // ------------------------------------------------------------------
    // Built-in defaults.
    //
    // These mirror the values that are serialized in the scenes today. The
    // game and the panel both use them as the fallback for an override, so
    // the panel can always show what the built-in behaviour is even if the
    // scene field is later changed.
    // ------------------------------------------------------------------

    public const float DefaultQuestionSeconds = 15f;
    public const int DefaultQuestionsPerGame = 10;
    public const float DefaultDelayNextQuestion = 2f;
    public const float DefaultTowerWaitMinutes = 3f;
    public const int DefaultTower4RequiredTotal = 26;

    public const int DefaultMaxHints = 3;
    public const int DefaultMaxRuneKeys = 3;
    public const float DefaultRuneRegenMinutes = 30f;

    public const int DefaultCostNewTower = 3;
    public const int DefaultCostRechallengeTower = 2;
    public const int DefaultCostNewTower4 = 4;
    public const int DefaultCostRechallengeTower4 = 3;

    public const float DefaultLoadingMinSeconds = 10.5f;

    public const int DefaultDropChanceSteps = 10;
    public const int DefaultDropChanceChallenger = 30;
    public const int DefaultDropChanceConqueror = 50;
    public const int DefaultDropChanceGenius = 70;

    public const int DefaultHintDropChanceSteps = 0;
    public const int DefaultHintDropChanceChallenger = 0;
    public const int DefaultHintDropChanceConqueror = 50;
    public const int DefaultHintDropChanceGenius = 100;

    public const int DefaultHintRewardAmount = 1;

    /// <summary>The built-in drop chance for a badge, matching the result screen.</summary>
    public static int DefaultDropChance(AchievementType type)
    {
        switch (type)
        {
            case AchievementType.GeniusOfTheTower: return DefaultDropChanceGenius;
            case AchievementType.ConquerorOfTheTower: return DefaultDropChanceConqueror;
            case AchievementType.ChallengerOfTheTower: return DefaultDropChanceChallenger;
            case AchievementType.StepsTowardsSuccess: return DefaultDropChanceSteps;
        }
        return 0;
    }

    /// <summary>The built-in hint drop chance for a badge, matching the result screen.</summary>
    public static int DefaultHintDropChance(AchievementType type)
    {
        switch (type)
        {
            case AchievementType.GeniusOfTheTower: return DefaultHintDropChanceGenius;
            case AchievementType.ConquerorOfTheTower: return DefaultHintDropChanceConqueror;
            case AchievementType.ChallengerOfTheTower: return DefaultHintDropChanceChallenger;
            case AchievementType.StepsTowardsSuccess: return DefaultHintDropChanceSteps;
        }
        return 0;
    }

    /// <summary>Localized label for a badge type.</summary>
    public static string BadgeLabelKey(AchievementType type)
    {
        switch (type)
        {
            case AchievementType.GeniusOfTheTower: return "badgeGenius";
            case AchievementType.ConquerorOfTheTower: return "badgeConqueror";
            case AchievementType.ChallengerOfTheTower: return "badgeChallenger";
            case AchievementType.StepsTowardsSuccess: return "badgeSteps";
        }
        return "badges";
    }

    /// <summary>A short, language-neutral letter for a badge type.</summary>
    public static string BadgeLetter(AchievementType type)
    {
        switch (type)
        {
            case AchievementType.GeniusOfTheTower: return "G";
            case AchievementType.ConquerorOfTheTower: return "C";
            case AchievementType.ChallengerOfTheTower: return "H";
            case AchievementType.StepsTowardsSuccess: return "S";
        }
        return "?";
    }

    // ------------------------------------------------------------------
    // Registry
    // ------------------------------------------------------------------

    private static readonly LevelEntry[] s_levels =
    {
        new LevelEntry { stageId = "Stage_1", towerName = "Tower_1", sceneName = "Level 1", labelEn = "Tower 1", labelFil = "Tore 1" },
        new LevelEntry { stageId = "Stage_2", towerName = "Tower_2", sceneName = "Level 2", labelEn = "Tower 2", labelFil = "Tore 2" },
        new LevelEntry { stageId = "Stage_3", towerName = "Tower_3", sceneName = "Level 3", labelEn = "Tower 3", labelFil = "Tore 3" },
        new LevelEntry { stageId = "Stage_4", towerName = "Tower_4", sceneName = "Level 4", labelEn = "Tower 4", labelFil = "Tore 4" },
    };

    private static readonly RecipeEntry[] s_recipes =
    {
        new RecipeEntry { id = "recipe_1", sceneName = "Recipe 1", unlockStageId = "Stage_1", nameEn = "Kid-Friendly Sinabawang Gulay", nameFil = "Kid-Friendly Sinabawang Gulay" },
        new RecipeEntry { id = "recipe_2", sceneName = "Recipe 2", unlockStageId = "Stage_2", nameEn = "Filipino Fruit Salad", nameFil = "Filipino Fresh Fruit Salad" },
        new RecipeEntry { id = "recipe_3", sceneName = "Recipe 3", unlockStageId = "Stage_3", nameEn = "Filipino Clubhouse Sandwich", nameFil = "Filipino Clubhouse Sandwich" },
        new RecipeEntry { id = "recipe_4", sceneName = "Recipe 4", unlockStageId = "Stage_4", nameEn = "Veggie Stir-fried Chicken Meal for Kids", nameFil = "Veggie Stir-Fried Chicken Meal for Kids" },
    };

    public static IReadOnlyList<LevelEntry> Levels => s_levels;
    public static IReadOnlyList<RecipeEntry> Recipes => s_recipes;

    public static LevelEntry LevelForStage(string stageId)
    {
        for (int i = 0; i < s_levels.Length; i++)
        {
            if (s_levels[i].stageId == stageId) return s_levels[i];
        }
        return null;
    }

    public static LevelEntry LevelForTower(string towerName)
    {
        for (int i = 0; i < s_levels.Length; i++)
        {
            if (s_levels[i].towerName == towerName) return s_levels[i];
        }
        return null;
    }

    public static LevelEntry LevelForScene(string sceneName)
    {
        for (int i = 0; i < s_levels.Length; i++)
        {
            if (s_levels[i].sceneName == sceneName) return s_levels[i];
        }
        return null;
    }

    public static RecipeEntry RecipeForId(string id)
    {
        for (int i = 0; i < s_recipes.Length; i++)
        {
            if (s_recipes[i].id == id) return s_recipes[i];
        }
        return null;
    }

    /// <summary>
    /// The groups of editable content the Content tab shows. The values are
    /// the same ones the game uses.
    /// </summary>
    public const string ContentGroupCategories = "categories";
    public const string ContentGroupLevels = "levels";
    public const string ContentGroupRewards = "rewards";

    /// <summary>
    /// The content the game ships with, in the shape the Content tab edits.
    ///
    /// The tab shows these until the teacher changes one, so the panel never
    /// starts out empty and the values on screen are the ones the game is
    /// actually using.
    /// </summary>
    public static List<AdminContentStore.ContentEntry> DefaultContent(string group)
    {
        List<AdminContentStore.ContentEntry> result = new List<AdminContentStore.ContentEntry>();

        if (group == ContentGroupLevels)
        {
            for (int i = 0; i < s_levels.Length; i++)
            {
                result.Add(new AdminContentStore.ContentEntry
                {
                    id = s_levels[i].stageId,
                    group = group,
                    name = s_levels[i].labelEn,
                    value = s_levels[i].sceneName,
                    enabled = true
                });
            }
        }
        else if (group == ContentGroupRewards)
        {
            AchievementType[] types = BadgeTypes;
            for (int i = 0; i < types.Length; i++)
            {
                result.Add(new AdminContentStore.ContentEntry
                {
                    id = types[i].ToString(),
                    group = group,
                    name = types[i].ToString(),
                    value = DefaultDropChance(types[i]).ToString(),
                    enabled = true
                });
            }
        }
        else if (group == ContentGroupCategories)
        {
            // The categories the questions are actually tagged with.
            List<string> seen = new List<string>();
            for (int i = 0; i < s_levels.Length; i++)
            {
                List<QuestionData> pool = AdminQuestionCatalog.GetBuiltIn(s_levels[i].sceneName);
                if (pool == null) continue;
                for (int q = 0; q < pool.Count; q++)
                {
                    QuestionData question = pool[q];
                    if (question == null) continue;
                    string category = question.category;
                    if (string.IsNullOrEmpty(category)) continue;
                    category = category.Trim();
                    if (seen.Contains(category)) continue;
                    seen.Add(category);
                    result.Add(new AdminContentStore.ContentEntry
                    {
                        id = category,
                        group = group,
                        name = category,
                        value = category,
                        enabled = true
                    });
                }
            }
        }

        return result;
    }

    /// <summary>Every badge the player can earn, in the order the result screen shows them.</summary>
    public static readonly AchievementType[] BadgeTypes =
    {
        AchievementType.GeniusOfTheTower,
        AchievementType.ConquerorOfTheTower,
        AchievementType.ChallengerOfTheTower,
        AchievementType.StepsTowardsSuccess,
    };

    /// <summary>
    /// Scenes that contain text worth editing. The Text tab can load any of
    /// them so the teacher can reach dialogue, the mascot's lines, the
    /// loading-screen trivia and the help library.
    /// </summary>
    public static readonly string[] TextScenes =
    {
        "MainMenu",
        "TitleScreen",
        "LoadingScene",
        "ChooseYourLanguage",
        "Level 1",
        "Level 2",
        "Level 3",
        "Level 4",
        "ResultsScene",
        "LibraryMenu",
        "TriviaSection",
        "HelpArchive",
        "RecipeChoice",
        "Recipe 1",
        "Recipe 2",
        "Recipe 3",
        "Recipe 4",
        "Introduction",
        "Tower1Cutscene",
        "Tower1CutsceneIntro",
        "Tower2Cutscene",
        "Tower3Cutscene",
        "Tower4Cutscene",
    };
}
