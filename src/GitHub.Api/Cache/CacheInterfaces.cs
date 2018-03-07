using System;
using System.Collections.Generic;

namespace GitHub.Unity
{
    public enum CacheType
    {
        RepositoryInfoCache,
        BranchCache,
        GitLogCache,
        GitTrackingStatusCache,
        GitStatusEntriesCache,
        GitLocksCache,
        GitUserCache
    }

    public interface ICacheContainer
    {
        event Action<CacheType> CacheInvalidated;
        event Action<CacheType, DateTimeOffset> CacheUpdated;

        IBranchCache BranchCache { get; }
        IGitLogCache GitLogCache { get; }
        IGitTrackingStatusCache GitTrackingStatusCache { get; }
        IGitStatusEntriesCache GitStatusEntriesCache { get; }
        IGitLocksCache GitLocksCache { get; }
        IGitUserCache GitUserCache { get; }
        IRepositoryInfoCache RepositoryInfoCache { get; }
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

    public interface IGitLocksCache : IManagedCache
    {
        List<GitLock> GitLocks { get; set; }
    }

    public interface IGitUserCache : IManagedCache
    {
        string Name { get; set; }
        string Email { get; set; }
    }

    public interface IGitTrackingStatusCache : IManagedCache
    {
        int Ahead { get; set; }
        int Behind { get; set; }
    }

    public interface IGitStatusEntriesCache : IManagedCache
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
        ConfigRemote? CurrentConfigRemote { get; set; }
        ConfigBranch? CurrentConfigBranch { get; set; }
        
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

    public interface IRepositoryInfoCache : IManagedCache
    {
        GitRemote? CurrentGitRemote { get; set; }
        GitBranch? CurrentGitBranch { get; set; }
    }

    public interface IGitLogCache : IManagedCache
    {
        List<GitLogEntry> Log { get; set; }
    }
}
