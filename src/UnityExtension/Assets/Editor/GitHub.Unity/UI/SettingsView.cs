using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
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
        private const string GitInstallTitle = "Git installation";
        private const string GitInstallMissingMessage =
            "GitHub was unable to locate a valid Git install. Please specify install location or install git.";
        private const string GitInstallBrowseTitle = "Select git binary";
        private const string GitInstallPickInvalidTitle = "Invalid Git install";
        private const string GitInstallPickInvalidMessage = "The selected file is not a valid Git install. {0}";
        private const string GitInstallPickInvalidOK = "OK";
        private const string GitInstallFindButton = "Find install";
        private const string GitInstallURL = "http://desktop.github.com";
        private const string GitIgnoreRulesTitle = "gitignore rules";
        private const string GitIgnoreRulesEffect = "Effect";
        private const string GitIgnoreRulesFile = "File";
        private const string GitIgnoreRulesLine = "Line";
        private const string GitIgnoreRulesDescription = "Description";
        private const string NewGitIgnoreRuleButton = "New";
        private const string DeleteGitIgnoreRuleButton = "Delete";
        private const string GitConfigTitle = "Git Configuration";
        private const string GitConfigNameLabel = "Name";
        private const string GitConfigEmailLabel = "Email";
        private const string GitConfigUserSave = "Save User";
        private const string GitConfigUserSaved = "Saved";
        private const string GitRepositoryTitle = "Repository Configuration";
        private const string GitRepositoryRemoteLabel = "Remote";
        private const string GitRepositorySave = "Save Repository";
        private const string DebugSettingsTitle = "Debug";
        private const string PrivacyTitle = "Privacy";
        private const string EnableTraceLoggingLabel = "Enable Trace Logging";
        private const string MetricsOptInLabel = "Help us improve by sending anonymous usage data";
        private const string DefaultRepositoryRemoteName = "origin";

        [NonSerialized] private int newGitIgnoreRulesSelection = -1;

        [SerializeField] private string gitName;
        [SerializeField] private string gitEmail;

        [SerializeField] private int gitIgnoreRulesSelection = 0;
        [SerializeField] private string initDirectory;
        [SerializeField] private List<GitLock> lockedFiles = new List<GitLock>();
        [SerializeField] private Vector2 lockScrollPos;
        [SerializeField] private string repositoryRemoteName;
        [SerializeField] private string repositoryRemoteUrl;
        [SerializeField] private Vector2 scroll;
        [SerializeField] private bool isBusy;
        [SerializeField] private int lockedFileSelection = -1;
        [SerializeField] private bool hasRemote;
        [SerializeField] private bool remoteHasChanged;
        [NonSerialized] private bool userDataHasChanged;

        [SerializeField] private string newGitName;
        [SerializeField] private string newGitEmail;
        [SerializeField] private string newRepositoryRemoteUrl;
        [SerializeField] private User cachedUser;

        public override void OnEnable()
        {
            base.OnEnable();
            AttachHandlers(Repository);

            remoteHasChanged = true;
        }

        public override void OnDisable()
        {
            base.OnDisable();
            DetachHandlers(Repository);
        }

        public override void OnDataUpdate()
        {
            base.OnDataUpdate();
            MaybeUpdateData();
        }

        public override void OnRepositoryChanged(IRepository oldRepository)
        {
            base.OnRepositoryChanged(oldRepository);

            DetachHandlers(oldRepository);
            AttachHandlers(Repository);

            remoteHasChanged = true;

            Refresh();
        }

        public override void Refresh()
        {
            base.Refresh();
            if (Repository != null && Repository.CurrentRemote.HasValue)
            {
                Repository.ListLocks().Start();
            }
        }

        private void AttachHandlers(IRepository repository)
        {
            if (repository == null)
                return;

            repository.OnActiveRemoteChanged += Repository_OnActiveRemoteChanged;
            repository.OnLocksUpdated += RunLocksUpdateOnMainThread;
        }

        private void DetachHandlers(IRepository repository)
        {
            if (repository == null)
                return;

            repository.OnActiveRemoteChanged -= Repository_OnActiveRemoteChanged;
            repository.OnLocksUpdated -= RunLocksUpdateOnMainThread;
        }

        public override void OnGUI()
        {
            scroll = GUILayout.BeginScrollView(scroll);
            {
                OnUserSettingsGUI();

                GUILayout.Space(EditorGUIUtility.standardVerticalSpacing);

                if (Repository != null)
                {
                    OnRepositorySettingsGUI();

                    GUILayout.Space(EditorGUIUtility.standardVerticalSpacing);

                    OnGitLfsLocksGUI();

                    GUILayout.Space(EditorGUIUtility.standardVerticalSpacing);
                }

                OnInstallPathGUI();
                OnPrivacyGui();
                OnLoggingSettingsGui();
            }

            GUILayout.EndScrollView();
        }

        private void MaybeUpdateData()
        {
            if (lockedFiles == null)
                lockedFiles = new List<GitLock>();

            if (Repository == null)
            {
                if ((cachedUser == null || String.IsNullOrEmpty(cachedUser.Name)) && GitClient != null)
                {
                    var user = new User();
                    GitClient.GetConfig("user.name", GitConfigSource.User)
                        .Then((success, value) => user.Name = value).Then(
                    GitClient.GetConfig("user.email", GitConfigSource.User)
                        .Then((success, value) => user.Email = value))
                    .FinallyInUI((success, ex) =>
                    {
                        if (success && !String.IsNullOrEmpty(user.Name))
                        {
                            cachedUser = user;
                            userDataHasChanged = true;
                            Redraw();
                        }
                    })
                    .Start();
                }

                if (userDataHasChanged)
                {
                    newGitName = gitName = cachedUser.Name;
                    newGitEmail = gitEmail = cachedUser.Email;
                    userDataHasChanged = false;
                }
                return;
            }

            userDataHasChanged = Repository.User.Name != gitName || Repository.User.Email != gitEmail;

            if (!remoteHasChanged && !userDataHasChanged)
                return;

            if (userDataHasChanged)
            {
                userDataHasChanged = false;
                newGitName = gitName = Repository.User.Name;
                newGitEmail = gitEmail = Repository.User.Email;
            }

            if (remoteHasChanged)
            {
                remoteHasChanged = false;
                var activeRemote = Repository.CurrentRemote;
                hasRemote = activeRemote.HasValue && !String.IsNullOrEmpty(activeRemote.Value.Url);
                if (!hasRemote)
                {
                    repositoryRemoteName = DefaultRepositoryRemoteName;
                    newRepositoryRemoteUrl = repositoryRemoteUrl = string.Empty;
                }
                else
                {
                    repositoryRemoteName = activeRemote.Value.Name;
                    newRepositoryRemoteUrl = repositoryRemoteUrl = activeRemote.Value.Url;
                }
            }
        }

        private void ResetToDefaults()
        {
            gitName = Repository != null ? Repository.User.Name : String.Empty;
            gitEmail = Repository != null ? Repository.User.Email : String.Empty;
            repositoryRemoteName = DefaultRepositoryRemoteName;
            repositoryRemoteUrl = string.Empty;
        }

        private void Repository_OnActiveRemoteChanged(string remote)
        {
            remoteHasChanged = true;
        }

        private void RunLocksUpdateOnMainThread(IEnumerable<GitLock> locks)
        {
            new ActionTask(TaskManager.Token, _ => OnLocksUpdate(locks))
                .ScheduleUI(TaskManager);
        }

        private void OnLocksUpdate(IEnumerable<GitLock> update)
        {
            if (update == null)
            {
                return;
            }
            lockedFiles = update.ToList();
            if (lockedFiles.Count <= lockedFileSelection)
            {
                lockedFileSelection = -1;
            }
            Redraw();
        }

        private void OnUserSettingsGUI()
        {
            GUILayout.Label(GitConfigTitle, EditorStyles.boldLabel);

            EditorGUI.BeginDisabledGroup(isBusy);
            {
                newGitName = EditorGUILayout.TextField(GitConfigNameLabel, newGitName);
                newGitEmail = EditorGUILayout.TextField(GitConfigEmailLabel, newGitEmail);

                var needsSaving = newGitName != gitName || newGitEmail != gitEmail;
                EditorGUI.BeginDisabledGroup(!needsSaving);
                {
                    if (GUILayout.Button(GitConfigUserSave, GUILayout.ExpandWidth(false)))
                    {
                        GitClient.SetConfig("user.name", newGitName, GitConfigSource.User)
                            .Then((success, value) =>
                            {
                                if (success)
                                {
                                    if (Repository != null)
                                    {
                                        Repository.User.Name = value;
                                    }
                                    else
                                    {
                                        if (cachedUser == null)
                                        {
                                            cachedUser = new User();
                                        }
                                        cachedUser.Name = value;
                                    }
                                }
                            })
                            .Then(
                        GitClient.SetConfig("user.email", newGitEmail, GitConfigSource.User)
                            .Then((success, value) =>
                            {
                                if (success)
                                {
                                    if (Repository != null)
                                    {
                                        Repository.User.Email = value;
                                    }
                                    else
                                    {
                                        cachedUser.Email = value;
                                        userDataHasChanged = true;
                                    }
                                }
                            }))
                        .FinallyInUI((_, __) =>
                        {
                            isBusy = false;
                            Redraw();
                        })
                        .Start();
                        isBusy = true;
                    }
                }
                EditorGUI.EndDisabledGroup();
            }
            EditorGUI.EndDisabledGroup();
        }

        private void OnRepositorySettingsGUI()
        {
            GUILayout.Label(GitRepositoryTitle, EditorStyles.boldLabel);

            EditorGUI.BeginDisabledGroup(isBusy);
            {
                newRepositoryRemoteUrl = EditorGUILayout.TextField(GitRepositoryRemoteLabel + ": " + repositoryRemoteName, newRepositoryRemoteUrl);
                var needsSaving = newRepositoryRemoteUrl != repositoryRemoteUrl && !String.IsNullOrEmpty(newRepositoryRemoteUrl);
                EditorGUI.BeginDisabledGroup(!needsSaving);
                {
                    if (GUILayout.Button(GitRepositorySave, GUILayout.ExpandWidth(false)))
                    {
                        try
                        {
                            isBusy = true;
                            Repository.SetupRemote(repositoryRemoteName, newRepositoryRemoteUrl)
                                .FinallyInUI((_, __) =>
                                {
                                    isBusy = false;
                                    Redraw();
                                })
                                .Start();
                        }
                        catch (Exception ex)
                        {
                            Logger.Error(ex);
                        }
                    }
                }
                EditorGUI.EndDisabledGroup();
            }
            EditorGUI.EndDisabledGroup();
        }

        private bool ValidateGitInstall(string path)
        {
            if (String.IsNullOrEmpty(path))
                return false;
            if (!GitClient.ValidateGitInstall(path.ToNPath()))
            {
                EditorUtility.DisplayDialog(GitInstallPickInvalidTitle, String.Format(GitInstallPickInvalidMessage, path),
                    GitInstallPickInvalidOK);
                return false;
            }

            return true;
        }

        private bool OnIssuesGUI()
        {
            IList<ProjectConfigurationIssue> projectConfigurationIssues;
            if (Utility.Issues != null)
            {
                projectConfigurationIssues = Utility.Issues;
            }
            else
            {
                projectConfigurationIssues = new ProjectConfigurationIssue[0];
            }

            var settingsIssues = projectConfigurationIssues.Select(i => i as ProjectSettingsIssue).FirstOrDefault(i => i != null);

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

            if (settingsIssues != null && !Manager.LocalSettings.Get(IgnoreSerialisationIssuesSetting, "0").Equals("1"))
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
                            Manager.LocalSettings.Set(IgnoreSerialisationIssuesSetting, "1");
                        }

                        GUILayout.FlexibleSpace();

                        if (GUILayout.Button(RefreshIssuesButton))
                        {
                            // TODO: Fix this
                        }

                        if (GUILayout.Button(SelectEditorSettingsButton))
                        {
                            Selection.activeObject = EvaluateProjectConfigurationTask.LoadEditorSettings();
                        }
                    }
                    GUILayout.EndHorizontal();
                }
            }

            var gitIgnoreException = projectConfigurationIssues.Select(i => i as GitIgnoreException).FirstOrDefault(i => i != null);
            if (gitIgnoreException != null)
            {
                Styles.Warning(String.Format(GitIgnoreExceptionWarning, gitIgnoreException.Exception));
            }

            foreach (var issue in projectConfigurationIssues.Select(i => i as GitIgnoreIssue).Where(i => i != null))
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
                        // TODO: Fix this
                    }
                }
                GUILayout.EndVertical();
            }
            GUILayout.EndVertical();
        }

        private void OnGitLfsLocksGUI()
        {
            EditorGUI.BeginDisabledGroup(isBusy || Repository == null);
            {
                GUILayout.BeginVertical();
                {
                    GUILayout.Label("Locked files", EditorStyles.boldLabel);

                    lockScrollPos = EditorGUILayout.BeginScrollView(lockScrollPos, Styles.GenericTableBoxStyle,
                        GUILayout.Height(125));
                    {
                        GUILayout.BeginVertical();
                        {
                            var lockedFilesCount = lockedFiles.Count;
                            for (var index = 0; index < lockedFilesCount; ++index)
                            {
                                GUIStyle rowStyle = (lockedFileSelection == index)
                                    ? Styles.LockedFileRowSelectedStyle
                                    : Styles.LockedFileRowStyle;
                                GUILayout.Box(lockedFiles[index].Path, rowStyle);

                                if (Event.current.type == EventType.MouseDown &&
                                    GUILayoutUtility.GetLastRect().Contains(Event.current.mousePosition))
                                {
                                    var currentEvent = Event.current;

                                    if (currentEvent.button == 0)
                                    {
                                        lockedFileSelection = index;
                                    }

                                    Event.current.Use();
                                }
                            }
                        }

                        GUILayout.EndVertical();
                    }

                    EditorGUILayout.EndScrollView();

                    if (lockedFileSelection > -1)
                    {
                        GUILayout.BeginVertical();
                        {
                            var lck = lockedFiles[lockedFileSelection];
                            GUILayout.Label(lck.Path, EditorStyles.boldLabel);

                            GUILayout.BeginHorizontal();
                            {
                                GUILayout.Label("Locked by " + lck.User);
                                GUILayout.FlexibleSpace();
                                if (GUILayout.Button("Unlock"))
                                {
                                    Repository.ReleaseLock(lck.Path, false).Start();
                                }
                            }
                            GUILayout.EndHorizontal();
                        }
                        GUILayout.EndVertical();
                    }
                }

                GUILayout.EndVertical();

            }
            EditorGUI.EndDisabledGroup();
        }

        private void OnInstallPathGUI()
        {
            string gitExecPath = null;
            string extension = null;
            string gitInstallPath = null;
            if (Environment != null)
            {
                extension = Environment.ExecutableExtension;
                if (Environment.IsWindows)
                {
                    extension = extension.TrimStart('.');
                }

                gitInstallPath = Environment.GitInstallPath;

                if (Environment.GitExecutablePath != null)
                    gitExecPath = Environment.GitExecutablePath.ToString();
            }

            // Install path
            GUILayout.Label(GitInstallTitle, EditorStyles.boldLabel);

            EditorGUI.BeginDisabledGroup(isBusy || gitExecPath == null);
            {
                // Install path field
                EditorGUI.BeginChangeCheck();
                {
                    //TODO: Verify necessary value for a non Windows OS
                    Styles.PathField(ref gitExecPath,
                        () => EditorUtility.OpenFilePanel(GitInstallBrowseTitle,
                            gitInstallPath,
                            extension), ValidateGitInstall);
                }
                if (EditorGUI.EndChangeCheck())
                {
                    Logger.Trace("Setting GitExecPath: " + gitExecPath);

                    Manager.SystemSettings.Set(Constants.GitInstallPathKey, gitExecPath);
                    Environment.GitExecutablePath = gitExecPath.ToNPath();
                }

                GUILayout.Space(EditorGUIUtility.standardVerticalSpacing);

                GUILayout.BeginHorizontal();
                {
                    // Find button - for attempting to locate a new install
                    if (GUILayout.Button(GitInstallFindButton, GUILayout.ExpandWidth(false)))
                    {
                        var task = new ProcessTask<NPath>(Manager.CancellationToken, new FirstLineIsPathOutputProcessor())
                            .Configure(Manager.ProcessManager, Environment.IsWindows ? "where" : "which", "git")
                            .FinallyInUI((success, ex, path) =>
                            {
                                if (success && !string.IsNullOrEmpty(path))
                                {
                                    Environment.GitExecutablePath = path;
                                    GUIUtility.keyboardControl = GUIUtility.hotControl = 0;
                                }
                            });
                    }
                }
                GUILayout.EndHorizontal();
            }
            EditorGUI.EndDisabledGroup();
        }

        private void OnPrivacyGui()
        {
            var service = Manager != null && Manager.UsageTracker != null ? Manager.UsageTracker : null;

            GUILayout.Label(PrivacyTitle, EditorStyles.boldLabel);

            EditorGUI.BeginDisabledGroup(isBusy || service == null);
            {
                var metricsEnabled = service != null ? service.Enabled : false;
                EditorGUI.BeginChangeCheck();
                {
                    metricsEnabled = GUILayout.Toggle(metricsEnabled, MetricsOptInLabel);
                }
                if (EditorGUI.EndChangeCheck())
                {
                    Manager.UsageTracker.Enabled = metricsEnabled;
                }

            }
            EditorGUI.EndDisabledGroup();
        }

        private void OnLoggingSettingsGui()
        {
            GUILayout.Label(DebugSettingsTitle, EditorStyles.boldLabel);

            EditorGUI.BeginDisabledGroup(isBusy);
            {
                var traceLogging = Logging.TracingEnabled;

                EditorGUI.BeginChangeCheck();
                {
                    traceLogging = GUILayout.Toggle(traceLogging, EnableTraceLoggingLabel);
                }
                if (EditorGUI.EndChangeCheck())
                {
                    Logging.TracingEnabled = traceLogging;
                    Manager.UserSettings.Set(Constants.TraceLoggingKey, traceLogging);
                }
            }
            EditorGUI.EndDisabledGroup();
        }

        private void ResetInitDirectory()
        {
            initDirectory = Utility.UnityProjectPath;
            GUIUtility.keyboardControl = GUIUtility.hotControl = 0;
        }

        private void ForceUnlockFile(object obj)
        {
            var fileName = obj;

            EditorUtility.DisplayDialog("Force unlock file?",
              "Are you sure you want to force unlock " + fileName + "? "
              + "This will notify the owner of the lock.",
              "Unlock",
              "Cancel");
        }

        private void Init()
        {
            //Logger.Debug("TODO: Init '{0}'", initDirectory);
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
    }
}
