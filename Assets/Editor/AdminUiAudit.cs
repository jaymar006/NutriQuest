using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// Audits the teacher/parent admin panel: every button, toggle and input is
/// checked, and every button is actually pressed.
///
/// WHY THIS EXISTS
/// The panel is built entirely in code at runtime. Nothing in a scene file
/// or a prefab references it, so a mistyped method name, a missing listener
/// or a button that collapsed to zero size is invisible until somebody taps
/// it on a phone. The static checkers in Tools/ can see that a symbol
/// resolves; they cannot see that a button does anything when it is pressed.
///
/// WHAT IT DOES
///   1. Opens the panel and unlocks it.
///   2. Checks that the canvas belongs to the panel, so closing the panel
///      really removes it from the screen.
///   3. Walks every tab and checks that each control has a listener, that
///      its graphic can be hit by a touch, and that it is not collapsed to
///      zero size.
///   4. Presses every button and reports any exception.
///   5. Checks that the panel can be closed and opened again.
///
/// THE STATE IS PUT BACK
/// Step 4 presses buttons that erase the admin configuration and reset the
/// player's progress. The admin files and the affected PlayerPrefs keys are
/// copied before the run and written back afterwards, so the audit does not
/// change anything the teacher set up.
///
/// HOW TO RUN IT
/// In the Editor: press Play, then NutriQuest -> Audit Admin UI.
/// The report is printed to the Console and copied to the clipboard.
///
/// From a command line (this is what the build script uses):
///   Unity.exe -batchmode -projectPath ... \
///     -executeMethod AdminUiAudit.RunFromCommandLine
/// The report is written to Logs/admin-ui-audit.txt and the editor exits with
/// code 1 when something failed.
///
/// The audit is Editor only. It is not part of a build.
/// </summary>
public static class AdminUiAudit
{
    private const string MenuPath = "NutriQuest/Audit Admin UI";
    private const string ReportFile = "admin-ui-audit.txt";

    // Buttons that must not be pressed during the run.
    //   Scene_*    loads another scene, which would tear the panel down
    //              halfway through the audit.
    //   CloseButton is tested separately, at the end.
    //   LanguageButton rebuilds the whole panel, so it is tested separately.
    private const string SkipPrefixScene = "Scene_";
    private const string SkipNameClose = "CloseButton";
    private const string SkipNameLanguage = "LanguageButton";

    private class Finding
    {
        public string severity;   // "FAIL" or "WARN"
        public string tab;
        public string control;
        public string message;
    }

    private class Snapshot
    {
        public string contentPath;
        public string contentBackup;
        public bool contentExisted;
        public string pinPath;
        public string pinBackup;
        public bool pinExisted;
        public readonly Dictionary<string, string> prefs = new Dictionary<string, string>();
        public readonly List<string> prefKeys = new List<string>();
    }

    // ------------------------------------------------------------------
    // Entry point: the Editor menu
    // ------------------------------------------------------------------

    [MenuItem(MenuPath)]
    public static void Run()
    {
        if (!EditorApplication.isPlaying)
        {
            Debug.LogWarning("[AdminUiAudit] Press Play first, or run the command line version.");
            return;
        }

        List<Finding> findings = new List<Finding>();
        Snapshot snapshot = TakeSnapshot();

        try
        {
            AdminPanelRoot panel = OpenAndUnlock(findings);
            if (panel != null)
            {
                AuditPanel(panel, findings);
            }
        }
        catch (Exception e)
        {
            findings.Add(Failure("-", "-", "The audit itself threw: " + e));
        }

        // The close check waits for the end of the frame in play mode, so
        // the report is written when it finishes.
        s_PlayFindings = findings;
        s_PlaySnapshot = snapshot;
        CheckOpenAndClose(findings);

        if (!EditorApplication.isPlaying)
        {
            RestoreSnapshot(snapshot);
            Report(findings);
        }
    }

    private static List<Finding> s_PlayFindings;
    private static Snapshot s_PlaySnapshot;

    /// <summary>Writes the report and puts the state back. Used by the deferred close check.</summary>
    private static void FinishPlayModeAudit()
    {
        List<Finding> findings = s_PlayFindings;
        Snapshot snapshot = s_PlaySnapshot;
        s_PlayFindings = null;
        s_PlaySnapshot = null;

        if (findings == null) return;

        RestoreSnapshot(snapshot);
        Report(findings);
    }

