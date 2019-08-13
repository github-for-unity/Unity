using System.Collections.Generic;
using System.Linq;
using Unity.VersionControl.Git;
using UnityEditor;
using UnityEngine;

namespace GitHub.Unity
{
    class LfsLocksModificationProcessor : UnityEditor.AssetModificationProcessor
    {
        private static ILogging Logger = LogHelper.GetLogger<LfsLocksModificationProcessor>();
        private static IRepository repository;
        private static IPlatform platform;
        private static IEnvironment environment;

        private static Dictionary<NPath, GitLock> locks = new Dictionary<NPath, GitLock>();
        private static CacheUpdateEvent lastLocksChangedEvent;
        private static string loggedInUser;

        public static void Initialize(IEnvironment env, IPlatform plat)
        {
            environment = env;
            platform = plat;
            platform.Keychain.ConnectionsChanged += UserMayHaveChanged;
            // we need to do this to get the initial user information up front
            UserMayHaveChanged();

            repository = environment.Repository;
            UnityShim.Editor_finishedDefaultHeaderGUI += InspectorHeaderFinished;

            if (repository != null)
            {
                repository.LocksChanged += RepositoryOnLocksChanged;
                repository.CheckAndRaiseEventsIfCacheNewer(CacheType.GitLocks, lastLocksChangedEvent);
            }
        }

        public static string[] OnWillSaveAssets(string[] paths)
        {
            return paths;
        }

        public static AssetMoveResult OnWillMoveAsset(string oldPath, string newPath)
        {
            return IsLockedBySomeoneElse(oldPath) || IsLockedBySomeoneElse(newPath) ? AssetMoveResult.FailedMove : AssetMoveResult.DidNotMove;
        }

        public static AssetDeleteResult OnWillDeleteAsset(string assetPath, RemoveAssetOptions option)
        {
            return IsLockedBySomeoneElse(assetPath) ? AssetDeleteResult.FailedDelete : AssetDeleteResult.DidNotDelete;
        }

        // Returns true if this file can be edited by this user
        public static bool IsOpenForEdit(string assetPath, out string message)
        {
            var lck = GetLock(assetPath);
            bool canEdit = true;
            if (assetPath.EndsWith(".meta"))
            {
                canEdit &= !IsLockedBySomeoneElse(lck);
                assetPath = assetPath.TrimEnd(".meta");
            }
            canEdit &= !IsLockedBySomeoneElse(lck);
            message = !canEdit ? string.Format("File is locked for editing by {0}", lck.Value.Owner.Name) : null;
            return canEdit;
        }

        private static void RepositoryOnLocksChanged(CacheUpdateEvent cacheUpdateEvent)
        {
            if (!lastLocksChangedEvent.Equals(cacheUpdateEvent))
            {
                lastLocksChangedEvent = cacheUpdateEvent;
                locks = repository.CurrentLocks.ToDictionary(gitLock => gitLock.Path);
            }
        }

        private static void UserMayHaveChanged()
        {
            loggedInUser = platform.Keychain.Connections.Select(x => x.Username).FirstOrDefault();
        }

        private static bool IsLockedBySomeoneElse(GitLock? lck)
        {
            return lck.HasValue && !lck.Value.Owner.Name.Equals(loggedInUser);
        }

        private static bool IsLockedBySomeoneElse(string assetPath)
        {
            return IsLockedBySomeoneElse(GetLock(assetPath));
        }

        private static GitLock? GetLock(string assetPath)
        {
            if (repository == null)
                return null;

            GitLock lck;
            var repositoryPath = environment.GetRepositoryPath(assetPath.ToNPath());
            if (locks.TryGetValue(repositoryPath, out lck))
                return lck;
            return null;
        }

        private static void InspectorHeaderFinished(Editor editor)
        {
            string message = "";
            if (!IsOpenForEdit(AssetDatabase.GetAssetPath(editor.target), out message))
            {
                var enabled = GUI.enabled;
                GUI.enabled = true;
                GUILayout.BeginVertical();
                {
                    GUILayout.Space(9);
                    GUILayout.BeginHorizontal();
                    {
                        GUILayout.BeginVertical(GUILayout.Width(32));
                        {
                            GUILayout.Label(Utility.GetIcon("big-logo.png", "big-logo@2x.png", Utility.IsDarkTheme), GUILayout.Width(32), GUILayout.Height(32));
                        }
                        GUILayout.EndVertical();

                        GUILayout.BeginVertical();
                        {
                            GUILayout.Space(9);
                            GUILayout.Label(message, Styles.HeaderBranchLabelStyle);
                        }
                        GUILayout.EndVertical();
                    }
                    GUILayout.EndHorizontal();
                }
                GUILayout.EndVertical();
                GUI.enabled = enabled;
            }

        }
    }
}
