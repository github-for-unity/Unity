using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using Object = UnityEngine.Object;
using System.Linq;


namespace GitHub.Unity
{
    [System.Serializable]
    class Window : EditorWindow, IView
    {
        public static void Initialize()
        {
            RefreshRunner.Initialize();
        }

        class RefreshRunner : AssetPostprocessor
        {
            public static void Initialize()
            {
                Tasks.ScheduleMainThread(Refresh);
            }


            static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moveDestination, string[] moveSource)
            {
                Refresh();
            }


            static void Refresh()
            {
                Utility.UnregisterReadyCallback(OnReady);
                Utility.RegisterReadyCallback(OnReady);
            }


            static void OnReady()
            {
                foreach (Window window in Object.FindObjectsOfTypeAll(typeof(Window)))
                {
                    window.Refresh();
                }
            }
        }


        enum SubTab
        {
            History,
            Changes,
            Branches,
            Settings
        }


        const float DefaultNotificationTimeout = 4f;
        const string
            Title = "GitHub",
            LaunchMenu = "Window/GitHub",
            RefreshButton = "Refresh",
            UnknownSubTabError = "Unsupported view mode: {0}",
            BadNotificationDelayError = "A delay of {0} is shorter than the default delay and thus would get pre-empted.",
            HistoryTitle = "History",
            ChangesTitle = "Changes",
            BranchesTitle = "Branches",
            SettingsTitle = "Settings";


        [MenuItem(LaunchMenu)]
        static void Launch()
        {
            GetWindow<Window>().Show();
        }


        [SerializeField] SubTab activeTab = SubTab.History;
        [SerializeField] HistoryView historyTab = new HistoryView();
        [SerializeField] ChangesView changesTab = new ChangesView();
        [SerializeField] BranchesView branchesTab = new BranchesView();
        [SerializeField] SettingsView settingsTab = new SettingsView();


        double notificationClearTime = -1;


        public HistoryView HistoryTab { get { return historyTab; } }
        public ChangesView ChangesTab { get { return changesTab; } }
        public BranchesView BranchesTab { get { return branchesTab; } }
        public SettingsView SettingsTab { get { return settingsTab; } }


        Subview ActiveTab
        {
            get
            {
                switch(activeTab)
                {
                    case SubTab.History:
                    return historyTab;
                    case SubTab.Changes:
                    return changesTab;
                    case SubTab.Branches:
                    return branchesTab;
                    case SubTab.Settings:
                    return settingsTab;
                    default:
                    throw new ArgumentException(string.Format(UnknownSubTabError, activeTab));
                }
            }
        }


        void OnEnable()
        {
            historyTab.Show(this);
            changesTab.Show(this);
            branchesTab.Show(this);
            settingsTab.Show(this);

            Utility.UnregisterReadyCallback(Refresh);
            Utility.RegisterReadyCallback(Refresh);
        }


        new public void ShowNotification(GUIContent content)
        {
            ShowNotification(content, DefaultNotificationTimeout);
        }


        public void ShowNotification(GUIContent content, float timeout)
        {
            System.Diagnostics.Debug.Assert(timeout <= DefaultNotificationTimeout, string.Format(BadNotificationDelayError, timeout));

            notificationClearTime = timeout < DefaultNotificationTimeout ? EditorApplication.timeSinceStartup + timeout : -1f;
            base.ShowNotification(content);
        }


        public void OnGUI()
        {
            // Set window title
            titleContent = new GUIContent(Title, Styles.TitleIcon);

            ProjectSettingsIssue settingsIssues = Utility.Issues.Select(i => i as ProjectSettingsIssue).FirstOrDefault(i => i != null);

            // Initial state
            if (
                !Utility.ActiveRepository ||
                !Utility.GitFound ||
                (settingsIssues != null && (
                    settingsIssues.WasCaught(ProjectSettingsEvaluation.EditorSettingsMissing) ||
                    settingsIssues.WasCaught(ProjectSettingsEvaluation.BadVCSSettings))
                )
            )
            {
                activeTab = SubTab.Settings; // If we do complete init, make sure that we return to the settings tab for further setup
                settingsTab.OnGUI();
                return;
            }

            // Subtabs & toolbar
            GUILayout.BeginHorizontal(EditorStyles.toolbar);
                EditorGUI.BeginChangeCheck();
                    TabButton(ref activeTab, SubTab.History, HistoryTitle);
                    TabButton(ref activeTab, SubTab.Changes, ChangesTitle);
                    TabButton(ref activeTab, SubTab.Branches, BranchesTitle);
                    TabButton(ref activeTab, SubTab.Settings, SettingsTitle);
                if (EditorGUI.EndChangeCheck())
                {
                    Refresh();
                }

                GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            // GUI for the active tab
            ActiveTab.OnGUI();
        }


        void Update()
        {
            // Notification auto-clear timer override
            if (notificationClearTime > 0f && EditorApplication.timeSinceStartup > notificationClearTime)
            {
                notificationClearTime = -1f;
                RemoveNotification();
                Repaint();
            }
        }


        static void TabButton(ref SubTab activeTab, SubTab tab, string title)
        {
            activeTab = GUILayout.Toggle(activeTab == tab, title, EditorStyles.toolbarButton) ? tab : activeTab;
        }


        public void Refresh()
        {
            EvaluateProjectConfigurationTask.Schedule();

            if (Utility.ActiveRepository)
            {
                ActiveTab.Refresh();
            }
        }


        void OnSelectionChange()
        {
            ActiveTab.OnSelectionChange();
        }
    }
}
