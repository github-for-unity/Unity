using System;
using UnityEngine;

namespace GitHub.Unity
{
    [Serializable]
    class AuthenticationView : Subview
    {
        const string serverLabel = "Server";
        const string usernameLabel = "Username";
        const string passwordLabel = "Password";
        const string twofaLabel = "Code";
        const string loginButton = "Login";

        [SerializeField] private Vector2 scroll;
        [SerializeField] private string username = "";
        [SerializeField] private string password = "";
        [SerializeField] private string[] two2fa = new string[6];

        public override void OnGUI()
        {
            scroll = GUILayout.BeginScrollView(scroll);
            {
                GUILayout.BeginVertical();
                {
                    GUILayout.BeginHorizontal();
                    {
                        GUILayout.Label(usernameLabel);
                        username = GUILayout.TextField(username);
                    }
                    GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal();
                    {
                        GUILayout.Label(passwordLabel);
                        password = GUILayout.TextField(password);
                    }
                    GUILayout.EndHorizontal();
                    if (GUILayout.Button(loginButton))
                    {
                        var serv = new AuthenticationService(new Program(), EntryPoint.Platform.CredentialManager);
                        serv.Login(null);
                    }
                }
            }
            GUILayout.EndScrollView();
        }
    }
}
