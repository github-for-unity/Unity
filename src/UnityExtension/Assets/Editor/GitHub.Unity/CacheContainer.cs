using GitHub.Logging;
using System;

namespace GitHub.Unity
{
    public class CacheContainer : ICacheContainer
    {
        private static ILogging Logger = LogHelper.GetLogger<CacheContainer>();

        private IRepositoryInfoCache repositoryInfoCache;

        private IBranchCache branchCache;

        private IGitLocksCache gitLocksCache;

        private IGitLogCache gitLogCache;

        private IGitTrackingStatusCache gitTrackingStatusCache;

        private IGitStatusEntriesCache gitStatusEntriesCache;

        private IGitUserCache gitUserCache;

        public event Action<CacheType> CacheInvalidated;

        public event Action<CacheType, DateTimeOffset> CacheUpdated;

        private IManagedCache GetManagedCache(CacheType cacheType)
        {
            switch (cacheType)
            {
                case CacheType.Branches:
                    return BranchCache;

                case CacheType.GitLog:
                    return GitLogCache;

                case CacheType.GitAheadBehind:
                    return GitTrackingStatusCache;

                case CacheType.GitStatus:
                    return GitStatusEntriesCache;

                case CacheType.GitLocks:
                    return GitLocksCache;

                case CacheType.GitUser:
                    return GitUserCache;

                default:
                    throw new ArgumentOutOfRangeException("cacheType", cacheType, null);
            }
        }

        public void Validate(CacheType cacheType)
        {
            GetManagedCache(cacheType).ValidateData();
        }

        public void ValidateAll()
        {
            RepositoryInfoCache.ValidateData();
            BranchCache.ValidateData();
            GitLogCache.ValidateData();
            GitTrackingStatusCache.ValidateData();
            GitLocksCache.ValidateData();
            GitUserCache.ValidateData();
        }

        public void Invalidate(CacheType cacheType)
        {
            GetManagedCache(cacheType).InvalidateData();
        }

        public void InvalidateAll()
        {
            RepositoryInfoCache.InvalidateData();
            BranchCache.InvalidateData();
            GitLogCache.InvalidateData();
            GitTrackingStatusCache.InvalidateData();
            GitLocksCache.InvalidateData();
            GitUserCache.InvalidateData();
        }

        public IRepositoryInfoCache RepositoryInfoCache
        {
            get
            {
                if (repositoryInfoCache == null)
                {
                    repositoryInfoCache = Unity.RepositoryInfoCache.Instance;
                    repositoryInfoCache.CacheInvalidated += () => OnCacheInvalidated(CacheType.RepositoryInfo);
                    repositoryInfoCache.CacheUpdated += datetime => OnCacheUpdated(CacheType.RepositoryInfo, datetime);
                }
                return repositoryInfoCache;
            }
        }

        public IBranchCache BranchCache
        {
            get
            {
                if (branchCache == null)
                {
                    branchCache = Unity.BranchCache.Instance;
                    branchCache.CacheInvalidated += () => OnCacheInvalidated(CacheType.Branches);
                    branchCache.CacheUpdated += datetime => OnCacheUpdated(CacheType.Branches, datetime);
                }
                return branchCache;
            }
        }

        public IGitLogCache GitLogCache
        {
            get
            {
                if (gitLogCache == null)
                {
                    gitLogCache = Unity.GitLogCache.Instance;
                    gitLogCache.CacheInvalidated += () => OnCacheInvalidated(CacheType.GitLog);
                    gitLogCache.CacheUpdated += datetime => OnCacheUpdated(CacheType.GitLog, datetime);
                }
                return gitLogCache;
            }
        }

        public IGitTrackingStatusCache GitTrackingStatusCache
        {
            get
            {
                if (gitTrackingStatusCache == null)
                {
                    gitTrackingStatusCache = Unity.GitTrackingStatusCache.Instance;
                    gitTrackingStatusCache.CacheInvalidated += () => OnCacheInvalidated(CacheType.GitAheadBehind);
                    gitTrackingStatusCache.CacheUpdated += datetime => OnCacheUpdated(CacheType.GitAheadBehind, datetime);
                }
                return gitTrackingStatusCache;
            }
        }

        public IGitStatusEntriesCache GitStatusEntriesCache
        {
            get
            {
                if (gitStatusEntriesCache == null)
                {
                    gitStatusEntriesCache = Unity.GitStatusEntriesCache.Instance;
                    gitStatusEntriesCache.CacheInvalidated += () => OnCacheInvalidated(CacheType.GitStatus);
                    gitStatusEntriesCache.CacheUpdated += datetime => OnCacheUpdated(CacheType.GitStatus, datetime);
                }
                return gitStatusEntriesCache;
            }
        }

        public IGitLocksCache GitLocksCache
        {
            get
            {
                if (gitLocksCache == null)
                {
                    gitLocksCache = Unity.GitLocksCache.Instance;
                    gitLocksCache.CacheInvalidated += () => OnCacheInvalidated(CacheType.GitLocks);
                    gitLocksCache.CacheUpdated += datetime => OnCacheUpdated(CacheType.GitLocks, datetime);
                }

                return gitLocksCache;
            }
        }

        public IGitUserCache GitUserCache
        {
            get
            {
                if (gitUserCache == null)
                {
                    gitUserCache = Unity.GitUserCache.Instance;
                    gitUserCache.CacheInvalidated += () => OnCacheInvalidated(CacheType.GitUser);
                    gitUserCache.CacheUpdated += datetime => OnCacheUpdated(CacheType.GitUser, datetime);
                }

                return gitUserCache;
            }
        }

        private void OnCacheUpdated(CacheType cacheType, DateTimeOffset datetime)
        {
            Logger.Trace("OnCacheUpdated cacheType:{0} datetime:{1}", cacheType, datetime);
            if (CacheUpdated != null)
            {
                CacheUpdated.Invoke(cacheType, datetime);
            }
        }

        private void OnCacheInvalidated(CacheType cacheType)
        {
            Logger.Trace("OnCacheInvalidated cacheType:{0}", cacheType);
            if (CacheInvalidated != null)
            {
                CacheInvalidated.Invoke(cacheType);
            }
        }
    }
}