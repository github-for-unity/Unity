using System;
using UnityEngine;
using UnityEditor;

namespace GitHub.Unity
{
    [Serializable]
    class AuthenticationView : Subview
    {
        const string usernameLabel = "Username";
        const string passwordLabel = "Password";
        const string twofaLabel = "2FA Code";
        const string loginButton = "Sign in";
        const string backButton = "Back";
        const string authTitle = "Sign in to GitHub";
        const string twofaTitle = "Two-Factor Authentication";
        const string twofaDescription = "Open the two-factor authentication app on your device to view your 2FA code and verify your identity.";
        const string twofaButton = "Verify";

        [SerializeField] private Vector2 scroll;
        [SerializeField] private string username = "";
        [SerializeField] private string two2fa = "";

        [NonSerialized] private bool need2fa;
        [NonSerialized] private bool isBusy;
        [NonSerialized] private string errorMessage;
        [NonSerialized] private bool enterPressed;
        [NonSerialized] private string password = "";

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

                    AuthenticationService = new AuthenticationService(host, Platform.Keychain);
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
            need2fa = isBusy = false;
        }

        public override void OnEnable()
        {
            base.OnEnable();
        }

        public override void OnDisable()
        {
            base.OnDisable();
        }

        public override void OnGUI()
        {
            HandleEnterPressed();

            EditorGUIUtility.labelWidth = 90f;

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
            }
            GUILayout.EndScrollView();
        }

        public override bool IsBusy
        {
            get { return isBusy; }
        }

        private void HandleEnterPressed()
        {
            if (Event.current.type != EventType.KeyDown)
                return;

            enterPressed = Event.current.keyCode == KeyCode.Return || Event.current.keyCode == KeyCode.KeypadEnter;
            if (enterPressed)
                Event.current.Use();
        }

        private void OnGUILogin()
        {
            EditorGUI.BeginDisabledGroup(isBusy);
            {
                GUILayout.Space(3);
                GUILayout.BeginHorizontal();
                {
                    username = EditorGUILayout.TextField(usernameLabel, username, Styles.TextFieldStyle);
                }
                GUILayout.EndHorizontal();

                GUILayout.Space(Styles.BaseSpacing);
                GUILayout.BeginHorizontal();
                {
                    password = EditorGUILayout.PasswordField(passwordLabel, password, Styles.TextFieldStyle);
                }
                GUILayout.EndHorizontal();

                ShowErrorMessage();

                GUILayout.Space(Styles.BaseSpacing + 3);
                GUILayout.BeginHorizontal();
                {
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button(loginButton) || (!isBusy && enterPressed))
                    {
                        GUI.FocusControl(null);
                        isBusy = true;
                        AuthenticationService.Login(username, password, DoRequire2fa, DoResult);
                    }
                }
                GUILayout.EndHorizontal();
            }
            EditorGUI.EndDisabledGroup();
        }

        private void OnGUI2FA()
        {
            GUILayout.BeginVertical();
            {
                GUILayout.Label(twofaTitle, EditorStyles.boldLabel);
                GUILayout.Label(twofaDescription, EditorStyles.wordWrappedLabel);

                EditorGUI.BeginDisabledGroup(isBusy);
                {
                    GUILayout.Space(Styles.BaseSpacing);
                    GUILayout.BeginHorizontal();
                    {
                        two2fa = EditorGUILayout.TextField(twofaLabel, two2fa, Styles.TextFieldStyle);
                    }
                    GUILayout.EndHorizontal();

                    GUILayout.Space(Styles.BaseSpacing);
                    ShowErrorMessage();

                    GUILayout.Space(Styles.BaseSpacing);
                    GUILayout.BeginHorizontal();
                    {
                        GUILayout.FlexibleSpace();
                        if (GUILayout.Button(backButton))
                        {
                            GUI.FocusControl(null);
                            need2fa = false;
                            Redraw();
                        }

                        if (GUILayout.Button(twofaButton) || (!isBusy && enterPressed))
                        {
                            GUI.FocusControl(null);
                            isBusy = true;
                            AuthenticationService.LoginWith2fa(two2fa);
                        }
                    }
                    GUILayout.EndHorizontal();

                    GUILayout.Space(Styles.BaseSpacing);
                }
                EditorGUI.EndDisabledGroup();
            }
            GUILayout.EndVertical();
        }

        private void DoRequire2fa(string msg)
        {
            Logger.Trace("Strating 2FA - Message:\"{0}\"", msg);

            need2fa = true;
            errorMessage = msg;
            isBusy = false;
            Redraw();
        }

        private void DoResult(bool success, string msg)
        {
            Logger.Trace("DoResult - Success:{0} Message:\"{1}\"", success, msg);

            errorMessage = msg;
            isBusy = false;

            if (success == true)
            {
                Finish(true);
            }
            else
            {
                Redraw();
            }
        }

        private void ShowErrorMessage()
        {
            if (errorMessage != null)
            {
                GUILayout.Label(errorMessage, Styles.ErrorLabel);
            }
        }
    }
}
