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

        [SerializeField] private string gitName;
        [SerializeField] private string gitEmail;
        [SerializeField] private string newGitName;
        [SerializeField] private string newGitEmail;
        [SerializeField] private bool needsSaving;
        [SerializeField] private CacheUpdateEvent lastCheckUserChangedEvent;

        [NonSerialized] private bool isBusy;
        [NonSerialized] private bool userHasChanges;

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
                        isBusy = true;

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

            User.CheckUserChangedEvent(lastCheckUserChangedEvent);
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
            Logger.Trace("UserOnChanged");

            if (!lastCheckUserChangedEvent.Equals(cacheUpdateEvent))
            {
                lastCheckUserChangedEvent = cacheUpdateEvent;
                userHasChanges = true;
                isBusy = false;
                Redraw();
            }
        }

        private void DetachHandlers()
        {
            User.Changed -= UserOnChanged;
        }

        private void MaybeUpdateData()
        {
            if (userHasChanges)
            {
                userHasChanges = false;
                gitName = newGitName = User.Name;
                gitEmail = newGitEmail = User.Email;
                needsSaving = false;
            }
        }

        public override bool IsBusy
        {
            get { return isBusy; }
        }
    }
}
