#pragma warning disable 649

using GitHub.Unity;
using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Debug = System.Diagnostics.Debug;

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
        [NonSerialized] private IRepository repository;

        [SerializeField] private SubTab activeTab = SubTab.History;
        [SerializeField] private BranchesView branchesTab = new BranchesView();
        [SerializeField] private ChangesView changesTab = new ChangesView();
        [SerializeField] private HistoryView historyTab = new HistoryView();
        [SerializeField] private SettingsView settingsTab = new SettingsView();

        private static bool initialized;

        [MenuItem(LaunchMenu)]
        public static void Launch()
        {
            var type = typeof(EditorWindow).Assembly.GetType("UnityEditor.InspectorWindow");
            GetWindow<Window>(type).Show();
        }

        [MenuItem("Window/GitHub Command Line")]
        public static void LaunchCommandLine()
        {
            EntryPoint.ProcessManager.RunCommandLineWindow(NPath.CurrentDirectory);
        }


        public static void Initialize()
        {
            initialized = true;
            //RefreshRunner.Initialize();
            foreach (Window window in Resources.FindObjectsOfTypeAll(typeof(Window)))
            {
                window.CreateViews();
                window.ShowActiveView();
            }
        }


        public override void OnEnable()
        {
            base.OnEnable();
            Selection.activeObject = this;

            // Set window title
            titleContent = new GUIContent(Title, Styles.SmallLogo);

            Utility.UnregisterReadyCallback(CreateViews);
            Utility.RegisterReadyCallback(CreateViews);

            Utility.UnregisterReadyCallback(ShowActiveView);
            Utility.RegisterReadyCallback(ShowActiveView);
        }

        public override void Refresh()
        {
            if (ActiveTab != null)
                ActiveTab.Refresh();
        }

        private void CreateViews()
        {
            repository = EntryPoint.Environment.Repository;
            historyTab.Initialize(this);
            changesTab.Initialize(this);
            branchesTab.Initialize(this);
            settingsTab.Initialize(this);
        }

        private void ShowActiveView()
        {
            if (repository == null)
                return;

            if (ActiveTab != null)
                ActiveTab.OnShow();
            Refresh();
        }

        public override void OnDisable()
        {
            base.OnDisable();

            if (repository == null)
                return;

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
            else if (repository == null)
            {
                DoOfferToInitializeRepositoryGUI();
                return;
            }
            //if (!ValidateSettings())
            //{
            //    activeTab = SubTab.Settings; // If we do complete init, make sure that we return to the settings tab for further setup
            //}


            DoHeaderGUI();

            // GUI for the active tab
            if (ActiveTab != null)
                ActiveTab.OnGUI();
        }

        private void DoOfferToInitializeRepositoryGUI()
        {
            GUILayout.BeginHorizontal(Styles.HeaderBoxStyle);
            {
                GUILayout.Space(3);
                GUILayout.BeginVertical(GUILayout.Width(16));
                {
                    GUILayout.Space(9);
                    GUILayout.Label(Styles.BigLogo, GUILayout.Height(20), GUILayout.Width(20));
                }
                GUILayout.EndVertical();

                GUILayout.BeginVertical();
                {
                    GUILayout.Space(9);
                    GUILayout.Label("GitHub", Styles.HeaderRepoLabelStyle);
                }
                GUILayout.EndVertical();
            }
            GUILayout.EndHorizontal();
            GUILayout.BeginVertical();
            {
                GUILayout.FlexibleSpace();
                GUILayout.BeginHorizontal();
                {
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button(Localization.InitializeRepositoryButtonText, "LargeButton"))
                    {
                        var repoInit = new RepositoryInitializer(EntryPoint.Environment, EntryPoint.ProcessManager, new TaskQueueScheduler(), EntryPoint.AppManager);
                        repoInit.Run();

                        //var task = new GitInitTask(EntryPoint.Environment, EntryPoint.ProcessManager,
                        //    new TaskResultDispatcher<string>(_ =>
                        //    {
                        //        EntryPoint.AppManager.RestartRepository();
                        //    })

                        //    //new MainThreadTaskResultDispatcher<string>(_ =>
                        //    //{
                        //    //    EditorUtility.DisplayDialog("Init!", "Init success!", Localization.Ok);
                        //    //})
                        //    );
                        //Tasks.Add(task);
                    }
                    GUILayout.FlexibleSpace();
                }
                GUILayout.EndHorizontal();
                GUILayout.FlexibleSpace();
            }
            GUILayout.EndVertical();

            //GUILayout.BeginVertical();
            //{
            //    var padding = 10;
            //    GUILayout.Label(Styles.BigLogo, GUILayout.Height(this.Position.width - padding * 2), GUILayout.Width(this.Position.width - padding * 2));
            //}
            //GUILayout.EndVertical();
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
                    GUILayout.Label(String.Format("{0}/{1}", repository.Owner, repository.Name), Styles.HeaderRepoLabelStyle);
                    GUILayout.Space(-2);
                    GUILayout.Label(repository.CurrentBranch, Styles.HeaderBranchLabelStyle);
                }
                GUILayout.EndVertical();
            }
            GUILayout.EndHorizontal();

            // Subtabs & toolbar
            GUILayout.BeginHorizontal(EditorStyles.toolbar);
            {
                EditorGUI.BeginChangeCheck();
                {
                    activeTab = TabButton(SubTab.Changes, ChangesTitle, activeTab);
                    activeTab = TabButton(SubTab.History, HistoryTitle, activeTab);
                    activeTab = TabButton(SubTab.Branches, BranchesTitle, activeTab);
                    activeTab = TabButton(SubTab.Settings, SettingsTitle, activeTab);
                }
                if (EditorGUI.EndChangeCheck())
                {
                    Refresh();
                }

                GUILayout.FlexibleSpace();
            }
            GUILayout.EndHorizontal();
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
                switch (activeTab)
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
                        //throw new ArgumentException(String.Format(UnknownSubTabError, activeTab));
                }
            }
        }

        //private class RefreshRunner : AssetPostprocessor
        //{
        //    public static void Initialize()
        //    {
        //        //Tasks.ScheduleMainThread(Refresh);
        //    }

        //    private static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moveDestination, string[] moveSource)
        //    {
        //        //Refresh();
        //    }

        //    private static void Refresh()
        //    {
        //        Utility.UnregisterReadyCallback(OnReady);
        //        Utility.RegisterReadyCallback(OnReady);
        //    }

        //    private static void OnReady()
        //    {
        //        foreach (Window window in Resources.FindObjectsOfTypeAll(typeof(Window)))
        //        {
        //            window.Refresh();
        //        }
        //    }
        //}

        private enum SubTab
        {
            History,
            Changes,
            Branches,
            Settings
        }
    }
}
