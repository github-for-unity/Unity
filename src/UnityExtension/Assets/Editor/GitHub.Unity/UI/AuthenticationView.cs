using System;
using System.Linq;
using System.Threading;
using UnityEngine;
using UnityEditor;

namespace GitHub.Unity
{
    [Serializable]
    class AuthenticationView : Subview
    {
        private static readonly Vector2 viewSize = new Vector2(290, 290);

        [SerializeField] private SubTab changeTab;
        [SerializeField] private SubTab activeTab;

        [SerializeField] private GitHubAuthenticationView gitHubAuthenticationView;
        [SerializeField] private GitHubEnterpriseAuthenticationView gitHubEnterpriseAuthenticationView;
        [SerializeField] private bool hasGitHubDotComConnection;
        [SerializeField] private bool hasGitHubEnterpriseConnection;
        [NonSerialized] private bool firstForThisView = true;

        public override void InitializeView(IView parent)
        {
            base.InitializeView(parent);

            gitHubAuthenticationView = gitHubAuthenticationView ?? new GitHubAuthenticationView();
            gitHubEnterpriseAuthenticationView = gitHubEnterpriseAuthenticationView ?? new GitHubEnterpriseAuthenticationView();

            try
            {
                OAuthCallbackManager.Start();
            }
            catch (Exception ex)
            {
                Logger.Trace(ex, "Error Starting OAuthCallbackManager");
            }

            gitHubAuthenticationView.InitializeView(this);
            gitHubEnterpriseAuthenticationView.InitializeView(this);

            hasGitHubDotComConnection = Platform.Keychain.Connections.Any(HostAddress.IsGitHubDotCom);
            hasGitHubEnterpriseConnection = Platform.Keychain.Connections.Any(connection => !HostAddress.IsGitHubDotCom(connection));
        }

        public void Initialize(Exception exception)
        {}

        public override void OnEnable()
        {
            base.OnEnable();
            activeTab = hasGitHubDotComConnection ? SubTab.GitHubEnterprise : SubTab.GitHub;
            ActiveView.OnEnable();
        }

        public override void OnDisable()
        {
            base.OnDisable();
            ActiveView.OnDisable();
        }

        public override void OnDataUpdate(bool first)
        {
            base.OnDataUpdate(first);
            ActiveView.OnDataUpdate(first || firstForThisView);

            Title = ActiveView.Title;
            Size = ActiveView.Size;
        }

        public override void Refresh()
        {
            ActiveView.Refresh();
            base.Refresh();
        }

        public override void OnSelectionChange()
        {
            base.OnSelectionChange();
            ActiveView.OnSelectionChange();
        }

        public override void OnUI()
        {
            DoToolbarGUI();
            ActiveView.OnUI();
        }
        
        public override void Finish(bool result)
        {
            OAuthCallbackManager.Stop();
            base.Finish(result);
        }

        private static SubTab TabButton(SubTab tab, string title, SubTab currentTab)
        {
            return GUILayout.Toggle(currentTab == tab, title, EditorStyles.toolbarButton) ? tab : currentTab;
        }

        private enum SubTab
        {
            GitHub,
            GitHubEnterprise
        }

        private void DoToolbarGUI()
        {
            GUILayout.BeginHorizontal(EditorStyles.toolbar);
            {
                EditorGUI.BeginChangeCheck();
                {
                    EditorGUI.BeginDisabledGroup(hasGitHubDotComConnection || IsBusy);
                    {
                        changeTab = TabButton(SubTab.GitHub, "GitHub", changeTab);
                    }
                    EditorGUI.EndDisabledGroup();

                    EditorGUI.BeginDisabledGroup(hasGitHubEnterpriseConnection || IsBusy);
                    {
                        changeTab = TabButton(SubTab.GitHubEnterprise, "GitHub Enterprise", changeTab);
                    }
                    EditorGUI.EndDisabledGroup();
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
        public override bool IsBusy { get; set; }
    }
}
