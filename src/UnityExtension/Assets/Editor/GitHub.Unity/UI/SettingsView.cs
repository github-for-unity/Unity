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
        private const string GitInstallTitle = "Git installation";
        private const string GitInstallBrowseTitle = "Select git binary";
        private const string GitInstallPickInvalidTitle = "Invalid Git install";
        private const string GitInstallPickInvalidMessage = "The selected file is not a valid Git install. {0}";
        private const string GitInstallPickInvalidOK = "OK";
        private const string GitInstallFindButton = "Find install";
        private const string GitConfigTitle = "Git Configuration";
        private const string GitConfigNameLabel = "Name";
        private const string GitConfigEmailLabel = "Email";
        private const string GitConfigUserSave = "Save User";
        private const string GitRepositoryTitle = "Repository Configuration";
        private const string GitRepositoryRemoteLabel = "Remote";
        private const string GitRepositorySave = "Save Repository";
        private const string DebugSettingsTitle = "Debug";
        private const string PrivacyTitle = "Privacy";
        private const string EnableTraceLoggingLabel = "Enable Trace Logging";
        private const string MetricsOptInLabel = "Help us improve by sending anonymous usage data";
        private const string DefaultRepositoryRemoteName = "origin";
        private const string BrowseButton = "...";
        private const string PathToGit = "Path to Git";
        private const string GitPathSaveButton = "Save Path";

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

        [SerializeField] private string gitExec;
        [SerializeField] private string gitExecParent;
        [SerializeField] private string gitExecExtension;
        [SerializeField] private string newGitExec;
        [NonSerialized] private bool gitExecHasChanged;

        public override void OnEnable()
        {
            base.OnEnable();
            AttachHandlers(Repository);

            remoteHasChanged = true;
            gitExecHasChanged = true;
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

            if (gitExecHasChanged)
            {
                if (Environment != null)
                {
                    if (gitExecExtension == null)
                    {
                        gitExecExtension = Environment.ExecutableExtension;

                        if (Environment.IsWindows)
                        {
                            gitExecExtension = gitExecExtension.TrimStart('.');
                        }
                    }

                    if (Environment.GitExecutablePath != null)
                    {
                        newGitExec = gitExec = Environment.GitExecutablePath.ToString();
                        gitExecParent = Environment.GitExecutablePath.Parent.ToString();
                    }

                    if (gitExecParent == null)
                    {
                        gitExecParent = Environment.GitInstallPath;
                    }
                }

                gitExecHasChanged = false;
            }

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
            // Install path
            GUILayout.Label(GitInstallTitle, EditorStyles.boldLabel);

            EditorGUI.BeginDisabledGroup(isBusy);
            {
                // Install path field
                GUILayout.BeginHorizontal();
                { 
                    newGitExec = EditorGUILayout.TextField(PathToGit, newGitExec);

                    if (GUILayout.Button(BrowseButton, EditorStyles.miniButton, GUILayout.Width(25)))
                    {
                        GUI.FocusControl(null);

                        var newValue = EditorUtility.OpenFilePanel(GitInstallBrowseTitle,
                            gitExecParent,
                            gitExecExtension);

                        if (!string.IsNullOrEmpty(newValue))
                        {
                            newGitExec = newValue;
                        }
                    }
                }
                GUILayout.EndHorizontal();

                GUILayout.Space(EditorGUIUtility.standardVerticalSpacing);

                GUILayout.BeginHorizontal();
                {
                    var needsSaving = !string.IsNullOrEmpty(newGitExec)
                        && newGitExec != gitExec
                        && newGitExec.ToNPath().FileExists();

                    EditorGUI.BeginDisabledGroup(!needsSaving);
                    { 
                        if (GUILayout.Button(GitPathSaveButton, GUILayout.ExpandWidth(false)))
                        {
                            Logger.Trace("Saving Git Path:{0}", newGitExec);

                            GUI.FocusControl(null);

                            Manager.SystemSettings.Set(Constants.GitInstallPathKey, newGitExec);
                            Environment.GitExecutablePath = newGitExec.ToNPath();
                            gitExecHasChanged = true;
                        }
                    }
                    EditorGUI.EndDisabledGroup();

                    //Find button - for attempting to locate a new install
                    if (GUILayout.Button(GitInstallFindButton, GUILayout.ExpandWidth(false)))
                    {
                        GUI.FocusControl(null);

                        var task = new ProcessTask<NPath>(Manager.CancellationToken, new FirstLineIsPathOutputProcessor())
                            .Configure(Manager.ProcessManager, Environment.IsWindows ? "where" : "which", "git")
                            .FinallyInUI((success, ex, path) =>
                            {
                                Logger.Trace("Find Git Completed Success:{0} Path:{1}", success, path);

                                if (success && !string.IsNullOrEmpty(path))
                                {
                                    Manager.SystemSettings.Set(Constants.GitInstallPathKey, path);
                                    Environment.GitExecutablePath = path;
                                    gitExecHasChanged = true;
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
            var service = Manager != null ? Manager.UsageTracker : null;

            GUILayout.Label(PrivacyTitle, EditorStyles.boldLabel);

            EditorGUI.BeginDisabledGroup(isBusy || service == null);
            {
                var metricsEnabled = service != null && service.Enabled;
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
    }
}
