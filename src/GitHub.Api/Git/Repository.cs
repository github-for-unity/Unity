using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading;

namespace GitHub.Unity
{
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    class Repository : IEquatable<Repository>, IRepository
    {
        private ConfigBranch? currentBranch;
        private IList<GitLock> currentLocks;
        private ConfigRemote? currentRemote;
        private GitStatus currentStatus;
        private Dictionary<string, ConfigBranch> localBranches = new Dictionary<string, ConfigBranch>();
        private Dictionary<string, Dictionary<string, ConfigBranch>> remoteBranches = new Dictionary<string, Dictionary<string, ConfigBranch>>();
        private Dictionary<string, ConfigRemote> remotes;
        private IRepositoryManager repositoryManager;
        public event Action<string> OnCurrentBranchChanged;
        public event Action<string> OnCurrentRemoteChanged;
        public event Action OnCurrentBranchUpdated;
        public event Action OnLocalBranchListChanged;
        public event Action<IEnumerable<GitLock>> OnLocksChanged;
        public event Action OnRemoteBranchListChanged;
        public event Action OnRepositoryInfoChanged;

        public event Action<GitStatus> OnStatusChanged;

        /// <summary>
        /// Initializes a new instance of the <see cref="Repository"/> class.
        /// </summary>
        /// <param name="name">The repository name.</param>
        /// <param name="localPath"></param>
        public Repository(string name, NPath localPath)
        {
            Guard.ArgumentNotNullOrWhiteSpace(name, nameof(name));
            Guard.ArgumentNotNull(localPath, nameof(localPath));

            Name = name;
            LocalPath = localPath;
            this.User = new User();
        }

        public void Initialize(IRepositoryManager repositoryManager)
        {
            Guard.ArgumentNotNull(repositoryManager, nameof(repositoryManager));

            this.repositoryManager = repositoryManager;

            repositoryManager.OnCurrentBranchUpdated += RepositoryManager_OnCurrentBranchUpdated;
            repositoryManager.OnCurrentRemoteUpdated += RepositoryManager_OnCurrentRemoteUpdated;
            repositoryManager.OnStatusUpdated += status => CurrentStatus = status;
            repositoryManager.OnLocksUpdated += locks => CurrentLocks = locks;
            repositoryManager.OnLocalBranchListUpdated += RepositoryManager_OnLocalBranchListUpdated;
            repositoryManager.OnRemoteBranchListUpdated += RepositoryManager_OnRemoteBranchListUpdated;
            repositoryManager.OnLocalBranchUpdated += RepositoryManager_OnLocalBranchUpdated;
            repositoryManager.OnLocalBranchAdded += RepositoryManager_OnLocalBranchAdded;
            repositoryManager.OnLocalBranchRemoved += RepositoryManager_OnLocalBranchRemoved;
            repositoryManager.OnRemoteBranchAdded += RepositoryManager_OnRemoteBranchAdded;
            repositoryManager.OnRemoteBranchRemoved += RepositoryManager_OnRemoteBranchRemoved;
            repositoryManager.OnGitUserLoaded += user => User = user;
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

        public ITask<List<GitLogEntry>> Log()
        {
            if (repositoryManager == null)
                return new FuncListTask<GitLogEntry>(TaskHelpers.GetCompletedTask(new List<GitLogEntry>()));

            return repositoryManager.Log();
        }

        public ITask CommitAllFiles(string message, string body)
        {
            return repositoryManager.CommitAllFiles(message, body);
        }

        public ITask CommitFiles(List<string> files, string message, string body)
        {
            return repositoryManager.CommitFiles(files, message, body);
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
            if (repositoryManager == null)
                return new ActionTask(TaskExtensions.CompletedTask);
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
            return Equals((IRepository)other);
        }

        public bool Equals(IRepository other)
        {
            if (ReferenceEquals(this, other))
                return true;
            return other != null &&
                object.Equals(LocalPath, other.LocalPath);
        }

        private void RepositoryManager_OnCurrentRemoteUpdated(ConfigRemote? remote)
        {
            if (!Nullable.Equals(currentRemote, remote))
            {
                currentRemote = remote;

                Logger.Trace("OnCurrentRemoteChanged: {0}", currentRemote.HasValue ? currentRemote.Value.ToString() : "[NULL]");
                OnCurrentRemoteChanged?.Invoke(currentRemote.HasValue ? currentRemote.Value.Name : null);

                UpdateRepositoryInfo();
            }
        }

        private void RepositoryManager_OnCurrentBranchUpdated(ConfigBranch? branch)
        {
            if (!Nullable.Equals(currentBranch, branch))
            {
                currentBranch = branch;

                Logger.Trace("OnCurrentBranchChanged: {0}", currentBranch.HasValue ? currentBranch.ToString() : "[NULL]");
                OnCurrentBranchChanged?.Invoke(currentBranch.HasValue ? currentBranch.Value.Name : null);
            }
        }

        private void RepositoryManager_OnLocalBranchUpdated(string name)
        {
            if (name == currentBranch?.Name)
            {
                Logger.Trace("OnCurrentBranchUpdated: {0}", name);
                OnCurrentBranchUpdated?.Invoke();
                Refresh();
            }
        }

        private void RepositoryManager_OnRemoteBranchListUpdated(Dictionary<string, ConfigRemote> updatedRemotes, Dictionary<string, Dictionary<string, ConfigBranch>> branches)
        {
            remotes = updatedRemotes;

            Remotes = remotes.Select(pair => GetGitRemote(pair.Value)).ToArray();

            remoteBranches = branches;

            Logger.Trace("OnRemoteBranchListChanged");
            OnRemoteBranchListChanged?.Invoke();
        }

        private void RepositoryManager_OnLocalBranchListUpdated(Dictionary<string, ConfigBranch> branches)
        {
            localBranches = branches;

            Logger.Trace("OnLocalBranchListChanged");
            OnLocalBranchListChanged?.Invoke();
        }

        private void UpdateRepositoryInfo()
        {
            if (CurrentRemote.HasValue)
            {
                CloneUrl = new UriString(CurrentRemote.Value.Url);
                Name = CloneUrl.RepositoryName;
                Logger.Trace("CloneUrl: {0}", CloneUrl.ToString());
            }
            else
            {
                CloneUrl = null;
                Name = LocalPath.FileName;
                Logger.Trace("CloneUrl: [NULL]");
            }

            OnRepositoryInfoChanged?.Invoke();
        }

        private void RepositoryManager_OnLocalBranchRemoved(string name)
        {
            if (localBranches.ContainsKey(name))
            {
                localBranches.Remove(name);

                Logger.Trace("OnLocalBranchListChanged");
                OnLocalBranchListChanged?.Invoke();
            }
            else
            {
                Logger.Warning("Branch {0} is not found", name);
            }
        }

        private void RepositoryManager_OnLocalBranchAdded(string name)
        {
            if (!localBranches.ContainsKey(name))
            {
                var branch = repositoryManager.Config.GetBranch(name);
                if (!branch.HasValue)
                {
                    branch = new ConfigBranch { Name = name };
                }
                localBranches.Add(name, branch.Value);

                Logger.Trace("OnLocalBranchListChanged");
                OnLocalBranchListChanged?.Invoke();
            }
            else
            {
                Logger.Warning("Branch {0} is already present", name);
            }
        }

        private void RepositoryManager_OnRemoteBranchAdded(string remote, string name)
        {
            Dictionary<string, ConfigBranch> branchList;
            if (remoteBranches.TryGetValue(remote, out branchList))
            {
                if (!branchList.ContainsKey(name))
                {
                    branchList.Add(name, new ConfigBranch { Name = name, Remote = remotes[remote] });

                    Logger.Trace("OnRemoteBranchListChanged");
                    OnRemoteBranchListChanged?.Invoke();
                }
                else
                {
                    Logger.Warning("Branch {0} is already present in Remote {1}", name, remote);
                }
            }
            else
            {
                Logger.Warning("Remote {0} is not found", remote);
            }
        }

        private void RepositoryManager_OnRemoteBranchRemoved(string remote, string name)
        {
            Dictionary<string, ConfigBranch> branchList;
            if (remoteBranches.TryGetValue(remote, out branchList))
            {
                if (localBranches.ContainsKey(name))
                {
                    localBranches.Remove(name);

                    Logger.Trace("OnRemoteBranchListChanged");
                    OnRemoteBranchListChanged?.Invoke();
                }
                else
                {
                    Logger.Warning("Branch {0} is not found in Remote {1}", name, remote);
                }
            }
            else
            {
                Logger.Warning("Remote {0} is not found", remote);
            }
        }

        private GitBranch GetLocalGitBranch(ConfigBranch x)
        {
            var name = x.Name;
            var trackingName = x.IsTracking ? x.Remote.Value.Name + "/" + name : "[None]";
            var isActive = name == currentBranch?.Name;

            return new GitBranch(name, trackingName, isActive);
        }

        private GitBranch GetRemoteGitBranch(ConfigBranch x)
        {
            var name = x.Remote.Value.Name + "/" + x.Name;
            var trackingName = "[None]";

            return new GitBranch(name, trackingName, false);
        }

        private GitRemote GetGitRemote(ConfigRemote configRemote)
        {
            return new GitRemote { Name = configRemote.Name, Url = configRemote.Url };
        }

        public IList<GitRemote> Remotes { get; private set; }

        public IEnumerable<GitBranch> LocalBranches => localBranches.Values.Select(GetLocalGitBranch);

        public IEnumerable<GitBranch> RemoteBranches => remoteBranches.Values.SelectMany(x => x.Values).Select(GetRemoteGitBranch);

        public GitBranch? CurrentBranch
        {
            get
            {
                if (currentBranch != null)
                {
                    return GetLocalGitBranch(currentBranch.Value);
                }

                return null;
            }
        }

        public string CurrentBranchName => currentBranch?.Name;

        public GitRemote? CurrentRemote
        {
            get
            {
                if (currentRemote != null)
                {
                    return GetGitRemote(currentRemote.Value);
                }

                return null;
            }
        }

        public UriString CloneUrl { get; private set; }

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
            CurrentBranch,
            CurrentRemote);

        public GitStatus CurrentStatus
        {
            get { return currentStatus; }
            private set
            {
                currentStatus = value;
                Logger.Trace("OnStatusChanged: {0}", value.ToString());
                OnStatusChanged?.Invoke(value);
            }
        }

        public IUser User { get; set; }

        public IList<GitLock> CurrentLocks
        {
            get { return currentLocks; }
            private set
            {
                Logger.Trace("OnLocksChanged: {0}", value.ToString());
                currentLocks = value;
                OnLocksChanged?.Invoke(value);
            }
        }

        protected static ILogging Logger { get; } = Logging.GetLogger<Repository>();
    }

    public interface IUser
    {
        string Name { get; set; }
        string Email { get; set; }
    }

    [Serializable]
    class User : IUser
    {
        public override string ToString()
        {
            return String.Format("Name: {0} Email: {1}", Name, Email);
        }

        public string Name { get; set; }
        public string Email { get; set; }
    }
}