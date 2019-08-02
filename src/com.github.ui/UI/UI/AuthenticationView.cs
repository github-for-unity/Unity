using System;
using System.Linq;
using System.Threading;
using UnityEngine;
using UnityEditor;
using Unity.VersionControl.Git;

namespace GitHub.Unity
{
    [Serializable]
    class AuthenticationView : Subview
    {
        private static readonly Vector2 viewSize = new Vector2(290, 290);

        private const string WindowTitle = "Authenticate";

        [SerializeField] private SubTab changeTab = SubTab.GitHub;
        [SerializeField] private SubTab activeTab = SubTab.GitHub;

        [SerializeField] private GitHubAuthenticationView gitHubAuthenticationView;
        [SerializeField] private GitHubEnterpriseAuthenticationView gitHubEnterpriseAuthenticationView;
        [SerializeField] private bool hasGitHubDotComConnection;
        [SerializeField] private bool hasGitHubEnterpriseConnection;

        public override void InitializeView(IView parent)
        {
            base.InitializeView(parent);
            Title = WindowTitle;
            Size = viewSize;

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

            if (hasGitHubDotComConnection)
            {
                changeTab = SubTab.GitHubEnterprise;
                UpdateActiveTab();
            }
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
            get { return (gitHubAuthenticationView != null && gitHubAuthenticationView.IsBusy) || (gitHubEnterpriseAuthenticationView != null && gitHubEnterpriseAuthenticationView.IsBusy); }
        }

        public override void OnDataUpdate()
        {
            base.OnDataUpdate();
            MaybeUpdateData();
        }

        public override void Finish(bool result)
        {
            OAuthCallbackManager.Stop();
            base.Finish(result);
        }

        private void MaybeUpdateData()
        {
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
    }
}
