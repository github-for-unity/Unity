using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace GitHub.Unity
{
    [Serializable]
    class SettingsView : Subview
    {
        private const string EditorSettingsMissingTitle = "Missing editor settings";
        private const string EditorSettingsMissingMessage =
            "No valid editor settings found when looking in expected path '{0}'. Please save the project.";
        private const string BadVCSSettingsTitle = "Update settings";
        private const string BadVCSSettingsMessage =
            "To use Git, you will need to set project Version Control Mode to either 'Visible Meta Files' or 'Hidden Meta Files'.";
        private const string SelectEditorSettingsButton = "View settings";
        private const string NoActiveRepositoryTitle = "No repository found";
        private const string NoActiveRepositoryMessage = "Your current project is not currently in an active Git repository:";
        private const string TextSerialisationMessage =
            "For optimal Git use, it is recommended that you configure Unity to serialize assets using text serialization. Note that this may cause editor slowdowns for projects with very large datasets.";
        private const string BinarySerialisationMessage = "This project is currently configured for binary serialization.";
        private const string MixedSerialisationMessage = "This project is currently configured for mixed serialization.";
        private const string IgnoreSerialisationIssuesSetting = "IgnoreSerializationIssues";
        private const string IgnoreSerialisationSettingsButton = "Ignore forever";
        private const string RefreshIssuesButton = "Refresh";
        private const string GitIgnoreExceptionWarning = "Exception when searching .gitignore files: {0}";
        private const string GitIgnoreIssueWarning = "{0}: {2}\n\nIn line \"{1}\"";
        private const string GitIgnoreIssueNoLineWarning = "{0}: {1}";
        private const string GitInitBrowseTitle = "Pick desired repository root";
        private const string GitInitButton = "Set up Git";
        private const string InvalidInitDirectoryTitle = "Invalid repository root";
        private const string InvalidInitDirectoryMessage =
            "Your selected folder '{0}' is not a valid repository root for your current project.";
        private const string InvalidInitDirectoryOK = "OK";
        private const string GitInstallTitle = "Git install";
        private const string GitInstallMissingMessage =
            "GitHub was unable to locate a valid Git install. Please specify install location or install git.";
        private const string GitInstallBrowseTitle = "Select git binary";
        private const string GitInstallPickInvalidTitle = "Invalid Git install";
        private const string GitInstallPickInvalidMessage = "The selected file is not a valid Git install. {0}";
        private const string GitInstallPickInvalidOK = "OK";
        private const string GitInstallFindButton = "Find install";
        private const string GitInstallButton = "New Git install";
        private const string GitInstallURL = "http://desktop.github.com";
        private const string GitIgnoreRulesTitle = "gitignore rules";
        private const string GitIgnoreRulesEffect = "Effect";
        private const string GitIgnoreRulesFile = "File";
        private const string GitIgnoreRulesLine = "Line";
        private const string GitIgnoreRulesDescription = "Description";
        private const string NewGitIgnoreRuleButton = "New";
        private const string DeleteGitIgnoreRuleButton = "Delete";

        [NonSerialized] private int newGitIgnoreRulesSelection = -1;

        [SerializeField] private int gitIgnoreRulesSelection = 0;
        [SerializeField] private string initDirectory;
        [SerializeField] private Vector2 scroll;

        public override void OnShow()
        {
            base.OnShow();
        }

        public override void OnHide()
        {
            base.OnHide();
        }

        public override void Refresh()
        {
            StatusService.Instance.Run();
        }

        public override void OnGUI()
        {
            bool onIssuesGui;
            scroll = GUILayout.BeginScrollView(scroll);
            {
                // Issues
                onIssuesGui = OnIssuesGUI();
                if (onIssuesGui)
                {
                    GUILayout.Space(EditorGUIUtility.standardVerticalSpacing);

                    GUILayout.Label("TODO: Favourite branches settings?");

                    GUILayout.Space(EditorGUIUtility.standardVerticalSpacing);

                    // gitignore rules list

                    OnGitIgnoreRulesGUI();

                    GUILayout.Space(EditorGUIUtility.standardVerticalSpacing);

                    GUILayout.Label("TODO: GitHub login settings");
                    GUILayout.Label("TODO: Auto-fetch toggle");
                    GUILayout.Label("TODO: Auto-push toggle");

                    // Install path

                    GUILayout.Label(GitInstallTitle, EditorStyles.boldLabel);
                    OnInstallPathGUI();
                }
            }

            GUILayout.EndScrollView();

            // Effectuate new selection at end of frame
            if (onIssuesGui)
            {
                if (Event.current.type == EventType.Repaint && newGitIgnoreRulesSelection > -1)
                {
                    gitIgnoreRulesSelection = newGitIgnoreRulesSelection;
                    newGitIgnoreRulesSelection = -1;
                    GUIUtility.hotControl = GUIUtility.keyboardControl = -1;
                }
            }
        }

        private static void TableCell(string label, float width)
        {
            GUILayout.Label(label, EditorStyles.miniLabel, GUILayout.Width(width), GUILayout.MaxWidth(width));
        }

        private static bool ValidateInitDirectory(string path)
        {
            if (Utility.UnityProjectPath.IndexOf(path) != 0)
            {
                EditorUtility.DisplayDialog(InvalidInitDirectoryTitle, String.Format(InvalidInitDirectoryMessage, path),
                    InvalidInitDirectoryOK);
                return false;
            }

            return true;
        }

        private static bool ValidateGitInstall(string path)
        {
            if (!EntryPoint.GitEnvironment.ValidateGitInstall(path))
            {
                EditorUtility.DisplayDialog(GitInstallPickInvalidTitle, String.Format(GitInstallPickInvalidMessage, path),
                    GitInstallPickInvalidOK);
                return false;
            }

            return true;
        }

        private bool OnIssuesGUI()
        {
            var settingsIssues = Utility.Issues.Select(i => i as ProjectSettingsIssue).FirstOrDefault(i => i != null);

            if (settingsIssues != null)
            {
                if (settingsIssues.WasCaught(ProjectSettingsEvaluation.EditorSettingsMissing))
                {
                    Styles.BeginInitialStateArea(EditorSettingsMissingTitle,
                        String.Format(EditorSettingsMissingMessage, EvaluateProjectConfigurationTask.EditorSettingsPath));
                    Styles.EndInitialStateArea();

                    return false;
                }
                else if (settingsIssues.WasCaught(ProjectSettingsEvaluation.BadVCSSettings))
                {
                    Styles.BeginInitialStateArea(BadVCSSettingsTitle, BadVCSSettingsMessage);
                    {
                        GUILayout.Space(EditorGUIUtility.standardVerticalSpacing);

                        // Button to select editor settings - for remedying the bad setting
                        if (Styles.InitialStateActionButton(SelectEditorSettingsButton))
                        {
                            Selection.activeObject = EvaluateProjectConfigurationTask.LoadEditorSettings();
                        }
                    }
                    Styles.EndInitialStateArea();

                    return false;
                }
            }

            if (!Utility.GitFound)
            {
                Styles.BeginInitialStateArea(GitInstallTitle, GitInstallMissingMessage);
                {
                    OnInstallPathGUI();
                }
                Styles.EndInitialStateArea();

                return false;
            }
            else if (!Utility.ActiveRepository)
            {
                Styles.BeginInitialStateArea(NoActiveRepositoryTitle, NoActiveRepositoryMessage);
                {
                    // Init directory path field
                    Styles.PathField(ref initDirectory, () => EditorUtility.OpenFolderPanel(GitInitBrowseTitle, initDirectory, ""),
                        ValidateInitDirectory);

                    GUILayout.Space(EditorGUIUtility.standardVerticalSpacing);

                    // Git init, which starts the config flow
                    if (Styles.InitialStateActionButton(GitInitButton))
                    {
                        if (ValidateInitDirectory(initDirectory))
                        {
                            Init();
                        }
                        else
                        {
                            ResetInitDirectory();
                        }
                    }
                }
                Styles.EndInitialStateArea();

                return false;
            }

            if (settingsIssues != null && !EntryPoint.LocalSettings.Get(IgnoreSerialisationIssuesSetting, "0").Equals("1"))
            {
                var binary = settingsIssues.WasCaught(ProjectSettingsEvaluation.BinarySerialization);
                var mixed = settingsIssues.WasCaught(ProjectSettingsEvaluation.MixedSerialization);

                if (binary || mixed)
                {
                    GUILayout.Label(TextSerialisationMessage, Styles.LongMessageStyle);
                    Styles.Warning(binary ? BinarySerialisationMessage : MixedSerialisationMessage);

                    GUILayout.BeginHorizontal();
                    {
                        if (GUILayout.Button(IgnoreSerialisationSettingsButton))
                        {
                            EntryPoint.LocalSettings.Set(IgnoreSerialisationIssuesSetting, "1");
                        }

                        GUILayout.FlexibleSpace();

                        if (GUILayout.Button(RefreshIssuesButton))
                        {
                            EvaluateProjectConfigurationTask.Schedule();
                        }

                        if (GUILayout.Button(SelectEditorSettingsButton))
                        {
                            Selection.activeObject = EvaluateProjectConfigurationTask.LoadEditorSettings();
                        }
                    }
                    GUILayout.EndHorizontal();
                }
            }

            var gitIgnoreException = Utility.Issues.Select(i => i as GitIgnoreException).FirstOrDefault(i => i != null);
            if (gitIgnoreException != null)
            {
                Styles.Warning(String.Format(GitIgnoreExceptionWarning, gitIgnoreException.Exception));
            }

            foreach (var issue in Utility.Issues.Select(i => i as GitIgnoreIssue).Where(i => i != null))
            {
                if (string.IsNullOrEmpty(issue.Line))
                {
                    Styles.Warning(String.Format(GitIgnoreIssueNoLineWarning, issue.File, issue.Description));
                }
                else
                {
                    Styles.Warning(String.Format(GitIgnoreIssueWarning, issue.File, issue.Line, issue.Description));
                }
            }

            return true;
        }

        private void OnGitIgnoreRulesGUI()
        {
            var gitignoreRulesWith = Position.width - Styles.GitIgnoreRulesTotalHorizontalMargin - Styles.GitIgnoreRulesSelectorWidth - 16f;
            var effectWidth = gitignoreRulesWith * Styles.GitIgnoreRulesEffectRatio;
            var fileWidth = gitignoreRulesWith * Styles.GitIgnoreRulesFileRatio;
            var lineWidth = gitignoreRulesWith * Styles.GitIgnoreRulesLineRatio;

            GUILayout.Label(GitIgnoreRulesTitle, EditorStyles.boldLabel);
            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.BeginHorizontal(EditorStyles.toolbar);
            {
                GUILayout.Space(Styles.GitIgnoreRulesSelectorWidth);
                TableCell(GitIgnoreRulesEffect, effectWidth);
                TableCell(GitIgnoreRulesFile, fileWidth);
                TableCell(GitIgnoreRulesLine, lineWidth);
            }
            GUILayout.EndHorizontal();

            var count = GitIgnoreRule.Count;
            for (var index = 0; index < count; ++index)
            {
                GitIgnoreRule rule;
                if (GitIgnoreRule.TryLoad(index, out rule))
                {
                    GUILayout.BeginHorizontal();
                    {
                        GUILayout.Space(Styles.GitIgnoreRulesSelectorWidth);

                        if (gitIgnoreRulesSelection == index && Event.current.type == EventType.Repaint)
                        {
                            var selectorRect = GUILayoutUtility.GetLastRect();
                            selectorRect.Set(selectorRect.x, selectorRect.y + 2f, selectorRect.width - 2f, EditorGUIUtility.singleLineHeight);
                            EditorStyles.foldout.Draw(selectorRect, false, false, false, false);
                        }

                        TableCell(rule.Effect.ToString(), effectWidth);
                        // TODO: Tint if the regex is null
                        TableCell(rule.FileString, fileWidth);
                        TableCell(rule.LineString, lineWidth);
                    }
                    GUILayout.EndHorizontal();

                    if (Event.current.type == EventType.MouseDown && GUILayoutUtility.GetLastRect().Contains(Event.current.mousePosition))
                    {
                        newGitIgnoreRulesSelection = index;
                        Event.current.Use();
                    }
                }
            }

            GUILayout.BeginHorizontal();
            {
                GUILayout.FlexibleSpace();
                if (GUILayout.Button(NewGitIgnoreRuleButton, EditorStyles.miniButton))
                {
                    GitIgnoreRule.New();
                    GUIUtility.hotControl = GUIUtility.keyboardControl = -1;
                }
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(EditorGUIUtility.standardVerticalSpacing);

            // Selected gitignore rule edit

            GitIgnoreRule selectedRule;
            if (GitIgnoreRule.TryLoad(gitIgnoreRulesSelection, out selectedRule))
            {
                GUILayout.BeginVertical(GUI.skin.box);
                {
                    GUILayout.BeginHorizontal();
                    {
                        GUILayout.FlexibleSpace();
                        if (GUILayout.Button(DeleteGitIgnoreRuleButton, EditorStyles.miniButton))
                        {
                            GitIgnoreRule.Delete(gitIgnoreRulesSelection);
                            newGitIgnoreRulesSelection = gitIgnoreRulesSelection - 1;
                        }
                    }
                    GUILayout.EndHorizontal();
                    EditorGUI.BeginChangeCheck();
                    var newEffect = (GitIgnoreRuleEffect)EditorGUILayout.EnumPopup(GitIgnoreRulesEffect, selectedRule.Effect);
                    var newFile = EditorGUILayout.TextField(GitIgnoreRulesFile, selectedRule.FileString);
                    var newLine = EditorGUILayout.TextField(GitIgnoreRulesLine, selectedRule.LineString);
                    GUILayout.Label(GitIgnoreRulesDescription);
                    var newDescription = EditorGUILayout.TextArea(selectedRule.TriggerText, Styles.CommitDescriptionFieldStyle);
                    if (EditorGUI.EndChangeCheck())
                    {
                        GitIgnoreRule.Save(gitIgnoreRulesSelection, newEffect, newFile, newLine, newDescription);
                        EvaluateProjectConfigurationTask.Schedule();
                    }
                }
                GUILayout.EndVertical();
            }
            GUILayout.EndVertical();
        }

        private void OnInstallPathGUI()
        {
            var gitExecPath = EntryPoint.Environment.GitExecutablePath;
            // Install path field
            EditorGUI.BeginChangeCheck();
            {
                //TODO: Verify necessary value for a non Windows OS
                var extension = EntryPoint.GitEnvironment.GetGitExecutableExtension();

                Styles.PathField(ref gitExecPath,
                    () => EditorUtility.OpenFilePanel(GitInstallBrowseTitle,
                        EntryPoint.Environment.GitInstallPath,
                        extension), ValidateGitInstall);
            }
            if (EditorGUI.EndChangeCheck())
            {
                EntryPoint.Environment.GitExecutablePath = gitExecPath;
            }

            GUILayout.Space(EditorGUIUtility.standardVerticalSpacing);

            GUILayout.BeginHorizontal();
            {
                // Find button - for attempting to locate a new install
                if (GUILayout.Button(GitInstallFindButton, GUILayout.ExpandWidth(false)))
                {
                    var task = new FindGitTask(
                        EntryPoint.Environment, EntryPoint.ProcessManager, EntryPoint.TaskResultDispatcher,
                        path => {
                        if (!string.IsNullOrEmpty(path))
                        {
                            EntryPoint.Environment.GitExecutablePath = path;
                            GUIUtility.keyboardControl = GUIUtility.hotControl = 0;
                        }
                    });
                }
                GUILayout.FlexibleSpace();

                // Install button if git is not installed or we want a new install
                if (GUILayout.Button(GitInstallButton, GUILayout.ExpandWidth(false)))
                {
                    Application.OpenURL(GitInstallURL);
                }
            }
            GUILayout.EndHorizontal();
        }

        private void ResetInitDirectory()
        {
            initDirectory = Utility.UnityProjectPath;
            GUIUtility.keyboardControl = GUIUtility.hotControl = 0;
        }

        private void Init()
        {
            Logger.Debug("TODO: Init '{0}'", initDirectory);
        }
    }
}
