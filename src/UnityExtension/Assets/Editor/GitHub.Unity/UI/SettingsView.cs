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
        [NonSerialized] private bool isBusy;

        [SerializeField] private string gitName;
        [SerializeField] private string gitEmail;

        [SerializeField] private int gitIgnoreRulesSelection = 0;
        [SerializeField] private string initDirectory;
        [SerializeField] private List<GitLock> lockedFiles = new List<GitLock>();
        [SerializeField] private Vector2 lockScrollPos;
        [SerializeField] private string repositoryRemoteName;
        [SerializeField] private string repositoryRemoteUrl;
        [SerializeField] private Vector2 scroll;
        [SerializeField] private int lockedFileSelection = -1;
        [SerializeField] private bool hasRemote;
        [NonSerialized] private bool remoteHasChanged;
        [NonSerialized] private bool userDataHasChanged;

        [SerializeField] private string newGitName;
        [SerializeField] private string newGitEmail;
        [SerializeField] private string newRepositoryRemoteUrl;
        [SerializeField] private User cachedUser;
        
        [SerializeField] private bool metricsEnabled;
        [NonSerialized] private bool metricsHasChanged;
        
        [SerializeField] private GitPathView gitPathView = new GitPathView();

        public override void InitializeView(IView parent)
        {
            base.InitializeView(parent);
            gitPathView.InitializeView(this);
        }


        public override void OnEnable()
        {
            base.OnEnable();
            gitPathView.OnEnable();
            AttachHandlers(Repository);

            remoteHasChanged = true;
            metricsHasChanged = true;
        }

        public override void OnDisable()
        {
            base.OnDisable();
            gitPathView.OnDisable();
            DetachHandlers(Repository);
        }

        public override void OnDataUpdate()
        {
            base.OnDataUpdate();

            if (gitPathView != null)
            {
                gitPathView.OnDataUpdate();
            }
            MaybeUpdateData();
        }

        public override void OnRepositoryChanged(IRepository oldRepository)
        {
            base.OnRepositoryChanged(oldRepository);
            gitPathView.OnRepositoryChanged(oldRepository);

            DetachHandlers(oldRepository);
            AttachHandlers(Repository);

            remoteHasChanged = true;

            Refresh();
        }

        public override bool IsBusy
        {
            get { return isBusy || gitPathView.IsBusy; }
        }

        public override void Refresh()
        {
            base.Refresh();
            gitPathView.Refresh();
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

                gitPathView.OnGUI();
                OnPrivacyGui();
                OnLoggingSettingsGui();
            }

            GUILayout.EndScrollView();
        }

        private void MaybeUpdateData()
        {
            if (metricsHasChanged)
            {
                metricsEnabled = Manager.UsageTracker.Enabled;
                metricsHasChanged = false;
            }

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
                                        Repository.User.Name = newGitName;
                                    }
                                    else
                                    {
                                        if (cachedUser == null)
                                        {
                                            cachedUser = new User();
                                        }
                                        cachedUser.Name = newGitName;
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
                                        Repository.User.Email = newGitEmail;
                                    }
                                    else
                                    {
                                        cachedUser.Email = newGitEmail;
                                    }

                                    userDataHasChanged = true;
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

            EditorGUI.BeginDisabledGroup(IsBusy);
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
            EditorGUI.BeginDisabledGroup(IsBusy || Repository == null);
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

        private void OnPrivacyGui()
        {
            GUILayout.Label(PrivacyTitle, EditorStyles.boldLabel);

            EditorGUI.BeginDisabledGroup(IsBusy);
            {
                
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

            EditorGUI.BeginDisabledGroup(IsBusy);
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

        public override bool IsBusy
        {
            get { return isBusy; }
        }
    }
}
