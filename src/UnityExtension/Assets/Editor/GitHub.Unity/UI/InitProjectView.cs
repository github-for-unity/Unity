using System;
using UnityEditor;
using UnityEngine;

namespace GitHub.Unity
{
    [Serializable]
    class InitProjectView : Subview
    {
        private const string NoRepoTitle = "To begin using GitHub, initialize a git repository";
        private const string NoUserOrEmailError = "Name and email not set in git. Go into the settings tab and enter the missing information";

        [SerializeField] private bool hasCompletedInitialCheck;
        [SerializeField] private bool isUserDataPresent;

        [SerializeField] private CacheUpdateEvent lastCheckUserChangedEvent;

        [NonSerialized] private bool userHasChanges;
        [NonSerialized] private bool gitExecutableIsSet;

        public override void InitializeView(IView parent)
        {
            base.InitializeView(parent);
            gitExecutableIsSet = Environment.GitExecutablePath.IsInitialized;
            Redraw();
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

        public override void OnGUI()
        {
            GUILayout.BeginVertical(Styles.GenericBoxStyle);
            {
                GUILayout.FlexibleSpace();
                GUILayout.Space(-140);

                DoEmptyGUI();

                GUILayout.Label(NoRepoTitle, Styles.BoldCenteredLabel);
                GUILayout.Space(4);

                GUILayout.BeginHorizontal();
                {
                    GUILayout.FlexibleSpace();

                    EditorGUI.BeginDisabledGroup(!gitExecutableIsSet || IsBusy || !isUserDataPresent);
                    {
                        if (GUILayout.Button(Localization.InitializeRepositoryButtonText, "Button"))
                        {
                            Manager.InitializeRepository();
                        }
                    }
                    EditorGUI.EndDisabledGroup();

                    GUILayout.FlexibleSpace();
                }
                GUILayout.EndHorizontal();

                if (gitExecutableIsSet && hasCompletedInitialCheck && !isUserDataPresent)
                {
                    EditorGUILayout.Space();
                    EditorGUILayout.HelpBox(NoUserOrEmailError, MessageType.Error);
                }

                GUILayout.FlexibleSpace();
            }
            GUILayout.EndVertical();
        }

        public override void OnDataUpdate()
        {
            base.OnDataUpdate();
            MaybeUpdateData();
        }

        private void AttachHandlers()
        {
            User.Changed += UserOnChanged;
        }

        private void UserOnChanged(CacheUpdateEvent cacheUpdateEvent)
        {
            if (!lastCheckUserChangedEvent.Equals(cacheUpdateEvent))
            {
                lastCheckUserChangedEvent = cacheUpdateEvent;
                userHasChanges = true;
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
                isUserDataPresent = !string.IsNullOrEmpty(User.Name)
                    && !string.IsNullOrEmpty(User.Email);
                hasCompletedInitialCheck = true;
            }
        }
    }
}
