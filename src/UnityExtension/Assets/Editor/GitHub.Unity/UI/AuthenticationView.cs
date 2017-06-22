using System;
using UnityEngine;
using UnityEditor;

namespace GitHub.Unity
{
    [Serializable]
    class AuthenticationView : Subview
    {
        const string serverLabel = "Server";
        const string usernameLabel = "Username";
        const string passwordLabel = "Password";
        const string twofaLabel = "Authentication code";
        const string loginButton = "Sign in";
        const string backButton = "Back";
        const string authTitle = "Sign in to GitHub";
        const string twofaTitle = "Two-factor authentication";
        const string twofaDescription = "Open the two-factor authentication app on your device to view your authentication code and verify your identity.";
        const string twofaButton = "Verify";

        [SerializeField] private Vector2 scroll;
        [SerializeField] private string username = "";
        [SerializeField] private string password = "";
        [SerializeField] private string two2fa = "";

        [NonSerialized] private bool need2fa;
        [NonSerialized] private bool busy;
        [NonSerialized] private string message;

        [NonSerialized] private AuthenticationService authenticationService;
        private AuthenticationService AuthenticationService
        {
            get
            {
                if (authenticationService == null)
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

                    AuthenticationService = new AuthenticationService(host, new AppConfiguration(), Platform.Keychain);
                }
                return authenticationService;
            }
            set
            {
                authenticationService = value;
            }
        }

        public override void InitializeView(IView parent)
        {
            base.InitializeView(parent);
            need2fa = busy = false;
        }

        public override void OnShow()
        {
            base.OnShow();
        }

        public override void OnHide()
        {
            base.OnHide();
        }

        public override void OnGUI()
        {
            scroll = GUILayout.BeginScrollView(scroll);
            {
                Rect authHeader = EditorGUILayout.BeginHorizontal(Styles.AuthHeaderBoxStyle);
                {
                    GUILayout.BeginVertical(GUILayout.Width(16));
                    {
                        GUILayout.Space(9);
                        GUILayout.Label(Styles.BigLogo, GUILayout.Height(20), GUILayout.Width(20));
                    }
                    GUILayout.EndVertical();

                    GUILayout.BeginVertical();
                    {
                        GUILayout.Space(11);
                        GUILayout.Label(authTitle, Styles.HeaderRepoLabelStyle);
                    }
                    GUILayout.EndVertical();
                }

                GUILayout.EndHorizontal();
                EditorGUI.DrawRect(
                  new Rect(authHeader.x, authHeader.yMax, authHeader.xMax, 1),
                  new Color(0.455F, 0.455F, 0.455F, 1F)
                );

                GUILayout.BeginVertical(Styles.GenericBoxStyle);
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

                GUILayout.EndVertical();
                GUILayout.Space(Styles.BaseSpacing);
            }
            GUILayout.EndScrollView();
        }

        private void OnGUILogin()
        {
            GUILayout.Space(3);
            GUILayout.BeginHorizontal();
            {
                if (busy) GUI.enabled = false;
                username = EditorGUILayout.TextField("Username" ,username, Styles.TextFieldStyle);
                GUI.enabled = true;
            }
            GUILayout.EndHorizontal();
            GUILayout.Space(Styles.BaseSpacing);
            GUILayout.BeginHorizontal();
            {
                if (busy) GUI.enabled = false;
                password = EditorGUILayout.PasswordField("Password", password, Styles.TextFieldStyle);
                GUI.enabled = true;
            }
            GUILayout.EndHorizontal();

            ShowMessage(message, Styles.ErrorLabel);

            GUILayout.Space(Styles.BaseSpacing + 3);

            if (busy) GUI.enabled = false;
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button(loginButton))
            {
                busy = true;
                AuthenticationService.Login(username, password, DoRequire2fa, DoResult);
            }
            GUILayout.EndHorizontal();
            GUI.enabled = true;
        }

        private void OnGUI2FA()
        {
            GUILayout.BeginVertical();
            GUILayout.Label(twofaTitle, EditorStyles.boldLabel);
            GUILayout.Label(twofaDescription, EditorStyles.wordWrappedLabel);

            GUILayout.Space(Styles.BaseSpacing);

            GUILayout.BeginHorizontal();
            {
                if (busy) GUI.enabled = false;
                two2fa = EditorGUILayout.TextField(twofaLabel, two2fa, Styles.TextFieldStyle);
                GUI.enabled = true;
            }
            GUILayout.EndHorizontal();
            GUILayout.Space(Styles.BaseSpacing);

            ShowMessage(message, Styles.ErrorLabel);

            GUILayout.Space(Styles.BaseSpacing);

            if (busy) GUI.enabled = false;
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button(backButton))
            {
                need2fa = false;
                Redraw();
            }

            GUILayout.Space(Styles.BaseSpacing);

            if (GUILayout.Button(twofaButton))
            {
                busy = true;
                AuthenticationService.LoginWith2fa(two2fa);
            }
            GUILayout.EndHorizontal();

            GUI.enabled = true;
            GUILayout.Space(Styles.BaseSpacing);
            GUILayout.EndVertical();
            GUILayout.Space(Styles.BaseSpacing);
        }

        private void DoRequire2fa(string msg)
        {
            Logger.Trace("Strating 2FA - Message:\"{0}\"", msg);

            need2fa = true;
            message = msg;
            busy = false;
            Redraw();
        }

        private void DoResult(bool success, string msg)
        {
            Logger.Trace("DoResult - Success:{0} Message:\"{1}\"", success, msg);

            message = msg;
            busy = false;

            if (success == true)
            {
                Finish(true);
            }
            else
            {
                Redraw();
            }
        }

        private void ShowMessage(string message, GUIStyle style)
        {
            if (message != null)
            {
                GUILayout.Label(message, style);
            }
        }
    }
}
