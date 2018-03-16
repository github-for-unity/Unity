using GitHub.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading;

namespace GitHub.Unity
{
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    sealed class Repository : IEquatable<Repository>, IRepository
    {
        private static ILogging Logger = LogHelper.GetLogger<Repository>();

        private IRepositoryManager repositoryManager;
        private ITaskManager taskManager;
        private ICacheContainer cacheContainer;
        private UriString cloneUrl;
        private string name;
        private HashSet<CacheType> cacheInvalidationRequests = new HashSet<CacheType>();
        private Dictionary<CacheType, Action<CacheUpdateEvent>> cacheUpdateEvents;

        public event Action<CacheUpdateEvent> LogChanged;
        public event Action<CacheUpdateEvent> TrackingStatusChanged;
        public event Action<CacheUpdateEvent> StatusEntriesChanged;
        public event Action<CacheUpdateEvent> CurrentBranchChanged;
        public event Action<CacheUpdateEvent> CurrentRemoteChanged;
        public event Action<CacheUpdateEvent> CurrentBranchAndRemoteChanged;
        public event Action<CacheUpdateEvent> LocalBranchListChanged;
        public event Action<CacheUpdateEvent> LocksChanged;
        public event Action<CacheUpdateEvent> RemoteBranchListChanged;
        public event Action<CacheUpdateEvent> LocalAndRemoteBranchListChanged;

        /// <summary>
        /// Initializes a new instance of the <see cref="Repository"/> class.
        /// </summary>
        /// <param name="localPath"></param>
        /// <param name="container"></param>
        public Repository(NPath localPath, ICacheContainer container)
        {
            Guard.ArgumentNotNull(localPath, nameof(localPath));

            LocalPath = localPath;

            cacheUpdateEvents = new Dictionary<CacheType, Action<CacheUpdateEvent>>
            {
                { CacheType.Branches, cacheUpdateEvent => {
                    LocalBranchListChanged?.Invoke(cacheUpdateEvent);
                    RemoteBranchListChanged?.Invoke(cacheUpdateEvent);
                    LocalAndRemoteBranchListChanged?.Invoke(cacheUpdateEvent);
                }},
                { CacheType.GitAheadBehind, c => TrackingStatusChanged?.Invoke(c) },
                { CacheType.GitLocks, c => LocksChanged?.Invoke(c) },
                { CacheType.GitLog, c => LogChanged?.Invoke(c) },
                { CacheType.GitStatus, c => StatusEntriesChanged?.Invoke(c) },
                { CacheType.GitUser, cacheUpdateEvent => { } },
                { CacheType.RepositoryInfo, cacheUpdateEvent => {
                    CurrentBranchChanged?.Invoke(cacheUpdateEvent);
                    CurrentRemoteChanged?.Invoke(cacheUpdateEvent);
                    CurrentBranchAndRemoteChanged?.Invoke(cacheUpdateEvent);
                }},
            };

            cacheContainer = container;
            cacheContainer.CacheInvalidated += CacheHasBeenInvalidated;
            cacheContainer.CacheUpdated += (cacheType, offset) =>
            {
                cacheUpdateEvents[cacheType](new CacheUpdateEvent(cacheType, offset));
            };
        }

        public void Initialize(IRepositoryManager repositoryManager, ITaskManager taskManager)
        {
            //Logger.Trace("Initialize");
            Guard.ArgumentNotNull(repositoryManager, nameof(repositoryManager));
            Guard.ArgumentNotNull(taskManager, nameof(taskManager));

            this.taskManager = taskManager;
            this.repositoryManager = repositoryManager;
            this.repositoryManager.CurrentBranchUpdated += RepositoryManagerOnCurrentBranchUpdated;
            this.repositoryManager.GitStatusUpdated += RepositoryManagerOnGitStatusUpdated;
            this.repositoryManager.GitAheadBehindStatusUpdated += RepositoryManagerOnGitAheadBehindStatusUpdated;
            this.repositoryManager.GitLogUpdated += RepositoryManagerOnGitLogUpdated;
            this.repositoryManager.GitLocksUpdated += RepositoryManagerOnGitLocksUpdated;
            this.repositoryManager.LocalBranchesUpdated += RepositoryManagerOnLocalBranchesUpdated;
            this.repositoryManager.RemoteBranchesUpdated += RepositoryManagerOnRemoteBranchesUpdated;
            this.repositoryManager.DataNeedsRefreshing += RefreshCache;
        }

        public void Start()
        {
            foreach (var cacheType in cacheInvalidationRequests)
            {
                RefreshCache(cacheType);
            }
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

        public ITask CommitAllFiles(string message, string body) => repositoryManager.CommitAllFiles(message, body);
        public ITask CommitFiles(List<string> files, string message, string body) => repositoryManager.CommitFiles(files, message, body);
        public ITask Pull() => repositoryManager.Pull(CurrentRemote.Value.Name, CurrentBranch?.Name);
        public ITask Push() => repositoryManager.Push(CurrentRemote.Value.Name, CurrentBranch?.Name);
        public ITask Fetch() => repositoryManager.Fetch(CurrentRemote.Value.Name);
        public ITask Revert(string changeset) => repositoryManager.Revert(changeset);
        public ITask RequestLock(string file) => repositoryManager.LockFile(file);
        public ITask ReleaseLock(string file, bool force) => repositoryManager.UnlockFile(file, force);
        public ITask DiscardChanges(GitStatusEntry[] gitStatusEntry) => repositoryManager.DiscardChanges(gitStatusEntry);

        public void CheckAndRaiseEventsIfCacheNewer(CacheType cacheType, CacheUpdateEvent cacheUpdateEvent) => cacheContainer.CheckAndRaiseEventsIfCacheNewer(cacheType, cacheUpdateEvent);


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

            return other != null && object.Equals(LocalPath, other.LocalPath);
        }

        private void RefreshCache(CacheType cacheType)
        {
            taskManager.RunInUI(() =>
            {
                var cache = cacheContainer.GetCache(cacheType);
                // if the cache has valid data, we need to force an invalidation to refresh it
                // if it doesn't have valid data, it will trigger an invalidation automatically
                if (cache.ValidateData())
                    cache.InvalidateData();
            });
        }

        private void CacheHasBeenInvalidated(CacheType cacheType)
        {
            if (repositoryManager == null)
            {
                if (!cacheInvalidationRequests.Contains(cacheType))
                    cacheInvalidationRequests.Add(cacheType);
                return;
            }

            Logger.Trace($"CacheInvalidated {cacheType.ToString()}");
            switch (cacheType)
            {
                case CacheType.Branches:
                    repositoryManager?.UpdateBranches();
                    break;

                case CacheType.GitLog:
                    repositoryManager?.UpdateGitLog();
                    break;

                case CacheType.GitAheadBehind:
                    repositoryManager?.UpdateGitAheadBehindStatus();
                    break;

                case CacheType.GitLocks:
                    if (CurrentRemote != null)
                        repositoryManager?.UpdateLocks();
                    break;

                case CacheType.GitUser:
                    // user handles its own invalidation event
                    break;

                case CacheType.RepositoryInfo:
                    repositoryManager?.UpdateRepositoryInfo();
                    break;

                case CacheType.GitStatus:
                    repositoryManager?.UpdateGitStatus();
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(cacheType), cacheType, null);
            }
        }

