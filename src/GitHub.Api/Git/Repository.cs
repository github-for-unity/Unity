using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace GitHub.Unity
{
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    class Repository : IRepository, IEquatable<Repository>
    {
        private readonly IGitClient gitClient;
        private readonly IRepositoryManager repositoryManager;

        public event Action<GitStatus> OnRepositoryChanged;
        public event Action<string> OnActiveBranchChanged;
        public event Action<string> OnActiveRemoteChanged;
        public event Action OnLocalBranchListChanged;
        public event Action OnCommitChanged;
        public event Action<IEnumerable<GitLock>> OnLocksUpdated;

        public IEnumerable<GitBranch> LocalBranches => repositoryManager.LocalBranches.Values.Select(
            x => new GitBranch(x.Name, (x.IsTracking ? (x.Remote.Value.Name + "/" + x.Name) : "[None]"), x.Name == CurrentBranch));

        public IEnumerable<GitBranch> RemoteBranches => repositoryManager.RemoteBranches.Values.SelectMany(
            x => x.Values).Select(x => new GitBranch(x.Remote.Value.Name + "/" + x.Name, "[None]", false));

        /// <summary>
        /// Initializes a new instance of the <see cref="Repository"/> class.
        /// </summary>
        /// <param name="gitClient"></param>
        /// <param name="repositoryManager"></param>
        /// <param name="name">The repository name.</param>
        /// <param name="cloneUrl">The repository's clone URL.</param>
        /// <param name="localPath"></param>
        public Repository(IGitClient gitClient, IRepositoryManager repositoryManager, NPath localPath)
        {
            Guard.ArgumentNotNull(repositoryManager, nameof(repositoryManager));

            this.gitClient = gitClient;
            this.repositoryManager = repositoryManager;
            LocalPath = localPath;
            if (repositoryManager.ActiveBranch.HasValue)
                SetCurrentBranch(repositoryManager.ActiveBranch?.Name);
            if (repositoryManager.ActiveRemote.HasValue)
                SetCurrentRemote(repositoryManager.ActiveRemote);
            SetCloneUrl();

            repositoryManager.OnRepositoryChanged += RepositoryManager_OnRepositoryChanged;
            repositoryManager.OnActiveBranchChanged += SetCurrentBranch;
            repositoryManager.OnActiveRemoteChanged += SetCurrentRemote;
            repositoryManager.OnLocalBranchListChanged += RepositoryManager_OnLocalBranchListChanged;
            repositoryManager.OnHeadChanged += RepositoryManager_OnHeadChanged;
            repositoryManager.OnLocksUpdated += RepositoryManager_OnLocksUpdated;
            repositoryManager.OnRemoteOrTrackingChanged += SetCloneUrl;

        }

        public void Refresh()
        {
            repositoryManager.Refresh();
        }

        public ITask SetupRemote(string remote, string remoteUrl)
        {
            Guard.ArgumentNotNullOrWhiteSpace(remote, "remote");
            Guard.ArgumentNotNullOrWhiteSpace(remoteUrl, "remoteUrl");
            if (!CurrentRemote.HasValue || String.IsNullOrEmpty(CurrentRemote.Value.Name)) // there's no remote at all
            {
                return repositoryManager.RemoteAdd(remote, remoteUrl);
            }
            else
            {
                return repositoryManager.RemoteChange(remote, remoteUrl);
            }
        }

        public ITask Pull()
        {
            return repositoryManager.Pull(CurrentRemote.Value.Name, CurrentBranch);
        }

        public ITask Push()
        {
            return repositoryManager.Push(CurrentRemote.Value.Name, CurrentBranch);
        }

        public ITask Fetch()
        {
            return repositoryManager.Fetch(CurrentRemote.Value.Name);
        }
        
        public ITask Revert(string changeset)
        {
            return repositoryManager.Revert(changeset);
        }

        public ITask ListLocks()
        {
            return repositoryManager.ListLocks(false);
        }

        public ITask RequestLock(string file)
        {
            return repositoryManager.LockFile(file);
        }

        public ITask ReleaseLock(string file, bool force)
        {
            return repositoryManager.UnlockFile(file, force);
        }

        private void SetCloneUrl()
        {
            if (CurrentRemote.HasValue)
                CloneUrl = new UriString(CurrentRemote.Value.Url);
            else
                CloneUrl = null;
            Name = CloneUrl != null ? CloneUrl.RepositoryName : LocalPath.FileName;
        }

        private void SetCurrentRemote(ConfigRemote? remote)
        {
            CurrentRemote = remote;
            SetCloneUrl();
            OnActiveRemoteChanged?.Invoke(CurrentRemote.HasValue ? CurrentRemote.Value.Name : null);
        }

        private void SetCurrentBranch(string branch)
        {
            CurrentBranch = branch;
            OnActiveBranchChanged?.Invoke(CurrentBranch);
        }

        private void RepositoryManager_OnHeadChanged(string head)
        {
            OnCommitChanged?.Invoke();
        }

        private void RepositoryManager_OnLocalBranchListChanged()
        {
            OnLocalBranchListChanged?.Invoke();
        }

        private void RepositoryManager_OnRepositoryChanged(GitStatus status)
        {
            CurrentStatus = status;
            //Logger.Debug("Got STATUS 2 {0} {1}", OnRepositoryChanged, status);
            OnRepositoryChanged?.Invoke(CurrentStatus);
        }

        private void RepositoryManager_OnLocksUpdated(IEnumerable<GitLock> locks)
        {
            CurrentLocks = locks;
            OnLocksUpdated?.Invoke(CurrentLocks);
        }

        /// <summary>
        /// Note: We don't consider CloneUrl a part of the hash code because it can change during the lifetime
        /// of a repository. Equals takes care of any hash collisions because of this
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            return 17 * 23 + (Name?.GetHashCode() ?? 0) * 23 + (Owner?.GetHashCode() ?? 0) * 23 + (LocalPath?.GetHashCode() ?? 0);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(this, obj))
                return true;
            var other = obj as Repository;
            return Equals(other);
        }

        public bool Equals(Repository other)
        {
            return (Equals((IRepository)other));
        }

        public bool Equals(IRepository other)
        {
            if (ReferenceEquals(this, other))
                return true;
            return other != null &&
                String.Equals(Name, other.Name) &&
                String.Equals(Owner, other.Owner) &&
                String.Equals(CloneUrl, other.CloneUrl) &&
                object.Equals(LocalPath, other.LocalPath);
        }

        public override string ToString()
        {
            return DebuggerDisplay;
        }

        /// <summary>
        /// Gets the current branch of the repository.
        /// </summary>
        public string CurrentBranch { get; private set; }

        /// <summary>
        /// Gets the current remote of the repository.
        /// </summary>
        public ConfigRemote? CurrentRemote { get; private set; }

        public string Name { get; private set; }
        public UriString CloneUrl { get; private set; }
        public NPath LocalPath { get; private set; }
        public string Owner => CloneUrl?.Owner ?? null;
        public bool IsGitHub { get { return HostAddress.IsGitHubDotCom(CloneUrl); } }

        internal string DebuggerDisplay => String.Format(
            CultureInfo.InvariantCulture,
            "{0}\tOwner: {1} Name: {2} CloneUrl: {3} LocalPath: {4} Branch: {5} Remote: {6}",
            GetHashCode(),
            Owner,
            Name,
            CloneUrl,
            LocalPath,
            CurrentBranch,
            CurrentRemote?.Name
            );

        public GitStatus CurrentStatus { get; private set; }
        public IUser User { get; set; }
        public IEnumerable<GitLock> CurrentLocks { get; private set; }
        protected static ILogging Logger { get; } = Logging.GetLogger<Repository>();
    }

    interface IUser
    {
        string Name { get; set; }
        string Email { get; set; }
    }
    class User : IUser
    {
        public string Name { get; set; }
        public string Email { get; set; }
    }
}