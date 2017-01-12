using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Debug = System.Diagnostics.Debug;

namespace GitHub.Unity
{
    [Serializable]
    class Window : EditorWindow, IView
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

        [NonSerialized] private double notificationClearTime = -1;

        [SerializeField] private SubTab activeTab = SubTab.History;
        [SerializeField] private BranchesView branchesTab = new BranchesView();
        [SerializeField] private ChangesView changesTab = new ChangesView();
        [SerializeField] private HistoryView historyTab = new HistoryView();
        [SerializeField] private SettingsView settingsTab = new SettingsView();

        public static void Initialize()
        {
            RefreshRunner.Initialize();
        }

        [MenuItem(LaunchMenu)]
        public static void Launch()
        {
            GetWindow<Window>().Show();
        }

        public void OnGUI()
        {
            // Set window title
            titleContent = new GUIContent(Title, Styles.TitleIcon);

            var settingsIssues = Utility.Issues.Select(i => i as ProjectSettingsIssue).FirstOrDefault(i => i != null);

            // Initial state
            if (!Utility.ActiveRepository || !Utility.GitFound ||
                (settingsIssues != null &&
                    (settingsIssues.WasCaught(ProjectSettingsEvaluation.EditorSettingsMissing) ||
                        settingsIssues.WasCaught(ProjectSettingsEvaluation.BadVCSSettings))))
            {
                activeTab = SubTab.Settings; // If we do complete init, make sure that we return to the settings tab for further setup
                settingsTab.OnGUI();
                return;
            }

            // Subtabs & toolbar
            GUILayout.BeginHorizontal(EditorStyles.toolbar);
            {
                EditorGUI.BeginChangeCheck();
                {
                    TabButton(ref activeTab, SubTab.History, HistoryTitle);
                    TabButton(ref activeTab, SubTab.Changes, ChangesTitle);
                    TabButton(ref activeTab, SubTab.Branches, BranchesTitle);
                    TabButton(ref activeTab, SubTab.Settings, SettingsTitle);
                }
                if (EditorGUI.EndChangeCheck())
                {
                    Refresh();
                }

                GUILayout.FlexibleSpace();
            }
            GUILayout.EndHorizontal();

            // GUI for the active tab
            ActiveTab.OnGUI();
        }

        public void OnEnable()
        {
            historyTab.Show(this);
            changesTab.Show(this);
            branchesTab.Show(this);
            settingsTab.Show(this);

            Utility.UnregisterReadyCallback(Refresh);
            Utility.RegisterReadyCallback(Refresh);
        }

        public void Update()
        {
            // Notification auto-clear timer override
            if (notificationClearTime > 0f && EditorApplication.timeSinceStartup > notificationClearTime)
            {
                notificationClearTime = -1f;
                RemoveNotification();
                Redraw();
            }
        }

        public void Refresh()
        {
            EvaluateProjectConfigurationTask.Schedule();

            if (Utility.ActiveRepository)
            {
                ActiveTab.Refresh();
            }
        }

        public void Redraw()
        {
            Repaint();
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

        private static void TabButton(ref SubTab activeTab, SubTab tab, string title)
        {
            activeTab = GUILayout.Toggle(activeTab == tab, title, EditorStyles.toolbarButton) ? tab : activeTab;
        }

        private void OnSelectionChange()
        {
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

        public Rect Position => position;

        private class RefreshRunner : AssetPostprocessor
        {
            public static void Initialize()
            {
                Tasks.ScheduleMainThread(Refresh);
            }

            private static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moveDestination, string[] moveSource)
            {
                Refresh();
            }

            private static void Refresh()
            {
                Utility.UnregisterReadyCallback(OnReady);
                Utility.RegisterReadyCallback(OnReady);
            }

            private static void OnReady()
            {
                foreach (Window window in FindObjectsOfTypeAll(typeof(Window)))
                {
                    window.Refresh();
                }
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
