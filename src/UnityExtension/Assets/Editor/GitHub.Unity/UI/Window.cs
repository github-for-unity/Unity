using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace GitHub.Unity
{
    [Serializable]
    class Window : BaseWindow
    {
        private const float DefaultNotificationTimeout = 4f;
        private const string Title = "GitHub";
        private const string LaunchMenu = "Window/GitHub";
        private const string BadNotificationDelayError = "A delay of {0} is shorter than the default delay and thus would get pre-empted.";
        private const string InitializeTitle = "Initialize";
        private const string HistoryTitle = "History";
        private const string ChangesTitle = "Changes";
        private const string BranchesTitle = "Branches";
        private const string SettingsTitle = "Settings";
        private const string DefaultRepoUrl = "No remote configured";
        private const string Window_RepoUrlTooltip = "Url of the {0} remote";
        private const string Window_RepoNoUrlTooltip = "Add a remote in the Settings tab";
        private const string Window_RepoBranchTooltip = "Active branch";

        [NonSerialized] private double notificationClearTime = -1;
        [SerializeField] private SubTab changeTab = SubTab.History;
        [SerializeField] private SubTab activeTab = SubTab.History;
        [SerializeField] private InitProjectView initProjectView = new InitProjectView();
        [SerializeField] private BranchesView branchesView = new BranchesView();
        [SerializeField] private ChangesView changesView = new ChangesView();
        [SerializeField] private HistoryView historyView = new HistoryView();
        [SerializeField] private SettingsView settingsView = new SettingsView();

        [SerializeField] private string repoRemote;
        [SerializeField] private string repoBranch;
        [SerializeField] private string repoUrl;
        [SerializeField] private GUIContent repoBranchContent;
        [SerializeField] private GUIContent repoUrlContent;

        [SerializeField] private CacheUpdateEvent lastCurrentBranchAndRemoteChangedEvent;
        [NonSerialized] private bool currentBranchAndRemoteHasUpdate;

        [MenuItem(LaunchMenu)]
        public static void Window_GitHub()
        {
            ShowWindow(EntryPoint.ApplicationManager);
        }

        [MenuItem("GitHub/Show Window")]
        public static void GitHub_ShowWindow()
        {
            ShowWindow(EntryPoint.ApplicationManager);
        }

        [MenuItem("GitHub/Command Line")]
        public static void GitHub_CommandLine()
        {
            EntryPoint.ApplicationManager.ProcessManager.RunCommandLineWindow(NPath.CurrentDirectory);
        }

#if DEBUG 
        [MenuItem("GitHub/Select Window")] 
        public static void GitHub_SelectWindow() 
        { 
            var window = Resources.FindObjectsOfTypeAll(typeof(Window)).FirstOrDefault() as Window; 
            Selection.activeObject = window; 
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

            if (!HasRepository && activeTab != SubTab.InitProject && activeTab != SubTab.Settings)
                changeTab = activeTab = SubTab.InitProject;

            HistoryView.InitializeView(this);
            ChangesView.InitializeView(this);
            BranchesView.InitializeView(this);
            SettingsView.InitializeView(this);
            InitProjectView.InitializeView(this);

            titleContent = new GUIContent(Title, Styles.SmallLogo);
        }

        public override void OnEnable()
        {
            base.OnEnable();

#if DEVELOPER_BUILD
            Selection.activeObject = this;
#endif
            if (Repository != null)
                Repository.CheckCurrentBranchAndRemoteChangedEvent(lastCurrentBranchAndRemoteChangedEvent);

            if (ActiveView != null)
                ActiveView.OnEnable();
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

            if (Repository != null && activeTab == SubTab.InitProject)
            {
                changeTab = SubTab.History;
                UpdateActiveTab();
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
            base.Refresh();
            if (ActiveView != null)
                ActiveView.Refresh();
            Repaint();
        }

        public override void OnUI()
        {
            base.OnUI();

            if (HasRepository)
            {
                DoHeaderGUI();
            }

            DoToolbarGUI();

            // GUI for the active tab
            if (ActiveView != null)
            {
                ActiveView.OnGUI();
            }
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
        }

        private void MaybeUpdateData()
        {
            string updatedRepoRemote = null;
            string updatedRepoUrl = DefaultRepoUrl;

            var shouldUpdateContentFields = false;

            if (Repository != null)
            {
                if (repoBranch == null || repoRemote == null || currentBranchAndRemoteHasUpdate)
                {
                    var repositoryCurrentBranch = Repository.CurrentBranch;
                    var updatedRepoBranch = repositoryCurrentBranch.HasValue ? repositoryCurrentBranch.Value.Name : null;

                    var repositoryCurrentRemote = Repository.CurrentRemote;
                    if (repositoryCurrentRemote.HasValue)
                    {
                        updatedRepoRemote = repositoryCurrentRemote.Value.Name;
                        if (!string.IsNullOrEmpty(repositoryCurrentRemote.Value.Url))
                        {
                            updatedRepoUrl = repositoryCurrentRemote.Value.Url;
                        }
                    }

                    if (repoRemote != updatedRepoRemote)
                    {
                        repoRemote = updatedRepoBranch;
                        shouldUpdateContentFields = true;
                    }

                    if (repoBranch != updatedRepoBranch)
                    {
                        repoBranch = updatedRepoBranch;
                        shouldUpdateContentFields = true;
                    }

                    if (repoUrl != updatedRepoUrl)
                    {
                        repoUrl = updatedRepoUrl;
                        shouldUpdateContentFields = true;
                    }
                }
            }
            else
            {
                if (repoRemote != null)
                {
                    repoRemote = null;
                    shouldUpdateContentFields = true;
                }

                if (repoBranch != null)
                {
                    repoBranch = null;
                    shouldUpdateContentFields = true;
                }

                if (repoUrl != DefaultRepoUrl)
                {
                    repoUrl = DefaultRepoUrl;
                    shouldUpdateContentFields = true;
                }
            }

            if (shouldUpdateContentFields || repoBranchContent == null || repoUrlContent == null)
            {
                repoBranchContent = new GUIContent(repoBranch, Window_RepoBranchTooltip);

                if (repoRemote != null)
                {
                    repoUrlContent = new GUIContent(repoUrl, string.Format(Window_RepoUrlTooltip, repoRemote));
                }
                else
                {
                    repoUrlContent = new GUIContent(repoUrl, Window_RepoNoUrlTooltip);
                }
            }
        }

        private void AttachHandlers(IRepository repository)
        {
            if (repository == null)
                return;
            repository.CurrentBranchAndRemoteChanged += RepositoryOnCurrentBranchAndRemoteChanged;
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

        private void DetachHandlers(IRepository repository)
        {
            if (repository == null)
                return;
            repository.CurrentBranchAndRemoteChanged -= RepositoryOnCurrentBranchAndRemoteChanged;
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

                    GUILayout.Label(repoUrlContent, Styles.HeaderRepoLabelStyle);
                    GUILayout.Space(-2);
                    GUILayout.Label(repoBranchContent, Styles.HeaderBranchLabelStyle);
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
                        changeTab = TabButton(SubTab.Changes, ChangesTitle, changeTab);
                        changeTab = TabButton(SubTab.History, HistoryTitle, changeTab);
                        changeTab = TabButton(SubTab.Branches, BranchesTitle, changeTab);
                    }
                    else
                    {
                        changeTab = TabButton(SubTab.InitProject, InitializeTitle, changeTab);
                    }
                    changeTab = TabButton(SubTab.Settings, SettingsTitle, changeTab);
                }

                if (EditorGUI.EndChangeCheck())
                {
                    UpdateActiveTab();
                }

                GUILayout.FlexibleSpace();

                if (GUILayout.Button("Account", EditorStyles.toolbarDropDown))
                    DoAccountDropdown();
            }
            EditorGUILayout.EndHorizontal();
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
            PopupWindow.OpenWindow(PopupWindow.PopupViewType.AuthenticationView);
        }

        private void GoToProfile(object obj)
        {
            Application.OpenURL(Platform.CredentialManager.CachedCredentials.Host.Combine(Platform.CredentialManager.CachedCredentials.Username));
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

            var apiClient = ApiClient.Create(host, Platform.Keychain);
            apiClient.Logout(host);
        }

        public new void ShowNotification(GUIContent content)
        {
            ShowNotification(content, DefaultNotificationTimeout);
        }

        public void ShowNotification(GUIContent content, float timeout)
        {
            Debug.Assert(timeout <= DefaultNotificationTimeout, String.Format(BadNotificationDelayError, timeout));

            notificationClearTime = timeout < DefaultNotificationTimeout ? EditorApplication.timeSinceStartup + timeout : -1f;
            base.ShowNotification(content);
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
            get { return false; }
        }

        private enum SubTab
        {
            None,
            InitProject,
            History,
            Changes,
            Branches,
            Settings
        }
    }
}
