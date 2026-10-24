using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// Command line build entry point for the Android player.
///
/// The project had no build script at all, which meant the only way to build
/// was the Build Settings window in an open Editor. That is fine for a one
/// off build but it makes the build impossible to run unattended, and it
/// means a failing build has to be reproduced by hand every time.
///
/// Usage from a terminal (the Editor must be CLOSED, Unity refuses to open
/// the same project twice):
///
///   "D:\Unity\6000.2.12f1\Editor\Unity.exe" ^
///     -batchmode -quit ^
///     -projectPath "D:\Codes\CODES\Codes\NutriQuest" ^
///     -executeMethod NutriQuestBuild.BuildFromCommandLine ^
///     -logFile "Logs/cli-build.log"
///
/// Add -aab to produce an Android App Bundle instead of an APK.
///
/// The scenes come from ProjectSettings/EditorBuildSettings.asset, so the
/// scene list stays in one place.
///
/// The Unity licence has to be active for a batch build. If the build exits
/// immediately with a licensing error, sign in through the Hub first.
/// </summary>
public static class NutriQuestBuild
{
    private const string OutputFolder = "Builds/Android";
    private const string ApkFileName = "NutriQuest.apk";
    private const string AabFileName = "NutriQuest.aab";

    [MenuItem("NutriQuest/Build/Android APK")]
    public static void BuildAndroidApk()
    {
        Run(BuildTarget.Android, false);
    }

    [MenuItem("NutriQuest/Build/Android App Bundle (.aab)")]
    public static void BuildAndroidAab()
    {
        Run(BuildTarget.Android, true);
    }

    /// <summary>
    /// Entry point for -executeMethod. Exits with 0 on success and 1 on
    /// failure so a CI script can branch on it.
    /// </summary>
    public static void BuildFromCommandLine()
    {
        bool appBundle = Array.IndexOf(Environment.GetCommandLineArgs(), "-aab") >= 0;
        bool succeeded = Run(BuildTarget.Android, appBundle);

        if (Application.isBatchMode)
        {
            EditorApplication.Exit(succeeded ? 0 : 1);
        }
    }

    private static bool Run(BuildTarget target, bool appBundle)
    {
        string[] scenes = EditorBuildSettings.scenes
            .Where(s => s.enabled)
            .Select(s => s.path)
            .ToArray();

        if (scenes.Length == 0)
        {
            Debug.LogError("[NutriQuestBuild] No enabled scenes in the Build Settings.");
            return false;
        }

        string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        string outputDir = Path.Combine(projectRoot, OutputFolder);
        Directory.CreateDirectory(outputDir);

        string outputPath = Path.Combine(outputDir, appBundle ? AabFileName : ApkFileName);
        if (File.Exists(outputPath)) File.Delete(outputPath);

        EditorUserBuildSettings.buildAppBundle = appBundle;

        Debug.Log("[NutriQuestBuild] Building " + target + " (" + scenes.Length + " scenes) -> " + outputPath);

        BuildPlayerOptions options = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = outputPath,
            target = target,
            targetGroup = BuildTargetGroup.Android,
            options = BuildOptions.None,
        };

        BuildReport report = BuildPipeline.BuildPlayer(options);
        BuildSummary summary = report.summary;

        Debug.Log("[NutriQuestBuild] Result: " + summary.result +
                  " | size " + (summary.totalSize / (1024 * 1024)) + " MB" +
                  " | " + summary.totalTime +
                  " | errors " + summary.totalErrors +
                  " | warnings " + summary.totalWarnings);

        if (summary.result != BuildResult.Succeeded)
        {
            Debug.LogError("[NutriQuestBuild] The build FAILED. See the log above for the first error.");
            return false;
        }

        Debug.Log("[NutriQuestBuild] SUCCESS: " + outputPath);
        return true;
    }
}
