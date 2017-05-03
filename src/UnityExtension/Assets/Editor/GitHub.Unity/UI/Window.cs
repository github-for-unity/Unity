#pragma warning disable 649

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
        private const string RefreshButton = "Refresh";
        private const string UnknownSubTabError = "Unsupported view mode: {0}";
        private const string BadNotificationDelayError = "A delay of {0} is shorter than the default delay and thus would get pre-empted.";
        private const string HistoryTitle = "History";
        private const string ChangesTitle = "Changes";
        private const string BranchesTitle = "Branches";
        private const string SettingsTitle = "Settings";
        private const string AuthenticationTitle = "Auth";

        [NonSerialized] private double notificationClearTime = -1;

        [SerializeField] private SubTab activeTab = SubTab.History;
        [SerializeField] private BranchesView branchesTab = new BranchesView();
        [SerializeField] private ChangesView changesTab = new ChangesView();
        [SerializeField] private HistoryView historyTab = new HistoryView();
        [SerializeField] private SettingsView settingsTab = new SettingsView();

        private static bool initialized;

        [MenuItem(LaunchMenu)]
        public static void Window_GitHub()
        {
            ShowWindow();
        }

        [MenuItem("GitHub/Show Window")]
        public static void GitHub_ShowWindow()
        {
            ShowWindow();
        }

        [MenuItem("GitHub/Command Line")]
        public static void GitHub_CommandLine()
        {
            EntryPoint.ProcessManager.RunCommandLineWindow(NPath.CurrentDirectory);
        }

        public static void ShowWindow()
        {
            var type = typeof(EditorWindow).Assembly.GetType("UnityEditor.InspectorWindow");
            var window = GetWindow<Window>(type);
            window.Setup(EntryPoint.Environment.Repository);
            window.Show();
        }

        public static void Initialize(IRepository repository)
        {
            initialized = true;
            //RefreshRunner.Initialize();
            foreach (Window window in Resources.FindObjectsOfTypeAll(typeof(Window)))
            {
                window.Setup(repository);
                window.ShowActiveView();
            }
        }


        public override void OnEnable()
        {
            base.OnEnable();

#if DEVELOPER_BUILD
            Selection.activeObject = this;
#endif

            // Set window title
            titleContent = new GUIContent(Title, Styles.SmallLogo);
        }

        public override void Refresh()
        {
            if (ActiveTab != null)
                ActiveTab.Refresh();
        }

        private void Setup(IRepository repository)
        {
            Repository = repository;
            historyTab.Initialize(this);
            changesTab.Initialize(this);
            branchesTab.Initialize(this);
            settingsTab.Initialize(this);
        }

        private void ShowActiveView()
        {
            if (Repository == null)
                return;

            if (ActiveTab != null)
                ActiveTab.OnShow();
            Refresh();
        }

        private void SwitchView(Subview from, Subview to)
        {
            GUI.FocusControl(null);
            from.OnHide();
            to.OnShow();
            Refresh();
        }

        public override void OnDisable()
        {
            base.OnDisable();

            if (ActiveTab != null)
                ActiveTab.OnHide();
        }


        public override void OnGUI()
        {
            base.OnGUI();

            if (!initialized)
            {
                DoNotInitializedGUI();
                return;
            }

            if (Repository != null)
            {
                DoHeaderGUI();
            }

            DoToolbarGUI();

            // GUI for the active tab
            if (ActiveTab != null)
            {
                ActiveTab.OnGUI();
            }
        }

        private void DoNotInitializedGUI()
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
            }
            GUILayout.EndHorizontal();
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

                    var headerRepoLabelText = String.IsNullOrEmpty(Repository.Owner)
                        ? Repository.Name
                        : String.Format("{0}/{1}", Repository.Owner, Repository.Name);

                    GUILayout.Label(headerRepoLabelText, Styles.HeaderRepoLabelStyle);
                    GUILayout.Space(-2);
                    GUILayout.Label(Repository.CurrentBranch, Styles.HeaderBranchLabelStyle);
                }
                GUILayout.EndVertical();
            }
            GUILayout.EndHorizontal();
        }

        private void DoToolbarGUI()
        {
            // Subtabs & toolbar
            Rect mainNavRect = EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            {
                SubTab tab = activeTab;
                EditorGUI.BeginChangeCheck();
                {
                    if (Repository != null)
                    {
                        tab = TabButton(SubTab.Changes, ChangesTitle, tab);
                        tab = TabButton(SubTab.History, HistoryTitle, tab);
                        tab = TabButton(SubTab.Branches, BranchesTitle, tab);
                    }
                    else
                    {
                        tab = TabButton(SubTab.History, HistoryTitle, tab);
                    }
                    tab = TabButton(SubTab.Settings, SettingsTitle, tab);
                }
                if (EditorGUI.EndChangeCheck())
                {
                    var from = ActiveTab;
                    activeTab = tab;
                    SwitchView(from, ActiveTab);
                }

                GUILayout.FlexibleSpace();

                if(GUILayout.Button("Account", EditorStyles.toolbarDropDown))
                  DoAccountDropdown();
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DoAccountDropdown()
        {
            GenericMenu accountMenu = new GenericMenu();

            if (!EntryPoint.Keychain.HasKeys)
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
            AuthenticationWindow.Open();
        }

        private void GoToProfile(object obj)
        {
            //Logger.Debug("{0} {1}", EntryPoint.CredentialManager.CachedCredentials.Host, EntryPoint.CredentialManager.CachedCredentials.Username);
            Application.OpenURL(EntryPoint.CredentialManager.CachedKeys.Host.Combine(EntryPoint.CredentialManager.CachedKeys.Username));
        }
        private void SignOut(object obj)
        {
            var task = new SimpleTask(() => EntryPoint.CredentialManager.Delete(EntryPoint.CredentialManager.CachedKeys.Host));
            TaskRunner.Add(task);
        }

        private bool ValidateSettings()
        {
            var settingsIssues = Utility.Issues.Select(i => i as ProjectSettingsIssue).FirstOrDefault(i => i != null);

            // Initial state
            if (!Utility.ActiveRepository || !Utility.GitFound ||
                (settingsIssues != null &&
                    (settingsIssues.WasCaught(ProjectSettingsEvaluation.EditorSettingsMissing) ||
                        settingsIssues.WasCaught(ProjectSettingsEvaluation.BadVCSSettings))))
            {
                return false;
            }

            return true;
        }

        public override void Update()
        {
            // Notification auto-clear timer override
            if (notificationClearTime > 0f && EditorApplication.timeSinceStartup > notificationClearTime)
            {
                notificationClearTime = -1f;
                RemoveNotification();
                Redraw();
            }
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

        private static SubTab TabButton(SubTab tab, string title, SubTab activeTab)
        {
            return GUILayout.Toggle(activeTab == tab, title, EditorStyles.toolbarButton) ? tab : activeTab;
        }

        public override void OnSelectionChange()
        {
            if (ActiveTab != null)
                ActiveTab.OnSelectionChange();
        }

        public HistoryView HistoryTab
        {
            get { return historyTab; }
        }

        public ChangesView ChangesTab
        {
            get { return changesTab; }
        }

        public BranchesView BranchesTab
        {
            get { return branchesTab; }
        }

        public SettingsView SettingsTab
        {
            get { return settingsTab; }
        }

        private Subview ActiveTab
        {
            get
            {
                return ToView(activeTab);
            }
        }

        private Subview ToView(SubTab tab)
        {
            switch (tab)
            {
                case SubTab.History:
                    return historyTab;
                case SubTab.Changes:
                    return changesTab;
                case SubTab.Branches:
                    return branchesTab;
                case SubTab.Settings:
                default:
                    return settingsTab;
            }
        }

        private enum SubTab
        {
            History,
            Changes,
            Branches,
            Settings
        }
    }
}