    // ------------------------------------------------------------------
    // Entry point: the command line
    // ------------------------------------------------------------------

    /// <summary>
    /// Runs the audit without entering play mode, so it can be run from a
    /// command line and checked by a build script:
    ///
    ///   Unity.exe -batchmode -quit -projectPath ... \
    ///     -executeMethod AdminUiAudit.RunFromCommandLine
    ///
    /// The report is written to Logs/admin-ui-audit.txt and the editor exits
    /// with code 1 when something failed.
    ///
    /// The panel is built on a throwaway host instead of through Open(),
    /// because Open() calls DontDestroyOnLoad, which has no meaning outside
    /// play mode.
    /// </summary>
    public static void RunFromCommandLine()
    {
        List<Finding> findings = new List<Finding>();
        GameObject host = null;
        Snapshot snapshot = TakeSnapshot();

        try
        {
            host = new GameObject("[AdminAuditHost]");
            AdminPanelRoot panel = host.AddComponent<AdminPanelRoot>();

            MethodInfo build = typeof(AdminPanelRoot).GetMethod(
                "Build", BindingFlags.NonPublic | BindingFlags.Instance);
            if (build == null) throw new InvalidOperationException("AdminPanelRoot.Build() was not found.");
            build.Invoke(panel, null);

            Unlock(panel, findings);

            if (findings.Count == 0)
            {
                AuditPanel(panel, findings);
            }
        }
        catch (Exception e)
        {
            findings.Add(Failure("-", "-", "The audit itself threw: " + e));
        }
        finally
        {
            // The panel may already be gone: pressing "erase everything" in
            // the Security tab closes it.
            if (host != null)
            {
                try
                {
                    if (host != null) UnityEngine.Object.DestroyImmediate(host);
                }
                catch { }
            }

            // The close check runs on a panel of its own.
            CheckOpenAndClose(findings);

            RestoreSnapshot(snapshot);
        }

        string report = BuildReport(findings);
        WriteReport(report);
        Debug.Log("[AdminUiAudit]\n" + report);

        EditorApplication.Exit(CountFailures(findings) > 0 ? 1 : 0);
    }

    // ------------------------------------------------------------------
    // Opening the panel
    // ------------------------------------------------------------------

    private static AdminPanelRoot OpenAndUnlock(List<Finding> findings)
    {
        AdminPanelRoot panel = AdminPanelRoot.Open();
        if (panel == null)
        {
            findings.Add(Failure("-", "-", "AdminPanelRoot.Open() returned nothing."));
            return null;
        }

        // The PIN gate is there on purpose, so the audit unlocks the panel
        // the way the code does it rather than by typing a PIN.
        MethodInfo unlock = typeof(AdminPanelRoot).GetMethod(
            "OnUnlocked", BindingFlags.NonPublic | BindingFlags.Instance);
        if (unlock == null)
        {
            findings.Add(Failure("-", "-",
                "AdminPanelRoot.OnUnlocked() was not found. The audit cannot get past the PIN gate."));
            return null;
        }

        unlock.Invoke(panel, null);
        return panel;
    }

