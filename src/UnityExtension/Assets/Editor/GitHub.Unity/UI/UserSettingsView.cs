using System;
using UnityEditor;
using UnityEngine;

namespace GitHub.Unity
{
    [Serializable]
    class UserSettingsView : Subview
    {
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
                if ((cachedUser == null || String.IsNullOrEmpty(cachedUser.Name)) && GitClient != null)
                {
                    var user = new User();
                    GitClient.GetConfig("user.name", GitConfigSource.User)
                        .Then((success, value) => user.Name = value).Then(
                    GitClient.GetConfig("user.email", GitConfigSource.User)
                        .Then((success, value) => user.Email = value))
                    .FinallyInUI((success, ex) =>
                    {
                        if (success && !String.IsNullOrEmpty(user.Name))
                        {
                            cachedUser = user;
                            userDataHasChanged = true;
                            Redraw();
                        }
                    })
                    .Start();
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
