using System;
using UnityEngine;
using UnityEditor;

namespace GitHub.Unity
{
    [Serializable]
    class AuthenticationView : Subview
    {
        private static readonly Vector2 viewSize = new Vector2(290, 290);

        private const string WindowTitle = "Authenticate";
        private const string UsernameLabel = "Username";
        private const string PasswordLabel = "Password";
        private const string TwofaLabel = "2FA Code";
        private const string LoginButton = "Sign in";
        private const string BackButton = "Back";
        private const string AuthTitle = "Sign in to GitHub";
        private const string TwofaTitle = "Two-Factor Authentication";
        private const string TwofaDescription = "Open the two-factor authentication app on your device to view your 2FA code and verify your identity.";
        private const string TwofaButton = "Verify";

        [SerializeField] private Vector2 scroll;
        [SerializeField] private string username = string.Empty;
        [SerializeField] private string two2fa = string.Empty;
        [SerializeField] private string message;

        [NonSerialized] private bool need2fa;
        [NonSerialized] private bool isBusy;
        [NonSerialized] private string errorMessage;
        [NonSerialized] private bool enterPressed;
        [NonSerialized] private string password = string.Empty;
        [NonSerialized] private AuthenticationService authenticationService;


        public override void InitializeView(IView parent)
        {
            base.InitializeView(parent);
            need2fa = isBusy = false;
            Title = WindowTitle;
            Size = viewSize;
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
                GUILayout.BeginHorizontal(Styles.AuthHeaderBoxStyle);
                {
                  GUILayout.Label(AuthTitle, Styles.HeaderRepoLabelStyle);
                }
                GUILayout.EndHorizontal();

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

        public void SetMessage(string value)
        {
            message = value;
        }

        public void ClearMessage()
        {
            message = null;
        }

        public void SetUsername(string value)
        {
            username = value;
        }

        public void ClearUsername()
        {
            username = string.Empty;
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
                ShowMessage();

                EditorGUILayout.Space();
                
                GUILayout.BeginHorizontal();
                {
                    username = EditorGUILayout.TextField(UsernameLabel ,username, Styles.TextFieldStyle);
                }
                GUILayout.EndHorizontal();

                EditorGUILayout.Space();

                GUILayout.BeginHorizontal();
                {
                    password = EditorGUILayout.PasswordField(PasswordLabel, password, Styles.TextFieldStyle);
                }
                GUILayout.EndHorizontal();

                EditorGUILayout.Space();

                ShowErrorMessage();

                GUILayout.Space(Styles.BaseSpacing + 3);
                GUILayout.BeginHorizontal();
                {
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button(LoginButton) || (!isBusy && enterPressed))
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
                GUILayout.Label(TwofaTitle, EditorStyles.boldLabel);
                GUILayout.Label(TwofaDescription, EditorStyles.wordWrappedLabel);

                EditorGUI.BeginDisabledGroup(isBusy);
                {
                    GUILayout.Space(Styles.BaseSpacing);
                    GUILayout.BeginHorizontal();
                    {
                        two2fa = EditorGUILayout.TextField(TwofaLabel, two2fa, Styles.TextFieldStyle);
                    }
                    GUILayout.EndHorizontal();

                    GUILayout.Space(Styles.BaseSpacing);
                    ShowErrorMessage();

                    GUILayout.Space(Styles.BaseSpacing);
                    GUILayout.BeginHorizontal();
                    {
                        GUILayout.FlexibleSpace();
                        if (GUILayout.Button(BackButton))
                        {
                            GUI.FocusControl(null);
                            need2fa = false;
                            Redraw();
                        }

                        if (GUILayout.Button(TwofaButton) || (!isBusy && enterPressed))
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

        private void ShowMessage()
        {
            if (message != null)
            {
                EditorGUILayout.HelpBox(message, MessageType.Warning);
            }
        }

        private void ShowErrorMessage()
        {
            if (errorMessage != null)
            {
                EditorGUILayout.HelpBox(errorMessage, MessageType.Error);
            }
        }

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

        public override bool IsBusy
        {
            get { return isBusy; }
        }
    }
}
