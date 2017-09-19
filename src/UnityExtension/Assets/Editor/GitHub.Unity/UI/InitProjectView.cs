using System;
using UnityEditor;
using UnityEngine;

namespace GitHub.Unity
{
    [Serializable]
    class InitProjectView : Subview
    {
        private const string NoRepoTitle = "No Git repository found for this project";
        private const string NoRepoDescription = "Initialize a Git repository to track changes and collaborate with others.";
        private const string NoUserOrEmailError = "Name and Email must be configured in Settings";

        [SerializeField] private bool isBusy;
        [SerializeField] private bool isUserDataPresent = true;

        [NonSerialized] private string errorMessage;
        [NonSerialized] private bool userDataHasChanged;

        public override void InitializeView(IView parent)
        {
            base.InitializeView(parent);

            if (!string.IsNullOrEmpty(Environment.GitExecutablePath))
            {
                CheckForUser();
            }
        }

        public override void OnEnable()
        {
            base.OnEnable();
            userDataHasChanged = Environment.GitExecutablePath != null;
        }

        public override void OnDataUpdate()
        {
            base.OnDataUpdate();
            MaybeUpdateData();
        }

        public override void OnRepositoryChanged(IRepository oldRepository)
        {
            base.OnRepositoryChanged(oldRepository);
            Refresh();
        }

        public override bool IsBusy
        {
            get { return isBusy; }
        }

        public override void OnGUI()
        {
            var headerRect = EditorGUILayout.BeginHorizontal(Styles.HeaderBoxStyle);
            {
                GUILayout.Space(5);
                GUILayout.BeginVertical(GUILayout.Width(16));
                {
                    GUILayout.Space(5);

                    var iconRect = GUILayoutUtility.GetRect(new GUIContent(Styles.BigLogo), GUIStyle.none, GUILayout.Height(20), GUILayout.Width(20));
                    iconRect.y = headerRect.center.y - (iconRect.height / 2);
                    GUI.DrawTexture(iconRect, Styles.BigLogo, ScaleMode.ScaleToFit);

                    GUILayout.Space(5);
                }
                GUILayout.EndVertical();

                GUILayout.Space(5);

                GUILayout.BeginVertical();
                {
                    var headerContent = new GUIContent(NoRepoTitle);
                    var headerTitleRect = GUILayoutUtility.GetRect(headerContent, Styles.HeaderTitleStyle);
                    headerTitleRect.y = headerRect.center.y - (headerTitleRect.height / 2);

                    GUI.Label(headerTitleRect, headerContent, Styles.HeaderTitleStyle);
                }
                GUILayout.EndVertical();
            }
            EditorGUILayout.EndHorizontal();

            GUILayout.BeginVertical(Styles.GenericBoxStyle);
            {
                GUILayout.FlexibleSpace();

                GUILayout.Label(NoRepoDescription, Styles.CenteredLabel);

                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();

                EditorGUI.BeginDisabledGroup(isBusy || !isUserDataPresent);
                {
                    if (GUILayout.Button(Localization.InitializeRepositoryButtonText, "Button"))
                    {
                        isBusy = true;
                        Manager.InitializeRepository()
                               .FinallyInUI(() => isBusy = false)
                               .Start();
                    }
                }
                EditorGUI.EndDisabledGroup();

                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();

                ShowErrorMessage();

                GUILayout.FlexibleSpace();
            }
            GUILayout.EndVertical();
        }

        private void ShowErrorMessage()
        {
            if (errorMessage != null)
            {
                GUILayout.Space(Styles.BaseSpacing);
                GUILayout.BeginHorizontal();
                {
                    GUILayout.Label(errorMessage, Styles.CenteredErrorLabel);
                }
                GUILayout.EndHorizontal();
            }
        }

        private void MaybeUpdateData()
        {
            if (userDataHasChanged)
            {
                userDataHasChanged = false;
                CheckForUser();
            }
        }

        private void CheckForUser()
        {
            isBusy = true;

            string username = null;
            string email = null;

            GitClient.GetConfig("user.name", GitConfigSource.User).Then((success, value) => {
                Logger.Trace("Return success:{0} user.name", success, value);
                if (success)
                {
                    username = value;
                }
            }).Then(GitClient.GetConfig("user.email", GitConfigSource.User).Then((success, value) => {
                Logger.Trace("Return success:{0} user.email", success, value);
                if (success)
                {
                    email = value;
                }
            })).FinallyInUI((success, ex) => {
                Logger.Trace("Return success:{0} name:{1} email:{2}", success, username, email);

                isBusy = false;
                isUserDataPresent = success && !String.IsNullOrEmpty(username) && !String.IsNullOrEmpty(email);
                errorMessage = isUserDataPresent ? null : NoUserOrEmailError;

                Logger.Trace("Finally: {0}", isUserDataPresent);

                Redraw();
            }).Start();
        }
    }
}
