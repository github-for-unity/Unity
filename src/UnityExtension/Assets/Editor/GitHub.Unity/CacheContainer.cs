using System;

namespace GitHub.Unity
{
    public class CacheContainer : ICacheContainer
    {
        private static ILogging Logger = Logging.GetLogger<CacheContainer>();

        private IBranchCache branchCache;

        private IGitLocksCache gitLocksCache;

        private IGitLogCache gitLogCache;

        private IGitStatusCache gitStatusCache;

        private IGitUserCache gitUserCache;

        private IRepositoryInfoCache repositoryInfoCache;

        public event Action<CacheType> CacheInvalidated;

        public event Action<CacheType, DateTimeOffset> CacheUpdated;

        private IManagedCache GetManagedCache(CacheType cacheType)
        {
            switch (cacheType)
            {
                case CacheType.BranchCache:
                    return BranchCache;

                case CacheType.GitLogCache:
                    return GitLogCache;

                case CacheType.RepositoryInfoCache:
                    return RepositoryInfoCache;

                case CacheType.GitStatusCache:
                    return GitStatusCache;

                case CacheType.GitLocksCache:
                    return GitLocksCache;

                case CacheType.GitUserCache:
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
            BranchCache.ValidateData();
            GitLogCache.ValidateData();
            RepositoryInfoCache.ValidateData();
            GitStatusCache.ValidateData();
            GitLocksCache.ValidateData();
            GitUserCache.ValidateData();
        }

        public void Invalidate(CacheType cacheType)
        {
            GetManagedCache(cacheType).InvalidateData();
        }

        public void InvalidateAll()
        {
            BranchCache.InvalidateData();
            GitLogCache.InvalidateData();
            RepositoryInfoCache.InvalidateData();
            GitStatusCache.InvalidateData();
            GitLocksCache.InvalidateData();
            GitUserCache.InvalidateData();
        }

        public IBranchCache BranchCache
        {
            get
            {
                if (branchCache == null)
                {
                    branchCache = Unity.BranchCache.Instance;
                    branchCache.CacheInvalidated += () => OnCacheInvalidated(CacheType.BranchCache);
                    branchCache.CacheUpdated += datetime => OnCacheUpdated(CacheType.BranchCache, datetime);
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
                    gitLogCache.CacheInvalidated += () => OnCacheInvalidated(CacheType.GitLogCache);
                    gitLogCache.CacheUpdated += datetime => OnCacheUpdated(CacheType.GitLogCache, datetime);
                }
                return gitLogCache;
            }
        }

        public IRepositoryInfoCache RepositoryInfoCache
        {
            get
            {
                if (repositoryInfoCache == null)
                {
                    repositoryInfoCache = Unity.RepositoryInfoCache.Instance;
                    repositoryInfoCache.CacheInvalidated += () => OnCacheInvalidated(CacheType.RepositoryInfoCache);
                    repositoryInfoCache.CacheUpdated += datetime => OnCacheUpdated(CacheType.RepositoryInfoCache, datetime);
                }
                return repositoryInfoCache;
            }
        }

        public IGitStatusCache GitStatusCache
        {
            get
            {
                if (gitStatusCache == null)
                {
                    gitStatusCache = Unity.GitStatusCache.Instance;
                    gitStatusCache.CacheInvalidated += () => OnCacheInvalidated(CacheType.GitStatusCache);
                    gitStatusCache.CacheUpdated += datetime => OnCacheUpdated(CacheType.GitStatusCache, datetime);
                }
                return gitStatusCache;
            }
        }

        public IGitLocksCache GitLocksCache
        {
            get
            {
                if (gitLocksCache == null)
                {
                    gitLocksCache = Unity.GitLocksCache.Instance;
                    gitLocksCache.CacheInvalidated += () => OnCacheInvalidated(CacheType.GitLocksCache);
                    gitLocksCache.CacheUpdated += datetime => OnCacheUpdated(CacheType.GitLocksCache, datetime);
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
                    gitUserCache.CacheInvalidated += () => OnCacheInvalidated(CacheType.GitUserCache);
                    gitUserCache.CacheUpdated += datetime => OnCacheUpdated(CacheType.GitUserCache, datetime);
                }

                return gitUserCache;
            }
        }

        private void OnCacheUpdated(CacheType cacheType, DateTimeOffset datetime)
        {
            //Logger.Trace("OnCacheUpdated cacheType:{0} datetime:{1}", cacheType, datetime);
            if (CacheUpdated != null)
            {
                CacheUpdated.Invoke(cacheType, datetime);
            }
        }

        private void OnCacheInvalidated(CacheType cacheType)
        {
            //Logger.Trace("OnCacheInvalidated cacheType:{0}", cacheType);
            if (CacheInvalidated != null)
            {
                CacheInvalidated.Invoke(cacheType);
            }
        }
    }
}