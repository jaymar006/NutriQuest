# NutriQuest — Agent Notes

Unity mobile quiz game (English / Filipino nutrition trivia) by SinagStudio. Portrait Android, URP.
Editor version: **6000.2.12f1** (`ProjectSettings/ProjectVersion.txt`).

## Toolchain

- **No `.asmdef` anywhere** → all game code compiles into `Assembly-CSharp`; `Assets/Editor` → `Assembly-CSharp-Editor`. Adding an asmdef changes what can see `UnityEditor` and the runtime code.
- **No tests, no CI, no build scripts** (nothing in `.github/`, no `Editor` build automation, no test assemblies). Verification is manual: open `Assets/Scenes/Bootstrap.unity` and press Play. There is no `dotnet`/`msbuild` on this machine, so you cannot compile-check C# from the CLI — reason carefully instead.
- Solution is `NutriQuest.slnx` (Unity 6 format). `.vscode/settings.json` still points at a non-existent `NutriQuest.sln` — ignore it.
- `.gitignore` excludes `*.csproj`, `*.sln`, `Library/`, `Temp/`. Never commit generated IDE files or Unity caches.
- Key packages: Input System 1.14.2, Localization 1.5.12 (pulls Addressables 2.7.4 transitively), URP 17.2.0, Post-processing 3.5.4.

## Boot & scene flow

- Build index 0 is `Assets/Scenes/Bootstrap.unity`. `BootstrapManager` spawns 4 persistent singletons **in a fixed order** — `SceneTransitionManager` → `UniversalMusicManager` → `QuizSFXManager` → `CutsceneManager` — then loads `TitleScene` one frame later. Both the order and the 1-frame delay are load-bearing (the file header explains why). Do not reorder.
- All scene changes go through `SceneTransitionManager.Instance.NavigateTo(sceneName, useLoadingScreen)` — pass the scene **name** (`"Level 1"`), not a path. It warns for scenes missing from `ProjectSettings/EditorBuildSettings.asset`, and a 20s watchdog force-resets a stuck transition; `[ContextMenu] Force Reset Transition State (debug)` is the escape hatch.
- A new scene must be added to `ProjectSettings/EditorBuildSettings.asset` or `NavigateTo` will not find it.
- `Assets/Scenes/Scripts/Core/NavigationManager.cs` is a parallel, mostly unused navigation path with hardcoded scene-name constants. Don't route new work through it.

## Where the content actually lives (the JSON is a decoy)

- The **live** questions are serialized **inside each level scene**: `QuestionGeneratorBeta.questionPool`, a `List<QuestionData>` on a component in `Assets/Scenes/Levels/Level 1-4.unity`. Each entry holds English + Filipino text for the question and 4 answers; `correctAnswer` is always `"A"/"B"/"C"/"D"` and is never translated.
- The **live** towers are `TowerUnlockManager.towers` (serialized `TowerEntry` list) in `Assets/Scenes/MainMenu.unity` — `Tower_1..Tower_4` with `requiredStageID` `Stage_1..Stage_3`.
- `Assets/Resources/Data/recipes.json` and `Assets/Resources/Data/questions.json` are **generated catalogues** of that scene content (built by the scripts in `Tools/`), not the live source. The game does not read them; the admin panel does, to seed an edit.
- `DataManager` / `QuestionManager` / the `Question` model are **dead code** — `DataManager.LoadGameData()` is never called. Don't "fix" them expecting the game to consume data.

### Regenerating the catalogues

```
powershell -File Tools/build_recipe_catalog.ps1     # -> Assets/Resources/Data/recipes.json
powershell -File Tools/build_question_catalog.ps1   # -> Assets/Resources/Data/questions.json
```

Both parse the Unity scene YAML directly. Run them after editing a level scene or a recipe scene, then commit the JSON.

## Teacher/parent admin panel

`Assets/Scenes/Scripts/Admin/` — a PIN-gated panel that lets a teacher change progress, questions, timers, difficulty, recipes, on-device text and the displayed version. It **ships in release builds**.

- **No scene or prefab is needed.** The whole UI is built in code at runtime (`AdminUi` factory + `AdminPanelRoot`), and the entry point is injected at runtime by `AdminEntryPoint` into the `SettingsWindow` of `MainMenu.unity` (a "Version X.Y.Z" button) and onto the title screen's version label. Nothing was authored into the scene, so there is no `.unity` diff to review.
- **The override layer is the key idea.** A shipped build cannot write to scene assets, so `AdminContentStore` keeps a JSON shadow document in `Application.persistentDataPath` and every tunable reads through it with the scene value as the fallback. Clearing an override restores the built-in behaviour.
- `AdminGameConfig` is the single registry of levels, towers, recipes and tunable keys. `ProgressKeys` is the single registry of every PlayerPrefs key. Both exist because the identifiers used to be duplicated in eight places and had already drifted.
- `AdminSecurity` stores a salted PBKDF2 hash in `admin_pin.json`. **There is no default PIN in the build** — the first visit asks the teacher to create one. This is a parental gate, not a security boundary: the hash and algorithm are both on the device.
- `AdminTextOverrideApplier` swaps any TextMeshPro string whose text matches an override, which is how dialogue, mascot lines, loading trivia and the help library become editable without touching the scene assets.
- Export/import go through the clipboard (`GUIUtility.systemCopyBuffer`) because Android gives an app no user-visible file path and the project has no file-picker plugin.

## Which script set is live

