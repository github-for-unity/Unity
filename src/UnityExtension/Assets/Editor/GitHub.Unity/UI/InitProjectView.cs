using System;
using UnityEditor;
using UnityEngine;

namespace GitHub.Unity
{
    [Serializable]
    class InitProjectView : Subview
    {
        private const string NoRepoTitle = "To begin using GitHub, initialize a git repository";
        private const string NoRepoDescription = "Initialize a Git repository to track changes and collaborate with others.";
        private const string NoUserOrEmailError = "Name and Email must be configured in Settings";

        [SerializeField] private UserSettingsView userSettingsView = new UserSettingsView();
        [SerializeField] private GitPathView gitPathView = new GitPathView();

        [NonSerialized] private bool isBusy;

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
            GUILayout.BeginVertical(Styles.GenericBoxStyle);
            {
                GUILayout.FlexibleSpace();
                GUILayout.Space(-140);

                GUILayout.BeginHorizontal();
                {
                  GUILayout.FlexibleSpace();
                  GUILayout.Label(Styles.EmptyStateInit, GUILayout.MaxWidth(265), GUILayout.MaxHeight(136));
                  GUILayout.FlexibleSpace();
                }
                GUILayout.EndHorizontal();

                GUILayout.Label(NoRepoTitle, Styles.BoldCenteredLabel);
                EditorGUILayout.Space();

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

                EditorGUILayout.Space();
                EditorGUILayout.HelpBox("There was an error initializing a repository.", MessageType.Error);

                GUILayout.FlexibleSpace();
            }
            GUILayout.EndVertical();
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
