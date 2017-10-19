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
        
        [SerializeField] private UserSettingsView userSettingsView = new UserSettingsView();
        [SerializeField] private GitPathView gitPathView = new GitPathView();
        [SerializeField] private bool isBusy;

        public override void InitializeView(IView parent)
        {
            base.InitializeView(parent);
            userSettingsView.InitializeView(this);
            gitPathView.InitializeView(this);
        }

        public override void OnDataUpdate()
        {
            base.OnDataUpdate();
            userSettingsView.OnDataUpdate();
            gitPathView.OnDataUpdate();
        }

        public override void OnEnable()
        {
            base.OnEnable();
            userSettingsView.OnEnable();
            gitPathView.OnEnable();
        }

        public override void OnDisable()
        {
            base.OnDisable();
            userSettingsView.OnDisable();
            gitPathView.OnDisable();
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

                EditorGUI.BeginDisabledGroup(IsBusy);
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

                GUILayout.FlexibleSpace();
            }
            GUILayout.EndVertical();
        }

        public override bool IsBusy
        {
            get { return isBusy || userSettingsView.IsBusy || gitPathView.IsBusy; }
        }
    }
}
