using System;
using System.Collections;
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

        public event Action<CacheUpdateEvent> GitStatusCacheUpdated;
        public event Action<CacheUpdateEvent> GitLogCacheUpdated;
        public event Action<CacheUpdateEvent> GitLockCacheUpdated;
        public event Action<CacheUpdateEvent> BranchCacheUpdated;
        public event Action<CacheUpdateEvent> RepositoryInfoCacheUpdated;

        /// <summary>
        /// Initializes a new instance of the <see cref="Repository"/> class.
        /// </summary>
        /// <param name="localPath"></param>
        /// <param name="container"></param>
        public Repository(NPath localPath, ICacheContainer container)
        {
            Guard.ArgumentNotNull(localPath, nameof(localPath));

            LocalPath = localPath;
            User = new User();

            cacheContainer = container;
            cacheContainer.CacheInvalidated += CacheContainer_OnCacheInvalidated;
            cacheContainer.CacheUpdated += CacheContainer_OnCacheUpdated;
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

                default:
                    throw new ArgumentOutOfRangeException(nameof(cacheType), cacheType, null);
            }
        }

        private void CacheContainer_OnCacheUpdated(CacheType cacheType, DateTimeOffset offset)
        {
            switch (cacheType)
            {
                case CacheType.BranchCache:
                    FireBranchCacheUpdated(offset);
                    break;

                case CacheType.GitLogCache:
                    FireGitLogCacheUpdated(offset);
                    break;

                case CacheType.GitStatusCache:
                    FireGitStatusCacheUpdated(offset);
                    break;

                case CacheType.GitLocksCache:
                    FireGitLocksCacheUpdated(offset);
                    break;

                case CacheType.GitUserCache:
                    break;

                case CacheType.RepositoryInfoCache:
                    FireRepositoryInfoCacheUpdated(offset);
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(cacheType), cacheType, null);
            }
        }

        private void FireGitLogCacheUpdated(DateTimeOffset dateTimeOffset)
        {
            Logger.Trace("GitLogCacheUpdated {0}", dateTimeOffset);
            GitLogCacheUpdated?.Invoke(new CacheUpdateEvent { UpdatedTimeString = dateTimeOffset.ToString() });
        }

        private void FireBranchCacheUpdated(DateTimeOffset dateTimeOffset)
        {
            Logger.Trace("BranchCacheUpdated {0}", dateTimeOffset);
            BranchCacheUpdated?.Invoke(new CacheUpdateEvent { UpdatedTimeString = dateTimeOffset.ToString() });
        }

        private void FireGitStatusCacheUpdated(DateTimeOffset dateTimeOffset)
        {
            Logger.Trace("GitStatusCacheUpdated {0}", dateTimeOffset);
            GitStatusCacheUpdated?.Invoke(new CacheUpdateEvent { UpdatedTimeString = dateTimeOffset.ToString() });
        }

        private void FireGitLocksCacheUpdated(DateTimeOffset dateTimeOffset)
        {
            Logger.Trace("GitStatusCacheUpdated {0}", dateTimeOffset);
            GitLockCacheUpdated?.Invoke(new CacheUpdateEvent { UpdatedTimeString = dateTimeOffset.ToString() });
        }

        private void FireRepositoryInfoCacheUpdated(DateTimeOffset dateTimeOffset)
        {
            Logger.Trace("RepositoryInfoCacheUpdated {0}", dateTimeOffset);
            RepositoryInfoCacheUpdated?.Invoke(new CacheUpdateEvent { UpdatedTimeString = dateTimeOffset.ToString() });
        }

        public void Initialize(IRepositoryManager initRepositoryManager)
        {
            Logger.Trace("Initialize");
            Guard.ArgumentNotNull(initRepositoryManager, nameof(initRepositoryManager));

            repositoryManager = initRepositoryManager;
            repositoryManager.OnCurrentBranchUpdated += RepositoryManager_OnCurrentBranchUpdated;
            repositoryManager.OnCurrentRemoteUpdated += RepositoryManager_OnCurrentRemoteUpdated;
            repositoryManager.OnRepositoryUpdated += RepositoryManager_OnRepositoryUpdated;
            repositoryManager.OnLocalBranchListUpdated += RepositoryManager_OnLocalBranchListUpdated;
            repositoryManager.OnRemoteBranchListUpdated += RepositoryManager_OnRemoteBranchListUpdated;
            repositoryManager.OnLocalBranchUpdated += RepositoryManager_OnLocalBranchUpdated;
            repositoryManager.OnLocalBranchAdded += RepositoryManager_OnLocalBranchAdded;
            repositoryManager.OnLocalBranchRemoved += RepositoryManager_OnLocalBranchRemoved;
            repositoryManager.OnRemoteBranchAdded += RepositoryManager_OnRemoteBranchAdded;
            repositoryManager.OnRemoteBranchRemoved += RepositoryManager_OnRemoteBranchRemoved;
            repositoryManager.OnGitUserLoaded += user => User = user;

            UpdateGitStatus();
            UpdateGitLog();

            new ActionTask(CancellationToken.None, UpdateLocks) { Affinity = TaskAffinity.UI }.Start();
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
            return repositoryManager.Push(CurrentRemote.Value.Name, CurrentBranch?.Name).Then(UpdateGitStatus);
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
            return repositoryManager.LockFile(file).Then(UpdateLocks);
        }

        public ITask ReleaseLock(string file, bool force)
        {
            return repositoryManager.UnlockFile(file, force).Then(UpdateLocks);
        }

        public void CheckRepositoryInfoCacheEvent(CacheUpdateEvent cacheUpdateEvent)
        {
            var managedCache = cacheContainer.RepositoryInfoCache;
            var raiseEvent = ShouldRaiseCacheEvent(cacheUpdateEvent, managedCache);

            Logger.Trace("CheckRepositoryInfoCacheEvent Current:{0} Check:{1} Result:{2}", managedCache.LastUpdatedAt,
                cacheUpdateEvent.UpdatedTimeString ?? "[NULL]", raiseEvent);

            if (raiseEvent)
            {
                FireBranchCacheUpdated(managedCache.LastUpdatedAt);
            }
        }

        public void CheckBranchCacheEvent(CacheUpdateEvent cacheUpdateEvent)
        {
            var managedCache = cacheContainer.BranchCache;
            var raiseEvent = ShouldRaiseCacheEvent(cacheUpdateEvent, managedCache);

            Logger.Trace("CheckBranchCacheEvent Current:{0} Check:{1} Result:{2}", managedCache.LastUpdatedAt,
                cacheUpdateEvent.UpdatedTimeString ?? "[NULL]", raiseEvent);

            if (raiseEvent)
            {
                FireBranchCacheUpdated(managedCache.LastUpdatedAt);
            }
        }

        public void CheckGitStatusCacheEvent(CacheUpdateEvent cacheUpdateEvent)
        {
            var managedCache = cacheContainer.GitStatusCache;
            var raiseEvent = ShouldRaiseCacheEvent(cacheUpdateEvent, managedCache);

            Logger.Trace("CheckGitStatusCacheEvent Current:{0} Check:{1} Result:{2}", managedCache.LastUpdatedAt,
                cacheUpdateEvent.UpdatedTimeString ?? "[NULL]", raiseEvent);

            if (raiseEvent)
            {
                FireGitStatusCacheUpdated(managedCache.LastUpdatedAt);
            }
        }

        public void CheckGitLogCacheEvent(CacheUpdateEvent cacheUpdateEvent)
        {
            var managedCache = cacheContainer.GitLogCache;
            var raiseEvent = ShouldRaiseCacheEvent(cacheUpdateEvent, managedCache);

            Logger.Trace("CheckGitLogCacheEvent Current:{0} Check:{1} Result:{2}", managedCache.LastUpdatedAt,
                cacheUpdateEvent.UpdatedTimeString ?? "[NULL]", raiseEvent);

            if (raiseEvent)
            {
                FireGitLogCacheUpdated(managedCache.LastUpdatedAt);
            }
        }

        public void CheckGitLocksCacheEvent(CacheUpdateEvent cacheUpdateEvent)
        {
            var managedCache = cacheContainer.GitLocksCache;
            var raiseEvent = ShouldRaiseCacheEvent(cacheUpdateEvent, managedCache);

            Logger.Trace("CheckGitLocksCacheEvent Current:{0} Check:{1} Result:{2}", managedCache.LastUpdatedAt,
                cacheUpdateEvent.UpdatedTimeString ?? "[NULL]", raiseEvent);

            if (raiseEvent)
            {
                FireGitLogCacheUpdated(managedCache.LastUpdatedAt);
            }
        }

        private static bool ShouldRaiseCacheEvent(CacheUpdateEvent cacheUpdateEvent, IManagedCache managedCache)
        {
            bool raiseEvent;
            if (cacheUpdateEvent.UpdatedTimeString == null)
            {
                raiseEvent = managedCache.LastUpdatedAt != DateTimeOffset.MinValue;
            }
            else
            {
                raiseEvent = managedCache.LastUpdatedAt.ToString() != cacheUpdateEvent.UpdatedTimeString;
            }
            return raiseEvent;
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

        private void RepositoryManager_OnCurrentRemoteUpdated(ConfigRemote? remote)
        {
            if (!Nullable.Equals(CurrentConfigRemote, remote))
            {
                new ActionTask(CancellationToken.None, () => {
                    CurrentConfigRemote = remote;
                    CurrentRemote = GetGitRemote(remote.Value);
                    UpdateRepositoryInfo();
                }) { Affinity = TaskAffinity.UI }.Start();
            }
        }

        private void RepositoryManager_OnRepositoryUpdated()
        {
            Logger.Trace("OnRepositoryUpdated");
            UpdateGitStatus();
            UpdateGitLog();
        }

        private void UpdateGitStatus()
        {
            repositoryManager?.Status().ThenInUI((b, status) => { CurrentStatus = status; }).Start();
        }

        private void UpdateGitLog()
        {
            repositoryManager?.Log().ThenInUI((b, log) => { CurrentLog = log; }).Start();
        }

        private void UpdateLocks()
        {
            if (CurrentRemote.HasValue)
            {
                repositoryManager?.ListLocks(false).ThenInUI((b, locks) => { CurrentLocks = locks; }).Start();
            }
        }

        private void RepositoryManager_OnCurrentBranchUpdated(ConfigBranch? branch)
        {
            if (!Nullable.Equals(CurrentConfigBranch, branch))
            {
                new ActionTask(CancellationToken.None, () => {
                    var currentBranch = branch != null ? (GitBranch?)GetLocalGitBranch(branch.Value) : null;

                    CurrentConfigBranch = branch;
                    CurrentBranch = currentBranch;
                    UpdateLocalBranches();
                }) { Affinity = TaskAffinity.UI }.Start();
            }
        }

        private void RepositoryManager_OnLocalBranchUpdated(string name)
        {
            if (name == CurrentConfigBranch?.Name)
            {
                UpdateGitStatus();
                UpdateGitLog();
            }
        }

        private void RepositoryManager_OnRemoteBranchListUpdated(IDictionary<string, ConfigRemote> remotes,
            IDictionary<string, IDictionary<string, ConfigBranch>> branches)
        {
            new ActionTask(CancellationToken.None, () => {
                cacheContainer.BranchCache.SetRemotes(remotes, branches);
                UpdateRemoteAndRemoteBranches();
            }) { Affinity = TaskAffinity.UI }.Start();
        }

        private void UpdateRemoteAndRemoteBranches()
        {
            cacheContainer.BranchCache.Remotes =
                cacheContainer.BranchCache.ConfigRemotes.Values.Select(GetGitRemote).ToArray();

            cacheContainer.BranchCache.RemoteBranches = cacheContainer
                .BranchCache.RemoteConfigBranches.Values.SelectMany(x => x.Values).Select(GetRemoteGitBranch).ToArray();
        }

        private void RepositoryManager_OnLocalBranchListUpdated(IDictionary<string, ConfigBranch> branches)
        {
            new ActionTask(CancellationToken.None, () => {
                cacheContainer.BranchCache.SetLocals(branches);
                UpdateLocalBranches();
            }) { Affinity = TaskAffinity.UI }.Start();
        }

        private void UpdateLocalBranches()
        {
            cacheContainer.BranchCache.LocalBranches = cacheContainer
                .BranchCache.LocalConfigBranches.Values.Select(GetLocalGitBranch).ToArray();
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

            return new GitBranch { Name = name, Tracking = trackingName, IsActive = isActive };
        }

        private static GitBranch GetRemoteGitBranch(ConfigBranch x)
        {
            var name = x.Remote.Value.Name + "/" + x.Name;

            return new GitBranch { Name = name };
        }

        private static GitRemote GetGitRemote(ConfigRemote configRemote)
        {
            return new GitRemote { Name = configRemote.Name, Url = configRemote.Url };
        }

        public GitRemote[] Remotes => cacheContainer.BranchCache.Remotes;

        public GitBranch[] LocalBranches => cacheContainer.BranchCache.LocalBranches;

        public GitBranch[] RemoteBranches => cacheContainer.BranchCache.RemoteBranches;

        private ConfigBranch? CurrentConfigBranch
        {
            get { return this.cacheContainer.BranchCache.CurentConfigBranch; }
            set { cacheContainer.BranchCache.CurentConfigBranch = value;}
        }

        private ConfigRemote? CurrentConfigRemote
        {
            get { return this.cacheContainer.BranchCache.CurrentConfigRemote; }
            set { cacheContainer.BranchCache.CurrentConfigRemote = value; }
        }

        public GitStatus CurrentStatus
        {
            get { return cacheContainer.GitStatusCache.GitStatus; }
            set { cacheContainer.GitStatusCache.GitStatus = value; }
        }

        public GitBranch? CurrentBranch
        {
            get { return cacheContainer.RepositoryInfoCache.CurentGitBranch; }
            set { cacheContainer.RepositoryInfoCache.CurentGitBranch = value; }
        }

        public string CurrentBranchName => CurrentConfigBranch?.Name;

        public GitRemote? CurrentRemote
        {
            get { return cacheContainer.RepositoryInfoCache.CurrentGitRemote; }
            set { cacheContainer.RepositoryInfoCache.CurrentGitRemote = value; }
        }

        public List<GitLogEntry> CurrentLog
        {
            get { return cacheContainer.GitLogCache.Log; }
            set { cacheContainer.GitLogCache.Log = value; }
        }

        public List<GitLock> CurrentLocks
        {
            get { return cacheContainer.GitLocksCache.GitLocks; }
            set { cacheContainer.GitLocksCache.GitLocks = value; }
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

        public IUser User { get; set; }

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
