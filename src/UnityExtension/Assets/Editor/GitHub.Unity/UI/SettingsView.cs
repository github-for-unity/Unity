using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace GitHub.Unity
{
    [Serializable]
    class SettingsView : Subview
    {
        private const string GitRepositoryTitle = "Repository Configuration";
        private const string GitRepositoryRemoteLabel = "Remote";
        private const string GitRepositorySave = "Save Repository";
        private const string DebugSettingsTitle = "Debug";
        private const string PrivacyTitle = "Privacy";
        private const string EnableTraceLoggingLabel = "Enable Trace Logging";
        private const string MetricsOptInLabel = "Help us improve by sending anonymous usage data";
        private const string DefaultRepositoryRemoteName = "origin";

        [NonSerialized] private bool currentLocksHasUpdate;
        [NonSerialized] private bool currentRemoteHasUpdate;
        [NonSerialized] private bool isBusy;
        [NonSerialized] private bool metricsHasChanged;

        [SerializeField] private GitPathView gitPathView = new GitPathView();
        [SerializeField] private bool hasRemote;
        [SerializeField] private CacheUpdateEvent lastCurrentRemoteChangedEvent;
        [SerializeField] private CacheUpdateEvent lastLocksChangedEvent;
        [SerializeField] private List<GitLock> lockedFiles = new List<GitLock>();
        [SerializeField] private int lockedFileSelection = -1;
        [SerializeField] private Vector2 lockScrollPos;
        [SerializeField] private bool metricsEnabled;
        [SerializeField] private string newRepositoryRemoteUrl;
        [SerializeField] private string repositoryRemoteName;
        [SerializeField] private string repositoryRemoteUrl;
        [SerializeField] private Vector2 scroll;
        [SerializeField] private UserSettingsView userSettingsView = new UserSettingsView();

        public override void InitializeView(IView parent)
        {
            base.InitializeView(parent);
            gitPathView.InitializeView(this);
            userSettingsView.InitializeView(this);
        }

        public override void OnEnable()
        {
            base.OnEnable();
            gitPathView.OnEnable();
            userSettingsView.OnEnable();
            AttachHandlers(Repository);

            if (Repository != null)
            {
                Repository.CheckCurrentRemoteChangedEvent(lastCurrentRemoteChangedEvent);
                Repository.CheckLocksChangedEvent(lastLocksChangedEvent);
            }

            metricsHasChanged = true;
        }

        public override void OnDisable()
        {
            base.OnDisable();
            gitPathView.OnDisable();
            userSettingsView.OnDisable();
            DetachHandlers(Repository);
        }

        public override void OnDataUpdate()
        {
            base.OnDataUpdate();
            userSettingsView.OnDataUpdate();
            gitPathView.OnDataUpdate();

            MaybeUpdateData();
        }

        public override void Refresh()
        {
            base.Refresh();
            gitPathView.Refresh();
            userSettingsView.Refresh();
        }

        public override void OnGUI()
        {
            scroll = GUILayout.BeginScrollView(scroll);
            {
                userSettingsView.OnGUI();

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

        private void AttachHandlers(IRepository repository)
        {
            if (repository == null)
            {
                return;
            }

            repository.CurrentRemoteChanged += RepositoryOnCurrentRemoteChanged;
            repository.LocksChanged += RepositoryOnLocksChanged;
        }

        private void RepositoryOnLocksChanged(CacheUpdateEvent cacheUpdateEvent)
        {
            if (!lastLocksChangedEvent.Equals(cacheUpdateEvent))
            {
                new ActionTask(TaskManager.Token, () => {
                    lastLocksChangedEvent = cacheUpdateEvent;
                    currentLocksHasUpdate = true;
                    Redraw();
                }) { Affinity = TaskAffinity.UI }.Start();
            }
        }

        private void RepositoryOnCurrentRemoteChanged(CacheUpdateEvent cacheUpdateEvent)
        {
            if (!lastCurrentRemoteChangedEvent.Equals(cacheUpdateEvent))
            {
                new ActionTask(TaskManager.Token, () => {
                    lastCurrentRemoteChangedEvent = cacheUpdateEvent;
                    currentRemoteHasUpdate = true;
                    Redraw();
                }) { Affinity = TaskAffinity.UI }.Start();
            }
        }

        private void DetachHandlers(IRepository repository)
        {
            if (repository == null)
            {
                return;
            }
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
                return;

            if (currentRemoteHasUpdate)
            {
                currentRemoteHasUpdate = false;
                var currentRemote = Repository.CurrentRemote;
                hasRemote = currentRemote.HasValue && !String.IsNullOrEmpty(currentRemote.Value.Url);
                if (!hasRemote)
                {
                    repositoryRemoteName = DefaultRepositoryRemoteName;
                    newRepositoryRemoteUrl = repositoryRemoteUrl = string.Empty;
                }
                else
                {
                    repositoryRemoteName = currentRemote.Value.Name;
                    newRepositoryRemoteUrl = repositoryRemoteUrl = currentRemote.Value.Url;
                }
            }

            if (currentLocksHasUpdate)
            {
                currentLocksHasUpdate = false;
                var repositoryCurrentLocks = Repository.CurrentLocks;
                lockedFiles = repositoryCurrentLocks != null
                    ? repositoryCurrentLocks.ToList()
                    : new List<GitLock>();
            }
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
            get { return isBusy || userSettingsView.IsBusy || gitPathView.IsBusy; }
        }
    }
}
