using System.Collections.Generic;
using System.Linq;
using GitHub.Logging;
using UnityEditor;

namespace GitHub.Unity
{
    class LfsLocksModificationProcessor : UnityEditor.AssetModificationProcessor
    {
        private static ILogging Logger = LogHelper.GetLogger<LfsLocksModificationProcessor>();
        private static IRepository repository;
        private static IPlatform platform;
        private static IEnvironment environment;

        private static Dictionary<string, GitLock> locks = new Dictionary<string, GitLock>();
        private static CacheUpdateEvent lastLocksChangedEvent;
        private static string loggedInUser;

        public static void Initialize(IEnvironment env, IPlatform plat)
        {
            //Logger.Trace("Initialize HasRepository:{0}", repo != null);
            environment = env;
            platform = plat;
            platform.Keychain.ConnectionsChanged += UserMayHaveChanged;

            repository = environment.Repository;
            if (repository != null)
            {
                repository.LocksChanged += RepositoryOnLocksChanged;
                repository.CheckLocksChangedEvent(lastLocksChangedEvent);
            }
        }

        public static string[] OnWillSaveAssets(string[] paths)
        {
            //Logger.Trace("OnWillSaveAssets: [{0}]", string.Join(", ", paths));
            return paths;
        }

        public static AssetMoveResult OnWillMoveAsset(string oldPath, string newPath)
        {
            //Logger.Trace("OnWillMoveAsset:{0}->{1}", oldPath, newPath);
            return IsLocked(oldPath) || IsLocked(newPath) ? AssetMoveResult.FailedMove : AssetMoveResult.DidNotMove;
        }

        public static AssetDeleteResult OnWillDeleteAsset(string assetPath, RemoveAssetOptions option)
        {
            //Logger.Trace("OnWillDeleteAsset:{0}", assetPath);
            return IsLocked(assetPath) ? AssetDeleteResult.FailedDelete : AssetDeleteResult.DidNotDelete;
        }

        public static bool IsOpenForEdit(string assetPath, out string message)
        {
            //Logger.Trace("IsOpenForEdit:{0}", assetPath);
            var lck = GetLock(assetPath);
            message = lck.HasValue ? "File is locked for editing by " + lck.Value.User : null;
            return !lck.HasValue;
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

        private static bool IsLocked(string assetPath)
        {
            return GetLock(assetPath).HasValue;
        }

        private static GitLock? GetLock(string assetPath)
        {
            if (repository == null)
                return null;

            GitLock lck;
            var repositoryPath = environment.GetRepositoryPath(assetPath.ToNPath());
            if (!locks.TryGetValue(repositoryPath, out lck) || lck.User.Equals(loggedInUser))
                return null;
            return lck;
        }
    }
}
