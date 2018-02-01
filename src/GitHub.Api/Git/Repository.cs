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
    class Repository : IEquatable<Repository>, IRepository
    {
        private IRepositoryManager repositoryManager;
        private ICacheContainer cacheContainer;
        private UriString cloneUrl;
        private string name;

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

            cacheContainer = container;
            cacheContainer.CacheInvalidated += CacheContainer_OnCacheInvalidated;
            cacheContainer.CacheUpdated += CacheContainer_OnCacheUpdated;
        }

        public void Initialize(IRepositoryManager initRepositoryManager)
        {
            Logger.Trace("Initialize");
            Guard.ArgumentNotNull(initRepositoryManager, nameof(initRepositoryManager));

            repositoryManager = initRepositoryManager;
            repositoryManager.CurrentBranchUpdated += RepositoryManagerOnCurrentBranchUpdated;
            repositoryManager.GitStatusUpdated += RepositoryManagerOnGitStatusUpdated;
            repositoryManager.GitAheadBehindStatusUpdated += RepositoryManagerOnGitAheadBehindStatusUpdated;
            repositoryManager.GitLogUpdated += RepositoryManagerOnGitLogUpdated;
            repositoryManager.GitLocksUpdated += RepositoryManagerOnGitLocksUpdated;
            repositoryManager.LocalBranchesUpdated += RepositoryManagerOnLocalBranchesUpdated;
            repositoryManager.RemoteBranchesUpdated += RepositoryManagerOnRemoteBranchesUpdated;
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

        public ITask RequestLock(string file)
        {
            return repositoryManager.LockFile(file);
        }

        public ITask ReleaseLock(string file, bool force)
        {
            return repositoryManager.UnlockFile(file, force);
        }

        public ITask DiscardChanges(GitStatusEntry[] gitStatusEntry)
        {
            return repositoryManager.DiscardChanges(gitStatusEntry);
        }

        public void CheckLogChangedEvent(CacheUpdateEvent cacheUpdateEvent)
        {
            var managedCache = cacheContainer.GitLogCache;
            var raiseEvent = managedCache.LastUpdatedAt != cacheUpdateEvent.UpdatedTime;

            Logger.Trace("Check GitLogCache CacheUpdateEvent Current:{0} Check:{1} Result:{2}", managedCache.LastUpdatedAt,
                cacheUpdateEvent.UpdatedTime, raiseEvent);

            if (raiseEvent)
            {
                var dateTimeOffset = managedCache.LastUpdatedAt;
                var updateEvent = new CacheUpdateEvent { UpdatedTime = dateTimeOffset };
                HandleGitLogCacheUpdatedEvent(updateEvent);
            }
        }

        public void CheckStatusChangedEvent(CacheUpdateEvent cacheUpdateEvent)
        {
            var managedCache = cacheContainer.GitTrackingStatusCache;
            var raiseEvent = managedCache.LastUpdatedAt != cacheUpdateEvent.UpdatedTime;

            Logger.Trace("Check GitStatusCache CacheUpdateEvent Current:{0} Check:{1} Result:{2}", managedCache.LastUpdatedAt,
                cacheUpdateEvent.UpdatedTime, raiseEvent);

            if (raiseEvent)
            {
                var dateTimeOffset = managedCache.LastUpdatedAt;
                var updateEvent = new CacheUpdateEvent { UpdatedTime = dateTimeOffset };
                HandleGitTrackingStatusCacheUpdatedEvent(updateEvent);
            }
        }

        public void CheckStatusEntriesChangedEvent(CacheUpdateEvent cacheUpdateEvent)
        {
            var managedCache = cacheContainer.GitStatusEntriesCache;
            var raiseEvent = managedCache.LastUpdatedAt != cacheUpdateEvent.UpdatedTime;

            Logger.Trace("Check GitStatusEntriesCache CacheUpdateEvent Current:{0} Check:{1} Result:{2}", managedCache.LastUpdatedAt,
                cacheUpdateEvent.UpdatedTime, raiseEvent);

            if (raiseEvent)
            {
                var dateTimeOffset = managedCache.LastUpdatedAt;
                var updateEvent = new CacheUpdateEvent { UpdatedTime = dateTimeOffset };
                HandleGitStatusEntriesCacheUpdatedEvent(updateEvent);
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
            var raiseEvent = managedCache.LastUpdatedAt != cacheUpdateEvent.UpdatedTime;

            Logger.Trace("Check RepositoryInfoCache CacheUpdateEvent Current:{0} Check:{1} Result:{2}", managedCache.LastUpdatedAt,
                cacheUpdateEvent.UpdatedTime, raiseEvent);

            if (raiseEvent)
            {
                var dateTimeOffset = managedCache.LastUpdatedAt;
                var updateEvent = new CacheUpdateEvent { UpdatedTime = dateTimeOffset};
                HandleRepositoryInfoCacheUpdatedEvent(updateEvent);
            }
        }

        public void CheckLocksChangedEvent(CacheUpdateEvent cacheUpdateEvent)
        {
            var managedCache = cacheContainer.GitLocksCache;
            var raiseEvent = managedCache.LastUpdatedAt != cacheUpdateEvent.UpdatedTime;

            Logger.Trace("Check GitLocksCache CacheUpdateEvent Current:{0} Check:{1} Result:{2}", managedCache.LastUpdatedAt,
                cacheUpdateEvent.UpdatedTime, raiseEvent);

            if (raiseEvent)
            {
                var dateTimeOffset = managedCache.LastUpdatedAt;
                var updateEvent = new CacheUpdateEvent { UpdatedTime = dateTimeOffset };
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
            var raiseEvent = managedCache.LastUpdatedAt != cacheUpdateEvent.UpdatedTime;

            Logger.Trace("Check BranchCache CacheUpdateEvent Current:{0} Check:{1} Result:{2}", managedCache.LastUpdatedAt,
                cacheUpdateEvent.UpdatedTime, raiseEvent);

            if (raiseEvent)
            {
                var dateTimeOffset = managedCache.LastUpdatedAt;
                var updateEvent = new CacheUpdateEvent { UpdatedTime = dateTimeOffset };
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
                    repositoryManager?.UpdateGitLog();
                    break;

                case CacheType.GitTrackingStatusCache:
                    repositoryManager?.UpdateGitAheadBehindStatus();
                    break;

                case CacheType.GitLocksCache:
                    UpdateLocks();
                    break;

                case CacheType.GitUserCache:
                    break;

                case CacheType.RepositoryInfoCache:
                    break;

                case CacheType.GitStatusEntriesCache:
                    repositoryManager?.UpdateGitStatus();
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(cacheType), cacheType, null);
            }
        }

        private void CacheContainer_OnCacheUpdated(CacheType cacheType, DateTimeOffset offset)
        {
            var cacheUpdateEvent = new CacheUpdateEvent { UpdatedTime = offset };
            switch (cacheType)
            {
                case CacheType.BranchCache:
                    HandleBranchCacheUpdatedEvent(cacheUpdateEvent);
                    break;

                case CacheType.GitLogCache:
                    HandleGitLogCacheUpdatedEvent(cacheUpdateEvent);
                    break;

                case CacheType.GitTrackingStatusCache:
                    HandleGitTrackingStatusCacheUpdatedEvent(cacheUpdateEvent);
                    break;

                case CacheType.GitLocksCache:
                    HandleGitLocksCacheUpdatedEvent(cacheUpdateEvent);
                    break;

                case CacheType.GitUserCache:
                    break;

                case CacheType.RepositoryInfoCache:
                    HandleRepositoryInfoCacheUpdatedEvent(cacheUpdateEvent);
                    break;

                case CacheType.GitStatusEntriesCache:
                    HandleGitStatusEntriesCacheUpdatedEvent(cacheUpdateEvent);
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

        private void HandleGitTrackingStatusCacheUpdatedEvent(CacheUpdateEvent cacheUpdateEvent)
        {
            Logger.Trace("GitTrackingStatusCache Updated {0}", cacheUpdateEvent.UpdatedTimeString);
            TrackingStatusChanged?.Invoke(cacheUpdateEvent);
        }

        private void HandleGitStatusEntriesCacheUpdatedEvent(CacheUpdateEvent cacheUpdateEvent)
        {
            Logger.Trace("GitStatusEntriesCache Updated {0}", cacheUpdateEvent.UpdatedTimeString);
            StatusEntriesChanged?.Invoke(cacheUpdateEvent);
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

        private void RepositoryManagerOnCurrentBranchUpdated(ConfigBranch? branch, ConfigRemote? remote)
        {
            new ActionTask(TaskManager.Instance.Token, () => {
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
                    CurrentRemote = remote.HasValue ? (GitRemote?)GetGitRemote(remote.Value) : null;
                    ClearRepositoryInfo();
                }
            }) { Affinity = TaskAffinity.UI }.Start();
        }

        private void RepositoryManagerOnGitStatusUpdated(GitStatus gitStatus)
        {
            new ActionTask(TaskManager.Instance.Token, () => {
                CurrentChanges = gitStatus.Entries;
                CurrentAhead = gitStatus.Ahead;
                CurrentBehind = gitStatus.Behind;
            }) { Affinity = TaskAffinity.UI }.Start();
        }

        private void RepositoryManagerOnGitAheadBehindStatusUpdated(GitAheadBehindStatus aheadBehindStatus)
        {
            new ActionTask(TaskManager.Instance.Token, () => {
                CurrentAhead = aheadBehindStatus.Ahead;
                CurrentBehind = aheadBehindStatus.Behind;
            }) { Affinity = TaskAffinity.UI }.Start();
        }

        private void RepositoryManagerOnGitLogUpdated(List<GitLogEntry> gitLogEntries)
        {
            new ActionTask(TaskManager.Instance.Token, () => {
                CurrentLog = gitLogEntries;
            }) { Affinity = TaskAffinity.UI }.Start();
        }

        private void RepositoryManagerOnGitLocksUpdated(List<GitLock> gitLocks)
        {
            new ActionTask(TaskManager.Instance.Token, () => {
                    CurrentLocks = gitLocks;
                })
                { Affinity = TaskAffinity.UI }.Start();
        }

        private void RepositoryManagerOnRemoteBranchesUpdated(Dictionary<string, ConfigRemote> remotes,
            Dictionary<string, Dictionary<string, ConfigBranch>> branches)
        {
            new ActionTask(TaskManager.Instance.Token, () => {
                cacheContainer.BranchCache.SetRemotes(remotes, branches);
                Remotes = ConfigRemotes.Values.Select(GetGitRemote).ToArray();
                RemoteBranches = RemoteConfigBranches.Values.SelectMany(x => x.Values).Select(GetRemoteGitBranch).ToArray();
            }) { Affinity = TaskAffinity.UI }.Start();
        }

        private void RepositoryManagerOnLocalBranchesUpdated(Dictionary<string, ConfigBranch> branches)
        {
            new ActionTask(TaskManager.Instance.Token, () => {
                cacheContainer.BranchCache.SetLocals(branches);
                UpdateLocalBranches();
            }) { Affinity = TaskAffinity.UI }.Start();
        }

        private void UpdateLocks()
        {
            if (CurrentRemote.HasValue)
            {
                repositoryManager?.UpdateLocks();
            }
        }

        private void UpdateLocalBranches()
        {
            LocalBranches = LocalConfigBranches.Values.Select(GetLocalGitBranch).ToArray();
        }

        private void ClearRepositoryInfo()
        {
            CloneUrl = null;
            Name = null;
        }

        private GitBranch GetLocalGitBranch(ConfigBranch x)
        {
            var branchName = x.Name;
            var trackingName = x.IsTracking ? x.Remote.Value.Name + "/" + branchName : "[None]";
            var isActive = branchName == CurrentBranchName;

            return new GitBranch(branchName, trackingName, isActive);
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

        public int CurrentAhead
        {
            get { return cacheContainer.GitTrackingStatusCache.Ahead; }
            private set { cacheContainer.GitTrackingStatusCache.Ahead = value; }
        }

        public int CurrentBehind
        {
            get { return cacheContainer.GitTrackingStatusCache.Behind; }
            private set { cacheContainer.GitTrackingStatusCache.Behind = value; }
        }

        public List<GitStatusEntry> CurrentChanges
        {
            get { return cacheContainer.GitStatusEntriesCache.Entries; }
            private set { cacheContainer.GitStatusEntriesCache.Entries = value; }
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

        protected static ILogging Logger { get; } = LogHelper.GetLogger<Repository>();
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

        public event Action<CacheUpdateEvent> Changed;

        public User(ICacheContainer cacheContainer)
        {
            this.cacheContainer = cacheContainer;
        }

        public void CheckUserChangedEvent(CacheUpdateEvent cacheUpdateEvent)
        {
            var managedCache = cacheContainer.GitUserCache;
            var raiseEvent = managedCache.LastUpdatedAt != cacheUpdateEvent.UpdatedTime;

            Logger.Trace("Check GitUserCache CacheUpdateEvent Current:{0} Check:{1} Result:{2}", managedCache.LastUpdatedAt,
                cacheUpdateEvent.UpdatedTime, raiseEvent);

            if (raiseEvent)
            {
                var dateTimeOffset = managedCache.LastUpdatedAt;
                var updateEvent = new CacheUpdateEvent { UpdatedTime = dateTimeOffset };
                HandleUserCacheUpdatedEvent(updateEvent);
            }
        }

        public void Initialize(IGitClient client)
        {
            Guard.ArgumentNotNull(client, nameof(client));

            Logger.Trace("Initialize");

            gitClient = client;

            cacheContainer.GitUserCache.CacheInvalidated += GitUserCacheOnCacheInvalidated;
            cacheContainer.GitUserCache.CacheUpdated += GitUserCacheOnCacheUpdated;
            cacheContainer.GitUserCache.ValidateData();
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

        private void GitUserCacheOnCacheUpdated(DateTimeOffset timeOffset)
        {
            HandleUserCacheUpdatedEvent(new CacheUpdateEvent
            {
                UpdatedTime = timeOffset
            });
        }

        private void GitUserCacheOnCacheInvalidated()
        {
            Logger.Trace("GitUserCache Invalidated");
            UpdateUserAndEmail();
        }

        private void HandleUserCacheUpdatedEvent(CacheUpdateEvent cacheUpdateEvent)
        {
            Logger.Trace("GitUserCache Updated {0}", cacheUpdateEvent.UpdatedTime);
            Changed?.Invoke(cacheUpdateEvent);
        }

        private void UpdateUserAndEmail()
        {
            Logger.Trace("UpdateUserAndEmail");

            if (gitClient == null)
            {
                Logger.Trace("GitClient is null");
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
        
        protected static ILogging Logger { get; } = LogHelper.GetLogger<User>();
    }

    [Serializable]
    public struct CacheUpdateEvent
    {
        [NonSerialized] private DateTimeOffset? updatedTimeValue;
        public string updatedTimeString;

        public override int GetHashCode()
        {
            int hash = 17;
            hash = hash * 23 + updatedTimeValue.GetHashCode();
            hash = hash * 23 + (updatedTimeString?.GetHashCode() ?? 0);
            return hash;
        }

        public override bool Equals(object other)
        {
            if (other is CacheUpdateEvent)
                return Equals((CacheUpdateEvent)other);
            return false;
        }

        public bool Equals(CacheUpdateEvent other)
        {
            return
                object.Equals(updatedTimeValue, other.updatedTimeValue) && 
                String.Equals(updatedTimeString, other.updatedTimeString)
                ;
        }

        public static bool operator ==(CacheUpdateEvent lhs, CacheUpdateEvent rhs)
        {
            // If both are null, or both are same instance, return true.
            if (ReferenceEquals(lhs, rhs))
                return true;

            // If one is null, but not both, return false.
            if (((object)lhs == null) || ((object)rhs == null))
                return false;

            // Return true if the fields match:
            return lhs.Equals(rhs);
        }

        public static bool operator !=(CacheUpdateEvent lhs, CacheUpdateEvent rhs)
        {
            return !(lhs == rhs);
        }

        public DateTimeOffset UpdatedTime
        {
            get
            {
                if (!updatedTimeValue.HasValue)
                {
                    DateTimeOffset result;
                    if (DateTimeOffset.TryParseExact(updatedTimeString, Constants.Iso8601Format, CultureInfo.InvariantCulture, DateTimeStyles.None, out result))
                    {
                        updatedTimeValue = result;
                    }
                    else
                    {
                        UpdatedTime = DateTimeOffset.MinValue;
                    }
                }

                return updatedTimeValue.Value;
            }
            set
            {
                updatedTimeValue = value;
                UpdatedTimeString = value.ToString(Constants.Iso8601Format);
            }
        }

        public string UpdatedTimeString
        {
            get { return updatedTimeString; }
            private set { updatedTimeString = value; }
        }
    }
}