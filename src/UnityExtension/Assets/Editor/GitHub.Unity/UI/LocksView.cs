using GitHub.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace GitHub.Unity
{
    [Serializable]
    class LocksView : Subview
    {
        [NonSerialized] private bool currentLocksHasUpdate;
        [NonSerialized] private bool isBusy;

        [SerializeField] private CacheUpdateEvent lastLocksChangedEvent;
        [SerializeField] private List<GitLock> lockedFiles = new List<GitLock>();
        [SerializeField] private int lockedFileSelection = -1;
        [SerializeField] private Vector2 scroll;

        public override void OnEnable()
        {
            base.OnEnable();
            AttachHandlers(Repository);

            if (Repository != null)
            {
                ValidateCachedData(Repository);
            }
        }


        public override void OnDisable()
        {
            base.OnDisable();
            DetachHandlers(Repository);
        }

        public override void OnDataUpdate()
        {
            base.OnDataUpdate();
            MaybeUpdateData();
        }

        public override void OnGUI()
        {
            scroll = GUILayout.BeginScrollView(scroll);
            {
                if (Repository != null)
                {
                    OnGitLfsLocksGUI();
                }

                GUILayout.Space(EditorGUIUtility.standardVerticalSpacing);
            }

            GUILayout.EndScrollView();
        }

        private void AttachHandlers(IRepository repository)
        {
            if (repository == null)
            {
                return;
            }

            repository.LocksChanged += RepositoryOnLocksChanged;
        }

        private void RepositoryOnLocksChanged(CacheUpdateEvent cacheUpdateEvent)
        {
            if (!lastLocksChangedEvent.Equals(cacheUpdateEvent))
            {
                lastLocksChangedEvent = cacheUpdateEvent;
                currentLocksHasUpdate = true;
                Redraw();
            }
        }

        private void DetachHandlers(IRepository repository)
        {
            if (repository == null)
            {
                return;
            }

            repository.LocksChanged -= RepositoryOnLocksChanged;
        }

        private void ValidateCachedData(IRepository repository)
        {
            repository.CheckAndRaiseEventsIfCacheNewer(CacheType.GitLocks, lastLocksChangedEvent);
        }

        private void MaybeUpdateData()
        {
            if (lockedFiles == null)
                lockedFiles = new List<GitLock>();

            if (Repository == null)
                return;

            if (currentLocksHasUpdate)
            {
                currentLocksHasUpdate = false;
                var repositoryCurrentLocks = Repository.CurrentLocks;
                lockedFileSelection = -1;
                lockedFiles = repositoryCurrentLocks != null
                    ? repositoryCurrentLocks.ToList()
                    : new List<GitLock>();
            }
        }

        private void OnGitLfsLocksGUI()
        {
            EditorGUI.BeginDisabledGroup(IsBusy || Repository == null);
            {
                GUILayout.BeginVertical();
                {
                    GUILayout.Label("Locked files", EditorStyles.boldLabel);

                    scroll = EditorGUILayout.BeginScrollView(scroll, Styles.GenericTableBoxStyle,
                        GUILayout.Height(125));
                    {
                        GUILayout.BeginVertical();
                        {
                            var lockedFilesCount = lockedFiles.Count;
                            for (var index = 0; index < lockedFilesCount; ++index)
                            {
                                GUIStyle rowStyle = (lockedFileSelection == index)
                                    ? Styles.LockedFileRowSelectedStyle
                                    : Styles.LockedFileRowStyle;
                                GUILayout.Box(lockedFiles[index].Path, rowStyle);

                                if (Event.current.type == EventType.MouseDown &&
                                    GUILayoutUtility.GetLastRect().Contains(Event.current.mousePosition))
                                {
                                    var currentEvent = Event.current;

                                    if (currentEvent.button == 0)
                                    {
                                        lockedFileSelection = index;
                                    }

                                    Event.current.Use();
                                }
                            }
                        }

                        GUILayout.EndVertical();
                    }

                    EditorGUILayout.EndScrollView();

                    if (lockedFileSelection > -1)
                    {
                        GUILayout.BeginVertical();
                        {
                            var lck = lockedFiles[lockedFileSelection];
                            GUILayout.Label(lck.Path, EditorStyles.boldLabel);

                            GUILayout.BeginHorizontal();
                            {
                                GUILayout.Label("Locked by " + lck.User);
                                GUILayout.FlexibleSpace();
                                if (GUILayout.Button("Unlock"))
                                {
                                    Repository.ReleaseLock(lck.Path, true).Start();
                                }
                            }
                            GUILayout.EndHorizontal();
                        }
                        GUILayout.EndVertical();
                    }
                }

                GUILayout.EndVertical();

            }
            EditorGUI.EndDisabledGroup();
        }

        public override bool IsBusy
        {
            get { return isBusy; }
        }
    }
}
