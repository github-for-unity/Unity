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
        private readonly IRepositoryManager repositoryManager;
        private GitStatus currentStatus;

        public event Action<GitStatus> OnRepositoryChanged;
        public event Action<string> OnActiveBranchChanged;
        public event Action<string> OnActiveRemoteChanged;
        public event Action OnLocalBranchListChanged;
        public event Action OnCommitChanged;

        public IEnumerable<GitBranch> LocalBranches => repositoryManager.LocalBranches.Values.Select(
            x => new GitBranch(x.Name, (x.IsTracking ? (x.Remote.Value.Name + "/" + x.Name) : "[None]"), x.Name == CurrentBranch));

        public IEnumerable<GitBranch> RemoteBranches => repositoryManager.RemoteBranches.Values.SelectMany(
            x => x.Values).Select(x => new GitBranch(x.Remote.Value.Name + "/" + x.Name, "[None]", false));


        /// <summary>
        /// Initializes a new instance of the <see cref="Repository"/> class.
        /// </summary>
        /// <param name="name">The repository name.</param>
        /// <param name="cloneUrl">The repository's clone URL.</param>
        /// <param name="localPath"></param>
        public Repository(IRepositoryManager repositoryManager, string name, UriString cloneUrl, string localPath, IUser user)
        {
            Guard.ArgumentNotNull(repositoryManager, nameof(repositoryManager));
            Guard.ArgumentNotNullOrWhiteSpace(name, nameof(name));
            Guard.ArgumentNotNull(cloneUrl, nameof(cloneUrl));

            this.repositoryManager = repositoryManager;
            Name = name;
            CloneUrl = cloneUrl;
            LocalPath = localPath;
            User = user;

            repositoryManager.OnRepositoryChanged += RepositoryManager_OnRepositoryChanged;
            repositoryManager.OnActiveBranchChanged += RepositoryManager_OnActiveBranchChanged;
            repositoryManager.OnActiveRemoteChanged += RepositoryManager_OnActiveRemoteChanged;
            repositoryManager.OnLocalBranchListChanged += RepositoryManager_OnLocalBranchListChanged;
            repositoryManager.OnHeadChanged += RepositoryManager_OnHeadChanged;

            if (String.IsNullOrEmpty(CloneUrl))
            {
                repositoryManager.OnRemoteOrTrackingChanged += RepositoryManager_OnRemoteOrTrackingChanged; ;
            }
            
        }

        public void Refresh()
        {
            repositoryManager.Refresh();
        }

        private void RepositoryManager_OnRemoteOrTrackingChanged()
        {
            if (String.IsNullOrEmpty(CloneUrl))
            {
                var remote = repositoryManager.Config.GetRemotes()
                               .Where(x => HostAddress.Create(new UriString(x.Url).ToRepositoryUri()).IsGitHubDotCom())
                               .FirstOrDefault();
                if (remote.Url != null)
                    CloneUrl = new UriString(remote.Url).ToRepositoryUrl();
                repositoryManager.OnRemoteOrTrackingChanged -= RepositoryManager_OnRemoteOrTrackingChanged;
            }
        }

        private void RepositoryManager_OnHeadChanged()
        {
            OnCommitChanged?.Invoke();
        }

        public void Pull(ITaskResultDispatcher<string> resultDispatcher)
        {
            repositoryManager.Pull(resultDispatcher, CurrentRemote, CurrentBranch);
        }

        public void Push(ITaskResultDispatcher<string> resultDispatcher)
        {
            repositoryManager.Push(resultDispatcher, CurrentRemote, CurrentBranch);
        }

        private void RepositoryManager_OnLocalBranchListChanged()
        {
            OnLocalBranchListChanged?.Invoke();
        }

        private void RepositoryManager_OnActiveRemoteChanged()
        {
            OnActiveRemoteChanged?.Invoke(CurrentRemote);
        }

        private void RepositoryManager_OnActiveBranchChanged()
        {
            OnActiveBranchChanged?.Invoke(CurrentBranch);
        }

        private void RepositoryManager_OnRepositoryChanged(GitStatus status)
        {
            currentStatus = status;
            //Logger.Debug("Got STATUS 2 {0} {1}", OnRepositoryChanged, status);
            OnRepositoryChanged?.Invoke(status);
        }

        /// <summary>
        /// Note: We don't consider CloneUrl a part of the hash code because it can change during the lifetime
        /// of a repository. Equals takes care of any hash collisions because of this
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            return 17 * 23 + (Name?.GetHashCode() ?? 0) * 23 + (Owner?.GetHashCode() ?? 0) * 23 + (LocalPath?.TrimEnd('\\').ToUpperInvariant().GetHashCode() ?? 0);
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
                String.Equals(LocalPath?.TrimEnd('\\'), other.LocalPath?.TrimEnd('\\'), StringComparison.CurrentCultureIgnoreCase);
        }

        /// <summary>
        /// Gets the current branch of the repository.
        /// </summary>
        public string CurrentBranch
        {
            get
            {
                return repositoryManager.ActiveBranch?.Name;
            }
        }

        /// <summary>
        /// Gets the current remote of the repository.
        /// </summary>
        public string CurrentRemote
        {
            get
            {
                return repositoryManager.ActiveRemote?.Name;
            }
        }

        public string Name { get; private set; }
        public UriString CloneUrl { get; private set; }
        public string LocalPath { get; private set; }
        public string Owner => CloneUrl?.Owner ?? string.Empty;
        public bool IsGitHub { get { return CloneUrl != ""; } }

        internal string DebuggerDisplay => String.Format(
            CultureInfo.InvariantCulture,
            "{4}\tOwner: {0} Name: {1} CloneUrl: {2} LocalPath: {3}",
            Owner,
            Name,
            CloneUrl,
            LocalPath,
            GetHashCode());

        public GitStatus CurrentStatus => currentStatus;
        public IUser User { get; set; }
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