        private void RepositoryManagerOnCurrentBranchUpdated(ConfigBranch? branch, ConfigRemote? remote)
        {
            taskManager.RunInUI(() =>
            {
                var data = new RepositoryInfoCacheData();
                data.CurrentConfigBranch = branch;
                data.CurrentGitBranch = branch.HasValue ? (GitBranch?)GetLocalGitBranch(branch.Value.name, branch.Value) : null;
                data.CurrentConfigRemote = remote;
                data.CurrentGitRemote = remote.HasValue ? (GitRemote?)GetGitRemote(remote.Value) : null;
                name = null;
                cloneUrl = null;
                cacheContainer.RepositoryInfoCache.UpdateData(data);
                var n = Name; // force refresh of the Name and CloneUrl property
                // update active state in local branches
                cacheContainer.BranchCache.LocalBranches = LocalConfigBranches;
                // update tracking state in remote branches
                cacheContainer.BranchCache.RemoteBranches = RemoteConfigBranches;
            });
        }

        private void RepositoryManagerOnGitStatusUpdated(GitStatus gitStatus)
        {
            taskManager.RunInUI(() =>
            {
                cacheContainer.GitStatusEntriesCache.Entries = gitStatus.Entries;
                cacheContainer.GitTrackingStatusCache.Ahead = gitStatus.Ahead;
                cacheContainer.GitTrackingStatusCache.Behind = gitStatus.Behind;
            });
        }

