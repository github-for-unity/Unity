using System;
using System.Collections.Generic;

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
                    throw new ArgumentOutOfRangeException(nameof(cacheType), cacheType, null);
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
            get { return branchCache; }
            set
            {
                if (branchCache == null)
                {
                    branchCache = value;
                    branchCache.CacheInvalidated += () => OnCacheInvalidated(CacheType.BranchCache);
                    branchCache.CacheUpdated += datetime => OnCacheUpdated(CacheType.BranchCache, datetime);
                }
            }
        }

        public IGitLogCache GitLogCache
        {
            get { return gitLogCache; }
            set
            {
                if (gitLogCache == null)
                {
                    gitLogCache = value;
                    gitLogCache.CacheInvalidated += () => OnCacheInvalidated(CacheType.GitLogCache);
                    gitLogCache.CacheUpdated += datetime => OnCacheUpdated(CacheType.GitLogCache, datetime);
                }
            }
        }

        public IRepositoryInfoCache RepositoryInfoCache
        {
            get { return repositoryInfoCache; }
            set
            {
                if (repositoryInfoCache == null)
                {
                    repositoryInfoCache = value;
                    repositoryInfoCache.CacheInvalidated += () => OnCacheInvalidated(CacheType.RepositoryInfoCache);
                    repositoryInfoCache.CacheUpdated += datetime => OnCacheUpdated(CacheType.RepositoryInfoCache, datetime);
                }
            }
        }

        public IGitStatusCache GitStatusCache
        {
            get { return gitStatusCache; }
            set
            {
                if (gitStatusCache == null)
                {
                    gitStatusCache = value;
                    gitStatusCache.CacheInvalidated += () => OnCacheInvalidated(CacheType.GitStatusCache);
                    gitStatusCache.CacheUpdated += datetime => OnCacheUpdated(CacheType.GitStatusCache, datetime);
                }
            }
        }

        public IGitLocksCache GitLocksCache
        {
            get { return gitLocksCache; }
            set
            {
                if (gitLocksCache == null)
                {
                    gitLocksCache = value;
                    gitLocksCache.CacheInvalidated += () => OnCacheInvalidated(CacheType.GitLocksCache);
                    gitLocksCache.CacheUpdated += datetime => OnCacheUpdated(CacheType.GitLocksCache, datetime);
                }
            }
        }

        public IGitUserCache GitUserCache
        {
            get { return gitUserCache; }
            set
            {
                if (gitUserCache == null)
                {
                    gitUserCache = value;
                    gitUserCache.CacheInvalidated += () => OnCacheInvalidated(CacheType.GitUserCache);
                    gitUserCache.CacheUpdated += datetime => OnCacheUpdated(CacheType.GitUserCache, datetime);
                }
            }
        }

        private void OnCacheUpdated(CacheType cacheType, DateTimeOffset datetime)
        {
            Logger.Trace("OnCacheUpdated cacheType:{0} datetime:{1}", cacheType, datetime);
            CacheUpdated?.Invoke(cacheType, datetime);
        }

        private void OnCacheInvalidated(CacheType cacheType)
        {
            Logger.Trace("OnCacheInvalidated cacheType:{0}", cacheType);
            CacheInvalidated?.Invoke(cacheType);
        }
    }

    public enum CacheType
    {
        BranchCache,
        GitLogCache,
        RepositoryInfoCache,
        GitStatusCache,
        GitLocksCache,
        GitUserCache
    }

    public interface ICacheContainer
    {
        event Action<CacheType> CacheInvalidated;
        event Action<CacheType, DateTimeOffset> CacheUpdated;

        IBranchCache BranchCache { get; }
        IGitLogCache GitLogCache { get; }
        IRepositoryInfoCache RepositoryInfoCache { get; }
        IGitStatusCache GitStatusCache { get; }
        IGitLocksCache GitLocksCache { get; }
        IGitUserCache GitUserCache { get; }
        ITestCache TestCache { get; }
        void Validate(CacheType cacheType);
        void ValidateAll();
        void Invalidate(CacheType cacheType);
        void InvalidateAll();
    }

    public interface IManagedCache
    {
        event Action CacheInvalidated;
        event Action<DateTimeOffset> CacheUpdated;

        void ValidateData();
        void InvalidateData();

        DateTimeOffset LastUpdatedAt { get; }
        DateTimeOffset LastVerifiedAt { get; }
    }

    public interface IGitLocks
    {
        List<GitLock> GitLocks { get; }
    }

    public interface IGitLocksCache : IManagedCache, IGitLocks
    { }

    public interface IGitUser
    {
        User User { get; }
    }

    public interface ITestCacheItem
    { }

    public interface IGitUserCache : IManagedCache, IGitUser
    { }

    public interface IGitStatus
    {
        GitStatus GitStatus { get; }
    }

    public interface IGitStatusCache : IManagedCache, IGitStatus
    { }

    public interface IRepositoryInfo
    {
        ConfigRemote? CurrentRemote { get; }
        ConfigBranch? CurentBranch { get; }
    }

    public interface IRepositoryInfoCache : IManagedCache, IRepositoryInfo
    {
        void UpdateData(ConfigRemote? gitRemoteUpdate);
        void UpdateData(ConfigBranch? gitBranchUpdate);
        void UpdateData(ConfigRemote? gitRemoteUpdate, ConfigBranch? gitBranchUpdate);
    }

    public interface IBranch
    {
        void UpdateData(List<GitBranch> localBranchUpdate, List<GitBranch> remoteBranchUpdate);
        List<GitBranch> LocalBranches { get; }
        List<GitBranch> RemoteBranches { get; }
    }

    public interface IBranchCache : IManagedCache, IBranch
    { }

    public interface IGitLogCache : IManagedCache
    {
        List<GitLogEntry> Log { get; }
    }
}
