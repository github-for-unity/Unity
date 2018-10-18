using System;
using System.Threading;
using UnityEngine;
using UnityEditor;

namespace GitHub.Unity
{
    [Serializable]
    class GitHubAuthenticationView : Subview
    {
        private static readonly Vector2 viewSize = new Vector2(290, 290);

        private const string CredentialsNeedRefreshMessage = "We've detected that your stored credentials are out of sync with your current user. This can happen if you have signed in to git outside of Unity. Sign in again to refresh your credentials.";
        private const string NeedAuthenticationMessage = "We need you to authenticate first";
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
        [SerializeField] private string errorMessage;
        [SerializeField] private bool need2fa;

        [NonSerialized] private bool isBusy;
        [NonSerialized] private bool enterPressed;
        [NonSerialized] private string password = string.Empty;
        [NonSerialized] private AuthenticationService authenticationService;


        public override void InitializeView(IView parent)
        {
            base.InitializeView(parent);
            need2fa = isBusy = false;
            message = errorMessage = null;
            Title = WindowTitle;
            Size = viewSize;
        }

        public void Initialize(Exception exception)
        {
            var usernameMismatchException = exception as TokenUsernameMismatchException;
            if (usernameMismatchException != null)
            {
                message = CredentialsNeedRefreshMessage;
                username = usernameMismatchException.CachedUsername;
            }

            var keychainEmptyException = exception as KeychainEmptyException;
            if (keychainEmptyException != null)
            {
                message = NeedAuthenticationMessage;
            }

            if (usernameMismatchException == null && keychainEmptyException == null)
            {
                message = exception.Message;
            }
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

                GUILayout.EndVertical();
            }
            GUILayout.EndScrollView();
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
                    EditorGUILayout.Space();
                    two2fa = EditorGUILayout.TextField(TwofaLabel, two2fa, Styles.TextFieldStyle);
                    EditorGUILayout.Space();
                    ShowErrorMessage();

                    GUILayout.BeginHorizontal();
                    {
                        GUILayout.FlexibleSpace();
                        if (GUILayout.Button(BackButton))
                        {
                            GUI.FocusControl(null);
                            Clear();
                        }

                        if (GUILayout.Button(TwofaButton) || (!isBusy && enterPressed))
                        {
                            GUI.FocusControl(null);
                            isBusy = true;
                            AuthenticationService.LoginWith2fa(two2fa);
                        }
                    }
                    GUILayout.EndHorizontal();

                    EditorGUILayout.Space();
                }
                EditorGUI.EndDisabledGroup();
            }
            GUILayout.EndVertical();
        }

        private void DoRequire2fa(string msg)
        {
            need2fa = true;
            errorMessage = msg;
            isBusy = false;
            Redraw();
        }

        private void Clear()
        {
            need2fa = false;
            errorMessage = null;
            isBusy = false;
            Redraw();
        }

        private void DoResult(bool success, string msg)
        {
            isBusy = false;
            if (success)
            {
                UsageTracker.IncrementAuthenticationViewButtonAuthentication();

                Clear();
                Finish(true);
            }
            else
            {
                errorMessage = msg;
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
                    AuthenticationService = new AuthenticationService(HostAddress.GitHubDotComHostAddress.WebUri.Host, Platform.Keychain, Manager.ProcessManager, Manager.TaskManager, Environment);
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