        private void RepositoryManagerOnGitAheadBehindStatusUpdated(GitAheadBehindStatus aheadBehindStatus)
        {
            taskManager.RunInUI(() =>
            {
                cacheContainer.GitTrackingStatusCache.Ahead = aheadBehindStatus.Ahead;
                cacheContainer.GitTrackingStatusCache.Behind = aheadBehindStatus.Behind;
            });
        }

        private void RepositoryManagerOnGitLogUpdated(List<GitLogEntry> gitLogEntries)
        {
            taskManager.RunInUI(() => cacheContainer.GitLogCache.Log = gitLogEntries);
        }

        private void RepositoryManagerOnGitLocksUpdated(List<GitLock> gitLocks)
        {
            taskManager.RunInUI(() => cacheContainer.GitLocksCache.GitLocks = gitLocks);
        }

        private void RepositoryManagerOnRemoteBranchesUpdated(Dictionary<string, ConfigRemote> remotes,
            Dictionary<string, Dictionary<string, ConfigBranch>> branches)
        {
            taskManager.RunInUI(() => {
                cacheContainer.BranchCache.SetRemotes(remotes, branches);
                cacheContainer.BranchCache.Remotes = ConfigRemotes;
                cacheContainer.BranchCache.RemoteBranches = RemoteConfigBranches;
            });
        }

        private void RepositoryManagerOnLocalBranchesUpdated(Dictionary<string, ConfigBranch> branches)
        {
            taskManager.RunInUI(() => {
                cacheContainer.BranchCache.SetLocals(branches);
                cacheContainer.BranchCache.LocalBranches = LocalConfigBranches;
            });
        }

        private static GitBranch GetLocalGitBranch(string currentBranchName, ConfigBranch x)
        {
            var branchName = x.Name;
            var trackingName = x.IsTracking ? x.Remote.Value.Name + "/" + branchName : "[None]";
            var isActive = branchName == currentBranchName;
            return new GitBranch(branchName, trackingName, isActive);
        }


