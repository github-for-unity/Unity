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
        private static Dictionary<string, GitLock> locks = new Dictionary<string, GitLock>();
        private static CacheUpdateEvent lastLocksChangedEvent;

        public static void Initialize(IRepository repo, IPlatform plat)
        {
            //Logger.Trace("Initialize HasRepository:{0}", repo != null);
            repository = repo;
            platform = plat;

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

            var result = AssetMoveResult.DidNotMove;
            if (IsLocked(oldPath))
            {
                result = AssetMoveResult.FailedMove;
            }
            else if (IsLocked(newPath))
            {
                result = AssetMoveResult.FailedMove;
            }
            return result;
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
            if (lck.HasValue)
            {
                message = "File is locked for editing by " + lck.Value.User;
                return false;
            }
            else
            {
                message = null;
                return true;
            }
        }

        private static void RepositoryOnLocksChanged(CacheUpdateEvent cacheUpdateEvent)
        {
            if (!lastLocksChangedEvent.Equals(cacheUpdateEvent))
            {
                lastLocksChangedEvent = cacheUpdateEvent;
                locks = repository.CurrentLocks.ToDictionary(gitLock => gitLock.Path);
            }
        }

        private static bool IsLocked(string assetPath)
        {
            return GetLock(assetPath).HasValue;
        }

        private static GitLock? GetLock(string assetPath)
        {
            GitLock? gitLock = null;
            if (repository != null)
            {
                var repositoryPath = EntryPoint.Environment.GetRepositoryPath(assetPath.ToNPath());
                GitLock lck;
                if(locks.TryGetValue(repositoryPath, out lck))
                {
                    var user = platform.Keychain.Connections.FirstOrDefault();
                    if (!lck.User.Equals(user.Username))
                    {
                        gitLock = lck;
                    }
                    //Logger.Trace("Lock found on: {0}", assetPath);
                }
            }
            return gitLock;
        }
    }
}
