using System;
using System.Collections.Generic;

namespace GitHub.Unity
{
    public enum CacheType
    {
        None,
        RepositoryInfo,
        Branches,
        GitLog,
        GitAheadBehind,
        GitStatus,
        GitLocks,
        GitUser
    }

    public interface ICacheContainer : IDisposable
    {
        event Action<CacheType> CacheInvalidated;
        event Action<CacheType, DateTimeOffset> CacheUpdated;

        IBranchCache BranchCache { get; }
        IGitLogCache GitLogCache { get; }
        IGitAheadBehindCache GitTrackingStatusCache { get; }
        IGitStatusCache GitStatusEntriesCache { get; }
        IGitLocksCache GitLocksCache { get; }
        IGitUserCache GitUserCache { get; }
        IRepositoryInfoCache RepositoryInfoCache { get; }
        void ValidateAll();
        void InvalidateAll();
        IManagedCache GetCache(CacheType cacheType);
        void CheckAndRaiseEventsIfCacheNewer(CacheUpdateEvent cacheUpdateEvent);
    }

    public interface IManagedCache
    {
        event Action<CacheType> CacheInvalidated;
        event Action<CacheType, DateTimeOffset> CacheUpdated;

        bool ValidateData();
        void InvalidateData();

        DateTimeOffset LastUpdatedAt { get; }
        CacheType CacheType { get; }
    }

    public interface IGitLocksCache : IManagedCache
    {
        List<GitLock> GitLocks { get; set; }
    }

    public interface IGitUserCache : IManagedCache
    {
        string Name { get; set; }
        string Email { get; set; }
    }

    public interface IGitAheadBehindCache : IManagedCache
    {
        int Ahead { get; set; }
        int Behind { get; set; }
    }

    public interface IGitStatusCache : IManagedCache
    {
        List<GitStatusEntry> Entries { get; set; }
    }

    public interface ILocalConfigBranchDictionary : IDictionary<string, ConfigBranch>
    {

    }

    public interface IRemoteConfigBranchDictionary : IDictionary<string, Dictionary<string, ConfigBranch>>
    {

    }

    public interface IConfigRemoteDictionary : IDictionary<string, ConfigRemote>
    {

    }

    public interface IBranchCache : IManagedCache
    {
        GitBranch[] LocalBranches { get; set; }
        GitBranch[] RemoteBranches { get; set; }
        GitRemote[] Remotes { get; set; }

        ILocalConfigBranchDictionary LocalConfigBranches { get; }
        IRemoteConfigBranchDictionary RemoteConfigBranches { get; }
        IConfigRemoteDictionary ConfigRemotes { get; }
        
        void RemoveLocalBranch(string branch);
        void AddLocalBranch(string branch);
        void AddRemoteBranch(string remote, string branch);
        void RemoveRemoteBranch(string remote, string branch);
        void SetRemotes(Dictionary<string, ConfigRemote> remoteDictionary, Dictionary<string, Dictionary<string, ConfigBranch>> branchDictionary);
        void SetLocals(Dictionary<string, ConfigBranch> branchDictionary);
    }

    public interface IRepositoryInfoCacheData
    {
        GitRemote? CurrentGitRemote { get; }
        GitBranch? CurrentGitBranch { get; }
        ConfigRemote? CurrentConfigRemote { get; }
        ConfigBranch? CurrentConfigBranch { get; }
    }

    public interface IRepositoryInfoCache : IManagedCache, IRepositoryInfoCacheData, ICanUpdate<IRepositoryInfoCacheData>
    {
    }

    public interface IGitLogCache : IManagedCache
    {
        List<GitLogEntry> Log { get; set; }
    }

    public interface ICanUpdate<T>
    {
        void UpdateData(T data);
    }
}