- `Assets/Scenes/Scripts/Gameplay/BetaScripts/` is the **live** gameplay path used by Level 1–4: `QuestionGeneratorBeta` → `QuestionDisplayBeta` → `AnswerBTNFunction2` (plus `QuestionTimer`, `ScoreManager`, `CatCompanion`).
- The non-Beta `Gameplay/QuestionGeneration.cs`, `QuestionDisplay.cs`, `AnswerBTNFunction.cs` are wired only into the legacy `Assets/Scenes/Gameplay.unity`, which is **not in build settings**. Editing them does not change the shipping game.

## Localization

- `com.unity.localization`; string tables in `Assets/Localization/<Table>_{en,fil}.asset` plus `fil-PH`; addressable groups under `Assets/AddressableAssetsData`.
- `LocalizationManager` only ever selects locale **`en`** or **`fil-PH`**, and stores PlayerPrefs code `en` / `fil`. The `fil` tables exist but are unused.
- The language codes are `fil` (Filipino) and `en` (English). The Unity locale used at runtime is `fil-PH`; the PlayerPrefs code is `fil`. The `fil` localisation tables in `Assets/Localization/` exist but are unused.
- `LocalizationManager.OnLanguageChanged` is a **static event**: subscribe in `OnEnable` and unsubscribe in `OnDisable` (or Awake/OnDestroy), or you leak callbacks across scene loads.

## Persistence

Two independent stores:

- **PlayerPrefs** — scores, unlocks, badges, hints, language, player name. Prefixed keys: `HighScore_<stageID>`, `FirstClear_<stageID>`, `Badge_<stageID>_<AchievementType>`, `Attempts_<stageID>`, `UnlockTime_<towerName>`, `Unlocked_…`. **Every key is declared once in `ProgressKeys`** — use it, do not write the literal.
- **`LocalSaveManager` + `SaveDataSerializer`** — JSON at `Application.persistentDataPath/nutriquest_save.json` (+ `nutriquest_save_backup.json`).

**stageID strings are not consistent**: towers use `Stage_1..Stage_4`, recipes use `Tower1_Stage1`. Mixing them silently breaks unlocks — `DevCheatTool` carries a comment describing exactly that bug. Always copy the literal from the Inspector field of the component that reads it.

`ResultScreenManager` (in `Assets/Scenes/Levels/ResultsScene.unity`) gets its stageID from `ResultData`, which the level scene writes to PlayerPrefs.

## Gotchas that cost real time

- **Source files mix encodings.** Most `.cs` files are Windows-1252 (CP1252) where an em dash is a single byte `0x97`; some are UTF-8 with a BOM. Reading a CP1252 file as UTF-8 shows `�`, and saving it back that way **silently replaces the character with U+FFFD**. Before editing one, check its encoding, and keep it byte-identical apart from your change. New files should be plain UTF-8 without a BOM.
- **Input System only** (`ProjectSettings.asset: activeInputHandler: 1`). Legacy `Input.GetKeyDown` / `Input.GetMouseButtonDown` **throw** at runtime. Use `Keyboard.current` / `Mouse.current` / `Assets/InputSystem_Actions.inputactions`. Two files still use legacy input (`Core/ScreneController.cs`, `UI/LandingPageSplashUI.cs`) but live in scenes excluded from the build.
- Every asset needs its `.meta`; GUIDs are how scenes and prefabs reference scripts. Moving/renaming a `.cs` breaks scene links unless the `.meta` travels with it.
- Names contain spaces and an ampersand: `"Level 1"`, `"Recipe 1"`, `Assets/Scenes/Scripts/Gameplay/Currency&Rwards/`.
- `Assets/Plugins/ParticleEffectForUGUI-main/` is vendored third-party — do not edit.
- `Assets/_Recovery/` holds 9 orphaned scenes (`0 (1).unity` … `0 (8).unity`) from an old autosave; they still reference old script GUIDs. Not source of truth.
- `Assets/Scenes/Scripts/UI/GameplayUI.cs` is a 0-byte file. `Core/ScreneController.cs` (note the typo) is referenced by no scene.
- Scratch areas excluded from the build: `Assets/Test Area/`, `Assets/ASSETS UPDATES/`, `Assets/Scenes/TEST AREA.unity`, `Assets/Scenes/Cutscenes/TEST.unity`, `Assets/Scenes/Levels/TestLevel0.unity`, `Assets/Scenes/LoginPage.unity`, `Assets/Scenes/Gameplay.unity`.

## Dev tooling

- `Assets/Prefabs/Game/CheatDevTool.prefab` (`DevCheatTool.cs`) — in-game cheat panel: unlock towers/badges, set scores, skip timers, reset everything. Self-disables in non-debug builds unless `enableInBuild` is ticked.
- `Assets/Scenes/Scripts/Debug/TowerDebugger.cs` and `ScrollDebugger.cs` — Inspector debug views.

## Repo-local skills & docs

- `.agents/skills/` (pinned by `skills-lock.json`): `unity-csharp-scripting`, `level-design`, `csharp-async`. The `unity-csharp-scripting` skill targets Unity 6.3 (6000.3) while this project is 6000.2 — trust `ProjectVersion.txt` over it.
- In-repo setup guides (authoritative over assumptions): `Assets/Scenes/Scripts/UI/UI_SETUP_GUIDE.md`, `PREFAB_GUIDE.md`, `TMP_SETUP_GUIDE.md`, `PORTRAIT_SETUP_GUIDE.md`.
