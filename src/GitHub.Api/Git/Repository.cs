using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;

namespace GitHub.Unity
{
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    class Repository : IRepository, IEquatable<Repository>
    {
        private IRepositoryManager repositoryManager;
        private ConfigBranch? currentBranch;
        private ConfigRemote? currentRemote;
        private GitStatus currentStatus;

        public event Action<GitStatus> OnStatusUpdated;
        public event Action<string> OnActiveBranchChanged;
        public event Action<string> OnActiveRemoteChanged;
        public event Action OnLocalBranchListChanged;
        public event Action OnHeadChanged;
        public event Action<IEnumerable<GitLock>> OnLocksUpdated;
        public event Action OnRepositoryInfoChanged;

        public IEnumerable<GitBranch> LocalBranches => repositoryManager.LocalBranches.Values.Select(
            x => new GitBranch(x.Name, (x.IsTracking ? (x.Remote.Value.Name + "/" + x.Name) : "[None]"), x.Name == CurrentBranch?.Name));

        public IEnumerable<GitBranch> RemoteBranches => repositoryManager.RemoteBranches.Values.SelectMany(
            x => x.Values).Select(x => new GitBranch(x.Remote.Value.Name + "/" + x.Name, "[None]", false));

        /// <summary>
        /// Initializes a new instance of the <see cref="Repository"/> class.
        /// </summary>
        /// <param name="repositoryManager"></param>
        /// <param name="name">The repository name.</param>
        /// <param name="cloneUrl">The repository's clone URL.</param>
        /// <param name="localPath"></param>
        public Repository(string name, NPath localPath)
        {
            Guard.ArgumentNotNullOrWhiteSpace(name, nameof(name));
            Guard.ArgumentNotNull(localPath, nameof(localPath));

            Name = name;
            LocalPath = localPath;
        }

        public void Initialize(IRepositoryManager repositoryManager)
        {
            Guard.ArgumentNotNull(repositoryManager, nameof(repositoryManager));

            this.repositoryManager = repositoryManager;
            repositoryManager.OnLocalBranchListChanged += RepositoryManager_OnLocalBranchListChanged;
            repositoryManager.OnCommitChanged += RepositoryManager_OnHeadChanged;
            repositoryManager.OnLocksUpdated += RepositoryManager_OnLocksUpdated;
            repositoryManager.OnStatusUpdated += status => CurrentStatus = status;
            repositoryManager.OnActiveBranchChanged += branch => CurrentBranch = branch;
            repositoryManager.OnActiveRemoteChanged += remote => CurrentRemote = remote;
        }

        public void Refresh()
        {
            repositoryManager?.Refresh();
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
            return repositoryManager.Pull(CurrentRemote.Value.Name, CurrentBranch?.Name);
        }

        public ITask Push()
        {
            return repositoryManager.Push(CurrentRemote.Value.Name, CurrentBranch?.Name);
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
            OnRepositoryInfoChanged?.Invoke();
        }

        private void RepositoryManager_OnActiveRemoteChanged(ConfigRemote? remote)
        {
            CurrentRemote = remote;
            SetCloneUrl();
            OnActiveRemoteChanged?.Invoke(CurrentRemote.HasValue ? CurrentRemote.Value.Name : null);
        }

        private void RepositoryManager_OnActiveBranchChanged(string branch)
        {
            CurrentBranch = branch;
            OnActiveBranchChanged?.Invoke(CurrentBranch);
        }

        private void RepositoryManager_OnHeadChanged()
        {
            OnHeadChanged?.Invoke();
        }

        private void RepositoryManager_OnLocalBranchListChanged()
        {
            OnLocalBranchListChanged?.Invoke();
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
            return LocalPath.GetHashCode();
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
                object.Equals(LocalPath, other.LocalPath);
        }

        public ConfigBranch? CurrentBranch
        {
            get { return currentBranch; }
            set
            {
                if (currentBranch.HasValue != value.HasValue || (currentBranch.HasValue && !currentBranch.Value.Equals(value.Value)))
                {
                    currentBranch = value;
                    Logger.Trace("OnActiveBranchChanged: {0}", value?.ToString() ?? "NULL");
                    OnActiveBranchChanged?.Invoke(currentBranch?.Name);
                }
            }
        }
        /// <summary>
        /// Gets the current branch of the repository.
        /// </summary>

        public string CurrentBranchName => currentBranch?.Name;

        /// <summary>
        /// Gets the current remote of the repository.
        /// </summary>
        public ConfigRemote? CurrentRemote
        {
            get { return currentRemote; }
            set
            {
                if (currentRemote.HasValue != value.HasValue || (currentRemote.HasValue && !currentRemote.Value.Equals(value.Value)))
                {
                    currentRemote = value;
                    Logger.Trace("OnActiveRemoteChanged: {0}", value?.ToString() ?? "NULL");
                    OnActiveRemoteChanged?.Invoke(value?.Name);
                }
            }
        }

        public UriString CloneUrl
        {
            get
            {
                if (CurrentRemote.HasValue && CurrentRemote.Value.Url != null)
                    return new UriString(CurrentRemote.Value.Url).ToRepositoryUrl();

                return null;
            }
        }


        public string Name { get; private set; }
        public NPath LocalPath { get; private set; }
        public string Owner => CloneUrl?.Owner ?? null;
        public bool IsGitHub { get { return HostAddress.IsGitHubDotCom(CloneUrl); } }

        internal string DebuggerDisplay => String.Format(
            CultureInfo.InvariantCulture,
            "{0} Owner: {1} Name: {2} CloneUrl: {3} LocalPath: {4} Branch: {5} Remote: {6}",
            GetHashCode(),
            Owner,
            Name,
            CloneUrl,
            LocalPath,
            GetHashCode());

        public GitStatus CurrentStatus
        {
            get { return currentStatus; }
            set
            {
                Logger.Trace("OnStatusUpdated: {0}", value.ToString());
                currentStatus = value;
                OnStatusUpdated?.Invoke(value);
            }
        }

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