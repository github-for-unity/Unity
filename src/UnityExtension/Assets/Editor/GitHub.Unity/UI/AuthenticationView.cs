using System;
using UnityEngine;

namespace GitHub.Unity
{
    [Serializable]
    class AuthenticationView : Subview
    {
        private static readonly ILogging logger = Logging.GetLogger<AuthenticationView>();

        const string serverLabel = "Server";
        const string usernameLabel = "Username";
        const string passwordLabel = "Password";
        const string twofaLabel = "Code";
        const string loginButton = "Login";
        const string authTitle = "You're currently not signed in";
        const string authDescription = "Log into GitHub to start collaborating together";

        [SerializeField] private Vector2 scroll;
        [SerializeField] private string username = "";
        [SerializeField] private string password = "";
        [SerializeField] private string two2fa = "";

        [NonSerialized] private bool need2fa;
        [NonSerialized] private bool busy;
        [NonSerialized] private bool finished;
        [NonSerialized] private string message;
        [NonSerialized] private AuthenticationService authenticationService;

        protected override void OnShow()
        {
            base.OnShow();
            logger.Debug("OnEnable");
            need2fa = busy = finished = false;
            authenticationService = new AuthenticationService(new Program(), EntryPoint.Platform.CredentialManager);
        }

        public override void OnGUI()
        {
            scroll = GUILayout.BeginScrollView(scroll);
            {
                GUILayout.BeginVertical();
                {
                    if (!need2fa)
                    {
                        OnGUILogin();
                    }
                    else
                    {
                        OnGUI2FA();
                    }
                }
                if (finished)
                {
                    GUILayout.Label("Finished");
                }
                if (message != null)
                {
                    GUILayout.Label(message);
                }
                GUILayout.EndVertical();
            }
            GUILayout.EndScrollView();
        }

        private void OnGUILogin()
        {
            GUILayout.BeginHorizontal(Styles.HeaderBoxStyle);
            {
                GUILayout.Space(3);
                GUILayout.BeginVertical(GUILayout.Width(16));
                {
                    GUILayout.Space(9);
                    GUILayout.Label(Styles.TitleIcon, GUILayout.Height(20), GUILayout.Width(20));
                }
                GUILayout.EndVertical();

                GUILayout.BeginVertical();
                {
                    GUILayout.Space(3);
                    GUILayout.Label(authTitle, Styles.HeaderRepoLabelStyle);
                    GUILayout.Space(-2);
                    GUILayout.Label(authDescription, Styles.HeaderBranchLabelStyle);
                }
                GUILayout.EndVertical();
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            {
                GUILayout.Label(usernameLabel);
                if (busy) GUI.enabled = false;
                username = GUILayout.TextField(username);
                GUI.enabled = true;
            }
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            {
                GUILayout.Label(passwordLabel);
                if (busy) GUI.enabled = false;
                password = GUILayout.PasswordField(password, '*');
                GUI.enabled = true;
            }
            GUILayout.EndHorizontal();
            if (busy) GUI.enabled = false;
            if (GUILayout.Button(loginButton))
            {
                busy = true;
                authenticationService.Login(username, password, DoRequire2fa, DoResult);
            }
            GUI.enabled = true;
        }

        private void OnGUI2FA()
        {
            GUILayout.BeginHorizontal();
            {
                GUILayout.Label(twofaLabel);
                if (busy) GUI.enabled = false;
                two2fa = GUILayout.TextField(two2fa);
                GUI.enabled = true;
            }
            GUILayout.EndHorizontal();
            if (busy) GUI.enabled = false;
            if (GUILayout.Button(loginButton))
            {
                busy = true;
                authenticationService.LoginWith2fa(two2fa);
            }
            GUI.enabled = true;

        }

        private void DoRequire2fa(string msg)
        {
            need2fa = true;
            message = msg;
            busy = false;
            parent.Redraw();
        }

        private void DoResult(bool success, string msg)
        {
            need2fa = false;
            finished = true;
            message = msg;
            busy = false;
            parent.Redraw();
        }
    }
}
