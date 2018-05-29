using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace GitHub.Unity
{
    [Serializable]
    class Window : BaseWindow
    {
        private const float DefaultNotificationTimeout = 2f;
        private const string Title = "GitHub";
        private const string Menu_Window_GitHub = "Window/GitHub";
        private const string Menu_Window_GitHub_Command_Line = "Window/GitHub Command Line";

        [NonSerialized] private Spinner spinner;
        [NonSerialized] private IProgress repositoryProgress;
        [NonSerialized] private IProgress appManagerProgress;

        [SerializeField] private double progressMessageClearTime = -1;
        [SerializeField] private double notificationClearTime = -1;
        [SerializeField] private double timeSinceLastRotation = -1f;
        [SerializeField] private bool currentBranchAndRemoteHasUpdate;
        [SerializeField] private bool currentTrackingStatusHasUpdate;
        [SerializeField] private bool currentStatusEntriesHasUpdate;
        [SerializeField] private bool repositoryProgressHasUpdate;
        [SerializeField] private bool appManagerProgressHasUpdate;
        [SerializeField] private SubTab changeTab = SubTab.InitProject;
        [SerializeField] private SubTab activeTab = SubTab.InitProject;
        [SerializeField] private InitProjectView initProjectView = new InitProjectView();
        [SerializeField] private BranchesView branchesView = new BranchesView();
        [SerializeField] private ChangesView changesView = new ChangesView();
        [SerializeField] private HistoryView historyView = new HistoryView();
        [SerializeField] private SettingsView settingsView = new SettingsView();
        [SerializeField] private LocksView locksView = new LocksView();
        [SerializeField] private bool hasRemote;
        [SerializeField] private string currentRemoteName;
        [SerializeField] private string currentBranch;
        [SerializeField] private string currentRemoteUrl;
        [SerializeField] private int statusAhead;
        [SerializeField] private int statusBehind;
        [SerializeField] private bool hasItemsToCommit;
        [SerializeField] private GUIContent currentBranchContent;
        [SerializeField] private GUIContent currentRemoteUrlContent;
        [SerializeField] private CacheUpdateEvent lastCurrentBranchAndRemoteChangedEvent;
        [SerializeField] private CacheUpdateEvent lastTrackingStatusChangedEvent;
        [SerializeField] private CacheUpdateEvent lastStatusEntriesChangedEvent;

        [SerializeField] private GUIContent pullButtonContent = new GUIContent(Localization.PullButton);
        [SerializeField] private GUIContent pushButtonContent = new GUIContent(Localization.PushButton);
        [SerializeField] private GUIContent refreshButtonContent = new GUIContent(Localization.RefreshButton);
        [SerializeField] private GUIContent fetchButtonContent = new GUIContent(Localization.FetchButton);
        [SerializeField] private float repositoryProgressValue;
        [SerializeField] private string repositoryProgressMessage;
        [SerializeField] private float appManagerProgressValue;
        [SerializeField] private string appManagerProgressMessage;

        [MenuItem(Menu_Window_GitHub)]
        public static void Window_GitHub()
        {
            ShowWindow(EntryPoint.ApplicationManager);
        }

        [MenuItem(Menu_Window_GitHub_Command_Line)]
        public static void GitHub_CommandLine()
        {
            EntryPoint.ApplicationManager.ProcessManager.RunCommandLineWindow(NPath.CurrentDirectory);
            EntryPoint.ApplicationManager.TaskManager.Run(EntryPoint.ApplicationManager.UsageTracker.IncrementApplicationMenuMenuItemCommandLine, null);
        }

#if DEBUG

        [MenuItem("GitHub/Select Window")]
        public static void GitHub_SelectWindow()
        {
            var window = Resources.FindObjectsOfTypeAll(typeof(Window)).FirstOrDefault() as Window;
            Selection.activeObject = window;
        }

        [MenuItem("GitHub/Restart")]
        public static void GitHub_Restart()
        {
            EntryPoint.Restart();
        }
#endif

        public static void ShowWindow(IApplicationManager applicationManager)
        {
            var type = typeof(EditorWindow).Assembly.GetType("UnityEditor.InspectorWindow");
            var window = GetWindow<Window>(type);
            window.InitializeWindow(applicationManager);
            window.Show();
        }

        public static Window GetWindow()
        {
            return Resources.FindObjectsOfTypeAll(typeof(Window)).FirstOrDefault() as Window;
        }

        public override void Initialize(IApplicationManager applicationManager)
        {
            base.Initialize(applicationManager);

            applicationManager.OnProgress += ApplicationManagerOnProgress;

            HistoryView.InitializeView(this);
            ChangesView.InitializeView(this);
            BranchesView.InitializeView(this);
            SettingsView.InitializeView(this);
            LocksView.InitializeView(this);
            InitProjectView.InitializeView(this);

            titleContent = new GUIContent(Title, Styles.SmallLogo);

            if (!HasRepository)
            {
                changeTab = activeTab = SubTab.InitProject;
            }
        }

        public override void OnEnable()
        {
            base.OnEnable();

            if (Repository != null)
                ValidateCachedData(Repository);

            if (ActiveView != null)
                ActiveView.OnEnable();

            if (spinner == null)
                spinner = new Spinner();

            ClearProgressMessage();
        }

        public override void OnDisable()
        {
            base.OnDisable();
            if (ActiveView != null)
                ActiveView.OnDisable();
        }

        public override void OnDataUpdate()
        {
            base.OnDataUpdate();
            MaybeUpdateData();

            if (ActiveView != null)
                ActiveView.OnDataUpdate();
        }

        public override void OnFocusChanged()
        {
            if (ActiveView != null)
                ActiveView.OnFocusChanged();
        }

        public override void OnRepositoryChanged(IRepository oldRepository)
        {
            base.OnRepositoryChanged(oldRepository);

            DetachHandlers(oldRepository);
            AttachHandlers(Repository);

            if (HasRepository)
            {
                if (activeTab == SubTab.InitProject)
                {
                    changeTab = SubTab.History;
                    UpdateActiveTab();
                }
            }
            else
            {
                if (activeTab != SubTab.InitProject)
                {
                    changeTab = SubTab.InitProject;
                    UpdateActiveTab();
                }
            }
        }

        public override void OnSelectionChange()
        {
            base.OnSelectionChange();
            if (ActiveView != null)
                ActiveView.OnSelectionChange();
        }

        public override void Refresh()
        {
            SetProgressMessage(Localization.MessageRefreshing, 0);
            base.Refresh();
            if (ActiveView != null)
                ActiveView.Refresh();
            Redraw();
        }

        public override void DoneRefreshing()
        {
            base.DoneRefreshing();
            SetProgressMessage(Localization.MessageRefreshed, 100);
        }

        private void ValidateCachedData(IRepository repository)
        {
            repository.CheckAndRaiseEventsIfCacheNewer(CacheType.RepositoryInfo, lastCurrentBranchAndRemoteChangedEvent);
        }

        private void MaybeUpdateData()
        {
            if (repositoryProgressHasUpdate)
            {
                if (repositoryProgress != null)
                {
                    repositoryProgressMessage = repositoryProgress.Message;
                    repositoryProgressValue = repositoryProgress.Percentage;
                    if (progressMessageClearTime == -1f || progressMessageClearTime < EditorApplication.timeSinceStartup + DefaultNotificationTimeout)
                        progressMessageClearTime = EditorApplication.timeSinceStartup + DefaultNotificationTimeout;
                }
                else
                {
                    repositoryProgressMessage = "";
                    repositoryProgressValue = 0;
                    progressMessageClearTime = -1f;
                }
                repositoryProgressHasUpdate = false;
            }

            if (appManagerProgressHasUpdate)
            {
                if (appManagerProgress != null)
                {
                    appManagerProgressValue = appManagerProgress.Percentage;
                    appManagerProgressMessage = appManagerProgress.Message;
                }
                else
                {
                    appManagerProgressValue = 0;
                    appManagerProgressMessage = "";
                }
                appManagerProgressHasUpdate = false;
            }

            string updatedRepoRemote = null;
            string updatedRepoUrl = Localization.DefaultRepoUrl;

            var shouldUpdateContentFields = false;

            if (currentTrackingStatusHasUpdate)
            {
                currentTrackingStatusHasUpdate = false;
                statusAhead = Repository.CurrentAhead;
                statusBehind = Repository.CurrentBehind;
            }

            if (currentStatusEntriesHasUpdate)
            {
                currentStatusEntriesHasUpdate = false;
                var currentChanges = Repository.CurrentChanges;
                hasItemsToCommit = currentChanges != null &&
                    currentChanges.Any(entry => entry.Status != GitFileStatus.Ignored && !entry.Staged);
            }

            if (currentBranchAndRemoteHasUpdate)
            {
                hasRemote = false;
            }

            if (Repository != null)
            {
                if (currentBranch == null || currentRemoteName == null || currentBranchAndRemoteHasUpdate)
                {
                    currentBranchAndRemoteHasUpdate = false;

                    var repositoryCurrentBranch = Repository.CurrentBranch;
                    var updatedRepoBranch = repositoryCurrentBranch.HasValue ? repositoryCurrentBranch.Value.Name : null;

                    var repositoryCurrentRemote = Repository.CurrentRemote;
                    if (repositoryCurrentRemote.HasValue)
                    {
                        hasRemote = true;
                        updatedRepoRemote = repositoryCurrentRemote.Value.Name;
                        if (!string.IsNullOrEmpty(repositoryCurrentRemote.Value.Url))
                        {
                            updatedRepoUrl = repositoryCurrentRemote.Value.Url;
                        }
                    }

                    if (currentRemoteName != updatedRepoRemote)
                    {
                        currentRemoteName = updatedRepoBranch;
                        shouldUpdateContentFields = true;
                    }

                    if (currentBranch != updatedRepoBranch)
                    {
                        currentBranch = updatedRepoBranch;
                        shouldUpdateContentFields = true;
                    }

                    if (currentRemoteUrl != updatedRepoUrl)
                    {
                        currentRemoteUrl = updatedRepoUrl;
                        shouldUpdateContentFields = true;
                    }
                }
            }
            else
            {
                if (currentRemoteName != null)
                {
                    currentRemoteName = null;
                    shouldUpdateContentFields = true;
                }

                if (currentBranch != null)
                {
                    currentBranch = null;
                    shouldUpdateContentFields = true;
                }

                if (currentRemoteUrl != Localization.DefaultRepoUrl)
                {
                    currentRemoteUrl = Localization.DefaultRepoUrl;
                    shouldUpdateContentFields = true;
                }
            }

            if (shouldUpdateContentFields || currentBranchContent == null || currentRemoteUrlContent == null)
            {
                currentBranchContent = new GUIContent(currentBranch, Localization.Window_RepoBranchTooltip);

                if (currentRemoteName != null)
                {
                    currentRemoteUrlContent = new GUIContent(currentRemoteUrl, string.Format(Localization.Window_RepoUrlTooltip, currentRemoteName));
                }
                else
                {
                    currentRemoteUrlContent = new GUIContent(currentRemoteUrl, Localization.Window_RepoNoUrlTooltip);
                }
            }
        }

        private void AttachHandlers(IRepository repository)
        {
            if (repository == null)
                return;
            repository.CurrentBranchAndRemoteChanged += RepositoryOnCurrentBranchAndRemoteChanged;
            repository.TrackingStatusChanged += RepositoryOnTrackingStatusChanged;
            repository.StatusEntriesChanged += RepositoryOnStatusEntriesChanged;
            repository.OnProgress += UpdateProgress;
        }

        private void DetachHandlers(IRepository repository)
        {
            if (repository == null)
                return;
            repository.CurrentBranchAndRemoteChanged -= RepositoryOnCurrentBranchAndRemoteChanged;
            repository.TrackingStatusChanged -= RepositoryOnTrackingStatusChanged;
            repository.StatusEntriesChanged -= RepositoryOnStatusEntriesChanged;
            repository.OnProgress -= UpdateProgress;
            Manager.OnProgress -= ApplicationManagerOnProgress;
        }

        private void RepositoryOnCurrentBranchAndRemoteChanged(CacheUpdateEvent cacheUpdateEvent)
        {
            if (!lastCurrentBranchAndRemoteChangedEvent.Equals(cacheUpdateEvent))
            {
                lastCurrentBranchAndRemoteChangedEvent = cacheUpdateEvent;
                currentBranchAndRemoteHasUpdate = true;
                Redraw();
            }
        }

        private void RepositoryOnTrackingStatusChanged(CacheUpdateEvent cacheUpdateEvent)
        {
            if (!lastTrackingStatusChangedEvent.Equals(cacheUpdateEvent))
            {
                lastTrackingStatusChangedEvent = cacheUpdateEvent;
                currentTrackingStatusHasUpdate = true;
                Redraw();
            }
        }

        private void RepositoryOnStatusEntriesChanged(CacheUpdateEvent cacheUpdateEvent)
        {
            if (!lastStatusEntriesChangedEvent.Equals(cacheUpdateEvent))
            {
                lastStatusEntriesChangedEvent = cacheUpdateEvent;
                currentStatusEntriesHasUpdate = true;
                Redraw();
            }
        }

        private static object lck = new object();
        public override void UpdateProgress(IProgress progress)
        {
            lock (lck)
            {
                repositoryProgress = progress;
                if (repositoryProgress != null && progress != null)
                {
                    repositoryProgress.UpdateProgress(progress.Value, progress.Total, progress.Message);
                }
                repositoryProgressHasUpdate = true;
            }

            if (!ThreadingHelper.InUIThread)
                TaskManager.RunInUI(Redraw);
            else
                Redraw();
        }

        private void ApplicationManagerOnProgress(IProgress progress)
        {
            appManagerProgress = progress;
            appManagerProgressHasUpdate = true;
        }

        public override void OnUI()
        {
            base.OnUI();

            GUILayout.BeginVertical(Styles.HeaderStyle);

            if (HasRepository)
            {
                DoActionbarGUI();
                DoHeaderGUI();
            }

            DoToolbarGUI();
            DoActiveViewGUI();

            GUILayout.EndVertical();
        }

        public override void Update()
        {
            base.Update();

            // Notification auto-clear timer override
            if (notificationClearTime > 0f && EditorApplication.timeSinceStartup > notificationClearTime)
            {
                notificationClearTime = -1f;
                RemoveNotification();
                Redraw();
            }

            // Notification auto-clear timer override
            if (progressMessageClearTime > 0f && EditorApplication.timeSinceStartup > progressMessageClearTime)
            {
                repositoryProgressHasUpdate = true;
                ClearProgressMessage();
            }
            else if (EditorApplication.timeSinceStartup < progressMessageClearTime)
            {
                Redraw();
            }

            if (IsBusy && activeTab != SubTab.Settings)
            {
                Redraw();
            }
            else
            {
                timeSinceLastRotation = -1f;
                spinner.Stop();
            }
        }

        public override void DoProgressGUI()
        {
            Rect rect1 = GUILayoutUtility.GetRect(position.width, 20);
            if (Event.current.GetTypeForControl(GUIUtility.GetControlID("ghu_ProgressBar".GetHashCode(), FocusType.Keyboard, position)) == EventType.Repaint)
            {
                var style = Styles.ProgressAreaBackStyle;
                style.Draw(rect1, false, false, false, false);
                Rect rect2 = new Rect(rect1.x, rect1.y, position.width * repositoryProgressValue, rect1.height);
                style = GUI.skin.FindStyle("ProgressBarBar");
                style.Draw(rect2, false, false, false, false);
                style = GUI.skin.FindStyle("ProgressBarText");
                style.Draw(rect1, repositoryProgressMessage, false, false, false, false);
            }

            if (repositoryProgressValue == 1f)
                Redraw();
        }

        private void DoHeaderGUI()
        {
            GUILayout.BeginHorizontal(Styles.HeaderBoxStyle);
            {
                GUILayout.Space(3);
                GUILayout.BeginVertical(GUILayout.Width(16));
                {
                    GUILayout.Space(9);
                    GUILayout.Label(Styles.RepoIcon, GUILayout.Height(20), GUILayout.Width(20));
                }
                GUILayout.EndVertical();

                GUILayout.BeginVertical();
                {
                    GUILayout.Space(3);

                    GUILayout.Label(currentRemoteUrlContent, Styles.HeaderRepoLabelStyle);
                    GUILayout.Space(-2);
                    GUILayout.Label(currentBranchContent, Styles.HeaderBranchLabelStyle);
                }
                GUILayout.EndVertical();
            }
            GUILayout.EndHorizontal();
        }

        private void DoToolbarGUI()
        {
            // Subtabs & toolbar
            GUILayout.BeginHorizontal(EditorStyles.toolbar);
            {
                EditorGUI.BeginChangeCheck();
                {
                    if (HasRepository)
                    {
                        changeTab = TabButton(SubTab.Changes, Localization.ChangesTitle, changeTab);
                        changeTab = TabButton(SubTab.Locks, Localization.LocksTitle, changeTab);
                        changeTab = TabButton(SubTab.History, Localization.HistoryTitle, changeTab);
                        changeTab = TabButton(SubTab.Branches, Localization.BranchesTitle, changeTab);
                    }
                    else if (!HasRepository)
                    {
                        changeTab = TabButton(SubTab.InitProject, Localization.InitializeTitle, changeTab);
                    }
                    changeTab = TabButton(SubTab.Settings, Localization.SettingsTitle, changeTab);
                }

                if (EditorGUI.EndChangeCheck())
                {
                    UpdateActiveTab();
                }

                GUILayout.FlexibleSpace();
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DoActionbarGUI()
        {
            GUILayout.BeginHorizontal(EditorStyles.toolbar);
            {
                if (hasRemote)
                {
                    EditorGUI.BeginDisabledGroup(currentRemoteName == null);
                    {
                        // Fetch button
                        var fetchClicked = GUILayout.Button(fetchButtonContent, Styles.ToolbarButtonStyle);
                        if (fetchClicked)
                        {
                            Fetch();
                        }

                        // Pull button
                        var pullButtonText = statusBehind > 0 ? new GUIContent(String.Format(Localization.PullButtonCount, statusBehind)) : pullButtonContent;
                        var pullClicked = GUILayout.Button(pullButtonText, Styles.ToolbarButtonStyle);

                        if (pullClicked &&
                            EditorUtility.DisplayDialog(Localization.PullConfirmTitle,
                                String.Format(Localization.PullConfirmDescription, currentRemoteName),
                                Localization.PullConfirmYes,
                                Localization.Cancel)
                        )
                        {
                            Pull();
                        }
                    }
                    EditorGUI.EndDisabledGroup();

                    // Push button
                    EditorGUI.BeginDisabledGroup(currentRemoteName == null || statusAhead == 0);
                    {
                        var pushButtonText = statusAhead > 0 ? new GUIContent(String.Format(Localization.PushButtonCount, statusAhead)) : pushButtonContent;
                        var pushClicked = GUILayout.Button(pushButtonText, Styles.ToolbarButtonStyle);

                        if (pushClicked &&
                            EditorUtility.DisplayDialog(Localization.PushConfirmTitle,
                                String.Format(Localization.PushConfirmDescription, currentRemoteName),
                                Localization.PushConfirmYes,
                                Localization.Cancel)
                        )
                        {
                            Push();
                        }
                    }
                    EditorGUI.EndDisabledGroup();
                }
                else
                {
                    // Publishing a repo
                    if (GUILayout.Button(Localization.PublishButton, Styles.ToolbarButtonStyle))
                    {
                        PopupWindow.OpenWindow(PopupViewType.PublishView, null, null);
                    }
                }

                if (GUILayout.Button(refreshButtonContent, Styles.ToolbarButtonStyle))
                {
                    Refresh();
                }

                GUILayout.FlexibleSpace();

                if (GUILayout.Button(Localization.AccountButton, EditorStyles.toolbarDropDown))
                    DoAccountDropdown();
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DoActiveViewGUI()
        {
            var rect = GUILayoutUtility.GetLastRect();
            // GUI for the active tab
            if (ActiveView != null)
            {
                ActiveView.OnGUI();
            }

            if (IsBusy && activeTab != SubTab.Settings && Event.current.type == EventType.Repaint)
            {
                if (timeSinceLastRotation < 0)
                {
                    timeSinceLastRotation = EditorApplication.timeSinceStartup;
                }
                else
                {
                    var elapsedTime = (float)(EditorApplication.timeSinceStartup - timeSinceLastRotation);
                    if (spinner == null)
                        spinner = new Spinner();
                    spinner.Start(elapsedTime);
                    spinner.Rotate(elapsedTime);

                    spinner.Render();

                    rect = new Rect(0f, rect.y + rect.height, Position.width, Position.height - (rect.height + rect.y));
                    rect = spinner.Layout(rect);
                    rect.y += rect.height + 30;
                    rect.height = 20;
                    if (!String.IsNullOrEmpty(appManagerProgressMessage))
                        EditorGUI.ProgressBar(rect, appManagerProgressValue, appManagerProgressMessage);
                }
            }
        }

        public override void DoEmptyGUI()
        {
            GUILayout.BeginVertical();
            GUILayout.FlexibleSpace();
            GUILayout.BeginHorizontal();
            {
                GUILayout.FlexibleSpace();
                GUILayout.Label(Styles.EmptyStateInit, GUILayout.MaxWidth(265), GUILayout.MaxHeight(136));
                GUILayout.FlexibleSpace();
            }
            GUILayout.EndHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.EndVertical();
        }

        private void Pull()
        {
            if (hasItemsToCommit)
            {
                EditorUtility.DisplayDialog("Pull", "You need to commit your changes before pulling.", "Cancel");
            }
            else
            {
                SetProgressMessage(Localization.MessagePulling, 0, 60f);
                Repository
                    .Pull()
                    .FinallyInUI((success, e) =>
                    {
                        if (success)
                        {
                            SetProgressMessage(Localization.MessagePulled, 100);
                            TaskManager.Run(EntryPoint.ApplicationManager.UsageTracker.IncrementHistoryViewToolbarPull, null);

                            EditorUtility.DisplayDialog(Localization.PullActionTitle,
                                String.Format(Localization.PullSuccessDescription, currentRemoteName),
                            Localization.Ok);
                        }
                        else
                        {
                            SetProgressMessage(Localization.MessagePullFailed, 100);
                            EditorUtility.DisplayDialog(Localization.PullActionTitle,
                                e.Message,
                            Localization.Ok);
                        }
                    })
                    .Start();
            }
        }

        private void Push()
        {
            SetProgressMessage(Localization.MessagePushing, 0, 60f);
            Repository
                .Push()
                .FinallyInUI((success, e) =>
                {
                    if (success)
                    {
                        SetProgressMessage(Localization.MessagePushed, 100);
                        TaskManager.Run(EntryPoint.ApplicationManager.UsageTracker.IncrementHistoryViewToolbarPush, null);

                        EditorUtility.DisplayDialog(Localization.PushActionTitle,
                            String.Format(Localization.PushSuccessDescription, currentRemoteName),
                        Localization.Ok);
                    }
                    else
                    {
                        SetProgressMessage(Localization.MessagePushFailed, 100);
                        EditorUtility.DisplayDialog(Localization.PushActionTitle,
                            e.Message,
                        Localization.Ok);
                    }
                })
                .Start();
        }

        private void Fetch()
        {
            SetProgressMessage(Localization.MessageFetching, 0, 60f);
            Repository
                .Fetch()
                .FinallyInUI((success, e) =>
                {
                    if (success)
                    {
                        SetProgressMessage(Localization.MessageFetched, 100);
                        TaskManager.Run(EntryPoint.ApplicationManager.UsageTracker.IncrementHistoryViewToolbarFetch, null);
                    }
                    else
                    {
                        SetProgressMessage(Localization.MessageFetchFailed, 100);
                        EditorUtility.DisplayDialog(Localization.FetchActionTitle, e.Message, Localization.Ok);
                    }
                })
                .Start();
        }

        private void UpdateActiveTab()
        {
            if (changeTab != activeTab)
            {
                var fromView = ActiveView;
                activeTab = changeTab;
                var toView = ActiveView;
                SwitchView(fromView, toView);
            }
        }

        private void SwitchView(Subview fromView, Subview toView)
        {
            GUI.FocusControl(null);

            if (fromView != null)
                fromView.OnDisable();

            toView.OnEnable();
            toView.OnDataUpdate();

            // this triggers a repaint
            Repaint();
        }

        private void DoAccountDropdown()
        {
            GenericMenu accountMenu = new GenericMenu();

            if (!Platform.Keychain.HasKeys)
            {
                accountMenu.AddItem(new GUIContent("Sign in"), false, SignIn, "sign in");
            }
            else
            {
                accountMenu.AddItem(new GUIContent("Go to Profile"), false, GoToProfile, "profile");
                accountMenu.AddSeparator("");
                accountMenu.AddItem(new GUIContent("Sign out"), false, SignOut, "sign out");
            }
            accountMenu.ShowAsContext();
        }

        private void SignIn(object obj)
        {
            PopupWindow.OpenWindow(PopupViewType.AuthenticationView, null, null);
        }

        private void GoToProfile(object obj)
        {
            //TODO: ONE_USER_LOGIN This assumes only ever one user can login
            var keychainConnection = Platform.Keychain.Connections.First();
            var uriString = new UriString(keychainConnection.Host).Combine(keychainConnection.Username);
            Application.OpenURL(uriString);
        }

        private void SignOut(object obj)
        {
            UriString host;
            if (Repository != null && Repository.CloneUrl != null && Repository.CloneUrl.IsValidUri)
            {
                host = new UriString(Repository.CloneUrl.ToRepositoryUri()
                                               .GetComponents(UriComponents.SchemeAndServer, UriFormat.SafeUnescaped));
            }
            else
            {
                host = UriString.ToUriString(HostAddress.GitHubDotComHostAddress.WebUri);
            }

            var apiClient = new ApiClient(host, Platform.Keychain, null, null, NPath.Default, NPath.Default);
            apiClient.Logout(host);
        }

        public new void ShowNotification(GUIContent content)
        {
            ShowNotification(content, DefaultNotificationTimeout);
        }

        public void ShowNotification(GUIContent content, float timeout)
        {
            notificationClearTime = timeout < DefaultNotificationTimeout ? EditorApplication.timeSinceStartup + timeout : -1f;
            base.ShowNotification(content);
        }

        private void SetProgressMessage(string message, long value)
        {
            SetProgressMessage(message, value, DefaultNotificationTimeout);
        }

        private void SetProgressMessage(string message, long value, float timeout)
        {
            progressMessageClearTime = EditorApplication.timeSinceStartup + timeout;
            if (repositoryProgress == null)
                repositoryProgress = new Progress(TaskBase.Default);
            repositoryProgress.UpdateProgress(value, repositoryProgress.Total, message);
            UpdateProgress(repositoryProgress);
            Redraw();
        }

        private void ClearProgressMessage()
        {
            progressMessageClearTime = -1f;
            UpdateProgress(null);
            Redraw();
        }

        private static SubTab TabButton(SubTab tab, string title, SubTab currentTab)
        {
            return GUILayout.Toggle(currentTab == tab, title, EditorStyles.toolbarButton) ? tab : currentTab;
        }

        private Subview ToView(SubTab tab)
        {
            switch (tab)
            {
                case SubTab.InitProject:
                    return initProjectView;
                case SubTab.History:
                    return historyView;
                case SubTab.Changes:
                    return changesView;
                case SubTab.Branches:
                    return branchesView;
                case SubTab.Settings:
                    return settingsView;
                case SubTab.Locks:
                    return locksView;
                default:
                    throw new ArgumentOutOfRangeException("tab");
            }
        }

        public HistoryView HistoryView
        {
            get { return historyView; }
        }

        public ChangesView ChangesView
        {
            get { return changesView; }
        }

        public BranchesView BranchesView
        {
            get { return branchesView; }
        }

        public SettingsView SettingsView
        {
            get { return settingsView; }
        }

        public LocksView LocksView
        {
            get { return locksView; }
        }

        public InitProjectView InitProjectView
        {
            get { return initProjectView; }
        }

        private Subview ActiveView
        {
            get { return ToView(activeTab); }
        }

        public override bool IsBusy
        {
            get { return Manager.IsBusy; }
        }

        private enum SubTab
        {
            None,
            InitProject,
            History,
            Changes,
            Branches,
            Settings,
            Locks
        }
    }
}
