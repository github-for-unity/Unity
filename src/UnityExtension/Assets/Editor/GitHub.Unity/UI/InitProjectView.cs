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
        
        [SerializeField] private UserSettingsView userSettingsView = new UserSettingsView();
        [SerializeField] private GitPathView gitPathView = new GitPathView();
        [SerializeField] private bool isBusy;

        [NonSerialized] private string errorMessage;
        [NonSerialized] private bool isUserDataPresent;
        [NonSerialized] private bool userDataHasChanged;

        public override void InitializeView(IView parent)
        {
            base.InitializeView(parent);

            userSettingsView.InitializeView(this);
            gitPathView.InitializeView(this);

            if (!string.IsNullOrEmpty(Environment.GitExecutablePath))
            {
                CheckForUser();
            }
        }

        public override void OnEnable()
        {
            base.OnEnable();
            gitPathView.OnEnable();
            userDataHasChanged = Environment.GitExecutablePath != null;
        }

        public override void OnDataUpdate()
        {
            base.OnDataUpdate();
            userSettingsView.OnDataUpdate();
            gitPathView.OnDataUpdate();
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

            gitPathView.OnGUI();

            userSettingsView.OnGUI();

            GUILayout.BeginVertical(Styles.GenericBoxStyle);
            {
                GUILayout.FlexibleSpace();

                GUILayout.Label(NoRepoDescription, Styles.CenteredLabel);

                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();

                EditorGUI.BeginDisabledGroup(IsBusy || !isUserDataPresent);
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

            GitClient.GetConfigUserAndEmail().FinallyInUI((success, ex, strings) => {
                var username = strings[0];
                var email = strings[1];


                isBusy = false;
                isUserDataPresent = success && !String.IsNullOrEmpty(username) && !String.IsNullOrEmpty(email);
                errorMessage = isUserDataPresent ? null : NoUserOrEmailError;

                Logger.Trace("Finally: {0}", isUserDataPresent);

                Redraw();
            }).Start();
        }

        public override bool IsBusy
        {
            get { return isBusy || userSettingsView.IsBusy || gitPathView.IsBusy; }
        }
    }
}
