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

        [SerializeField] private string gitName;
        [SerializeField] private string gitEmail;
        [SerializeField] private string newGitName;
        [SerializeField] private string newGitEmail;
        [SerializeField] private bool needsSaving;
        [SerializeField] private CacheUpdateEvent lastCheckUserChangedEvent;

        [NonSerialized] private bool gitExecutableIsSet;

        public override void Refresh()
        {
            base.Refresh();
            Refresh(CacheType.GitUser);
        }

        public override void OnDataUpdate(bool first)
        {
            base.OnDataUpdate(first);
            MaybeUpdateData(first);
        }

        private void MaybeUpdateData(bool first)
        {
            gitExecutableIsSet = Environment.GitExecutablePath.IsInitialized;

            if (first || IsRefreshing)
            {
                gitName = newGitName = User.Name;
                gitEmail = newGitEmail = User.Email;
                needsSaving = false;
            }
        }

        public override void OnUI()
        {
            GUILayout.Label(GitConfigTitle, EditorStyles.boldLabel);

            EditorGUI.BeginDisabledGroup(!gitExecutableIsSet || IsBusy || Parent.IsBusy);
            {
                EditorGUI.BeginChangeCheck();
                {
                    newGitName = EditorGUILayout.TextField(GitConfigNameLabel, newGitName);
                    newGitEmail = EditorGUILayout.TextField(GitConfigEmailLabel, newGitEmail);
                }

                if (EditorGUI.EndChangeCheck())
                {
                    needsSaving = !(string.IsNullOrEmpty(newGitName) || string.IsNullOrEmpty(newGitEmail))
                        && (newGitName != gitName || newGitEmail != gitEmail);
                }

                EditorGUI.BeginDisabledGroup(!needsSaving);
                {
                    if (GUILayout.Button(GitConfigUserSave, GUILayout.ExpandWidth(false)))
                    {
                        GUI.FocusControl(null);
                        IsBusy = true;

                        User.SetNameAndEmail(newGitName, newGitEmail);
                    }
                }
                EditorGUI.EndDisabledGroup();
            }
            EditorGUI.EndDisabledGroup();
        }

        public override void OnEnable()
        {
            base.OnEnable();
            AttachHandlers();

            User.CheckAndRaiseEventsIfCacheNewer(CacheType.GitUser, lastCheckUserChangedEvent);
        }

        public override void OnDisable()
        {
            base.OnDisable();
            DetachHandlers();
        }
        
        private void AttachHandlers()
        {
            User.Changed += UserOnChanged;
        }

        private void UserOnChanged(CacheUpdateEvent cacheUpdateEvent)
        {
            lastCheckUserChangedEvent = cacheUpdateEvent;
            Refresh();
        }

        private void DetachHandlers()
        {
            User.Changed -= UserOnChanged;
        }
    }
}
