using System;
using System.Collections.Generic;

namespace GitHub.Unity
{
    public enum CacheType
    {
        BranchCache,
        GitLogCache,
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

    public interface IGitLocksCache : IManagedCache
    {
        List<GitLock> GitLocks { get; }
    }

    public interface IGitUserCache : IManagedCache
    {
        User User { get; }
    }

    public interface IGitStatusCache : IManagedCache
    {
        GitStatus GitStatus { get; set; }
    }

    public interface ILocalConfigBranchDictionary : IDictionary<string, ConfigBranch>
    {

    }

    public interface IRemoteConfigBranchDictionary : IDictionary<string, IDictionary<string, ConfigBranch>>
    {

    }

    public interface IConfigRemoteDictionary : IDictionary<string, ConfigRemote>
    {

    }

    public interface IBranchCache : IManagedCache
    {
        GitRemote? CurrentGitRemote { get; set; }
        GitBranch? CurentGitBranch { get; set; }
        ConfigRemote? CurrentConfigRemote { get; set; }
        ConfigBranch? CurentConfigBranch { get; set; }
        
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
        void SetRemotes(IDictionary<string, ConfigRemote> remoteDictionary, IDictionary<string, IDictionary<string, ConfigBranch>> branchDictionary);
        void SetLocals(IDictionary<string, ConfigBranch> branchDictionary);
    }

    public interface IGitLogCache : IManagedCache
    {
        List<GitLogEntry> Log { get; set; }
    }
}
