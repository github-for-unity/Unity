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

        [NonSerialized] private bool isBusy;
        [NonSerialized] private bool isUserDataPresent;
        [NonSerialized] private bool hasCompletedInitialCheck;

        [SerializeField] private CacheUpdateEvent lastCheckUserChangedEvent;
        [NonSerialized] private bool userHasChanges;

        public override void OnEnable()
        {
            base.OnEnable();
            AttachHandlers();

            if (GitClient != null)
            {
                GitClient.CheckUserChangedEvent(lastCheckUserChangedEvent);
            }
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

                GUILayout.BeginHorizontal();
                {
                  GUILayout.FlexibleSpace();
                  GUILayout.Label(Styles.EmptyStateInit, GUILayout.MaxWidth(265), GUILayout.MaxHeight(136));
                  GUILayout.FlexibleSpace();
                }
                GUILayout.EndHorizontal();

                GUILayout.Label(NoRepoTitle, Styles.BoldCenteredLabel);
                GUILayout.Space(4);

                GUILayout.BeginHorizontal();
                {
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
                }
                GUILayout.EndHorizontal();

                if (hasCompletedInitialCheck && !isUserDataPresent)
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
            if (GitClient != null)
            {
                GitClient.CurrentUserChanged+=GitClientOnCurrentUserChanged;
            }
        }

        private void GitClientOnCurrentUserChanged(CacheUpdateEvent cacheUpdateEvent)
        {
            if (!lastCheckUserChangedEvent.Equals(cacheUpdateEvent))
            {
                new ActionTask(TaskManager.Token, () =>
                    {
                        lastCheckUserChangedEvent = cacheUpdateEvent;
                        userHasChanges = true;
                        Redraw();
                    })
                    { Affinity = TaskAffinity.UI }.Start();
            }
        }

        private void DetachHandlers()
        {
            if (GitClient != null)
            {
                GitClient.CurrentUserChanged -= GitClientOnCurrentUserChanged;
            }
        }

        private void MaybeUpdateData()
        {
            if (userHasChanges)
            {
                userHasChanges = false;
                hasCompletedInitialCheck = true;
                isUserDataPresent = !string.IsNullOrEmpty(GitClient.CurrentUser.Name)
                    && !string.IsNullOrEmpty(GitClient.CurrentUser.Email);
            }
        }

        public override bool IsBusy
        {
            get { return isBusy;  }
        }
    }
}
