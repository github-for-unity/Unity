using System;
using System.Collections.Generic;

namespace GitHub.Unity
{
    /// <summary>
    /// Represents a repository, either local or retreived via the GitHub API.
    /// </summary>
    interface IRepository : IEquatable<IRepository>
    {
        void Initialize();
        void Refresh();
        ITask SetupRemote(string remoteName, string remoteUrl);
        ITask Pull();
        ITask Push();
        ITask Fetch();
        ITask Revert(string changeset);
        ITask ListLocks();
        ITask RequestLock(string file);
        ITask ReleaseLock(string file, bool force);

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
        ConfigRemote? CurrentRemote { get; }
        /// <summary>
        /// Gets the current branch of the repository.
        /// </summary>
        string CurrentBranch { get; }
        GitStatus CurrentStatus { get; }
        IEnumerable<GitBranch> LocalBranches { get; }
        IEnumerable<GitBranch> RemoteBranches { get; }
        IUser User { get; set; }
        IEnumerable<GitLock> CurrentLocks { get; }

        event Action<GitStatus> OnStatusUpdated;
        event Action<string> OnActiveBranchChanged;
        event Action<string> OnActiveRemoteChanged;
        event Action OnLocalBranchListChanged;
        event Action OnHeadChanged;
        event Action<IEnumerable<GitLock>> OnLocksUpdated;
        event Action OnRepositoryInfoChanged;
    }
}