using System;
using UnityEngine;
using UnityEditor;

namespace GitHub.Unity
{
    [Serializable]
    class AuthenticationView : Subview
    {
        private static readonly ILogging logger = Logging.GetLogger<AuthenticationView>();

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

        int cellWidth;

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
            // Ensure a nice looking grid for our auth UI
            // Not really sure why I need to divide by 4... Retina perhaps??
            // If so, seems very brittle...
            cellWidth = (Screen.width / 4) - Convert.ToInt32(Styles.BaseSpacing * 2);

            scroll = GUILayout.BeginScrollView(scroll);
            {
              Rect authHeader = EditorGUILayout.BeginHorizontal(Styles.AuthHeaderBoxStyle);
              {
                  GUILayout.BeginVertical(GUILayout.Width(16));
                  {
                      GUILayout.Space(9);
                      GUILayout.Label(Styles.TitleIcon, GUILayout.Height(20), GUILayout.Width(20));
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

                if (finished)
                {
                    // Debug.Log("finished");
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
                GUILayout.Label(usernameLabel, GUILayout.Width(cellWidth));
                GUILayout.FlexibleSpace();
                if (busy) GUI.enabled = false;
                username = GUILayout.TextField(username, Styles.TextFieldStyle, GUILayout.Width(cellWidth));
                GUI.enabled = true;
            }
            GUILayout.EndHorizontal();
            GUILayout.Space(Styles.BaseSpacing);
            GUILayout.BeginHorizontal();
            {
                GUILayout.Label(passwordLabel, GUILayout.Width(cellWidth));
                GUILayout.FlexibleSpace();
                if (busy) GUI.enabled = false;
                password = GUILayout.PasswordField(password, '•', Styles.TextFieldStyle, GUILayout.Width(cellWidth));
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
                  authenticationService.Login(username, password, DoRequire2fa, DoResult);
              }
            GUILayout.EndHorizontal();
            GUI.enabled = true;
        }

        private void OnGUI2FA()
        {
            GUILayout.BeginVertical();
            GUILayout.Label(twofaTitle, EditorStyles.boldLabel);
            GUILayout.Label(twofaDescription, EditorStyles.wordWrappedLabel, GUILayout.Width(cellWidth * 2));

            GUILayout.Space(Styles.BaseSpacing);

            GUILayout.BeginHorizontal();
            {
                GUILayout.Label(twofaLabel, GUILayout.Width(cellWidth));
                GUILayout.FlexibleSpace();
                if (busy) GUI.enabled = false;
                two2fa = GUILayout.TextField(two2fa, Styles.TextFieldStyle, GUILayout.Width(55));
                GUI.enabled = true;
            }
            GUILayout.EndHorizontal();

            ShowMessage(message, Styles.ErrorLabel);

            GUILayout.Space(Styles.BaseSpacing);

            if (busy) GUI.enabled = false;
            GUILayout.BeginHorizontal();
              GUILayout.FlexibleSpace();
              if (GUILayout.Button(backButton))
              {
                need2fa = false;
                parent.Redraw();
              }

              GUILayout.Space(Styles.BaseSpacing);

              if (GUILayout.Button(twofaButton))
              {
                  busy = true;
                  authenticationService.LoginWith2fa(two2fa);
              }
            GUILayout.EndHorizontal();

            GUI.enabled = true;
            GUILayout.Space(Styles.BaseSpacing);
            GUILayout.EndVertical();
            GUILayout.Space(Styles.BaseSpacing);
        }

        private void DoRequire2fa(string msg)
        {
            Debug.Log("DoRequire2fa");
            need2fa = true;
            message = msg;
            busy = false;
            parent.Redraw();
        }

        private void DoResult(bool success, string msg)
        {
            finished = true;
            message = msg;
            busy = false;

            if (success == true && need2fa == false)
            {
              Debug.Log("DoResult Success");
              parent.Close();
            }
            else if (success == true && need2fa == true)
            {
              Debug.Log("DoResult Success Need2fa");
              parent.Close();
            }
            else
            {
              Debug.Log("DoResult Else");
              parent.Redraw();
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
