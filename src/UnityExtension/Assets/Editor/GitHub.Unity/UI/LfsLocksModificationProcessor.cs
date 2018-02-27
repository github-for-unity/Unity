using System.Collections.Generic;
using System.Linq;
using GitHub.Logging;
using UnityEditor;

namespace GitHub.Unity
{
    class LfsLocksModificationProcessor : UnityEditor.AssetModificationProcessor
    {
        private static ILogging logger;
        private static ILogging Logger { get { return logger = logger ?? LogHelper.GetLogger<LfsLocksModificationProcessor>(); } }

        private static IRepository repository;

        private static List<GitLock> locks = new List<GitLock>();

        private static CacheUpdateEvent lastLocksChangedEvent;

        public static void Initialize(IRepository repo)
        {
            Logger.Trace("Initialize HasRepository:{0}", repo != null);

            repository = repo;

            if (repository != null)
            {
                repository.LocksChanged += RepositoryOnLocksChanged;
                repository.CheckLocksChangedEvent(lastLocksChangedEvent);
            }
        }

        private static void RepositoryOnLocksChanged(CacheUpdateEvent cacheUpdateEvent)
        {
            if (!lastLocksChangedEvent.Equals(cacheUpdateEvent))
            {
                lastLocksChangedEvent = cacheUpdateEvent;
                locks = repository.CurrentLocks;
            }
        }

        public static string[] OnWillSaveAssets(string[] paths)
        {
            Logger.Trace("OnWillSaveAssets: [{0}]", string.Join(", ", paths));
            return paths;
        }

        public static AssetMoveResult OnWillMoveAsset(string oldPath, string newPath)
        {
            Logger.Trace("OnWillMoveAsset:{0}->{1}", oldPath, newPath);

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
            Logger.Trace("OnWillDeleteAsset:{0}", assetPath);

            if (IsLocked(assetPath))
            {
                return AssetDeleteResult.FailedDelete;
            }
            return AssetDeleteResult.DidNotDelete;
        }

        public static bool IsOpenForEdit(string assetPath, out string message)
        {
            Logger.Trace("IsOpenForEdit:{0}", assetPath);

            if (IsLocked(assetPath))
            {
                message = "File is locked for editing!";
                return false;
            }
            else
            {
                message = null;
                return true;
            }
        }

        private static bool IsLocked(string assetPath)
        {
            if(repository != null)
            { 
                var repositoryPath = EntryPoint.Environment.GetRepositoryPath(assetPath.ToNPath());
                var gitLock = locks.FirstOrDefault(@lock => @lock.Path == repositoryPath);
                if (!gitLock.Equals(GitLock.Default))
                {
                    Logger.Trace("Lock found on: {0}", assetPath);   
                
                    //TODO: Check user and return true
                }
            }

            return false;
        }
    }
}