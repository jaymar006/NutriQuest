# NutriQuest — Agent Notes

Unity mobile quiz game (English / Filipino nutrition trivia) by SinagStudio. Portrait Android, URP.
Editor version: **6000.2.12f1** (`ProjectSettings/ProjectVersion.txt`).

## Platform

- **This is an Android game.** Portrait 1080x1920, URP, Google Play. `com.unity.adaptiveperformance.google.android` is the real Adaptive Performance provider and `Assets/Plugins/Android/AndroidManifest.xml` is the Android manifest.
- **The Android package name is still the URP template default** — `applicationIdentifier` is `com.UnityTechnologies.UniversalMobile3DTemplate` in `ProjectSettings/ProjectSettings.asset`. That is not unique and will not pass a Google Play review. Change it to an owned name **before** the first release: the package name is the app's identity on the device, so changing it later orphans every existing install and the Play listing.
- **Set the Active Build Target to Android** when doing platform work. With Standalone active you get a harmless but noisy `[Adaptive Performance] No Active Loader was found` warning: `AdaptivePerformanceGeneralSettings.asset` has a loader for the Android target and an **empty** `m_Loaders` list for Standalone, because nobody ever filled in the Editor target. It is pre-existing and cosmetic — do not "fix" it.
- **Burst AOT is disabled for Android** (`ProjectSettings/BurstAotSettings_Android.json` → `EnableBurstCompilation: false`). The game uses no Burst, Mathematics or Collections code; Burst only arrives transitively through URP → collections → burst. Leaving it on added a 175 MB cache and a build step that fails outright when the project is moved (see below).
- Anything that is Android-only should be guarded with `#if UNITY_ANDROID && !UNITY_EDITOR` (see `GooglePlayGamesService` for the pattern).

## Toolchain

- **No `.asmdef` anywhere** → all game code compiles into `Assembly-CSharp`; `Assets/Editor` → `Assembly-CSharp-Editor`. Adding an asmdef changes what can see `UnityEditor` and the runtime code.
- **No tests, no CI** (nothing in `.github/`, no test assemblies). Verification is manual: open `Assets/Scenes/Bootstrap.unity` and press Play. There is no `dotnet`/`msbuild` on this machine, so you cannot compile-check C# from the CLI — reason carefully instead.
- **Run the static checkers before opening Unity.** They stand in for the compiler:
  ```
  powershell -File Tools/check_refs.ps1          -Roots Assets/Scenes/Scripts   # member resolution, using directives, abstract members, downcasts, braces
  powershell -File Tools/check_scopes.ps1        -Roots Assets/Scenes/Scripts   # CS0136: a name reused in a nested scope
  powershell -File Tools/check_missing_scripts.ps1 -Root Assets/Scenes           # GameObjects whose script GUID no longer resolves
  ```
  All three are approximations, not a compiler — they will not catch wrong uGUI/TMP API signatures. The real compiler is the only authority on those.

### Building the Android player

