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
        public event Action OnLocalBranchChanged;
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

            repositoryManager.OnCurrentBranchUpdated += branch => CurrentBranch = branch;
            repositoryManager.OnCurrentRemoteUpdated += remote => CurrentRemote = remote;
            repositoryManager.OnStatusUpdated += status => CurrentStatus = status;
            repositoryManager.OnLocksUpdated += locks => CurrentLocks = locks;
            repositoryManager.OnLocalBranchListUpdated += OnRepositoryManager_OnLocalBranchListUpdated;
            repositoryManager.OnRemoteBranchListUpdated += OnRepositoryManager_OnRemoteBranchListUpdated;
            repositoryManager.OnLocalBranchUpdated += OnRepositoryManager_OnLocalBranchUpdated;
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

        private void OnRepositoryManager_OnLocalBranchUpdated(string name)
        {
            if (name == currentBranch?.Name)
            {
                OnLocalBranchChanged?.Invoke();
                Refresh();
            }
        }

        private void OnRepositoryManager_OnRemoteBranchListUpdated(Dictionary<string, ConfigRemote> updatedRemotes, Dictionary<string, Dictionary<string, ConfigBranch>> branches)
        {
            remotes = updatedRemotes;

            Remotes = remotes.Select(pair => new GitRemote {
                Name = pair.Value.Name,
                Url = pair.Value.Url
            }).ToArray();

            remoteBranches = branches;

            RemoteBranches = remoteBranches.Values
                .SelectMany(x => x.Values)
                .Select(x =>
                {
                    var name = x.Remote.Value.Name + "/" + x.Name;
                    return new GitBranch(name, "[None]", false);
                }).ToArray();


            Logger.Trace("OnRemoteBranchListChanged");
            OnRemoteBranchListChanged?.Invoke();
        }

        private void OnRepositoryManager_OnLocalBranchListUpdated(Dictionary<string, ConfigBranch> branches)
        {
            localBranches = branches;

            LocalBranches = localBranches.Values.Select(x =>
            {
                var argName = x.Name;

                var tracking = x.IsTracking
                    ? x.Remote.Value.Name + "/" + argName
                    : "[None]";

                var active = argName == CurrentBranch?.Name;

                return new GitBranch(argName, tracking, active);
            }).ToArray();

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

        public IList<GitRemote> Remotes { get; private set; }

        public IList<GitBranch> LocalBranches { get; private set; }

        public IList<GitBranch> RemoteBranches { get; private set; }

        public ConfigBranch? CurrentBranch
        {
            get { return currentBranch; }
            set
            {
                if (!Nullable.Equals(currentBranch, value))
                {
                    currentBranch = value;
                    Logger.Trace("OnCurrentBranchChanged: {0}", currentBranch.HasValue ? currentBranch.ToString() : "[NULL]");
                    OnCurrentBranchChanged?.Invoke(currentBranch.HasValue ? currentBranch.Value.Name : null);
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
                if (!Nullable.Equals(currentRemote, value))
                {
                    currentRemote = value;
                    Logger.Trace("OnCurrentRemoteChanged: {0}", currentRemote.HasValue ? currentRemote.Value.ToString() : "[NULL]");
                    OnCurrentRemoteChanged?.Invoke(currentRemote.HasValue ? currentRemote.Value.Name : null);
                    UpdateRepositoryInfo();
                }
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
            set
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