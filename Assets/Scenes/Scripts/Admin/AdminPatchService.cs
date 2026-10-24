using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// Content patches: a shareable snapshot of the teacher's admin overrides.
///
/// A patch is the override document with some metadata around it, so it can
/// be validated before it is applied and so the device that applies it can
/// tell what it is getting.
///
/// Applying a patch is deliberately not a blind overwrite. The patch is
/// validated first, and the current configuration is backed up before
/// anything changes, so a bad patch can be rolled back.
///
/// NETWORKING
/// There is no networking in this class, and there should not be: the admin
/// system is not allowed to depend on the internet to work. There is one
/// hook, ApplyPatchFromRemote, which takes the text of a patch that has
/// already been fetched by someone else. That keeps the fetch (a retry, a
/// timeout, a certificate check, an offline mode) out of the admin code and
/// makes it obvious where that decision belongs.
/// </summary>
public static class AdminPatchService
{
    public const string PatchFileName = "nutriquest_patch.json";
    public const int PatchSchemaVersion = 1;

    /// <summary>A patch file: the overrides plus some metadata.</summary>
    [Serializable]
    public class Patch
    {
        public int patchSchemaVersion = PatchSchemaVersion;
        public string gameVersion = "";
        public string createdUtc = "";
        public int contentRevision = 0;
        public AdminContentStore.Document document = new AdminContentStore.Document();
    }

    public static string ExportPatchJson()
    {
        Patch patch = new Patch
        {
            gameVersion = AdminVersionService.BuildVersion,
            createdUtc = DateTime.UtcNow.ToString("o"),
            contentRevision = AdminContentStore.ContentRevision,
            document = BuildDocumentFromOverrides(),
        };
        return JsonUtility.ToJson(patch, true);
    }

    /// <summary>
    /// Builds a document that holds only the overrides, so importing a patch
    /// does not drag the device's own audit log or content revision along.
    /// </summary>
    private static AdminContentStore.Document BuildDocumentFromOverrides()
    {
        AdminContentStore.Document source = AdminContentStore.ExportDocument();
        AdminContentStore.Document patch = new AdminContentStore.Document
        {
            schemaVersion = source.schemaVersion,
            contentRevision = source.contentRevision,
            updatedUtc = source.updatedUtc,
        };

        for (int i = 0; i < source.strings.Count; i++) patch.strings.Add(source.strings[i]);
        for (int i = 0; i < source.numbers.Count; i++) patch.numbers.Add(source.numbers[i]);
        for (int i = 0; i < source.flags.Count; i++) patch.flags.Add(source.flags[i]);
        for (int i = 0; i < source.texts.Count; i++) patch.texts.Add(source.texts[i]);
        for (int i = 0; i < source.questionPools.Count; i++) patch.questionPools.Add(source.questionPools[i]);
        for (int i = 0; i < source.recipes.Count; i++) patch.recipes.Add(source.recipes[i]);
        for (int i = 0; i < source.content.Count; i++) patch.content.Add(source.content[i]);

        return patch;
    }

    /// <summary>
    /// Checks a patch without applying it. Returns false with a reason when
    /// the patch is not usable on this device.
    /// </summary>
    public static bool ValidatePatchJson(string json, out string error)
    {
        error = "";
        if (string.IsNullOrWhiteSpace(json))
        {
            error = "The patch is empty.";
            return false;
        }

        Patch patch = null;
        try
        {
            patch = JsonUtility.FromJson<Patch>(json);
        }
        catch (Exception e)
        {
            error = "The patch could not be read: " + e.Message;
            return false;
        }

        if (patch == null || patch.document == null)
        {
            error = "The patch does not contain an admin configuration.";
            return false;
        }

        if (patch.patchSchemaVersion > PatchSchemaVersion)
        {
            error = "The patch was made by a newer version of the game.";
            return false;
        }

        if (patch.document.schemaVersion > AdminContentStore.SchemaVersion)
        {
            error = "The patch uses a newer content format.";
            return false;
        }

        return true;
    }

    /// <summary>
    /// Applies a patch over the current configuration. The current state is
    /// backed up first, so it can be rolled back.
    /// </summary>
    public static bool ApplyPatchJson(string json, out string error)
    {
        if (!ValidatePatchJson(json, out error)) return false;

        Patch patch = JsonUtility.FromJson<Patch>(json);

        string importError;
        if (!AdminContentStore.ImportDocument(patch.document, out importError))
        {
            error = importError;
            return false;
        }

        AdminContentStore.AddAudit("Content", "Applied a content patch", "", patch.gameVersion,
            "Patch from " + patch.createdUtc);
        AdminContentStore.Save();

        Debug.Log("[AdminPatchService] Applied a content patch from " + patch.createdUtc +
                  " (game version " + patch.gameVersion + ").");
        return true;
    }

    /// <summary>Writes the patch to a file and returns its path.</summary>
    public static string ExportPatchToFile()
    {
        try
        {
            string folder = AdminContentStore.ExportFolder;
            if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

            string path = Path.Combine(folder,
                "nutriquest_patch_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".json");
            File.WriteAllText(path, ExportPatchJson());
            Debug.Log("[AdminPatchService] Exported the patch to " + path);
            return path;
        }
        catch (Exception e)
        {
            Debug.LogError("[AdminPatchService] Could not export the patch: " + e.Message);
            return null;
        }
    }

    /// <summary>
    /// Applies a patch that was fetched by someone else.
    ///
    /// The admin system does no networking of its own. The caller supplies
    /// the text of the patch, which keeps the retry, the timeout and the
    /// certificate handling out of here. When the project grows a proper
    /// downloader, this is the single place it has to call.
    /// </summary>
    public static bool ApplyPatchFromRemote(string fetchedText, out string error)
    {
        if (string.IsNullOrEmpty(fetchedText))
        {
            error = "The downloaded patch was empty.";
            return false;
        }
        return ApplyPatchJson(fetchedText, out error);
    }

    /// <summary>Lists the patch files that were exported on this device.</summary>
    public static List<string> GetPatchFiles()
    {
        List<string> result = new List<string>();
        try
        {
            string folder = AdminContentStore.ExportFolder;
            if (!Directory.Exists(folder)) return result;
            string[] files = Directory.GetFiles(folder, "nutriquest_patch*.json");
            Array.Sort(files, (a, b) => File.GetLastWriteTimeUtc(b).CompareTo(File.GetLastWriteTimeUtc(a)));
            result.AddRange(files);
        }
        catch { }
        return result;
    }
}
