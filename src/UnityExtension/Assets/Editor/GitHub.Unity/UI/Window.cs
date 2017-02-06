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
        private const string AuthenticationTitle = "Auth";

        [NonSerialized] private double notificationClearTime = -1;
        [NonSerialized] private bool enabled = false;

        [SerializeField] private SubTab activeTab = SubTab.History;
        [SerializeField] private BranchesView branchesTab;
        [SerializeField] private ChangesView changesTab;
        [SerializeField] private HistoryView historyTab;
        [SerializeField] private SettingsView settingsTab;
        [SerializeField] private AuthenticationView authTab;

        private static bool initialized;

        public static void Initialize()
        {
            RefreshRunner.Initialize();
            initialized = true;
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

            if (!initialized)
            {
                return;
            }

            //if (!ValidateSettings())
            //{
            //    activeTab = SubTab.Settings; // If we do complete init, make sure that we return to the settings tab for further setup
            //}

            //activeTab = SubTab.Authentication;

            // Subtabs & toolbar
            GUILayout.BeginHorizontal(EditorStyles.toolbar);
            {
                EditorGUI.BeginChangeCheck();
                {
                    activeTab = TabButton(SubTab.History, HistoryTitle, activeTab);
                    activeTab = TabButton(SubTab.Changes, ChangesTitle, activeTab);
                    activeTab = TabButton(SubTab.Branches, BranchesTitle, activeTab);
                    activeTab = TabButton(SubTab.Settings, SettingsTitle, activeTab);
                    activeTab = TabButton(SubTab.Authentication, AuthenticationTitle, activeTab);
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

        public void OnEnable()
        {
            enabled = true;

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
            if (enabled)
            {
                enabled = false;
                if (historyTab == null)
                    historyTab = new HistoryView();
                historyTab.Show(this);
                if (changesTab == null)
                    changesTab = new ChangesView();
                changesTab.Show(this);
                if (branchesTab == null)
                    branchesTab = new BranchesView();
                branchesTab.Show(this);
                if (settingsTab == null)
                    settingsTab = new SettingsView();
                settingsTab.Show(this);
                if (authTab == null)
                    authTab = new AuthenticationView();
                authTab.Show(this);
            }

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

        private static SubTab TabButton(SubTab tab, string title, SubTab activeTab)
        {
            return GUILayout.Toggle(activeTab == tab, title, EditorStyles.toolbarButton) ? tab : activeTab;
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
                    case SubTab.Authentication:
                        return authTab;
                    case SubTab.Settings:
                    default:
                        return settingsTab;
                        //throw new ArgumentException(String.Format(UnknownSubTabError, activeTab));
                }
            }
        }

        public Rect Position { get { return position; } }

        private class RefreshRunner : AssetPostprocessor
        {
            public static void Initialize()
            {
                //Tasks.ScheduleMainThread(Refresh);
            }

            private static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moveDestination, string[] moveSource)
            {
                //Refresh();
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
            Settings,
            Authentication
        }
    }
}