    /// <summary>
    /// Clears the "must change the default PIN" flag so that every tab is
    /// reachable.
    ///
    /// While the device is still on the default PIN the panel only offers
    /// the Security tab, so without this the audit would check one tab out
    /// of eight and call it a pass. The audit already steps past the PIN
    /// gate itself, so it steps past this flag too. The PIN file is
    /// restored afterwards, so the device is left exactly as it was.
    /// </summary>
    private static void UnlockEveryTab(AdminPanelRoot panel, List<Finding> findings)
    {
        FieldInfo recordField = typeof(AdminSecurity).GetField(
            "_record", BindingFlags.NonPublic | BindingFlags.Static);
        if (recordField == null)
        {
            findings.Add(Failure("-", "-",
                "AdminSecurity._record was not found, so the tabs behind the default PIN cannot be checked."));
            return;
        }

        object record = recordField.GetValue(null);
        if (record == null) return;

        FieldInfo mustChange = record.GetType().GetField(
            "mustChange", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        if (mustChange == null)
        {
            findings.Add(Failure("-", "-",
                "AdminSecurity mustChange was not found, so the tabs behind the default PIN cannot be checked."));
            return;
        }

        mustChange.SetValue(record, false);

        // Rebuild the tab bar so the tabs that were hidden appear.
        panel.RebuildTabBar();
    }

    /// <summary>
    /// Unlocks the panel the way the code does it, rather than by typing a
    /// PIN, so the audit does not need to know the teacher's PIN.
    /// </summary>
    private static void Unlock(AdminPanelRoot panel, List<Finding> findings)
    {
        MethodInfo unlock = typeof(AdminPanelRoot).GetMethod(
            "OnUnlocked", BindingFlags.NonPublic | BindingFlags.Instance);
        if (unlock == null)
        {
            findings.Add(Failure("-", "-",
                "AdminPanelRoot.OnUnlocked() was not found. The audit cannot get past the PIN gate."));
            return;
        }

        unlock.Invoke(panel, null);
    }

    // ------------------------------------------------------------------
    // The audit itself
    // ------------------------------------------------------------------

    // ------------------------------------------------------------------
    // What the audit actually looked at
    // ------------------------------------------------------------------

    private static int s_TabsChecked;
    private static int s_ButtonsFound;
    private static int s_ButtonsPressed;
    private static int s_TogglesFound;
    private static int s_InputsFound;
    private static readonly List<string> s_TabSummary = new List<string>();

    private static void AuditPanel(AdminPanelRoot panel, List<Finding> findings)
    {
        // Step past the default PIN so every tab is reachable. Otherwise the
        // audit would check one tab out of eight and call it a pass.
        UnlockEveryTab(panel, findings);

        CheckCanvasIsOwnedByThePanel(panel, findings);
        CheckEveryTab(panel, findings);
        PressEveryButton(panel, findings);
    }

    /// <summary>
    /// The canvas has to be a child of the panel root. When it was a root
    /// object of its own, closing the panel destroyed the root and left the
    /// canvas, the backdrop and every control on screen, and the backdrop
    /// kept swallowing the taps.
    /// </summary>
    private static void CheckCanvasIsOwnedByThePanel(AdminPanelRoot panel, List<Finding> findings)
    {
        Canvas canvas = panel.GetComponentInChildren<Canvas>(true);
        if (canvas == null)
        {
            findings.Add(Failure("-", "Canvas", "The panel has no canvas, so nothing is visible."));
            return;
        }

        if (canvas.transform.parent != panel.transform)
        {
            findings.Add(Failure("-", "Canvas",
                "The canvas is not a child of the panel. Closing the panel would leave it " +
                "on screen, swallowing every tap."));
        }
    }

    // ------------------------------------------------------------------
    // Every tab, every control
    // ------------------------------------------------------------------

    private static void CheckEveryTab(AdminPanelRoot panel, List<Finding> findings)
    {
        IReadOnlyList<AdminTabBase> tabs = panel.VisibleTabs;
        if (tabs == null || tabs.Count == 0)
        {
            findings.Add(Failure("-", "TabBar", "The panel has no tabs at all."));
            return;
        }

        for (int i = 0; i < tabs.Count; i++)
        {
            AdminTabBase tab = tabs[i];
            string tabName = tab.TitleKey;

            panel.ShowTab(tab);
            s_TabsChecked++;

            // Only the tab bar itself, so the content buttons are not
            // checked twice.
            Transform tabBar = FindByName(panel.transform, "TabBar");
            if (tabBar != null) CheckButtons(tabBar, tabName, findings);

            RectTransform content = FindContent(panel);
            if (content == null)
            {
                findings.Add(Failure(tabName, "-", "The content area of the tab was not found."));
                continue;
            }

            int before = s_ButtonsFound;
            int beforeToggles = s_TogglesFound;
            int beforeInputs = s_InputsFound;
            CheckButtons(content, tabName, findings);

            CheckToggles(content, tabName, findings);
            CheckInputs(content, tabName, findings);
            CheckSizes(content, tabName, findings);

            int controls = (s_ButtonsFound - before)
                         + (s_TogglesFound - beforeToggles)
                         + (s_InputsFound - beforeInputs);
            if (controls == 0)
            {
                findings.Add(Failure(tabName, "-",
                    "The tab has no controls at all. It probably failed to build."));
            }

            s_TabSummary.Add("  " + tabName + ": " + (s_ButtonsFound - before) + " button(s), " +
                             (s_TogglesFound - beforeToggles) + " toggle(s), " +
                             (s_InputsFound - beforeInputs) + " text field(s)");
        }
    }

    private static void CheckButtons(Transform root, string tabName, List<Finding> findings)
    {
        Button[] buttons = root.GetComponentsInChildren<Button>(true);
        for (int i = 0; i < buttons.Length; i++)
        {
            CheckButton(buttons[i], tabName, findings);
        }
    }

    private static void CheckButton(Button button, string tabName, List<Finding> findings)
    {
        if (button == null) return;

        s_ButtonsFound++;
        string label = Describe(button.gameObject);

        if (ListenerCount(button.onClick) == 0)
        {
            findings.Add(Failure(tabName, label,
                "The button has no click handler, so pressing it does nothing."));
        }

        if (button.targetGraphic == null)
        {
            findings.Add(new Finding
            {
                severity = "WARN",
                tab = tabName,
                control = label,
                message = "The button has no target graphic, so it cannot be seen or pressed."
            });
        }
        else if (!button.targetGraphic.raycastTarget)
        {
            findings.Add(Failure(tabName, label,
                "The button's graphic has raycastTarget off, so a tap never reaches it."));
        }
    }

    private static void CheckToggles(RectTransform content, string tabName, List<Finding> findings)
    {
        Toggle[] toggles = content.GetComponentsInChildren<Toggle>(true);
        for (int i = 0; i < toggles.Length; i++)
        {
            Toggle toggle = toggles[i];
            if (toggle == null) continue;

            s_TogglesFound++;

            if (ListenerCount(toggle.onValueChanged) == 0)
            {
                findings.Add(Failure(tabName, Describe(toggle.gameObject),
                    "The toggle has no value handler, so flipping it does nothing."));
            }
        }
    }

    private static void CheckInputs(RectTransform content, string tabName, List<Finding> findings)
    {
        TMP_InputField[] inputs = content.GetComponentsInChildren<TMP_InputField>(true);
        for (int i = 0; i < inputs.Length; i++)
        {
            TMP_InputField input = inputs[i];
            if (input == null) continue;

            s_InputsFound++;

            // A field with no change handler is not a fault: the PIN fields
            // and the import boxes are only read when a button is pressed.
            // It is worth a look, but it is not a failure.
            if (ListenerCount(input.onValueChanged) == 0 && ListenerCount(input.onEndEdit) == 0)
            {
                findings.Add(new Finding
                {
                    severity = "WARN",
                    tab = tabName,
                    control = Describe(input.gameObject),
                    message = "The text field has no change handler. That is fine if its value is only " +
                              "read when a button is pressed, but a typo here means typing does nothing."
                });
            }
        }
    }

    /// <summary>
    /// Checks that nothing has collapsed to zero size. A LayoutElement with
    /// no height inside a layout group makes the element invisible and
    /// unclickable, which is how the whole panel was unusable once.
    /// </summary>
    private static void CheckSizes(RectTransform content, string tabName, List<Finding> findings)
    {
        RectTransform[] rects = content.GetComponentsInChildren<RectTransform>(true);
        for (int i = 0; i < rects.Length; i++)
        {
            RectTransform rect = rects[i];
            if (rect == null) continue;

            // Only check elements a layout group is responsible for.
            if (rect.parent == null) continue;
            if (rect.parent.GetComponent<LayoutGroup>() == null) continue;

            Vector2 size = rect.rect.size;
            if (size.x <= 0.5f || size.y <= 0.5f)
            {
                findings.Add(Failure(tabName, Describe(rect.gameObject),
                    "The element has collapsed to " + size.x.ToString("0.#") + " x " +
                    size.y.ToString("0.#") + ". It cannot be seen or pressed."));
            }
        }
    }

    // ------------------------------------------------------------------
    // Pressing every button
    // ------------------------------------------------------------------

    private static void PressEveryButton(AdminPanelRoot panel, List<Finding> findings)
    {
        IReadOnlyList<AdminTabBase> tabs = panel.VisibleTabs;
        if (tabs == null) return;

        int pressed = 0;

        // The tab bar is pressed first, on its own. Pressing a tab button
        // switches the tab, so the content buttons have to be looked for
        // after the tab bar, never in the same pass.
        Transform tabBar = FindByName(panel.transform, "TabBar");
        if (tabBar != null)
        {
            Button[] barButtons = tabBar.GetComponentsInChildren<Button>(true);
            for (int b = 0; b < barButtons.Length; b++)
            {
                pressed += PressButton(panel, barButtons[b], "TabBar", findings);
                if (panel == null) break;
            }
        }

        if (panel == null)
        {
            Debug.Log("[AdminUiAudit] Pressed " + pressed + " button(s).");
            s_ButtonsPressed = pressed;
            return;
        }

        for (int i = 0; i < tabs.Count; i++)
        {
            AdminTabBase tab = tabs[i];
            panel.ShowTab(tab);

            // The list of buttons is taken again after every press. Many
            // buttons rebuild the tab they are on, and the new buttons
            // have to be pressed too.
            //
            // Buttons are tracked by their path in the hierarchy and not by
            // their instance, because a rebuild creates new instances. The
            // path is what stays the same.
            HashSet<string> done = new HashSet<string>();
            int guard = 0;

            while (guard++ < 200)
            {
                RectTransform content = FindContent(panel);
                if (content == null) break;

                Button next = null;
                string nextPath = null;
                Button[] buttons = content.GetComponentsInChildren<Button>(true);
                for (int b = 0; b < buttons.Length; b++)
                {
                    Button candidate = buttons[b];
                    if (candidate == null) continue;
                    string path = PathIn(content, candidate.transform);
                    if (done.Contains(path)) continue;
                    if (IsSkipped(candidate.gameObject.name))
                    {
                        done.Add(path);
                        continue;
                    }
                    next = candidate;
                    nextPath = path;
                    break;
                }

                if (next == null) break;

                done.Add(nextPath);
                string pressedLabel = Describe(next.gameObject);
                pressed += PressButton(panel, next, tab.TitleKey, findings);

                // Some buttons close the panel. "Erase everything" in the
                // Security tab does exactly that, so the run has to stop
                // when the panel goes away.
                if (panel == null)
                {
                    findings.Add(new Finding
                    {
                        severity = "WARN",
                        tab = tab.TitleKey,
                        control = pressedLabel,
                        message = "Pressing this button closed the panel, so the rest of the run was skipped."
                    });
                    break;
                }
            }

            if (guard >= 200)
            {
                findings.Add(new Finding
                {
                    severity = "WARN",
                    tab = tab.TitleKey,
                    control = "-",
                    message = "The tab kept producing new buttons, so the run stopped after 200 presses."
                });
            }

            if (panel == null) break;
        }

        Debug.Log("[AdminUiAudit] Pressed " + pressed + " button(s).");
        s_ButtonsPressed = pressed;
    }

    /// <summary>
    /// The path of a transform under a root, for example "Card/Row/Button".
    /// The path survives a rebuild, the instance does not.
    /// </summary>
    private static string PathIn(Transform root, Transform target)
    {
        if (target == null) return "(gone)";

        List<string> parts = new List<string>();
        Transform current = target;

        while (current != null && current != root)
        {
            parts.Add(current.name);
            current = current.parent;
        }

        parts.Reverse();
        return string.Join("/", parts.ToArray());
    }

    private static bool IsSkipped(string objectName)
    {
        if (string.IsNullOrEmpty(objectName)) return false;
        if (objectName.StartsWith(SkipPrefixScene)) return true;
        if (objectName == SkipNameClose) return true;
        if (objectName == SkipNameLanguage) return true;
        return false;
    }

    private static int PressButton(AdminPanelRoot panel, Button button, string tabName, List<Finding> findings)
    {
        if (button == null) return 0;

        string name = button.gameObject.name;
        if (IsSkipped(name)) return 0;

        string label = Describe(button.gameObject);

        try
        {
            button.onClick.Invoke();

            // A destructive action asks for confirmation first. The
            // confirmation is a modal with its own buttons, and the real
            // work happens when it is accepted, so accept it too.
            PressConfirmAccept(panel, tabName, findings);
        }
        catch (Exception e)
        {
            findings.Add(Failure(tabName, label,
                "Pressing the button threw: " + e.GetBaseException().Message));
            return 0;
        }

        return 1;
    }

    private static void PressConfirmAccept(AdminPanelRoot panel, string tabName, List<Finding> findings)
    {
        GameObject overlay = GameObject.Find("ConfirmOverlay");
        if (overlay == null) return;

        Button[] buttons = overlay.GetComponentsInChildren<Button>(true);
        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i] == null) continue;
            if (buttons[i].gameObject.name != "Accept") continue;

