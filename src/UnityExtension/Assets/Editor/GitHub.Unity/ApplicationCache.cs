using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace GitHub.Unity
{
    sealed class ApplicationCache : ScriptObjectSingleton<ApplicationCache>
    {
        [NonSerialized] private bool? val;
        [SerializeField] private bool firstRun = true;

        public bool FirstRun
        {
            get
            {
                if (!val.HasValue)
                {
                    val = firstRun;
                }

                if (firstRun)
                {
                    firstRun = false;
                    Save(true);
                }

                return val.Value;
            }
        }
    }

    sealed class EnvironmentCache : ScriptObjectSingleton<EnvironmentCache>
    {
        [NonSerialized] private IEnvironment environment;
        [SerializeField] private string extensionInstallPath;
        [SerializeField] private string repositoryPath;
        [SerializeField] private string unityApplication;
        [SerializeField] private string unityAssetsPath;
        [SerializeField] private string unityVersion;

        public void Flush()
        {
            repositoryPath = Environment.RepositoryPath;
            unityApplication = Environment.UnityApplication;
            unityAssetsPath = Environment.UnityAssetsPath;
            extensionInstallPath = Environment.ExtensionInstallPath;
            Save(true);
        }

        private NPath DetermineInstallationPath()
        {
            // Juggling to find out where we got installed
            var shim = CreateInstance<RunLocationShim>();
            var script = MonoScript.FromScriptableObject(shim);
            var scriptPath = AssetDatabase.GetAssetPath(script).ToNPath();
            DestroyImmediate(shim);
            return scriptPath.Parent;
        }

        public IEnvironment Environment
        {
            get
            {
                if (environment == null)
                {
                    environment = new DefaultEnvironment();
                    if (unityApplication == null)
                    {
                        unityAssetsPath = Application.dataPath;
                        unityApplication = EditorApplication.applicationPath;
                        extensionInstallPath = DetermineInstallationPath();
                        unityVersion = Application.unityVersion;
                    }
                    environment.Initialize(unityVersion, extensionInstallPath.ToNPath(), unityApplication.ToNPath(),
                        unityAssetsPath.ToNPath());
                    environment.InitializeRepository(EntryPoint.ApplicationManager.CacheContainer, !String.IsNullOrEmpty(repositoryPath)
                        ? repositoryPath.ToNPath()
                        : null);
                    Flush();
                }
                return environment;
            }
        }
    }

    abstract class ManagedCacheBase<T> : ScriptObjectSingleton<T> where T : ScriptableObject, IManagedCache
    {
        private static readonly TimeSpan DataTimeout = TimeSpan.FromSeconds(30);

        [NonSerialized] private DateTimeOffset? lastUpdatedAtValue;

        [NonSerialized] private DateTimeOffset? lastVerifiedAtValue;

        public event Action CacheInvalidated;
        public event Action<DateTimeOffset> CacheUpdated;

        protected ManagedCacheBase()
        {
            Logger = Logging.GetLogger(GetType());
        }

        public void ValidateData()
        {
            if (DateTimeOffset.Now - LastUpdatedAt > DataTimeout)
            {
                InvalidateData();
            }
        }

        public void InvalidateData()
        {
            Logger.Trace("Invalidated");
            CacheInvalidated.SafeInvoke();
            ResetData();
            SaveData(DateTimeOffset.Now, true);
        }

        protected abstract void ResetData();

        protected void SaveData(DateTimeOffset now, bool isUpdated)
        {
            if (isUpdated)
            {
                LastUpdatedAt = now;
            }

            LastVerifiedAt = now;
            Save(true);

            if (isUpdated)
            {
                Logger.Trace("Updated: {0}", now);
                CacheUpdated.SafeInvoke(now);
            }
            else
            {
                Logger.Trace("Verified: {0}", now);
            }
        }

        public abstract string LastUpdatedAtString { get; protected set; }
        public abstract string LastVerifiedAtString { get; protected set; }

        public DateTimeOffset LastUpdatedAt
        {
            get
            {
                if (!lastUpdatedAtValue.HasValue)
                {
                    lastUpdatedAtValue = DateTimeOffset.Parse(LastUpdatedAtString);
                }

                return lastUpdatedAtValue.Value;
            }
            set
            {
                LastUpdatedAtString = value.ToString();
                lastUpdatedAtValue = null;
            }
        }

        public DateTimeOffset LastVerifiedAt
        {
            get
            {
                if (!lastVerifiedAtValue.HasValue)
                {
                    lastVerifiedAtValue = DateTimeOffset.Parse(LastVerifiedAtString);
                }

                return lastVerifiedAtValue.Value;
            }
            set
            {
                LastVerifiedAtString = value.ToString();
                lastVerifiedAtValue = null;
            }
        }

        protected ILogging Logger { get; private set; }
    }

    [Location("cache/branches.yaml", LocationAttribute.Location.LibraryFolder)]
    sealed class BranchCache : ManagedCacheBase<BranchCache>, IBranchCache
    {
        [SerializeField] private string lastUpdatedAtString = DateTimeOffset.MinValue.ToString();
        [SerializeField] private string lastVerifiedAtString = DateTimeOffset.MinValue.ToString();
        [SerializeField] private List<GitBranch> localBranches = new List<GitBranch>();
        [SerializeField] private List<GitBranch> remoteBranches = new List<GitBranch>();

        public void UpdateData(List<GitBranch> localBranchUpdate, List<GitBranch> remoteBranchUpdate)
        {
            var now = DateTimeOffset.Now;
            var isUpdated = false;

            Logger.Trace("Processing Update: {0}", now);

            var localBranchesIsNull = localBranches == null;
            var localBranchUpdateIsNull = localBranchUpdate == null;

            if (localBranchesIsNull != localBranchUpdateIsNull ||
                !localBranchesIsNull && !localBranches.SequenceEqual(localBranchUpdate))
            {
                localBranches = localBranchUpdate;
                isUpdated = true;
            }

            var remoteBranchesIsNull = remoteBranches == null;
            var remoteBranchUpdateIsNull = remoteBranchUpdate == null;

            if (remoteBranchesIsNull != remoteBranchUpdateIsNull ||
                !remoteBranchesIsNull && !remoteBranches.SequenceEqual(remoteBranchUpdate))
            {
                remoteBranches = remoteBranchUpdate;
                isUpdated = true;
            }

            SaveData(now, isUpdated);
        }

        public List<GitBranch> LocalBranches {
            get { return localBranches;  }
        }

        public List<GitBranch> RemoteBranches
        {
            get { return remoteBranches; }
        }

        public void UpdateData()
        {
            SaveData(DateTimeOffset.Now, false);
        }

        protected override void ResetData()
        {
            localBranches = new List<GitBranch>();
            remoteBranches = new List<GitBranch>();
        }

        public override string LastUpdatedAtString
        {
            get { return lastUpdatedAtString; }
            protected set { lastUpdatedAtString = value; }
        }

        public override string LastVerifiedAtString
        {
            get { return lastVerifiedAtString; }
            protected set { lastVerifiedAtString = value; }
        }
    }

    [Location("views/branches.yaml", LocationAttribute.Location.LibraryFolder)]
    sealed class Favorites : ScriptObjectSingleton<Favorites>
    {
        [SerializeField] private List<string> favoriteBranches;

        public void SetFavorite(string branchName)
        {
            if (FavoriteBranches.Contains(branchName))
            {
                return;
            }

            FavoriteBranches.Add(branchName);
            Save(true);
        }

        public void UnsetFavorite(string branchName)
        {
            if (!FavoriteBranches.Contains(branchName))
            {
                return;
            }

            FavoriteBranches.Remove(branchName);
            Save(true);
        }

        public void ToggleFavorite(string branchName)
        {
            if (FavoriteBranches.Contains(branchName))
            {
                FavoriteBranches.Remove(branchName);
            }
            else
            {
                FavoriteBranches.Add(branchName);
            }
            Save(true);
        }

        public bool IsFavorite(string branchName)
        {
            return FavoriteBranches.Contains(branchName);
        }

        public List<string> FavoriteBranches
        {
            get
            {
                if (favoriteBranches == null)
                {
                    FavoriteBranches = new List<string>();
                }
                return favoriteBranches;
            }
            set
            {
                favoriteBranches = value;
                Save(true);
            }
        }
    }

    [Location("cache/repoinfo.yaml", LocationAttribute.Location.LibraryFolder)]
    sealed class RepositoryInfoCache : ManagedCacheBase<RepositoryInfoCache>, IRepositoryInfoCache
    {
        [SerializeField] private string lastUpdatedAtString = DateTimeOffset.MinValue.ToString();
        [SerializeField] private string lastVerifiedAtString = DateTimeOffset.MinValue.ToString();
        [SerializeField] private ConfigBranch? gitBranch;
        [SerializeField] private ConfigRemote? gitRemote;

        public void UpdateData(ConfigRemote? gitRemoteUpdate)
        {
            UpdateData(gitRemoteUpdate, gitBranch);
        }

        public void UpdateData(ConfigBranch? gitBranchUpdate)
        {
            UpdateData(gitRemote, gitBranchUpdate);
        }

        public void UpdateData(ConfigRemote? gitRemoteUpdate, ConfigBranch? gitBranchUpdate)
        {
            var now = DateTimeOffset.Now;
            var isUpdated = false;

            Logger.Trace("Processing Update: {0}", now);

            if (!Nullable.Equals(gitRemote, gitRemoteUpdate))
            {
                gitRemote = gitRemoteUpdate;
                isUpdated = true;
            }

            if (!Nullable.Equals(gitBranch, gitBranchUpdate))
            {
                gitBranch = gitBranchUpdate;
                isUpdated = true;
            }

            SaveData(now, isUpdated);
        }

        protected override void ResetData()
        {
            gitBranch = null;
            gitRemote = null;
        }

        public override string LastUpdatedAtString
        {
            get { return lastUpdatedAtString; }
            protected set { lastUpdatedAtString = value; }
        }

        public override string LastVerifiedAtString
        {
            get { return lastVerifiedAtString; }
            protected set { lastVerifiedAtString = value; }
        }

        public ConfigRemote? CurrentRemote
        {
            get
            {
                ValidateData();
                return gitRemote;
            }
        }

        public ConfigBranch? CurentBranch
        {
            get
            {
                ValidateData();
                return gitBranch;
            }
        }
    }

    [Location("cache/gitlog.yaml", LocationAttribute.Location.LibraryFolder)]
    sealed class GitLogCache : ManagedCacheBase<GitLogCache>, IGitLogCache
    {
        [SerializeField] private string lastUpdatedAtString = DateTimeOffset.MinValue.ToString();
        [SerializeField] private string lastVerifiedAtString = DateTimeOffset.MinValue.ToString();
        [SerializeField] private List<GitLogEntry> log = new List<GitLogEntry>();

        public void UpdateData(List<GitLogEntry> logUpdate)
        {
            var now = DateTimeOffset.Now;
            var isUpdated = false;

            Logger.Trace("Processing Update: {0}", now);

            var logIsNull = log == null;
            var updateIsNull = logUpdate == null;
            if (logIsNull != updateIsNull || !logIsNull && !log.SequenceEqual(logUpdate))
            {
                log = logUpdate;
                isUpdated = true;
            }

            SaveData(now, isUpdated);
        }

        public List<GitLogEntry> Log
        {
            get
            {
                ValidateData();
                return log;
            }
        }

        protected override void ResetData()
        {
            log = new List<GitLogEntry>();
        }

        public override string LastUpdatedAtString
        {
            get { return lastUpdatedAtString; }
            protected set { lastUpdatedAtString = value; }
        }

        public override string LastVerifiedAtString
        {
            get { return lastVerifiedAtString; }
            protected set { lastVerifiedAtString = value; }
        }
    }

    [Location("cache/gitstatus.yaml", LocationAttribute.Location.LibraryFolder)]
    sealed class GitStatusCache : ManagedCacheBase<GitStatusCache>, IGitStatusCache
    {
        [SerializeField] private string lastUpdatedAtString = DateTimeOffset.MinValue.ToString();
        [SerializeField] private string lastVerifiedAtString = DateTimeOffset.MinValue.ToString();
        [SerializeField] private GitStatus status;

        public void UpdateData(GitStatus statusUpdate)
        {
            var now = DateTimeOffset.Now;
            var isUpdated = false;

            Logger.Trace("Processing Update: {0}", now);

            if (!status.Equals(statusUpdate))
            {
                status = statusUpdate;
                isUpdated = true;
            }

            SaveData(now, isUpdated);
        }

        public GitStatus GitStatus
        {
            get
            {
                ValidateData();
                return status;
            }
        }

        protected override void ResetData()
        {
            status = new GitStatus();
        }

        public override string LastUpdatedAtString
        {
            get { return lastUpdatedAtString; }
            protected set { lastUpdatedAtString = value; }
        }

        public override string LastVerifiedAtString
        {
            get { return lastVerifiedAtString; }
            protected set { lastVerifiedAtString = value; }
        }
    }

    [Location("cache/gitlocks.yaml", LocationAttribute.Location.LibraryFolder)]
    sealed class GitLocksCache : ManagedCacheBase<GitLocksCache>, IGitLocksCache
    {
        [SerializeField] private string lastUpdatedAtString = DateTimeOffset.MinValue.ToString();
        [SerializeField] private string lastVerifiedAtString = DateTimeOffset.MinValue.ToString();
        [SerializeField] private List<GitLock> locks = new List<GitLock>();

        public void UpdateData(List<GitLock> locksUpdate)
        {
            var now = DateTimeOffset.Now;
            var isUpdated = false;

            Logger.Trace("Processing Update: {0}", now);

            var locksIsNull = locks == null;
            var locksUpdateIsNull = locksUpdate == null;

            if (locksIsNull != locksUpdateIsNull || !locksIsNull && !locks.SequenceEqual(locksUpdate))
            {
                locks = locksUpdate;
                isUpdated = true;
            }

            SaveData(now, isUpdated);
        }

        public List<GitLock> GitLocks
        {
            get
            {
                ValidateData();
                return locks;
            }
        }

        protected override void ResetData()
        {
            locks = new List<GitLock>();
        }

        public override string LastUpdatedAtString
        {
            get { return lastUpdatedAtString; }
            protected set { lastUpdatedAtString = value; }
        }

        public override string LastVerifiedAtString
        {
            get { return lastVerifiedAtString; }
            protected set { lastVerifiedAtString = value; }
        }
    }

    [Location("cache/gituser.yaml", LocationAttribute.Location.LibraryFolder)]
    sealed class GitUserCache : ManagedCacheBase<GitUserCache>, IGitUserCache
    {
        [SerializeField] private string lastUpdatedAtString = DateTimeOffset.MinValue.ToString();
        [SerializeField] private string lastVerifiedAtString = DateTimeOffset.MinValue.ToString();
        [SerializeField] private User user;

        public void UpdateData(User userUpdate)
        {
            var now = DateTimeOffset.Now;
            var isUpdated = false;

            Logger.Trace("Processing Update: {0}", now);

            if (user != userUpdate)
            {
                user = userUpdate;
                isUpdated = true;
            }

            SaveData(now, isUpdated);
        }

        public User User
        {
            get
            {
                ValidateData();
                return user;
            }
        }

        protected override void ResetData()
        {
            user = null;
        }

        public override string LastUpdatedAtString
        {
            get { return lastUpdatedAtString; }
            protected set { lastUpdatedAtString = value; }
        }

        public override string LastVerifiedAtString
        {
            get { return lastVerifiedAtString; }
            protected set { lastVerifiedAtString = value; }
        }
    }
}
