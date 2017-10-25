using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace GitHub.Unity
{
    sealed class ApplicationCache : ScriptObjectSingleton<ApplicationCache>
    {
        [SerializeField] private bool firstRun = true;

        [NonSerialized] private bool? val;

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
        [SerializeField] private string repositoryPath;
        [SerializeField] private string unityApplication;
        [SerializeField] private string unityAssetsPath;
        [SerializeField] private string extensionInstallPath;
        [SerializeField] private string unityVersion;

        [NonSerialized] private IEnvironment environment;
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
                    environment.Initialize(unityVersion, extensionInstallPath.ToNPath(), unityApplication.ToNPath(), unityAssetsPath.ToNPath());
                    environment.InitializeRepository(!String.IsNullOrEmpty(repositoryPath) ? repositoryPath.ToNPath() : null);
                    Flush();
                }
                return environment;
            }
        }

        private NPath DetermineInstallationPath()
        {
            // Juggling to find out where we got installed
            var shim = ScriptableObject.CreateInstance<RunLocationShim>();
            var script = MonoScript.FromScriptableObject(shim);
            var scriptPath = AssetDatabase.GetAssetPath(script).ToNPath();
            ScriptableObject.DestroyImmediate(shim);
            return scriptPath.Parent;
        }

        public void Flush()
        {
            repositoryPath = Environment.RepositoryPath;
            unityApplication = Environment.UnityApplication;
            unityAssetsPath = Environment.UnityAssetsPath;
            extensionInstallPath = Environment.ExtensionInstallPath;
            Save(true);
        }
    }

    abstract class ManagedCacheBase<T> : ScriptObjectSingleton<T> where T : ScriptableObject, IManagedCache
    {
        private ILogging logger;
        private static readonly TimeSpan DataTimeout = TimeSpan.FromSeconds(30);

        public event Action CacheInvalidated;
        public event Action<DateTimeOffset> CacheUpdated;

        public abstract string LastUpdatedAtString { get; protected set; }
        public abstract string LastVerifiedAtString { get; protected set; }

        [NonSerialized] private DateTimeOffset? lastUpdatedAtValue;
        public DateTimeOffset LastUpdatedAt {
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

        [NonSerialized] private DateTimeOffset? lastVerifiedAtValue;
        public DateTimeOffset LastVerifiedAt {
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

        protected ManagedCacheBase()
        {
            logger = Logging.GetLogger(GetType());
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
            logger.Trace("Invalidated");
            CacheInvalidated.SafeInvoke();
            ResetData();
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
                logger.Trace("Updated: {0}", now);
                CacheUpdated.SafeInvoke(now);
            }
            else
            {
                logger.Trace("Verified: {0}", now);
            }
        }
    }

    [Location("cache/testCache.yaml", LocationAttribute.Location.LibraryFolder)]
    sealed class TestCache : ManagedCacheBase<TestCache>, ITestCache
    {
        [SerializeField] private string lastUpdatedAtString;
        [SerializeField] private string lastVerifiedAtString;

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

        protected override void ResetData()
        {
            
        }

        public void UpdateData()
        {
            SaveData(DateTimeOffset.Now, false);
        }
    }

    [Location("cache/branches.yaml", LocationAttribute.Location.LibraryFolder)]
    sealed class BranchCache : ScriptObjectSingleton<BranchCache>, IBranchCache
    {
        private static ILogging Logger = Logging.GetLogger<RepositoryInfoCache>();
        private static readonly TimeSpan DataTimeout = TimeSpan.FromSeconds(30);

        [SerializeField] private DateTimeOffset lastUpdatedAt;
        [SerializeField] private DateTimeOffset lastVerifiedAt;
        [SerializeField] private List<GitBranch> localBranches = new List<GitBranch>();
        [SerializeField] private List<GitBranch> remoteBranches = new List<GitBranch>();

        public event Action CacheInvalidated;
        public event Action<DateTimeOffset> CacheUpdated;

        public BranchCache()
        { }

        public void UpdateData(List<GitBranch> localBranchUpdate, List<GitBranch> remoteBranchUpdate)
        {
            var now = DateTimeOffset.Now;
            var isUpdated = false;

            Logger.Trace("Processing Update: {0}", now);

            var localBranchesIsNull = localBranches == null;
            var localBranchUpdateIsNull = localBranchUpdate == null;

            if (localBranchesIsNull != localBranchUpdateIsNull
                || !localBranchesIsNull && !localBranches.SequenceEqual(localBranchUpdate))
            {
                localBranches = localBranchUpdate;
                isUpdated = true;
            }

            var remoteBranchesIsNull = remoteBranches == null;
            var remoteBranchUpdateIsNull = remoteBranchUpdate == null;

            if (remoteBranchesIsNull != remoteBranchUpdateIsNull
                || !remoteBranchesIsNull && !remoteBranches.SequenceEqual(remoteBranchUpdate))
            {
                remoteBranches = remoteBranchUpdate;
                isUpdated = true;
            }

            SaveData(now, isUpdated);
        }

        private void SaveData(DateTimeOffset now, bool isUpdated)
        {
            if (isUpdated)
            {
                lastUpdatedAt = now;
            }

            lastVerifiedAt = now;
            Save(true);

            if (isUpdated)
            {
                Logger.Trace("Updated: {0}", now);
                CacheUpdated.SafeInvoke(lastUpdatedAt);
            }
            else
            {
                Logger.Trace("Verified: {0}", now);
            }
        }

        public void ValidateData()
        {
            if (DateTimeOffset.Now - lastUpdatedAt > DataTimeout)
            {
                InvalidateData();
            }
        }

        public void InvalidateData()
        {
            Logger.Trace("Invalidated");
            CacheInvalidated.SafeInvoke();
            UpdateData(new List<GitBranch>(), new List<GitBranch>());
        }

        public List<GitBranch> LocalBranches
        {
            get
            {
                ValidateData();
                return localBranches;
            }
        }

        public List<GitBranch> RemoteBranches
        {
            get
            {
                ValidateData();
                return remoteBranches;
            }
        }

        public DateTimeOffset LastUpdatedAt
        {
            get { return lastUpdatedAt; }
        }

        public DateTimeOffset LastVerifiedAt
        {
            get { return lastVerifiedAt; }
        }
    }

    [Location("views/branches.yaml", LocationAttribute.Location.LibraryFolder)]
    sealed class Favorites : ScriptObjectSingleton<Favorites>
    {
        [SerializeField] private List<string> favoriteBranches;
        public List<string> FavoriteBranches
        {
            get
            {
                if (favoriteBranches == null)
                    FavoriteBranches = new List<string>();
                return favoriteBranches;
            }
            set
            {
                favoriteBranches = value;
                Save(true);
            }
        }

        public void SetFavorite(string branchName)
        {
            if (FavoriteBranches.Contains(branchName))
                return;
            FavoriteBranches.Add(branchName);
            Save(true);
        }

        public void UnsetFavorite(string branchName)
        {
            if (!FavoriteBranches.Contains(branchName))
                return;
            FavoriteBranches.Remove(branchName);
            Save(true);
        }

        public void ToggleFavorite(string branchName)
        {
            if (FavoriteBranches.Contains(branchName))
                FavoriteBranches.Remove(branchName);
            else
                FavoriteBranches.Add(branchName);
            Save(true);
        }

        public bool IsFavorite(string branchName)
        {
            return FavoriteBranches.Contains(branchName);
        }
    }

    [Location("cache/repoinfo.yaml", LocationAttribute.Location.LibraryFolder)]
    sealed class RepositoryInfoCache : ScriptObjectSingleton<RepositoryInfoCache>, IRepositoryInfoCache
    {
        private static ILogging Logger = Logging.GetLogger<RepositoryInfoCache>();
        private static readonly TimeSpan DataTimeout = TimeSpan.FromSeconds(30);

        [SerializeField] private DateTimeOffset lastUpdatedAt;
        [SerializeField] private DateTimeOffset lastVerifiedAt;

        [SerializeField] private ConfigRemote? gitRemote;
        [SerializeField] private ConfigBranch? gitBranch;

        public event Action CacheInvalidated;
        public event Action<DateTimeOffset> CacheUpdated;

        public RepositoryInfoCache()
        { }

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

            if (isUpdated)
            {
                lastUpdatedAt = now;
            }

            lastVerifiedAt = now;
            Save(true);

            if (isUpdated)
            {
                Logger.Trace("Updated: {0}", now);
                CacheUpdated.SafeInvoke(lastUpdatedAt);
            }
            else
            {
                Logger.Trace("Verified: {0}", now);
            }
        }


        public void ValidateData()
        {
            if (DateTimeOffset.Now - lastUpdatedAt > DataTimeout)
            {
                InvalidateData();
            }
        }

        public void InvalidateData()
        {
            Logger.Trace("Invalidated");
            CacheInvalidated.SafeInvoke();
            UpdateData(null, null);
        }

        public DateTimeOffset LastUpdatedAt
        {
            get { return lastUpdatedAt; }
        }

        public DateTimeOffset LastVerifiedAt
        {
            get { return lastVerifiedAt; }
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
    sealed class GitLogCache : ScriptObjectSingleton<GitLogCache>, IGitLogCache
    {
        private static ILogging Logger = Logging.GetLogger<RepositoryInfoCache>();
        private static readonly TimeSpan DataTimeout = TimeSpan.FromSeconds(30);

        [SerializeField] private DateTimeOffset lastUpdatedAt;
        [SerializeField] private DateTimeOffset lastVerifiedAt;
        [SerializeField] private List<GitLogEntry> log = new List<GitLogEntry>();

        public event Action CacheInvalidated;
        public event Action<DateTimeOffset> CacheUpdated;

        public GitLogCache()
        { }

        public void UpdateData(List<GitLogEntry> logUpdate)
        {
            var now = DateTimeOffset.Now;
            var isUpdated = false;

            Logger.Trace("Processing Update: {0}", now);

            var logIsNull = log == null;
            var updateIsNull = logUpdate == null;
            if (logIsNull != updateIsNull ||
                !logIsNull && !log.SequenceEqual(logUpdate))
            {
                log = logUpdate;
                lastUpdatedAt = now;
                isUpdated = true;
            }

            lastVerifiedAt = now;
            Save(true);

            if (isUpdated)
            {
                Logger.Trace("Updated: {0}", now);
                CacheUpdated.SafeInvoke(lastUpdatedAt);
            }
            else
            {
                Logger.Trace("Verified: {0}", now);
            }
        }

        public List<GitLogEntry> Log
        {
            get
            {
                ValidateData();
                return log;
            }
        }

        public void ValidateData()
        {
            if (DateTimeOffset.Now - lastUpdatedAt > DataTimeout)
            {
                InvalidateData();
            }
        }

        public void InvalidateData()
        {
            Logger.Trace("Invalidated");
            CacheInvalidated.SafeInvoke();
            UpdateData(new List<GitLogEntry>());
        }

        public DateTimeOffset LastUpdatedAt
        {
            get { return lastUpdatedAt; }
        }

        public DateTimeOffset LastVerifiedAt
        {
            get { return lastVerifiedAt; }
        }
    }

    [Location("cache/gitstatus.yaml", LocationAttribute.Location.LibraryFolder)]
    sealed class GitStatusCache : ScriptObjectSingleton<GitStatusCache>, IGitStatusCache
    {
        private static ILogging Logger = Logging.GetLogger<RepositoryInfoCache>();
        private static readonly TimeSpan DataTimeout = TimeSpan.FromSeconds(30);

        [SerializeField] private DateTimeOffset lastUpdatedAt;
        [SerializeField] private DateTimeOffset lastVerifiedAt;
        [SerializeField] private GitStatus status;

        public event Action CacheInvalidated;
        public event Action<DateTimeOffset> CacheUpdated;

        public GitStatusCache()
        { }

        public void UpdateData(GitStatus statusUpdate)
        {
            var now = DateTimeOffset.Now;
            var isUpdated = false;

            Logger.Trace("Processing Update: {0}", now);

            if (!status.Equals(statusUpdate))
            {
                status = statusUpdate;
                lastUpdatedAt = now;
                isUpdated = true;
            }

            lastVerifiedAt = now;
            Save(true);

            if (isUpdated)
            {
                Logger.Trace("Updated: {0}", now);
                CacheUpdated.SafeInvoke(lastUpdatedAt);
            }
            else
            {
                Logger.Trace("Verified: {0}", now);
            }
        }

        public GitStatus GitStatus
        {
            get
            {
                ValidateData();
                return status;
            }
        }

        public void ValidateData()
        {
            if (DateTimeOffset.Now - lastUpdatedAt > DataTimeout)
            {
                InvalidateData();
            }
        }

        public void InvalidateData()
        {
            Logger.Trace("Invalidated");
            CacheInvalidated.SafeInvoke();
            UpdateData(new GitStatus());
        }

        public DateTimeOffset LastUpdatedAt
        {
            get { return lastUpdatedAt; }
        }

        public DateTimeOffset LastVerifiedAt
        {
            get { return lastVerifiedAt; }
        }
    }

    [Location("cache/gitlocks.yaml", LocationAttribute.Location.LibraryFolder)]
    sealed class GitLocksCache : ScriptObjectSingleton<GitLocksCache>, IGitLocksCache
    {
        private static ILogging Logger = Logging.GetLogger<RepositoryInfoCache>();
        private static readonly TimeSpan DataTimeout = TimeSpan.FromSeconds(30);

        [SerializeField] private DateTimeOffset lastUpdatedAt;
        [SerializeField] private DateTimeOffset lastVerifiedAt;
        [SerializeField] private List<GitLock> locks;

        public event Action CacheInvalidated;
        public event Action<DateTimeOffset> CacheUpdated;

        public GitLocksCache()
        { }

        public void UpdateData(List<GitLock> locksUpdate)
        {
            var now = DateTimeOffset.Now;
            var isUpdated = false;

            Logger.Trace("Processing Update: {0}", now);

            var locksIsNull = locks == null;
            var locksUpdateIsNull = locksUpdate == null;

            if (locksIsNull != locksUpdateIsNull
                || !locksIsNull && !locks.SequenceEqual(locksUpdate))
            {
                locks = locksUpdate;
                isUpdated = true;
                lastUpdatedAt = now;
            }

            lastVerifiedAt = now;
            Save(true);

            if (isUpdated)
            {
                Logger.Trace("Updated: {0}", now);
                CacheUpdated.SafeInvoke(lastUpdatedAt);
            }
            else
            {
                Logger.Trace("Verified: {0}", now);
            }
        }

        public List<GitLock> GitLocks
        {
            get
            {
                ValidateData();
                return locks;
            }
        }

        public void ValidateData()
        {
            if (DateTimeOffset.Now - lastUpdatedAt > DataTimeout)
            {
                InvalidateData();
            }
        }

        public void InvalidateData()
        {
            Logger.Trace("Invalidated");
            CacheInvalidated.SafeInvoke();
            UpdateData(null);
        }

        public DateTimeOffset LastUpdatedAt
        {
            get { return lastUpdatedAt; }
        }

        public DateTimeOffset LastVerifiedAt
        {
            get { return lastVerifiedAt; }
        }
    }

    [Location("cache/gituser.yaml", LocationAttribute.Location.LibraryFolder)]
    sealed class GitUserCache : ScriptObjectSingleton<GitUserCache>, IGitUserCache
    {
        private static ILogging Logger = Logging.GetLogger<RepositoryInfoCache>();
        private static readonly TimeSpan DataTimeout = TimeSpan.FromSeconds(30);

        [SerializeField] private DateTimeOffset lastUpdatedAt;
        [SerializeField] private DateTimeOffset lastVerifiedAt;
        [SerializeField] private User user;

        public event Action CacheInvalidated;
        public event Action<DateTimeOffset> CacheUpdated;

        public GitUserCache()
        { }

        public void UpdateData(User userUpdate)
        {
            var now = DateTimeOffset.Now;
            var isUpdated = false;

            Logger.Trace("Processing Update: {0}", now);

            if (user != userUpdate)
            {
                user = userUpdate;
                isUpdated = true;
                lastUpdatedAt = now;
            }

            lastVerifiedAt = now;
            Save(true);

            if (isUpdated)
            {
                Logger.Trace("Updated: {0}", now);
                CacheUpdated.SafeInvoke(lastUpdatedAt);
            }
            else
            {
                Logger.Trace("Verified: {0}", now);
            }
        }

        public User User
        {
            get
            {
                ValidateData();
                return user;
            }
        }

        public void ValidateData()
        {
            if (DateTimeOffset.Now - lastUpdatedAt > DataTimeout)
            {
                InvalidateData();
            }
        }

        public void InvalidateData()
        {
            Logger.Trace("Invalidated");
            CacheInvalidated.SafeInvoke();
            UpdateData(null);
        }

        public DateTimeOffset LastUpdatedAt
        {
            get { return lastUpdatedAt; }
        }

        public DateTimeOffset LastVerifiedAt
        {
            get { return lastVerifiedAt; }
        }
    }
}
