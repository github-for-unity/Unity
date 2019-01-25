using GitHub.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
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
        private const string GeneralSettingsTitle = "General";
        private const string DebugSettingsTitle = "Debug";
        private const string PrivacyTitle = "Privacy";
        private const string WebTimeoutLabel = "Timeout of web requests";
        private const string GitTimeoutLabel = "Timeout of git commands";
        private const string EnableTraceLoggingLabel = "Enable Trace Logging";
        private const string MetricsOptInLabel = "Help us improve by sending anonymous usage data";
        private const string DefaultRepositoryRemoteName = "origin";
        private const string DisableGCMLabel = "Don't let GCM (Git Credential Manager for Windows) prompt for credentials.";

        private const string EnableGitAuthPromptsLabel = "Let git prompt for credentials on the command line. This might hang git operations.";
        private const string DisableSetEnvironmentLabel = "Don't customize the environment when spawning processes.";

        [NonSerialized] private bool currentRemoteHasUpdate;
        [NonSerialized] private bool metricsHasChanged;

        [SerializeField] private GitPathView gitPathView;
        [SerializeField] private bool hasRemote;
        [SerializeField] private CacheUpdateEvent lastCurrentRemoteChangedEvent;
        [SerializeField] private bool metricsEnabled;
        [SerializeField] private string newRepositoryRemoteUrl;
        [SerializeField] private string repositoryRemoteName;
        [SerializeField] private string repositoryRemoteUrl;
        [SerializeField] private Vector2 scroll;
        [SerializeField] private UserSettingsView userSettingsView;
        [SerializeField] private int webTimeout;
        [SerializeField] private int gitTimeout;
        [SerializeField] private bool traceLogging;
        [SerializeField] private bool disableGCM;
        [SerializeField] private bool enableGitAuthPrompts;
        [SerializeField] private bool disableSetEnvironment;

        public override void InitializeView(IView parent)
        {
            base.InitializeView(parent);
            gitPathView = gitPathView ?? new GitPathView();
            userSettingsView = userSettingsView ?? new UserSettingsView();

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
                ValidateCachedData(Repository);
            }
        }

        public override void OnDisable()
        {
            base.OnDisable();
            gitPathView.OnDisable();
            userSettingsView.OnDisable();
            DetachHandlers(Repository);
        }

        public override void OnDataUpdate(bool first)
        {
            base.OnDataUpdate(first);
            userSettingsView.OnDataUpdate(first);
            gitPathView.OnDataUpdate(first);

            MaybeUpdateData(first);
        }

        public override void Refresh()
        {
            base.Refresh();
            gitPathView.Refresh();
            userSettingsView.Refresh();
            Refresh(CacheType.RepositoryInfo);
        }

        public override void OnUI()
        {
            var fieldWidth = EditorGUIUtility.fieldWidth;
            var labelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.fieldWidth = 0.0f;
            EditorGUIUtility.labelWidth = 0.0f;

            scroll = GUILayout.BeginScrollView(scroll);
            {
                userSettingsView.OnUI();

                GUILayout.Space(EditorGUIUtility.standardVerticalSpacing);

                if (Repository != null)
                {
                    OnRepositorySettingsUI();
                }

                GUILayout.Space(EditorGUIUtility.standardVerticalSpacing);
                GUILayout.Box(GUIContent.none, Styles.HorizontalLine, GUILayout.ExpandWidth(true), GUILayout.Height(1));
                GUILayout.Space(EditorGUIUtility.standardVerticalSpacing);
                gitPathView.OnUI();
                GUILayout.Space(EditorGUIUtility.standardVerticalSpacing);
                GUILayout.Box(GUIContent.none, Styles.HorizontalLine, GUILayout.ExpandWidth(true), GUILayout.Height(1));
                GUILayout.Space(EditorGUIUtility.standardVerticalSpacing);
                OnPrivacyUI();
                GUILayout.Space(EditorGUIUtility.standardVerticalSpacing);
                GUILayout.Box(GUIContent.none, Styles.HorizontalLine, GUILayout.ExpandWidth(true), GUILayout.Height(1));
                GUILayout.Space(EditorGUIUtility.standardVerticalSpacing);
                OnGeneralSettingsUI();
                GUILayout.Space(EditorGUIUtility.standardVerticalSpacing);
                GUILayout.Box(GUIContent.none, Styles.HorizontalLine, GUILayout.ExpandWidth(true), GUILayout.Height(1));
                GUILayout.Space(EditorGUIUtility.standardVerticalSpacing);
                OnExperimentalSettingsUI();
            }

            GUILayout.EndScrollView();

            EditorGUIUtility.fieldWidth = fieldWidth;
            EditorGUIUtility.labelWidth = labelWidth;

            DoProgressUI();
        }

        private void AttachHandlers(IRepository repository)
        {
            if (repository == null)
            {
                return;
            }

            repository.CurrentRemoteChanged += RepositoryOnCurrentRemoteChanged;
        }

        private void RepositoryOnCurrentRemoteChanged(CacheUpdateEvent cacheUpdateEvent)
        {
            if (!lastCurrentRemoteChangedEvent.Equals(cacheUpdateEvent))
            {
                lastCurrentRemoteChangedEvent = cacheUpdateEvent;
                currentRemoteHasUpdate = true;
                Redraw();
            }
        }

        private void DetachHandlers(IRepository repository)
        {
            if (repository == null)
            {
                return;
            }

            repository.CurrentRemoteChanged -= RepositoryOnCurrentRemoteChanged;
        }

        private void ValidateCachedData(IRepository repository)
        {
            repository.CheckAndRaiseEventsIfCacheNewer(CacheType.RepositoryInfo, lastCurrentRemoteChangedEvent);
        }

        private void MaybeUpdateData(bool first)
        {
            if (first || metricsHasChanged)
            {
                metricsEnabled = Manager.UsageTracker != null ? Manager.UsageTracker.Enabled : false;
                metricsHasChanged = false;
            }

            traceLogging = LogHelper.TracingEnabled;
            webTimeout = ApplicationConfiguration.WebTimeout;
            gitTimeout = ApplicationConfiguration.GitTimeout;
            disableGCM = Manager.UserSettings.Get(Constants.DisableGCMKey, false);
            enableGitAuthPrompts = Manager.UserSettings.Get(Constants.EnableGitAuthPromptsKey, false);
            disableSetEnvironment = Manager.UserSettings.Get(Constants.DisableSetEnvironmentKey, false);

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
        }

        private void OnRepositorySettingsUI()
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
                            IsBusy = true;
                            Repository.SetupRemote(repositoryRemoteName, newRepositoryRemoteUrl)
                                .FinallyInUI((_, __) => { Refresh(); })
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

        private void OnPrivacyUI()
        {
            GUILayout.Label(PrivacyTitle, EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            {
                metricsEnabled = GUILayout.Toggle(metricsEnabled, MetricsOptInLabel, Styles.ToggleNoWrap);
            }
            if (EditorGUI.EndChangeCheck())
            {
                if (Manager.UsageTracker != null)
                    Manager.UsageTracker.Enabled = metricsEnabled;
            }
            EditorGUI.EndDisabledGroup();
        }

        private void OnGeneralSettingsUI()
        {
            GUILayout.Label(GeneralSettingsTitle, EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            {
                webTimeout = EditorGUILayout.IntField(WebTimeoutLabel, webTimeout);
            }
            if (EditorGUI.EndChangeCheck())
            {
                ApplicationConfiguration.WebTimeout = webTimeout;
                Manager.UserSettings.Set(Constants.WebTimeoutKey, webTimeout);
            }

            EditorGUI.BeginChangeCheck();
            {
                gitTimeout = EditorGUILayout.IntField(GitTimeoutLabel, gitTimeout);
            }
            if (EditorGUI.EndChangeCheck())
            {
                ApplicationConfiguration.GitTimeout = gitTimeout;
                Manager.UserSettings.Set(Constants.GitTimeoutKey, gitTimeout);
            }
        }

        private void OnExperimentalSettingsUI()
        {
            GUILayout.Label(DebugSettingsTitle, EditorStyles.boldLabel);

            if (DoBoolSettingUI(ref traceLogging, EnableTraceLoggingLabel, Constants.TraceLoggingKey))
            {
                LogHelper.TracingEnabled = traceLogging;
            }

            DoBoolSettingUI(ref disableGCM, DisableGCMLabel, Constants.DisableGCMKey);
            DoBoolSettingUI(ref enableGitAuthPrompts, EnableGitAuthPromptsLabel, Constants.EnableGitAuthPromptsKey);
            DoBoolSettingUI(ref disableSetEnvironment, DisableSetEnvironmentLabel, Constants.DisableSetEnvironmentKey);
        }

        private bool DoBoolSettingUI(ref bool value, string label, string key)
        {
            EditorGUI.BeginChangeCheck();
            {
                value = GUILayout.Toggle(value, label, Styles.Toggle);
            }
            if (EditorGUI.EndChangeCheck())
            {
                Manager.UserSettings.Set(key, value);
                return true;
            }
            return false;
        }

        public override bool IsBusy { get; set; }
    }
}
