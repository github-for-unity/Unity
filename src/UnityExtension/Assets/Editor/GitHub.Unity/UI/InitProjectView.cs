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

        [SerializeField] private bool isBusy;
        [SerializeField] private bool isUserDataPresent = true;

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
            GUILayout.BeginVertical(Styles.GenericBoxStyle);
            {
                if (!isUserDataPresent)
                {
                    GUILayout.FlexibleSpace();


                    var emptyTitleStyle = new GUIStyle(Styles.BoldLabel);
                    emptyTitleStyle.alignment = TextAnchor.MiddleCenter;

                    GUILayout.Label("Almost there", emptyTitleStyle);
                    GUILayout.Label("There's a few more things that need attention first", Styles.CenteredLabel);

                    GUILayout.Space(7);

                    var missingGitConfigText = "Missing name and email in Git Configuration";
                    var missingGitConfigContent = new GUIContent(missingGitConfigText);
                    var missingGitConfigRect = GUILayoutUtility.GetRect(missingGitConfigContent, Styles.ErrorLabel);
                    EditorGUI.DrawRect(missingGitConfigRect, Color.white);
                    GUI.Label(missingGitConfigRect, missingGitConfigContent);

                    GUILayout.Space(7);

                    EditorGUI.BeginDisabledGroup(isBusy);
                    {
                        EditorGUILayout.BeginHorizontal();

                        {
                          GUILayout.FlexibleSpace();

                          if (GUILayout.Button("Finish Git Configuration", "Button"))
                          {
                              PopupWindow.Open(PopupWindow.PopupViewType.UserSettingsView, completed => {
                                  if (completed)
                                  {
                                      userDataHasChanged = true;
                                  }
                              });
                          }

                          GUILayout.FlexibleSpace();
                        }
                        EditorGUILayout.EndHorizontal();
                    }
                    EditorGUI.EndDisabledGroup();
                }

                GUILayout.FlexibleSpace();

                if (isUserDataPresent)
                {

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

                }

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

                Logger.Trace("Finally: {0}", isUserDataPresent);

                Redraw();
            }).Start();
        }
    }
}