Unity is installed at `D:\Unity\6000.2.12f1\Editor\Unity.exe` (not in the Hub's default location). `Assets/Editor/NutriQuestBuild.cs` is the build entry point; it takes the scene list from `ProjectSettings/EditorBuildSettings.asset` and writes to `Builds/Android/`.

```
# the Editor must be CLOSED - Unity refuses to open the same project twice
"D:\Unity\6000.2.12f1\Editor\Unity.exe" -batchmode -quit ^
  -projectPath "D:\Codes\CODES\Codes\NutriQuest" ^
  -executeMethod NutriQuestBuild.BuildFromCommandLine ^
  -logFile "Logs/cli-build.log"
```

Add `-aab` for an App Bundle instead of an APK. Exit code 0 means success. The same build is available from the Editor menu: **NutriQuest → Build → Android APK**. A cold build takes about 10 minutes; with the caches warm it is under 3.

- Solution is `NutriQuest.slnx` (Unity 6 format). `.vscode/settings.json` still points at a non-existent `NutriQuest.sln` — ignore it.
- `.gitignore` excludes `*.csproj`, `*.sln`, `Library/`, `Temp/`, `Builds/`, `Logs/`. Never commit generated IDE files or Unity caches, and never commit the APK or build logs.
- Key packages: Input System 1.14.2, Localization 1.5.12 (pulls Addressables 2.7.4 transitively), URP 17.2.0, Post-processing 3.5.4.

## Release readiness

The Android player builds and installs, but it is **not yet shippable to Google Play**. From `aapt2 dump badging` on the built APK:

| Item | Current value | Problem |
|---|---|---|
| Package name | `com.UnityTechnologies.UniversalMobile3DTemplate` | Still the URP template default. Not unique, Play will reject it. |
| Signing | `CN=Android Debug` | `androidUseCustomKeystore: 0` and `AndroidKeystoreName` is empty, so it is signed with the **debug** key. A release keystore must be created and **backed up** — losing it means the listing can never be updated. |
| `versionName` | `B_1.0` | A placeholder. |
| `versionCode` | `1` | Must be incremented for every Play upload; a duplicate is rejected. |
| `minSdk` / `targetSdk` | 23 / 36 | Fine. Play needs target 35+. |
| Architectures | ARM64 only | Fine, and it keeps the APK small. |
| Permissions | `INTERNET`, `ACCESS_NETWORK_STATE`, `POST_NOTIFICATIONS` | `INTERNET` is needed by `NetworkChecker`. Check this against the **Families policy** if the game is aimed at children — the Unity Cloud Diagnostics service is also enabled. |

### APK size (118 MB)

| Entry | Size | Note |
|---|---|---|
| `libil2cpp.so` | 82 MB | `com.unity.visualscripting` is installed but **completely unused** (0 graph assets, 0 code references, 0 assets point at it). Removing it from `Packages/manifest.json` should shrink this substantially. Managed stripping is also left at the default. |
| `sharedassets0.resource` | 51 MB | Mostly the 7 `Assets/Sprites/UI/Icons/Trivia *.png` sprites. Their sources are 6112x10012 and import at `maxTextureSize: 2048`, so each is 2048x3354 — about 6.9 MB of ETC2/ASTC each. Dropping them to 1024 would save roughly 35 MB. |
| `libunity.so` | 22 MB | Engine, normal. |
| `global-metadata.dat` | 13 MB | IL2CPP metadata. |

47 textures in `Assets` are over 1 MB (138 MB of source art) even though the import settings already cap them at 2048 and compress them. The problem is the **source** resolution, not the import settings.

## Boot & scene flow

- Build index 0 is `Assets/Scenes/Bootstrap.unity`. `BootstrapManager` spawns 4 persistent singletons **in a fixed order** — `SceneTransitionManager` → `UniversalMusicManager` → `QuizSFXManager` → `CutsceneManager` — then loads `TitleScene` one frame later. Both the order and the 1-frame delay are load-bearing (the file header explains why). Do not reorder.
- Full flow: `Bootstrap` → `TitleScreen` → `ChooseYourLanguage` (fresh install) → name input → `Introduction` cutscene → `MainMenu`. The title screen is **tap-to-continue**, so it looks hung if you just watch it. Setting PlayerPrefs `LanguageSelected = 1` sends a returning player straight to `MainMenu`.
- **The main menu is tabbed, not a stack of screens.** `TabsManager` swaps three full-screen panels: `ProfileWindow` (active on load), `SettingsWindow`, `QuitWindow`. The Settings and Quit panels are **inactive until their nav button is clicked**, so anything you parent to `SettingsWindow` only exists while that tab is open.
- All scene changes go through `SceneTransitionManager.Instance.NavigateTo(sceneName, useLoadingScreen)` — pass the scene **name** (`"Level 1"`), not a path. It warns for scenes missing from `ProjectSettings/EditorBuildSettings.asset`, and a 20s watchdog force-resets a stuck transition; `[ContextMenu] Force Reset Transition State (debug)` is the escape hatch.
- A new scene must be added to `ProjectSettings/EditorBuildSettings.asset` or `NavigateTo` will not find it.
- `Assets/Scenes/Scripts/Core/NavigationManager.cs` is a parallel, mostly unused navigation path with hardcoded scene-name constants. Don't route new work through it.
- **`LoginPage.unity` and `Levels/TestLevel0.unity` are orphans** — not in Build Settings, and their Main Camera has no `UniversalAdditionalCameraData`. Playing either gives a black screen plus `Camera Main Camera does not contain an additional camera data component`. Every scene you are supposed to play has that component. Don't debug the camera when you opened the wrong scene.

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

- **The entry point is a real GameObject in the scene, not a runtime injection.** `Assets/Scenes/MainMenu.unity` has an `AdminVersionButton` under the `SettingsTXT` heading (the "Settings" title), carrying `Assets/Scenes/Scripts/Admin/AdminVersionButton.cs`. `Tools/add_admin_version_button.ps1` is the script that authored it and documents exactly what was added. You can move and restyle it like any other UI.
- **The panel is hidden behind a gesture.** One tap does nothing: `AdminVersionButton` needs 5 taps within 3 seconds before the PIN prompt appears. The title screen's version label uses the same gesture.
- `AdminEntryPoint` is only a **fallback**: it injects a version label into a settings panel that has no authored `AdminVersionButton`, and adds one to the title screen's version label. It skips the main menu because that one is authored.
- The main menu's nav bar (`tabs`) sits in the **top-right** of the tab area and the `Pages` panel is drawn over it, so the top-right of the settings tab is not free space. The version label is anchored to the "Settings" heading instead.
- **The override layer is the key idea.** A shipped build cannot write to scene assets, so `AdminContentStore` keeps a JSON shadow document in `Application.persistentDataPath` and every tunable reads through it with the scene value as the fallback. Clearing an override restores the built-in behaviour.
- **The game reads the catalogues, not the store.** `QuestionGeneratorBeta` gets its pool from `AdminQuestionCatalog.GetEffective(levelKey)`, and `AdminTextOverrideApplier` writes the merged recipe from `AdminRecipeCatalog.GetMerged(id)` into the recipe scene. One source of truth, so what the panel shows is what the player gets.
- `AdminGameConfig` is the single registry of levels, towers, recipes, tunable keys and the shipped content for the categories/levels/rewards groups. `ProgressKeys` is the single registry of every PlayerPrefs key. Both exist because the identifiers used to be duplicated in eight places and had already drifted.
- `AdminSecurity` stores a salted PBKDF2 hash in `admin_pin.json`. A fresh device starts on the **default PIN `1234`** and is flagged `MustChangePin`; the panel then only offers the Security tab until it is replaced. This is a parental gate, not a security boundary: the hash and algorithm are both on the device.
- `AdminContentStore` saves atomically (temp file, then move), quarantines a config it cannot parse to `admin_content.corrupt.json` instead of silently losing it, keeps the last 5 saves as backups, and migrates older schema versions on load.
- `AdminTextOverrideApplier` swaps any TextMeshPro string whose text matches an override, which is how dialogue, mascot lines, loading trivia and the help library become editable without touching the scene assets.
- Export/import go through the clipboard (`GUIUtility.systemCopyBuffer`) because Android gives an app no user-visible file path and the project has no file-picker plugin. They are also written to `persistentDataPath/admin_exports/`.
- `AdminPatchService` handles content patches: export, validate before applying, back up first, and roll back. `ApplyPatchFromRemote` takes patch text that someone else fetched, so the admin system itself never does networking.
- **The panel is built entirely in code** (`AdminUi` + `AdminPanelRoot` + 8 tabs), so it needs no prefab and no scene wiring. `AdminSafeAreaFitter` keeps it out of the notch and the gesture bar, and `AdminPanelRoot.Confirm` is the shared modal used by every destructive action.
- `AdminValidation` holds the input rules shared by the Questions and Recipes tabs (no empty text, exactly one correct answer, no duplicates), so the two cannot drift apart on what "valid" means.
- `QuestionData` gained `isEnabled`, `category`, `difficulty` and `explanation`. The new fields default to enabled/empty, so the questions already in the level scenes keep working unchanged, and `questions.json` was regenerated to match.


## Which script set is live

- `Assets/Scenes/Scripts/Gameplay/BetaScripts/` is the **live** gameplay path used by Level 1–4: `QuestionGeneratorBeta` → `QuestionDisplayBeta` → `AnswerBTNFunction2` (plus `QuestionTimer`, `ScoreManager`, `CatCompanion`).
- The non-Beta `Gameplay/QuestionGeneration.cs`, `QuestionDisplay.cs`, `AnswerBTNFunction.cs` are wired only into the legacy `Assets/Scenes/Gameplay.unity`, which is **not in build settings**. Editing them does not change the shipping game.

## Localization

- `com.unity.localization`; string tables in `Assets/Localization/<Table>_{en,fil}.asset` plus `fil-PH`; addressable groups under `Assets/AddressableAssetsData`.
- `LocalizationManager` only ever selects locale **`en`** or **`fil-PH`**, and stores the PlayerPrefs code `en` / `fil`. The `fil` localisation tables in `Assets/Localization/` exist but are unused.
- `LocalizationManager.OnLanguageChanged` is a **static event**: subscribe in `OnEnable` and unsubscribe in `OnDisable` (or Awake/OnDestroy), or you leak callbacks across scene loads.

## Persistence

Two independent stores:

- **PlayerPrefs** — scores, unlocks, badges, hints, language, player name. Prefixed keys: `HighScore_<stageID>`, `FirstClear_<stageID>`, `Badge_<stageID>_<AchievementType>`, `Attempts_<stageID>`, `UnlockTime_<towerName>`, `Unlocked_…`. **Every key is declared once in `ProgressKeys`** — use it, do not write the literal.
- **`LocalSaveManager` + `SaveDataSerializer`** — JSON at `Application.persistentDataPath/nutriquest_save.json` (+ `nutriquest_save_backup.json`).

**stageID strings are not consistent**: towers use `Stage_1..Stage_4`, recipes use `Tower1_Stage1`. Mixing them silently breaks unlocks — `DevCheatTool` carries a comment describing exactly that bug. Always copy the literal from the Inspector field of the component that reads it.

`ResultScreenManager` (in `Assets/Scenes/Levels/ResultsScene.unity`) gets its stageID from `ResultData`, which the level scene writes to PlayerPrefs.

## Gotchas that cost real time

- **Source files mix encodings.** Most `.cs` files are Windows-1252 (CP1252) where an em dash is a single byte `0x97`; some are UTF-8 with a BOM. Reading a CP1252 file as UTF-8 shows `�`, and saving it back that way **silently replaces the character with U+FFFD**. Before editing one, check its encoding, and keep it byte-identical apart from your change. New files should be plain UTF-8 without a BOM.
- **Moving the project breaks the Burst cache.** `Library/BurstCache` stores absolute paths, so after a move the Burst AOT step dies with `EndOfStreamException: Unable to read beyond the end of the stream` while reading a `.bhc` header, and the whole Android build fails at `AsyncPluginsFromLinker`. Fix: delete `Library/BurstCache` and `Library/Bee/artifacts/Android/AsyncPluginsFromLinker*`. Burst AOT is now off for Android, so this should not recur.
- **Three GameObjects reference scripts that were deleted in old refactors.** Unity logs `Script attached to X is missing` for them: `Debug` in Level 1-4 (was `Assets/Scripts/DebugTool.cs`), `Back Button` in `RecipeChoice.unity` (was `UISoundFX.cs`), and a null script on `Button` in `Recipe 3.unity`. They are warnings, not build failures, and the objects are dead. `Tools/check_missing_scripts.ps1` lists them.
- **A `LayoutElement` with no height collapses to zero size inside a layout group**, which makes the element invisible *and* unclickable. The admin panel's factory (`AdminUi.CreateText/CreateButton/CreateInput/CreateToggle`) always sets a height for this reason, and every caller goes through `AdminUi.EnsureLayout` rather than `AddComponent<LayoutElement>()` — a second one would be ignored by `GetComponent` and the caller's width silently lost. `Tools/check_refs.ps1` fails the build if anything outside `AdminUi.cs` adds one directly.
- **`File.Move` has no overwrite overload** in the .NET Standard 2.1 profile Unity uses. `File.Move(from, to, true)` is a compile error; delete the target first.
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
