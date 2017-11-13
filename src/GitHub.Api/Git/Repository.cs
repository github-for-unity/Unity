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
        private IRepositoryManager repositoryManager;
        private ICacheContainer cacheContainer;
        private UriString cloneUrl;
        private string name;

        public event Action<CacheUpdateEvent> LogChanged;
        public event Action<CacheUpdateEvent> StatusChanged;
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

            cacheContainer = container;
            cacheContainer.CacheInvalidated += CacheContainer_OnCacheInvalidated;
            cacheContainer.CacheUpdated += CacheContainer_OnCacheUpdated;
        }

        public void Initialize(IRepositoryManager initRepositoryManager)
        {
            Logger.Trace("Initialize");
            Guard.ArgumentNotNull(initRepositoryManager, nameof(initRepositoryManager));

            repositoryManager = initRepositoryManager;
            repositoryManager.OnCurrentBranchAndRemoteUpdated += RepositoryManager_OnCurrentBranchAndRemoteUpdated;
            repositoryManager.OnRepositoryUpdated += RepositoryManager_OnRepositoryUpdated;
            repositoryManager.OnLocalBranchListUpdated += RepositoryManager_OnLocalBranchListUpdated;
            repositoryManager.OnRemoteBranchListUpdated += RepositoryManager_OnRemoteBranchListUpdated;
            repositoryManager.OnLocalBranchUpdated += RepositoryManager_OnLocalBranchUpdated;
            repositoryManager.OnLocalBranchAdded += RepositoryManager_OnLocalBranchAdded;
            repositoryManager.OnLocalBranchRemoved += RepositoryManager_OnLocalBranchRemoved;
            repositoryManager.OnRemoteBranchAdded += RepositoryManager_OnRemoteBranchAdded;
            repositoryManager.OnRemoteBranchRemoved += RepositoryManager_OnRemoteBranchRemoved;
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

        public ITask CommitAllFiles(string message, string body)
        {
            return repositoryManager
                .CommitAllFiles(message, body)
                .Then(() => {
                    UpdateGitStatus();
                    UpdateGitLog();
                });
        }

        public ITask CommitFiles(List<string> files, string message, string body)
        {
            return repositoryManager
                .CommitFiles(files, message, body)
                .Then(() => {
                    UpdateGitStatus();
                    UpdateGitLog();
                });
        }

        public ITask Pull()
        {
            return repositoryManager.Pull(CurrentRemote.Value.Name, CurrentBranch?.Name);
        }

        public ITask Push()
        {
            return repositoryManager.Push(CurrentRemote.Value.Name, CurrentBranch?.Name)
                .Then(UpdateGitStatus);
        }

        public ITask Fetch()
        {
            return repositoryManager.Fetch(CurrentRemote.Value.Name);
        }

        public ITask Revert(string changeset)
        {
            return repositoryManager.Revert(changeset);
        }

        public ITask RequestLock(string file)
        {
            return repositoryManager.LockFile(file)
                .Then(UpdateLocks);
        }

        public ITask ReleaseLock(string file, bool force)
        {
            return repositoryManager.UnlockFile(file, force)
                .Then(UpdateLocks);
        }

        public void RefreshLog()
        {
            UpdateGitLog();
        }

        public void RefreshStatus()
        {
            UpdateGitStatus();
        }

        public void CheckLogChangedEvent(CacheUpdateEvent cacheUpdateEvent)
        {
            var managedCache = cacheContainer.GitLogCache;
            var raiseEvent = managedCache.ShouldRaiseCacheEvent(cacheUpdateEvent);

            Logger.Trace("Check GitLogCache CacheUpdateEvent Current:{0} Check:{1} Result:{2}", managedCache.LastUpdatedAt,
                cacheUpdateEvent.UpdatedTimeString ?? "[NULL]", raiseEvent);

            if (raiseEvent)
            {
                var dateTimeOffset = managedCache.LastUpdatedAt;
                var updateEvent = new CacheUpdateEvent { UpdatedTimeString = dateTimeOffset.ToString() };
                HandleGitLogCacheUpdatedEvent(updateEvent);
            }
        }

        public void CheckStatusChangedEvent(CacheUpdateEvent cacheUpdateEvent)
        {
            var managedCache = cacheContainer.GitStatusCache;
            var raiseEvent = managedCache.ShouldRaiseCacheEvent(cacheUpdateEvent);

            Logger.Trace("Check GitStatusCache CacheUpdateEvent Current:{0} Check:{1} Result:{2}", managedCache.LastUpdatedAt,
                cacheUpdateEvent.UpdatedTimeString ?? "[NULL]", raiseEvent);

            if (raiseEvent)
            {
                var dateTimeOffset = managedCache.LastUpdatedAt;
                var updateEvent = new CacheUpdateEvent { UpdatedTimeString = dateTimeOffset.ToString() };
                HandleGitStatusCacheUpdatedEvent(updateEvent);
            }
        }

        public void CheckCurrentBranchChangedEvent(CacheUpdateEvent cacheUpdateEvent)
        {
            CheckRepositoryInfoCacheEvent(cacheUpdateEvent);
        }

        public void CheckCurrentRemoteChangedEvent(CacheUpdateEvent cacheUpdateEvent)
        {
            CheckRepositoryInfoCacheEvent(cacheUpdateEvent);
        }

        public void CheckCurrentBranchAndRemoteChangedEvent(CacheUpdateEvent cacheUpdateEvent)
        {
            CheckRepositoryInfoCacheEvent(cacheUpdateEvent);
        }

        private void CheckRepositoryInfoCacheEvent(CacheUpdateEvent cacheUpdateEvent)
        {
            var managedCache = cacheContainer.RepositoryInfoCache;
            var raiseEvent = managedCache.ShouldRaiseCacheEvent(cacheUpdateEvent);

            Logger.Trace("Check RepositoryInfoCache CacheUpdateEvent Current:{0} Check:{1} Result:{2}", managedCache.LastUpdatedAt,
                cacheUpdateEvent.UpdatedTimeString ?? "[NULL]", raiseEvent);

            if (raiseEvent)
            {
                var dateTimeOffset = managedCache.LastUpdatedAt;
                var updateEvent = new CacheUpdateEvent { UpdatedTimeString = dateTimeOffset.ToString() };
                HandleRepositoryInfoCacheUpdatedEvent(updateEvent);
            }
        }

        public void CheckLocksChangedEvent(CacheUpdateEvent cacheUpdateEvent)
        {
            CacheUpdateEvent cacheUpdateEvent1 = cacheUpdateEvent;
            var managedCache = cacheContainer.GitLocksCache;
            var raiseEvent = managedCache.ShouldRaiseCacheEvent(cacheUpdateEvent1);

            Logger.Trace("Check GitLocksCache CacheUpdateEvent Current:{0} Check:{1} Result:{2}", managedCache.LastUpdatedAt,
                cacheUpdateEvent1.UpdatedTimeString ?? "[NULL]", raiseEvent);

            if (raiseEvent)
            {
                var dateTimeOffset = managedCache.LastUpdatedAt;
                var updateEvent = new CacheUpdateEvent { UpdatedTimeString = dateTimeOffset.ToString() };
                HandleGitLocksCacheUpdatedEvent(updateEvent);
            }
        }

        public void CheckLocalBranchListChangedEvent(CacheUpdateEvent cacheUpdateEvent)
        {
            CheckBranchCacheEvent(cacheUpdateEvent);
        }

        public void CheckRemoteBranchListChangedEvent(CacheUpdateEvent cacheUpdateEvent)
        {
            CheckBranchCacheEvent(cacheUpdateEvent);
        }

        public void CheckLocalAndRemoteBranchListChangedEvent(CacheUpdateEvent cacheUpdateEvent)
        {
            CheckBranchCacheEvent(cacheUpdateEvent);
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

            return other != null && object.Equals(LocalPath, other.LocalPath);
        }

        private void CheckBranchCacheEvent(CacheUpdateEvent cacheUpdateEvent)
        {
            var managedCache = cacheContainer.BranchCache;
            var raiseEvent = managedCache.ShouldRaiseCacheEvent(cacheUpdateEvent);

            Logger.Trace("Check BranchCache CacheUpdateEvent Current:{0} Check:{1} Result:{2}", managedCache.LastUpdatedAt,
                cacheUpdateEvent.UpdatedTimeString ?? "[NULL]", raiseEvent);

            if (raiseEvent)
            {
                var dateTimeOffset = managedCache.LastUpdatedAt;
                var updateEvent = new CacheUpdateEvent { UpdatedTimeString = dateTimeOffset.ToString() };
                HandleBranchCacheUpdatedEvent(updateEvent);
            }
        }

        private void CacheContainer_OnCacheInvalidated(CacheType cacheType)
        {
            switch (cacheType)
            {
                case CacheType.BranchCache:
                    break;

                case CacheType.GitLogCache:
                    break;

                case CacheType.GitStatusCache:
                    break;

                case CacheType.GitLocksCache:
                    break;

                case CacheType.GitUserCache:
                    break;

                case CacheType.RepositoryInfoCache:
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(cacheType), cacheType, null);
            }
        }

        private void CacheContainer_OnCacheUpdated(CacheType cacheType, DateTimeOffset offset)
        {
            var cacheUpdateEvent = new CacheUpdateEvent { UpdatedTimeString = offset.ToString() };
            switch (cacheType)
            {
                case CacheType.BranchCache:
                    HandleBranchCacheUpdatedEvent(cacheUpdateEvent);
                    break;

                case CacheType.GitLogCache:
                    HandleGitLogCacheUpdatedEvent(cacheUpdateEvent);
                    break;

                case CacheType.GitStatusCache:
                    HandleGitStatusCacheUpdatedEvent(cacheUpdateEvent);
                    break;

                case CacheType.GitLocksCache:
                    HandleGitLocksCacheUpdatedEvent(cacheUpdateEvent);
                    break;

                case CacheType.GitUserCache:
                    break;

                case CacheType.RepositoryInfoCache:
                    HandleRepositoryInfoCacheUpdatedEvent(cacheUpdateEvent);
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(cacheType), cacheType, null);
            }
        }

        private void HandleRepositoryInfoCacheUpdatedEvent(CacheUpdateEvent cacheUpdateEvent)
        {
            Logger.Trace("RepositoryInfoCache Updated {0}", cacheUpdateEvent.UpdatedTimeString);
            CurrentBranchChanged?.Invoke(cacheUpdateEvent);
            CurrentRemoteChanged?.Invoke(cacheUpdateEvent);
            CurrentBranchAndRemoteChanged?.Invoke(cacheUpdateEvent);
        }

        private void HandleGitLocksCacheUpdatedEvent(CacheUpdateEvent cacheUpdateEvent)
        {
            Logger.Trace("GitLocksCache Updated {0}", cacheUpdateEvent.UpdatedTimeString);
            LocksChanged?.Invoke(cacheUpdateEvent);
        }

        private void HandleGitStatusCacheUpdatedEvent(CacheUpdateEvent cacheUpdateEvent)
        {
            Logger.Trace("GitStatusCache Updated {0}", cacheUpdateEvent.UpdatedTimeString);
            StatusChanged?.Invoke(cacheUpdateEvent);
        }

        private void HandleGitLogCacheUpdatedEvent(CacheUpdateEvent cacheUpdateEvent)
        {
            Logger.Trace("GitLogCache Updated {0}", cacheUpdateEvent.UpdatedTimeString);
            LogChanged?.Invoke(cacheUpdateEvent);
        }

        private void HandleBranchCacheUpdatedEvent(CacheUpdateEvent cacheUpdateEvent)
        {
            Logger.Trace("BranchCache Updated {0}", cacheUpdateEvent.UpdatedTimeString);
            LocalBranchListChanged?.Invoke(cacheUpdateEvent);
            RemoteBranchListChanged?.Invoke(cacheUpdateEvent);
            LocalAndRemoteBranchListChanged?.Invoke(cacheUpdateEvent);
        }

        private void RepositoryManager_OnRepositoryUpdated()
        {
            Logger.Trace("OnRepositoryUpdated");
            UpdateGitStatus();
            UpdateGitLog();
        }

        private void UpdateGitStatus()
        {
            repositoryManager?.Status()
                .ThenInUI((b, status) => { CurrentStatus = status; })
                .Start();
        }

        private void UpdateGitLog()
        {
            repositoryManager?.Log()
                .ThenInUI((b, log) => { CurrentLog = log; })
                .Start();
        }

        private void UpdateLocks()
        {
            if (CurrentRemote.HasValue)
            {
                repositoryManager?.ListLocks(false)
                    .ThenInUI((b, locks) => { CurrentLocks = locks; })
                    .Start();
            }
        }
        
        private void RepositoryManager_OnCurrentBranchAndRemoteUpdated(ConfigBranch? branch, ConfigRemote? remote)
        {
            new ActionTask(CancellationToken.None, () => {
                if (!Nullable.Equals(CurrentConfigBranch, branch))
                {
                        var currentBranch = branch != null ? (GitBranch?)GetLocalGitBranch(branch.Value) : null;

                        CurrentConfigBranch = branch;
                        CurrentBranch = currentBranch;
                        UpdateLocalBranches();
                }

                if (!Nullable.Equals(CurrentConfigRemote, remote))
                {
                        CurrentConfigRemote = remote;
                        CurrentRemote = GetGitRemote(remote.Value);
                        UpdateRepositoryInfo();
                }
            }) { Affinity = TaskAffinity.UI }.Start();
        }

        private void RepositoryManager_OnLocalBranchUpdated(string name)
        {
            if (name == CurrentConfigBranch?.Name)
            {
                UpdateGitStatus();
                UpdateGitLog();
            }
        }

        private void RepositoryManager_OnRemoteBranchListUpdated(Dictionary<string, ConfigRemote> remotes,
            Dictionary<string, Dictionary<string, ConfigBranch>> branches)
        {
            new ActionTask(CancellationToken.None, () => {
                cacheContainer.BranchCache.SetRemotes(remotes, branches);
                UpdateRemoteAndRemoteBranches();
            }) { Affinity = TaskAffinity.UI }.Start();
        }

        private void UpdateRemoteAndRemoteBranches()
        {
            Remotes = ConfigRemotes.Values.Select(GetGitRemote).ToArray();

            RemoteBranches = RemoteConfigBranches.Values.SelectMany(x => x.Values).Select(GetRemoteGitBranch).ToArray();
        }

        private void RepositoryManager_OnLocalBranchListUpdated(Dictionary<string, ConfigBranch> branches)
        {
            new ActionTask(CancellationToken.None, () => {
                cacheContainer.BranchCache.SetLocals(branches);
                UpdateLocalBranches();
            }) { Affinity = TaskAffinity.UI }.Start();
        }

        private void UpdateLocalBranches()
        {
            LocalBranches = LocalConfigBranches.Values.Select(GetLocalGitBranch).ToArray();
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
        }

        private void RepositoryManager_OnLocalBranchRemoved(string name)
        {
            new ActionTask(CancellationToken.None, () => {
                cacheContainer.BranchCache.RemoveLocalBranch(name);
                UpdateLocalBranches();
            }) { Affinity = TaskAffinity.UI }.Start();
        }

        private void RepositoryManager_OnLocalBranchAdded(string name)
        {
            new ActionTask(CancellationToken.None, () => {
                cacheContainer.BranchCache.AddLocalBranch(name);
                UpdateLocalBranches();
            }) { Affinity = TaskAffinity.UI }.Start();
        }

        private void RepositoryManager_OnRemoteBranchAdded(string remote, string name)
        {
            new ActionTask(CancellationToken.None, () => {
                cacheContainer.BranchCache.AddRemoteBranch(remote, name);
                UpdateRemoteAndRemoteBranches();
            }) { Affinity = TaskAffinity.UI }.Start();
        }

        private void RepositoryManager_OnRemoteBranchRemoved(string remote, string name)
        {
            new ActionTask(CancellationToken.None, () => {
                cacheContainer.BranchCache.RemoveRemoteBranch(remote, name);
                UpdateRemoteAndRemoteBranches();
            }) { Affinity = TaskAffinity.UI }.Start();
        }

        private GitBranch GetLocalGitBranch(ConfigBranch x)
        {
            var name = x.Name;
            var trackingName = x.IsTracking ? x.Remote.Value.Name + "/" + name : "[None]";
            var isActive = name == CurrentBranchName;

            return new GitBranch(name, trackingName, isActive);
        }

        private static GitBranch GetRemoteGitBranch(ConfigBranch x)
        {
            var name = x.Remote.Value.Name + "/" + x.Name;

            return new GitBranch(name, "[None]", false);
        }

        private static GitRemote GetGitRemote(ConfigRemote configRemote)
        {
            return new GitRemote(configRemote.Name, configRemote.Url);
        }

        private IRemoteConfigBranchDictionary RemoteConfigBranches => cacheContainer.BranchCache.RemoteConfigBranches;

        private IConfigRemoteDictionary ConfigRemotes => cacheContainer.BranchCache.ConfigRemotes;

        private ILocalConfigBranchDictionary LocalConfigBranches => cacheContainer.BranchCache.LocalConfigBranches;

        public GitRemote[] Remotes
        {
            get { return cacheContainer.BranchCache.Remotes; }
            private set { cacheContainer.BranchCache.Remotes = value; }
        }

        public GitBranch[] LocalBranches
        {
            get { return cacheContainer.BranchCache.LocalBranches; }
            private set { cacheContainer.BranchCache.LocalBranches = value; }
        }

        public GitBranch[] RemoteBranches
        {
            get { return cacheContainer.BranchCache.RemoteBranches; }
            private set { cacheContainer.BranchCache.RemoteBranches = value; }
        }

        private ConfigBranch? CurrentConfigBranch
        {
            get { return this.cacheContainer.BranchCache.CurentConfigBranch; }
            set { cacheContainer.BranchCache.CurentConfigBranch = value; }
        }

        private ConfigRemote? CurrentConfigRemote
        {
            get { return this.cacheContainer.BranchCache.CurrentConfigRemote; }
            set { cacheContainer.BranchCache.CurrentConfigRemote = value; }
        }

        public GitStatus CurrentStatus
        {
            get { return cacheContainer.GitStatusCache.GitStatus; }
            private set { cacheContainer.GitStatusCache.GitStatus = value; }
        }

        public GitBranch? CurrentBranch
        {
            get { return cacheContainer.RepositoryInfoCache.CurentGitBranch; }
            private set { cacheContainer.RepositoryInfoCache.CurentGitBranch = value; }
        }

        public string CurrentBranchName => CurrentConfigBranch?.Name;

        public GitRemote? CurrentRemote
        {
            get { return cacheContainer.RepositoryInfoCache.CurrentGitRemote; }
            private set { cacheContainer.RepositoryInfoCache.CurrentGitRemote = value; }
        }

        public List<GitLogEntry> CurrentLog
        {
            get { return cacheContainer.GitLogCache.Log; }
            private set { cacheContainer.GitLogCache.Log = value; }
        }

        public List<GitLock> CurrentLocks
        {
            get { return cacheContainer.GitLocksCache.GitLocks; }
            private set { cacheContainer.GitLocksCache.GitLocks = value; }
        }

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

        public bool IsGitHub
        {
            get { return HostAddress.IsGitHubDotCom(CloneUrl); }
        }

        internal string DebuggerDisplay => String.Format(CultureInfo.InvariantCulture,
            "{0} Owner: {1} Name: {2} CloneUrl: {3} LocalPath: {4} Branch: {5} Remote: {6}", GetHashCode(), Owner, Name,
            CloneUrl, LocalPath, CurrentBranch, CurrentRemote);

        protected static ILogging Logger { get; } = Logging.GetLogger<Repository>();
    }

    public interface IUser
    {
        string Name { get; set; }
        string Email { get; set; }
    }

    [Serializable]
    public class User : IUser
    {
        public override string ToString()
        {
            return String.Format("Name: {0} Email: {1}", Name, Email);
        }

        public string Name { get; set; }
        public string Email { get; set; }
    }

    [Serializable]
    public struct CacheUpdateEvent
    {
        public string UpdatedTimeString;
    }
}
