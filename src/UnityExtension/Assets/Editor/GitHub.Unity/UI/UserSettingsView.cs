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

                        GitClient.SetConfig("user.name", newGitName, GitConfigSource.User)
                                 .Then((success, value) =>
                                 {
                                     if (success)
                                     {
                                         if (Repository != null)
                                         {
                                             Repository.User.Name = newGitName;
                                         }
                                         else
                                         {
                                             if (cachedUser == null)
                                             {
                                                 cachedUser = new User();
                                             }
                                             cachedUser.Name = newGitName;
                                         }
                                     }
                                 })
                                 .Then(
                                     GitClient.SetConfig("user.email", newGitEmail, GitConfigSource.User)
                                              .Then((success, value) =>
                                              {
                                                  if (success)
                                                  {
                                                      if (Repository != null)
                                                      {
                                                          Repository.User.Email = newGitEmail;
                                                      }
                                                      else
                                                      {
                                                          cachedUser.Email = newGitEmail;
                                                      }

                                                      userDataHasChanged = true;
                                                  }
                                              }))
                                 .FinallyInUI((_, __) =>
                                 {
                                     isBusy = false;
                                     Redraw();
                                     Finish(true);
                                 })
                                 .Start();
                    }
                }
                EditorGUI.EndDisabledGroup();
            }
            EditorGUI.EndDisabledGroup();
        }

        public override void OnEnable()
        {
            base.OnEnable();
            userDataHasChanged = true;
        }

        private void MaybeUpdateData()
        {
            if (userDataHasChanged)
            {
                userDataHasChanged = false;

                if (Repository == null)
                {
                    UpdateUserDataFromClient();
                }
                else
                {
                    newGitName = gitName = Repository.User.Name;
                    newGitEmail = gitEmail = Repository.User.Email;
                }
            }
        }

        private void UpdateUserDataFromClient()
        {
            if (String.IsNullOrEmpty(EntryPoint.Environment.GitExecutablePath))
            {
                return;
            }

            if (GitClient == null)
            {
                return;
            }

            Logger.Trace("Update user data from GitClient");

            GitClient.GetConfigUserAndEmail()
                .ThenInUI((success, strings) => {
                    var username = strings[0];
                    var email = strings[1];

                    if (success && !String.IsNullOrEmpty(username) && !String.IsNullOrEmpty(email))
                    {
                        cachedUser = new User { Name = username, Email = email };
                        newGitName = gitName = cachedUser.Name;
                        newGitEmail = gitEmail = cachedUser.Email;
                        Redraw();
                    }
                }).Start();
        }

        public override bool IsBusy
        {
            get { return isBusy; }
        }
    }
}
