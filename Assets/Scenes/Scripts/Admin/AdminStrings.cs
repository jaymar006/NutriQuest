using System.Collections.Generic;

/// <summary>
/// Bilingual (English / Filipino) strings for the admin panel.
///
/// The panel builds its whole UI at runtime from these strings rather than
/// duplicating scene objects for each language the way the rest of the game
/// does. That keeps the admin panel to one code path and lets the teacher
/// flip languages from inside the panel without leaving the screen.
///
/// Index 0 is English, index 1 is Filipino.
/// </summary>
public static class AdminStrings
{
    private static readonly Dictionary<string, string[]> s_table = new Dictionary<string, string[]>
    {
        // ----- Shell -----
        { "title",            new[] { "Admin Panel",           "Panel ng Tagapangasiwa" } },
        { "subtitle",         new[] { "Teacher / parent tools", "Mga kagamit para sa guro / magulang" } },
        { "close",            new[] { "Close",                 "Isara" } },
        { "reset",            new[] { "Reset",                 "I-reset" } },
        { "language",         new[] { "Language",              "Wika" } },
        { "english",          new[] { "English",               "Ingles" } },
        { "filipino",         new[] { "Filipino",              "Filipino" } },
        { "revision",         new[] { "Content revision",      "Pagbabago ng nilalaman" } },
        { "buildVersion",     new[] { "Build version",         "Bersyon ng build" } },
        { "search",           new[] { "Search",                "Maghanap" } },
        { "builtIn",          new[] { "built-in",              "nakapaloob" } },
        { "yes",              new[] { "Yes",                   "Oo" } },
        { "no",               new[] { "No",                    "Hindi" } },
        // ----- Tabs -----
        { "tabProgress",      new[] { "Progress",              "Usad" } },
        { "tabTimers",        new[] { "Timers & Difficulty",   "Mga timer at Antas" } },
        { "tabQuestions",     new[] { "Questions",             "Mga tanong" } },
        { "tabRecipes",       new[] { "Recipes & Ingredients", "Mga resipe at sangkap" } },
        { "tabText",          new[] { "Text & Dialogue",       "Teksto at Usapan" } },
        { "tabContent",       new[] { "Content & Version",     "Nilalaman at Bersyon" } },
        { "tabAudit",         new[] { "Activity Log",          "Talaan ng Gawain" } },
        { "tabSecurity",      new[] { "Security",              "Seguridad" } },

        // ----- PIN gate -----
        { "pinEnterTitle",    new[] { "Enter your PIN",        "Maglagay ng iyong PIN" } },
        { "pinCreateTitle",   new[] { "Create a PIN",          "Gumawa ng PIN" } },
        { "pinCreateExplain", new[] {
            "This device has no admin PIN yet. Choose one only you know. You will need it every time you open this panel.",
            "Wala pang admin PIN sa device na ito. Pumili ng isang alam mo lamang. Kailangan mo ito tuwing bubuksan ang panel na ito." } },
        { "pinNew",           new[] { "New PIN",               "Bagong PIN" } },
        { "pinConfirm",       new[] { "Confirm PIN",           "Kumpirmahin ang PIN" } },
        { "pinEnter",         new[] { "PIN",                   "PIN" } },
        { "pinRequired",      new[] { "Enter your PIN.",        "Maglagay ng iyong PIN." } },
        { "unlock",           new[] { "Unlock",                "Buksan" } },
        { "create",           new[] { "Create",                "Gawin" } },
        { "pinMismatch",      new[] { "The two PINs do not match.", "Hindi magkatugma ang dalawang PIN." } },
        { "pinTooShort",      new[] { "Use at least 6 digits.", "Gumamit ng hindi bababa sa 6 na digit." } },
        { "pinDigitsOnly",    new[] { "Digits only.",          "Digit lamang." } },
        { "pinLocked",        new[] { "Too many attempts. Try again in {0} seconds. To reset now, clear this app's data in the Android settings.",
                                      "Masyadong maraming pagsubok. Subukan muli sa {0} segundo. Para i-reset ngayon, burahin ang data ng app na ito sa mga setting ng Android." } },

        // ----- Progress -----
        { "highScore",        new[] { "High score",            "Mataas na iskor" } },
        { "firstClear",       new[] { "Cleared at least once", "Nasunod nang minsan" } },
        { "attempts",         new[] { "Attempts",              "Mga pagsubok" } },
        { "badges",           new[] { "Badges",                "Mga badge" } },
        { "runeKeys",         new[] { "Rune keys",             "Mga susing rune" } },
        { "hints",            new[] { "Hints",                 "Mga pahint" } },
        { "resetProgress",    new[] { "Reset all progress",    "I-reset ang lahat ng usad" } },
        { "confirmReset",     new[] { "This clears every score, badge, hint and unlock. This cannot be undone.",
                                      "I-clear ang lahat ng iskor, badge, pahint at unlock. Hindi na ito mababawi." } },

        // ----- Timers and difficulty -----
        { "timers",           new[] { "Timers & Difficulty",   "Mga timer at Antas" } },
        { "questionSeconds",  new[] { "Seconds per question",  "Segundo bawat tanong" } },
        { "questionsPerGame", new[] { "Questions per round",   "Mga tanong koplay" } },
        { "delayNext",        new[] { "Delay between questions (s)", "Paghintay sa pagitan ng tanong (s)" } },
        { "towerWait",        new[] { "Tower wait time (min)", "Oras ng paghintay ng tore (min)" } },
        { "tower4Total",      new[] { "Tower 4 combined score", "Kombinasyong iskor ng Tore 4" } },
        { "maxHints",         new[] { "Hints per tower",       "Mga pahint kada tore" } },
        { "maxRuneKeys",      new[] { "Rune key cap",          "Limitas ng susing rune" } },
        { "regenMinutes",     new[] { "Rune key regen (min)",  "Regenerasyon ng rune (min)" } },
        { "costNewTower",     new[] { "Rune keys: first try",  "Rune key: unang subok" } },
        { "costRetry",        new[] { "Rune keys: replay",     "Rune key: ulitin" } },
        { "dropChance",       new[] { "Rune key drop chance (%)", "Pagkakataon ng rune key (%)" } },
        { "hintDropChance",   new[] { "Hint drop chance (%)",  "Pagkakataon ng pahint (%)" } },
        { "passTarget",       new[] { "Correct answers to pass", "Tama na sagot para pumasa" } },
        { "conquerorTarget",  new[] { "Correct answers for Conqueror", "Tama na sagot para sa Conqueror" } },
        { "loadingMin",       new[] { "Minimum loading time (s)", "Pinakamaikling oras ng loading (s)" } },
        { "badgeGenius",      new[] { "Genius of the Tower",   "Genius ng Tore" } },
        { "badgeConqueror",   new[] { "Conqueror of the Tower", "Conqueror ng Tore" } },
        { "badgeChallenger",  new[] { "Challenger of the Tower", "Challenger ng Tore" } },
        { "badgeSteps",       new[] { "Steps toward success",  "Hakbang tungo sa tagumpay" } },

        // ----- Questions -----
        { "questions",        new[] { "Questions",             "Mga tanong" } },
        { "level",            new[] { "Level",                 "Antas" } },
        { "questionText",     new[] { "Question",              "Tanong" } },
        { "answerA",          new[] { "Answer A",              "Sagot A" } },
        { "answerB",          new[] { "Answer B",              "Sagot B" } },
        { "answerC",          new[] { "Answer C",              "Sagot C" } },
        { "answerD",          new[] { "Answer D",              "Sagot D" } },
        { "correctAnswer",    new[] { "Correct answer",        "Tama na sagot" } },
        { "addQuestion",      new[] { "Add question",          "Magdagdag ng tanong" } },
        { "removeQuestion",   new[] { "Remove",                "Alisin" } },
        { "restoreQuestions", new[] { "Restore built-in questions", "Ibalik ang mga tanong" } },
        { "questionCount",    new[] { "{0} questions",          "{0} tanong" } },

        // ----- Recipes -----
        { "recipes",          new[] { "Recipes & Ingredients", "Mga resipe at sangkap" } },
        { "recipe",           new[] { "Recipe",                "Resipe" } },
        { "unlockStage",      new[] { "Unlocks after clearing", "Nakabukas pagkatapos" } },
        { "restoreRecipe",    new[] { "Restore built-in recipe", "Ibalik ang resipe" } },
        { "sectionText",      new[] { "Text",                  "Teksto" } },

        // ----- Text editing -----
        { "source",           new[] { "Source",                "Pinagmulan" } },
        { "currentScene",     new[] { "Current scene",         "Kasalukuyang scene" } },
        { "originalText",     new[] { "Original",              "Original" } },
        { "newText",          new[] { "New text",              "Bagong teksto" } },
        { "noEditableText",   new[] { "No editable text found here.", "Walang m-editableng teksto dito." } },
        { "textCount",        new[] { "{0} strings",           "{0} na string" } },

        // ----- Content and version -----
        { "content",          new[] { "Content & Version",     "Nilalaman at Bersyon" } },
        { "displayVersion",   new[] { "Displayed version",     "Ipinapakitang bersyon" } },
        { "exportConfig",     new[] { "Export configuration",  "I-export ang konpigurasyon" } },
        { "importConfig",     new[] { "Import configuration",  "I-import ang konpigurasyon" } },
        { "copyClipboard",    new[] { "Copy to clipboard",     "Kopyahin sa clipboard" } },
        { "pasteClipboard",   new[] { "Paste from clipboard",  "I-paste mula sa clipboard" } },
        { "importHint",       new[] { "Paste an exported configuration here. It merges with the current settings.",
                                      "I-paste ang nai-export na konpigurasyon dito. Pinagsasama ito sa kasalukuyang mga setting." } },
        { "copied",           new[] { "Copied to clipboard.",  "Nakopya sa clipboard." } },
        { "imported",         new[] { "Configuration imported.", "Na-import ang konpigurasyon." } },
        { "importFailed",     new[] { "Import failed: {0}",    "Nabigo ang import: {0}" } },
        { "restoreSection",   new[] { "Restore this section",  "Ibalik ang seksyong ito" } },

        // ----- Audit -----
        { "audit",            new[] { "Activity Log",          "Talaan ng Gawain" } },
        { "auditEmpty",       new[] { "Nothing has been changed yet.", "Wala pang binago." } },

        // ----- Security -----
        { "security",         new[] { "Security",              "Seguridad" } },
        { "changePin",        new[] { "Change PIN",            "Palitan ang PIN" } },
        { "currentPin",       new[] { "Current PIN",           "Kasalukuyang PIN" } },
        { "pinChanged",       new[] { "PIN changed.",          "Napalitan ang PIN." } },
        { "failedAttempts",   new[] { "Failed attempts",       "Mga nabigong pagsubok" } },
        { "wipeConfig",       new[] { "Erase all admin settings", "Burahin ang lahat ng admin setting" } },
        { "confirmWipe",      new[] { "This erases every admin setting and the PIN on this device. Continue?",
                                      "Buburahin nito ang lahat ng admin setting at ang PIN sa device na ito. Magpatuloy?" } },

        // ----- Shared by several tabs -----
        { "cancel",           new[] { "Cancel",                "Kanselahin" } },
        { "apply",            new[] { "Apply",                 "Ilapat" } },
        { "save",             new[] { "Save",                  "I-save" } },
        { "back",             new[] { "Back",                  "Bumalik" } },
        { "add",              new[] { "Add",                   "Magdagdag" } },
        { "delete",           new[] { "Delete",                "Burahin" } },
        { "enable",           new[] { "Enabled",               "Naka-enable" } },
        { "disable",          new[] { "Disabled",              "Naka-disable" } },
        { "category",         new[] { "Category",              "Kategorya" } },
        { "difficulty",       new[] { "Difficulty",            "Antas ng hirap" } },
        { "explanation",      new[] { "Explanation",           "Paliwanag" } },
        { "description",      new[] { "Description",           "Paglalarawan" } },
        { "ingredients",      new[] { "Ingredients",           "Mga sangkap" } },
        { "steps",            new[] { "Steps",                 "Mga hakbang" } },
        { "none",             new[] { "None",                  "Wala" } },
        { "invalidValue",     new[] { "That value is not valid.", "Hindi wasto ang halagang iyon." } },
        { "emptyText",        new[] { "The text cannot be empty.", "Hindi maaaring walang teksto." } },
        { "duplicateText",    new[] { "That text already exists.", "Nauunang teksto na iyon." } },
        { "oneCorrect",       new[] { "Pick exactly one correct answer.", "Pumili ng eksaktong isang tamang sagot." } },

        // ----- Security -----
        { "adminEnabled",     new[] { "Admin system enabled",   "Naka-enable ang admin system" } },
        { "adminDisabledNote",new[] { "When this is off, no gesture opens the panel.",
                                      "Kapag naka-off ito, walang gesture na makakabukas ng panel." } },
        { "lockoutAttempts",  new[] { "Attempts before lockout", "Mga pagsubok bago mag-lockout" } },
        { "lockoutMinutes",   new[] { "Lockout length (minutes)", "Haba ng lockout (minuto)" } },
        { "mustChangePin",    new[] { "Change the default PIN to continue.", "Palitan ang default PIN para magpatuloy." } },
        { "defaultPinNote",   new[] { "This device is still on the default PIN. Change it below before using the other tabs.",
                                      "Nasa default PIN pa ang device na ito. Palitan ito sa ibaba bago gamitin ang iba pang tab." } },
        { "pinSet",           new[] { "A PIN is set on this device.", "May PIN nang nakatakda sa device na ito." } },

        // ----- Content and patches -----
        { "categories",       new[] { "Categories",             "Mga kategorya" } },
        { "levels",           new[] { "Levels",                "Mga antas" } },
        { "rewards",          new[] { "Rewards",               "Mga gantig" } },
        { "addCategory",      new[] { "Add category",          "Magdagdag ng kategorya" } },
        { "addLevel",         new[] { "Add level",             "Magdagdag ng antas" } },
        { "addReward",        new[] { "Add reward",            "Magdagdag ng gantig" } },
        { "exportPatch",      new[] { "Export patch",          "I-export ang patch" } },
        { "importPatch",      new[] { "Import patch",          "I-import ang patch" } },
        { "patchValid",       new[] { "Patch applied.",        "Nailapat ang patch." } },
        { "patchInvalid",     new[] { "The patch is not valid for this game.", "Hindi wasto ang patch para sa laro na ito." } },
        { "rollback",         new[] { "Roll back",             "Ibalik" } },
        { "backups",          new[] { "Backups",               "Mga backup" } },
        { "restoreBackup",    new[] { "Restore this backup",   "Ibalik ang backup na ito" } },
        { "clearAudit",       new[] { "Clear the log",         "Burahin ang talaan" } },
        { "auditCleared",     new[] { "The log was cleared.",  "Binura ang talaan." } },
        { "oldValue",         new[] { "From",                  "Mula" } },
        { "newValue",         new[] { "To",                    "Patungo" } },

        // ----- Confirmation messages -----
        { "confirmDeleteRecipe", new[] { "Delete this recipe? The built-in recipe comes back if you restore it.",
                                        "Burahin ang resip na ito? Balik ang built-in na resipe kung ito ang ibalik mo." } },
        { "confirmDeleteQuestion", new[] { "Delete this question? It cannot be brought back except by restoring the built-in questions.",
                                          "Burahin ang tanong na ito? Hindi ito maibalik maliban sa pagbalik ng built-in na mga tanong." } },
        { "confirmClearAudit", new[] { "Clear the whole activity log?",
                                       "Burahin ang buong talaan ng aktibidad?" } },
    };

    /// <summary>Which language the panel currently renders in.</summary>
    public static bool UseFilipino { get; set; }

    /// <summary>Gets a string in the active language, falling back to English.</summary>
    public static string Get(string key)
    {
        if (!s_table.TryGetValue(key, out string[] pair) || pair == null || pair.Length == 0)
        {
            return key;
        }
        if (UseFilipino && pair.Length > 1 && !string.IsNullOrEmpty(pair[1]))
        {
            return pair[1];
        }
        return pair[0];
    }

    /// <summary>Gets a string with {0} replaced by a value.</summary>
    public static string Get(string key, object arg0)
    {
        return string.Format(Get(key), arg0);
    }

    /// <summary>True when the key exists in the table.</summary>
    public static bool Has(string key)
    {
        return s_table.ContainsKey(key);
    }
}
