using System;
using System.Threading;
using UnityEngine;
using UnityEditor;

namespace GitHub.Unity
{
    [Serializable]
    class AuthenticationView : Subview
    {
        private static readonly Vector2 viewSize = new Vector2(290, 290);

        private const string WindowTitle = "Authenticate";

        [NonSerialized] private bool isBusy;

        [SerializeField] private SubTab changeTab = SubTab.GitHub;
        [SerializeField] private SubTab activeTab = SubTab.GitHub;

        [SerializeField] private GitHubAuthenticationView gitHubAuthenticationView;
        [SerializeField] private GitHubEnterpriseAuthenticationView gitHubEnterpriseAuthenticationView;

        public override void InitializeView(IView parent)
        {
            base.InitializeView(parent);
            Title = WindowTitle;
            Size = viewSize;

            gitHubAuthenticationView = gitHubAuthenticationView ?? new GitHubAuthenticationView();
            gitHubEnterpriseAuthenticationView = gitHubEnterpriseAuthenticationView ?? new GitHubEnterpriseAuthenticationView();

            gitHubAuthenticationView.InitializeView(parent);
            gitHubEnterpriseAuthenticationView.InitializeView(parent);
        }

        public void Initialize(Exception exception)
        {
            
        }

        public override void OnGUI()
        {
            DoToolbarGUI();
            ActiveView.OnGUI();
        }
        
        public override bool IsBusy
        {
            get { return isBusy; }
        }

        private static SubTab TabButton(SubTab tab, string title, SubTab currentTab)
        {
            return GUILayout.Toggle(currentTab == tab, title, EditorStyles.toolbarButton) ? tab : currentTab;
        }

        private enum SubTab
        {
            None,
            GitHub,
            GitHubEnterprise
        }

        private void DoToolbarGUI()
        {
            GUILayout.BeginHorizontal(EditorStyles.toolbar);
            {
                EditorGUI.BeginChangeCheck();
                {
                    changeTab = TabButton(SubTab.GitHub, "GitHub", changeTab);
                    changeTab = TabButton(SubTab.GitHubEnterprise, "GitHub Enterprise", changeTab);
                }

                if (EditorGUI.EndChangeCheck())
                {
                    UpdateActiveTab();
                }

                GUILayout.FlexibleSpace();
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

            // this triggers a repaint
            Parent.Redraw();
        }

        private Subview ActiveView
        {
            get
            {
                switch (activeTab)
                {
                    case SubTab.GitHub:
                        return gitHubAuthenticationView;
                    case SubTab.GitHubEnterprise:
                        return gitHubEnterpriseAuthenticationView;
                    default:
                        throw new NotImplementedException();
                }
            }
        }
    }
}