        private bool disposed;
        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (!disposed)
                {
                    disposed = true;
                }
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }


        private static GitBranch GetRemoteGitBranch(ConfigBranch x) => new GitBranch(x.Remote.Value.Name + "/" + x.Name, "[None]", false);
        private static GitRemote GetGitRemote(ConfigRemote configRemote) => new GitRemote(configRemote.Name, configRemote.Url);

        public GitRemote[] Remotes => cacheContainer.BranchCache.Remotes;
        public GitBranch[] LocalBranches => cacheContainer.BranchCache.LocalBranches;
        public GitBranch[] RemoteBranches => cacheContainer.BranchCache.RemoteBranches;
        private ConfigBranch? CurrentConfigBranch => cacheContainer.RepositoryInfoCache.CurrentConfigBranch;
        private ConfigRemote? CurrentConfigRemote => cacheContainer.RepositoryInfoCache.CurrentConfigRemote;
        public int CurrentAhead => cacheContainer.GitTrackingStatusCache.Ahead;
        public int CurrentBehind => cacheContainer.GitTrackingStatusCache.Behind;
        public List<GitStatusEntry> CurrentChanges => cacheContainer.GitStatusEntriesCache.Entries;
        public GitBranch? CurrentBranch => cacheContainer.RepositoryInfoCache.CurrentGitBranch;
        public string CurrentBranchName => CurrentConfigBranch?.Name;
        public GitRemote? CurrentRemote => cacheContainer.RepositoryInfoCache.CurrentGitRemote;
        public List<GitLogEntry> CurrentLog => cacheContainer.GitLogCache.Log;
        public List<GitLock> CurrentLocks => cacheContainer.GitLocksCache.GitLocks;

        public UriString CloneUrl
        {
            get
            {
                if (cloneUrl == null)
                {
                    var currentRemote = CurrentRemote;
                    if (currentRemote.HasValue && currentRemote.Value.Url != null)
                    {
                        cloneUrl = new UriString(currentRemote.Value.Url);
                    }
                }
                return cloneUrl;
            }
            private set
            {
                cloneUrl = value;
            }
        }

        public string Name
        {
            get
            {
                if (name == null)
                {
                    var url = CloneUrl;
                    if (url != null)
                    {
                        name = url.RepositoryName;
                    }
                    else
                    {
                        name = LocalPath.FileName;
                    }
                }
                return name;
            }
            private set { name = value; }
        }

        public NPath LocalPath { get; private set; }
        public string Owner => CloneUrl?.Owner ?? null;
        public bool IsGitHub => HostAddress.IsGitHubDotCom(CloneUrl);
        public bool IsBusy => repositoryManager?.IsBusy ?? false;

        internal string DebuggerDisplay => String.Format(CultureInfo.InvariantCulture,
            "{0} Owner: {1} Name: {2} CloneUrl: {3} LocalPath: {4} Branch: {5} Remote: {6}", GetHashCode(), Owner, Name,
            CloneUrl, LocalPath, CurrentBranch, CurrentRemote);

        private GitBranch[] RemoteConfigBranches => cacheContainer.BranchCache.RemoteConfigBranches.Values.SelectMany(x => x.Values).Select(GetRemoteGitBranch).ToArray();
        private GitRemote[] ConfigRemotes => cacheContainer.BranchCache.ConfigRemotes.Values.Select(GetGitRemote).ToArray();
        private GitBranch[] LocalConfigBranches => cacheContainer.BranchCache.LocalConfigBranches.Values.Select(x => GetLocalGitBranch(CurrentBranchName, x)).ToArray();
    }

    public interface IUser
    {
        string Name { get; }
        string Email { get; }
        event Action<CacheUpdateEvent> Changed;
        void CheckUserChangedEvent(CacheUpdateEvent cacheUpdateEvent);
        void Initialize(IGitClient client);
        void SetNameAndEmail(string name, string email);
    }

    [Serializable]
    public class User : IUser
    {
        private ICacheContainer cacheContainer;
        private IGitClient gitClient;
        private bool needsRefresh;

        public event Action<CacheUpdateEvent> Changed;

        public User(ICacheContainer cacheContainer)
        {
            this.cacheContainer = cacheContainer;
            cacheContainer.CacheInvalidated += (type) => { if (type == CacheType.GitUser) GitUserCacheOnCacheInvalidated(); };
            cacheContainer.CacheUpdated += (type, dt) => { if (type == CacheType.GitUser) CacheHasBeenUpdated(dt); };
        }

        public void CheckUserChangedEvent(CacheUpdateEvent cacheUpdateEvent) => cacheContainer.CheckAndRaiseEventsIfCacheNewer(CacheType.GitUser, cacheUpdateEvent);

        public void Initialize(IGitClient client)
        {
            Guard.ArgumentNotNull(client, nameof(client));
            gitClient = client;
            if (needsRefresh)
                cacheContainer.GitUserCache.InvalidateData();
        }

        public void SetNameAndEmail(string name, string email)
        {
            gitClient.SetConfigNameAndEmail(name, email)
                     .ThenInUI((success, value) => {
                         if (success)
                         {
                             Name = value.Name;
                             Email = value.Email;
                         }
                     }).Start();
        }

        public override string ToString()
        {
            return String.Format("Name: {0} Email: {1}", Name, Email);
        }

        private void CacheHasBeenUpdated(DateTimeOffset timeOffset)
        {
            HandleUserCacheUpdatedEvent(new CacheUpdateEvent(CacheType.GitUser, timeOffset));
        }

        private void GitUserCacheOnCacheInvalidated()
        {
            //Logger.Trace("GitUserCache Invalidated");
            UpdateUserAndEmail();
        }

        private void HandleUserCacheUpdatedEvent(CacheUpdateEvent cacheUpdateEvent)
        {
            //Logger.Trace("GitUserCache Updated {0}", cacheUpdateEvent.UpdatedTime);
            Changed?.Invoke(cacheUpdateEvent);
        }

        private void UpdateUserAndEmail()
        {
            //Logger.Trace("UpdateUserAndEmail");
            if (gitClient == null)
            {
                needsRefresh = true;
                return;
            }

            gitClient.GetConfigUserAndEmail()
                     .ThenInUI((success, value) =>
                     {
                         if (success)
                         {
                             Name = value.Name;
                             Email = value.Email;
                         }
                     }).Start();
        }
        
        public string Name
        {
            get { return cacheContainer.GitUserCache.Name; }
            private set { cacheContainer.GitUserCache.Name = value; }
        }

        public string Email
        {
            get { return cacheContainer.GitUserCache.Email; }
            private set { cacheContainer.GitUserCache.Email = value; }
        }

        protected static ILogging Logger { get; } = LogHelper.GetLogger<User>();
    }
}