using System;
using System.Collections.Generic;

namespace GitHub.Unity
{
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
        ConfigRemote? CurrentConfigRemote { get; set; }
        ConfigBranch? CurentConfigBranch { get; set; }
    }

    public interface IRepositoryInfoCache : IManagedCache, IRepositoryInfo
    {
        GitRemote? CurrentGitRemote { get; set; }
        GitBranch? CurentGitBranch { get; set; }
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
