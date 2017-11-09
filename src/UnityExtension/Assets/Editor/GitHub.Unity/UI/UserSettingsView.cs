using System;
using UnityEditor;
using UnityEngine;

namespace GitHub.Unity
{
    [Serializable]
    class UserSettingsView : Subview
    {
        private static readonly Vector2 viewSize = new Vector2(325, 125);
        private const string WindowTitle = "User Settings";

        private const string GitConfigTitle = "Git Configuration";
        private const string GitConfigNameLabel = "Name";
        private const string GitConfigEmailLabel = "Email";
        private const string GitConfigUserSave = "Save User";

        [NonSerialized] private bool isBusy;
        [NonSerialized] private bool userDataHasChanged;

        [SerializeField] private string gitName;
        [SerializeField] private string gitEmail;
        [SerializeField] private string newGitName;
        [SerializeField] private string newGitEmail;
        [SerializeField] private User cachedUser;

        public override void InitializeView(IView parent)
        {
            base.InitializeView(parent);
            Title = WindowTitle;
            Size = viewSize;
        }

        public override void OnDataUpdate()
        {
            base.OnDataUpdate();
            MaybeUpdateData();
        }

        public override void OnGUI()
        {
            GUILayout.Label(GitConfigTitle, EditorStyles.boldLabel);

            EditorGUI.BeginDisabledGroup(IsBusy || Parent.IsBusy);
            {
                newGitName = EditorGUILayout.TextField(GitConfigNameLabel, newGitName);
                newGitEmail = EditorGUILayout.TextField(GitConfigEmailLabel, newGitEmail);

                var needsSaving = (newGitName != gitName || newGitEmail != gitEmail)
                    && !(string.IsNullOrEmpty(newGitName) || string.IsNullOrEmpty(newGitEmail));

                EditorGUI.BeginDisabledGroup(!needsSaving);
                {
                    if (GUILayout.Button(GitConfigUserSave, GUILayout.ExpandWidth(false)))
                    {
                        GUI.FocusControl(null);
                        isBusy = true;

                        GitClient.SetConfigUserAndEmail(newGitName, newGitEmail)
                                 .FinallyInUI((success, exception, user) => {
                                     isBusy = false;
                                     if (success)
                                     {
                                         if (Repository != null)
                                         {
                                             Repository.User.Name = newGitName;
                                             Repository.User.Email = newGitEmail;
                                         }
                                         else
                                         {
                                             if (cachedUser == null)
                                             {
                                                 cachedUser = new User();
                                             }
                                             cachedUser.Name = newGitName;
                                             cachedUser.Email = newGitEmail;
                                         }

                                         Redraw();
                                         Finish(true);
                                     }
                                 })
                                 .Start();
                    }
                }
                EditorGUI.EndDisabledGroup();
            }
            EditorGUI.EndDisabledGroup();
        }

        private void MaybeUpdateData()
        {
            if (Repository == null)
            {
                if (!String.IsNullOrEmpty(EntryPoint.Environment.GitExecutablePath))
                {
                    if ((cachedUser == null || String.IsNullOrEmpty(cachedUser.Name)) && GitClient != null)
                    {
                        GitClient.GetConfigUserAndEmail().FinallyInUI((success, ex, user) => {
                            if (success && !String.IsNullOrEmpty(user.Name) && !String.IsNullOrEmpty(user.Email))
                            {
                                cachedUser = user;

                                userDataHasChanged = true;
                                Redraw();
                            }
                        }).Start();
                    }
                }

                if (userDataHasChanged)
                {
                    newGitName = gitName = cachedUser.Name;
                    newGitEmail = gitEmail = cachedUser.Email;
                    userDataHasChanged = false;
                }
                return;
            }

            userDataHasChanged = Repository.User.Name != gitName || Repository.User.Email != gitEmail;

            if (!userDataHasChanged)
                return;

            userDataHasChanged = false;
            newGitName = gitName = Repository.User.Name;
            newGitEmail = gitEmail = Repository.User.Email;
        }

        public override bool IsBusy
        {
            get { return isBusy; }
        }
    }
}