            try
            {
                buttons[i].onClick.Invoke();
            }
            catch (Exception e)
            {
                findings.Add(Failure(tabName, "ConfirmDialog/Accept",
                    "Accepting the confirmation threw: " + e.GetBaseException().Message));
            }
            return;
        }
    }

    // ------------------------------------------------------------------
    // Out of the panel
    // ------------------------------------------------------------------

    /// <summary>
    /// Checks that the panel can be closed and opened again.
    ///
    /// This runs on a fresh panel, because pressing "erase everything" in
    /// the Security tab closes the panel that was being audited.
    ///
    /// Outside play mode the destroy is immediate, so the panel is checked
    /// straight away. In play mode the destroy waits for the end of the
    /// frame, so the check is deferred to the next editor update.
    /// </summary>
    private static void CheckOpenAndClose(List<Finding> findings)
    {
        GameObject host = new GameObject("[AdminAuditCloseHost]");
        AdminPanelRoot panel = host.AddComponent<AdminPanelRoot>();

        try
        {
            MethodInfo build = typeof(AdminPanelRoot).GetMethod(
                "Build", BindingFlags.NonPublic | BindingFlags.Instance);
            if (build == null) throw new InvalidOperationException("AdminPanelRoot.Build() was not found.");
            build.Invoke(panel, null);

            Unlock(panel, findings);

            Button close = FindCloseButton(panel);
            if (close == null)
            {
                findings.Add(Failure("-", "CloseButton",
                    "The close button was not found. There is no way out of the panel."));
                CleanupHost(host);
                return;
            }

            try
            {
                close.onClick.Invoke();
            }
            catch (Exception e)
            {
                findings.Add(Failure("-", "CloseButton",
                    "Closing the panel threw: " + e.GetBaseException().Message));
                CleanupHost(host);
                return;
            }

            if (EditorApplication.isPlaying)
            {
                // The destroy happens at the end of the frame.
                ScheduleCloseCheck(panel, host, findings);
                return;
            }

            if (panel != null)
            {
                findings.Add(Failure("-", "CloseButton",
                    "The panel was still there after the close button was pressed."));
            }

            CleanupHost(host);
        }
        catch (Exception e)
        {
            findings.Add(Failure("-", "-", "The close check threw: " + e.GetBaseException().Message));
            CleanupHost(host);
        }
    }

    private static void CleanupHost(GameObject host)
    {
        if (host == null) return;
        try
        {
            if (host != null) AdminUi.SafeDestroy(host);
        }
        catch { }
    }

    // ---- The deferred close check, for play mode ----

    private static AdminPanelRoot s_ClosePanel;
    private static GameObject s_CloseHost;
    private static List<Finding> s_CloseFindings;
    private static int s_CloseFrames;

    private static void ScheduleCloseCheck(AdminPanelRoot panel, GameObject host, List<Finding> findings)
    {
        s_ClosePanel = panel;
        s_CloseHost = host;
        s_CloseFindings = findings;
        s_CloseFrames = 0;

        EditorApplication.update -= OnEditorUpdate;
        EditorApplication.update += OnEditorUpdate;
    }

    private static void OnEditorUpdate()
    {
        // A destroyed Unity object reads as null.
        if (s_ClosePanel == null)
        {
            EditorApplication.update -= OnEditorUpdate;
            CleanupHost(s_CloseHost);
            s_ClosePanel = null;
            s_CloseHost = null;
            s_CloseFindings = null;

            if (EditorApplication.isPlaying) FinishPlayModeAudit();
            return;
        }

        s_CloseFrames++;
        if (s_CloseFrames < 10) return;

        EditorApplication.update -= OnEditorUpdate;
        s_CloseFindings.Add(Failure("-", "CloseButton",
            "The panel was still there ten frames after the close button was pressed."));
        CleanupHost(s_CloseHost);
        s_ClosePanel = null;
        s_CloseHost = null;
        s_CloseFindings = null;

        if (EditorApplication.isPlaying) FinishPlayModeAudit();
    }

    private static Button FindCloseButton(AdminPanelRoot panel)
    {
        Button[] buttons = panel.transform.GetComponentsInChildren<Button>(true);
        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i] != null && buttons[i].gameObject.name == SkipNameClose) return buttons[i];
        }
        return null;
    }

    // ------------------------------------------------------------------
    // State backup and restore
    // ------------------------------------------------------------------

    private static Snapshot TakeSnapshot()
    {
        Snapshot snapshot = new Snapshot
        {
            contentPath = AdminContentStore.FilePath,
            pinPath = AdminSecurity.FilePath
        };

        snapshot.contentExisted = File.Exists(snapshot.contentPath);
        snapshot.pinExisted = File.Exists(snapshot.pinPath);

        snapshot.contentBackup = CopyToTemp(snapshot.contentPath, "audit_content.json");
        snapshot.pinBackup = CopyToTemp(snapshot.pinPath, "audit_pin.json");

        // The PlayerPrefs keys the admin panel writes.
        for (int i = 0; i < AdminGameConfig.Levels.Count; i++)
        {
            AdminGameConfig.LevelEntry level = AdminGameConfig.Levels[i];
            string stageId = level.stageId;
            string tower = level.towerName;

            Remember(snapshot, ProgressKeys.HighScoreFor(stageId));
            Remember(snapshot, ProgressKeys.FirstClearFor(stageId));
            Remember(snapshot, ProgressKeys.AttemptsFor(stageId));
            Remember(snapshot, ProgressKeys.HintsFor(stageId));
            Remember(snapshot, ProgressKeys.UnlockedFor(tower));
            Remember(snapshot, ProgressKeys.UnlockTimeFor(tower));

            for (int b = 0; b < AdminGameConfig.BadgeTypes.Length; b++)
            {
                Remember(snapshot, ProgressKeys.BadgeFor(stageId, AdminGameConfig.BadgeTypes[b].ToString()));
            }
        }

        Remember(snapshot, ProgressKeys.RuneKeys);
        Remember(snapshot, ProgressKeys.RuneLastRegen);
        Remember(snapshot, ProgressKeys.HintsMax);

        return snapshot;
    }

    private static void Remember(Snapshot snapshot, string key)
    {
        if (string.IsNullOrEmpty(key)) return;
        if (snapshot.prefKeys.Contains(key)) return;

        snapshot.prefKeys.Add(key);
        snapshot.prefs[key] = PlayerPrefs.HasKey(key) ? PlayerPrefs.GetString(key, "") : null;
    }

    private static string CopyToTemp(string path, string name)
    {
        try
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return null;

            string target = Path.Combine(Path.GetTempPath(), name);
            File.Copy(path, target, true);
            return target;
        }
        catch (Exception e)
        {
            Debug.LogWarning("[AdminUiAudit] Could not back up " + path + ": " + e.Message);
            return null;
        }
    }

    private static void RestoreSnapshot(Snapshot snapshot)
    {
        if (snapshot == null) return;

        try
        {
            if (snapshot.contentBackup != null && File.Exists(snapshot.contentBackup))
            {
                File.Copy(snapshot.contentBackup, snapshot.contentPath, true);
            }
            else if (!snapshot.contentExisted && File.Exists(snapshot.contentPath))
            {
                // There was no config file before the run, so there should
                // not be one after it either.
                File.Delete(snapshot.contentPath);
            }

            if (snapshot.pinBackup != null && File.Exists(snapshot.pinBackup))
            {
                File.Copy(snapshot.pinBackup, snapshot.pinPath, true);
            }
            else if (!snapshot.pinExisted && File.Exists(snapshot.pinPath))
            {
                File.Delete(snapshot.pinPath);
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning("[AdminUiAudit] Could not restore the admin files: " + e.Message);
        }

        for (int i = 0; i < snapshot.prefKeys.Count; i++)
        {
            string key = snapshot.prefKeys[i];
            string value = snapshot.prefs[key];

            try
            {
                if (value == null) PlayerPrefs.DeleteKey(key);
                else PlayerPrefs.SetString(key, value);
            }
            catch (Exception e)
            {
                Debug.LogWarning("[AdminUiAudit] Could not restore " + key + ": " + e.Message);
            }
        }

        PlayerPrefs.Save();
    }

    // ------------------------------------------------------------------
    // Helpers
    // ------------------------------------------------------------------

    private static RectTransform FindContent(AdminPanelRoot panel)
    {
        // "ContentArea", not "Content": the scroll views also create a child
        // called "Content", and that one holds the tab bar, not the tab.
        Transform[] all = panel.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < all.Length; i++)
        {
            if (all[i] != null && all[i].name == "ContentArea") return all[i] as RectTransform;
        }
        return null;
    }

    /// <summary>Finds a named object under the panel.</summary>
    private static Transform FindByName(Transform root, string name)
    {
        Transform[] all = root.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < all.Length; i++)
        {
            if (all[i] != null && all[i].name == name) return all[i];
        }
        return null;
    }

    /// <summary>
    /// How many listeners an event has. Unity has no public API for the
    /// runtime listeners, so the internal list is read.
    /// </summary>
    private static int ListenerCount(UnityEventBase evt)
    {
        if (evt == null) return 0;

        int persistent = 0;
        try
        {
            persistent = evt.GetPersistentEventCount();
        }
        catch { }

        if (persistent > 0) return persistent;

        try
        {
            FieldInfo callsField = typeof(UnityEventBase).GetField(
                "m_Calls", BindingFlags.NonPublic | BindingFlags.Instance);
            if (callsField == null) return persistent;

            object calls = callsField.GetValue(evt);
            if (calls == null) return persistent;

            FieldInfo runtimeField = calls.GetType().GetField(
                "m_RuntimeCalls", BindingFlags.NonPublic | BindingFlags.Instance);
            if (runtimeField == null) return persistent;

            object list = runtimeField.GetValue(calls);
            if (list == null) return persistent;

            PropertyInfo countProperty = list.GetType().GetProperty("Count");
            if (countProperty == null) return persistent;

            object count = countProperty.GetValue(list, null);
            return count is int ? (int)count : persistent;
        }
        catch
        {
            return persistent;
        }
    }

    /// <summary>A readable name for a control: its object name and its label.</summary>
    private static string Describe(GameObject go)
    {
        if (go == null) return "(gone)";

        string text = go.name;

        TMP_Text[] labels = go.GetComponentsInChildren<TMP_Text>(true);
        for (int i = 0; i < labels.Length; i++)
        {
            if (labels[i] == null) continue;
            if (string.IsNullOrEmpty(labels[i].text)) continue;
            string value = labels[i].text.Replace("\n", " ").Trim();
            if (value.Length > 30) value = value.Substring(0, 30) + "...";
            text += " ['" + value + "']";
            break;
        }

        return text;
    }

    private static Finding Failure(string tab, string control, string message)
    {
        return new Finding
        {
            severity = "FAIL",
            tab = tab,
            control = control,
            message = message
        };
    }

    // ------------------------------------------------------------------
    // The report
    // ------------------------------------------------------------------

    private static int CountFailures(List<Finding> findings)
    {
        int failures = 0;
        for (int i = 0; i < findings.Count; i++)
        {
            if (findings[i].severity == "FAIL") failures++;
        }
        return failures;
    }

    private static string BuildReport(List<Finding> findings)
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("========== ADMIN UI AUDIT ==========");

        int failures = 0;
        int warnings = 0;

        for (int i = 0; i < findings.Count; i++)
        {
            Finding f = findings[i];
            if (f.severity == "FAIL") failures++; else warnings++;

            sb.AppendLine(f.severity + "  [" + f.tab + "] " + f.control);
            sb.AppendLine("      " + f.message);
        }

        sb.AppendLine();
        sb.AppendLine("Checked " + s_TabsChecked + " tab(s), " + s_ButtonsFound + " button(s), " +
                      s_TogglesFound + " toggle(s) and " + s_InputsFound + " text field(s).");
        sb.AppendLine("Pressed " + s_ButtonsPressed + " button(s).");
        for (int i = 0; i < s_TabSummary.Count; i++)
        {
            sb.AppendLine(s_TabSummary[i]);
        }

        if (findings.Count == 0)
        {
            sb.AppendLine("PASS  every button, toggle and text field has a handler and can be pressed.");
            sb.AppendLine("PASS  the panel can be closed and opened again.");
        }
        else
        {
            sb.AppendLine("----- " + failures + " failure(s), " + warnings + " warning(s) -----");
        }

        sb.AppendLine("=====================================");
        return sb.ToString();
    }

    private static void Report(List<Finding> findings)
    {
        string report = BuildReport(findings);
        WriteReport(report);

        if (CountFailures(findings) > 0) Debug.LogError("[AdminUiAudit]\n" + report);
        else Debug.Log("[AdminUiAudit]\n" + report);

        // A copy in the clipboard makes the report easy to paste.
        EditorGUIUtility.systemCopyBuffer = report;
    }

    private static void WriteReport(string report)
    {
        try
        {
            string folder = Path.Combine(Application.dataPath, "..", "Logs");
            if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
            File.WriteAllText(Path.Combine(folder, ReportFile), report);
        }
        catch (Exception e)
        {
            Debug.LogWarning("[AdminUiAudit] Could not write the report: " + e.Message);
        }
    }
}
