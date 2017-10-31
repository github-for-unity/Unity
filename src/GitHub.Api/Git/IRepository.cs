using System;
using System.Collections.Generic;

namespace GitHub.Unity
{
    /// <summary>
    /// Represents a repository, either local or retreived via the GitHub API.
    /// </summary>
    public interface IRepository : IEquatable<IRepository>
    {
        void Initialize(IRepositoryManager repositoryManager);
        ITask CommitAllFiles(string message, string body);
        ITask CommitFiles(List<string> files, string message, string body);
        ITask SetupRemote(string remoteName, string remoteUrl);
        ITask Pull();
        ITask Push();
        ITask Fetch();
        ITask Revert(string changeset);
        ITask RequestLock(string file);
        ITask ReleaseLock(string file, bool force);

        void CheckBranchCacheEvent(CacheUpdateEvent cacheUpdateEvent);
        void CheckGitStatusCacheEvent(CacheUpdateEvent cacheUpdateEvent);
        void CheckGitLogCacheEvent(CacheUpdateEvent cacheUpdateEvent);
        void CheckGitLocksCacheEvent(CacheUpdateEvent cacheUpdateEvent);

        /// <summary>
        /// Gets the name of the repository.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets the repository clone URL.
        /// </summary>
        UriString CloneUrl { get; }

        /// <summary>
        /// Gets the name of the owner of the repository, taken from the clone URL.
        /// </summary>
        string Owner { get; }

        /// <summary>
        /// Gets the local path of the repository.
        /// </summary>
        NPath LocalPath { get; }
        bool IsGitHub { get; }
        /// <summary>
        /// Gets the current remote of the repository.
        /// </summary>
        GitRemote? CurrentRemote { get; }
        /// <summary>
        /// Gets the current branch of the repository.
        /// </summary>
        GitBranch? CurrentBranch { get; }
        GitStatus CurrentStatus { get; }
        GitRemote[] Remotes { get; }
        GitBranch[] LocalBranches { get; }
        GitBranch[] RemoteBranches { get; }
        IUser User { get; set; }
        List<GitLock> CurrentLocks { get; }
        string CurrentBranchName { get; }
        List<GitLogEntry> CurrentLog { get; }

        event Action<CacheUpdateEvent> GitStatusCacheUpdated;
        event Action<CacheUpdateEvent> GitLogCacheUpdated;
        event Action<CacheUpdateEvent> GitLockCacheUpdated;
        event Action<CacheUpdateEvent> BranchCacheUpdated;
    }
}