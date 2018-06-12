using System.Collections.Generic;
using System.Linq;
using GitHub.Logging;
using UnityEditor;

namespace GitHub.Unity
{
    class LfsLocksModificationProcessor : UnityEditor.AssetModificationProcessor
    {
        private static ILogging Logger = LogHelper.GetLogger<LfsLocksModificationProcessor>();
        private static IApplicationManager manager;
        private static IRepository Repository { get { return manager != null ? manager.Environment.Repository : null; } }
        private static bool HasRepository { get { return Repository != null && Repository.CurrentRemote.HasValue; } }
        private static IPlatform Platform { get { return manager != null ? manager.Platform : null; } }
        private static IEnvironment Environment { get { return manager != null ? manager.Environment : null; } }

        private static Dictionary<NPath, GitLock> locks = new Dictionary<NPath, GitLock>();
        private static CacheUpdateEvent lastLocksChangedEvent;
        private static string loggedInUser;

        public static void Initialize(IApplicationManager theManager)
        {
            DetachHandlers();
            manager = theManager;
            AttachHandlers();
        }

        private static bool EnsureInitialized()
        {
            if (locks == null)
                locks = new Dictionary<NPath, GitLock>();
            return HasRepository;
        }

        private static void AttachHandlers()
        {
            if (!HasRepository)
                return;
            Repository.LocksChanged += RepositoryOnLocksChanged;
            Platform.Keychain.ConnectionsChanged += UserMayHaveChanged;
            ValidateCachedData();
        }

        private static void DetachHandlers()
        {
            if (!HasRepository)
                return;
            Repository.LocksChanged -= RepositoryOnLocksChanged;
            Platform.Keychain.ConnectionsChanged -= UserMayHaveChanged;
        }

        private static void ValidateCachedData()
        {
            if (!HasRepository)
                return;
            Repository.CheckAndRaiseEventsIfCacheNewer(CacheType.GitLocks, lastLocksChangedEvent);
        }

        public static string[] OnWillSaveAssets(string[] paths)
        {
            return paths;
        }

        public static AssetMoveResult OnWillMoveAsset(string oldPath, string newPath)
        {
            if (!EnsureInitialized())
                return AssetMoveResult.DidNotMove;
            return IsLocked(oldPath) || IsLocked(newPath) ? AssetMoveResult.FailedMove : AssetMoveResult.DidNotMove;
        }

        public static AssetDeleteResult OnWillDeleteAsset(string assetPath, RemoveAssetOptions option)
        {
            if (!EnsureInitialized())
                return AssetDeleteResult.DidNotDelete;
            return IsLocked(assetPath) ? AssetDeleteResult.FailedDelete : AssetDeleteResult.DidNotDelete;
        }

        public static bool IsOpenForEdit(string assetPath, out string message)
        {
            message = null;
            if (!EnsureInitialized())
                return true;
            var lck = GetLock(assetPath);
            message = lck.HasValue ? "File is locked for editing by " + lck.Value.Owner : null;
            return !lck.HasValue;
        }

        private static void RepositoryOnLocksChanged(CacheUpdateEvent cacheUpdateEvent)
        {
            if (!lastLocksChangedEvent.Equals(cacheUpdateEvent))
            {
                lastLocksChangedEvent = cacheUpdateEvent;
                locks = Repository.CurrentLocks.ToDictionary(gitLock => gitLock.Path);
            }
        }

        private static void UserMayHaveChanged()
        {
            if (!EnsureInitialized())
                return;

            loggedInUser = Platform.Keychain.Connections.Select(x => x.Username).FirstOrDefault();
        }

        private static bool IsLocked(string assetPath)
        {
            return GetLock(assetPath).HasValue;
        }

        private static GitLock? GetLock(string assetPath)
        {
            GitLock lck;
            var repositoryPath = Environment.GetRepositoryPath(assetPath.ToNPath());
            if (!locks.TryGetValue(repositoryPath, out lck) || lck.Owner.Name.Equals(loggedInUser))
                return null;
            return lck;
        }
    }
}
