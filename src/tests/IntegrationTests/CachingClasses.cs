using GitHub.Logging;
using GitHub.Unity;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.Serialization;

namespace IntegrationTests
{
    class ScriptObjectSingleton<T> where T : class
    {
        private static T instance;
        public static T Instance
        {
            get
            {
                if (instance == null)
                    CreateAndLoad();
                return instance;
            }
            set { instance = value; }
        }

        protected ScriptObjectSingleton()
        {
            if (instance != null)
            {
                LogHelper.Instance.Error("Singleton already exists!");
            }
            else
            {
                instance = this as T;
                System.Diagnostics.Debug.Assert(instance != null);
            }
        }

        private static void CreateAndLoad()
        {
            if (instance == null)
            {
                var inst = Activator.CreateInstance<T>() as ScriptObjectSingleton<T>;
                inst.Save(true);
            }
        }

        protected virtual void Save(bool saveAsText)
        {
        }
    }

    sealed class ApplicationCache : ScriptObjectSingleton<ApplicationCache>
    {
        private bool firstRun = true;
        public string firstRunAtString;
        private bool? firstRunValue;
        public DateTimeOffset? firstRunAtValue;

        public bool FirstRun
        {
            get
            {
                EnsureFirstRun();
                return firstRunValue.Value;
            }
        }

        public DateTimeOffset FirstRunAt
        {
            get
            {
                EnsureFirstRun();

                if (!firstRunAtValue.HasValue)
                {
                    firstRunAtValue = DateTimeOffset.ParseExact(firstRunAtString, Constants.Iso8601Format, CultureInfo.InvariantCulture);
                }

                return firstRunAtValue.Value;
            }
            private set
            {
                firstRunAtString = value.ToString(Constants.Iso8601Format);
                firstRunAtValue = value;
            }
        }

        private void EnsureFirstRun()
        {
            if (!firstRunValue.HasValue)
            {
                firstRunValue = firstRun;
            }

            if (firstRun)
            {
                firstRun = false;
                FirstRunAt = DateTimeOffset.Now;
                Save(true);
            }
        }
    }

    abstract class ManagedCacheBase<T> : ScriptObjectSingleton<T> where T : class, IManagedCache
    {
        private string lastUpdatedAtString = DateTimeOffset.MinValue.ToString(Constants.Iso8601Format);
        private string initializedAtString = DateTimeOffset.MinValue.ToString(Constants.Iso8601Format);
        private DateTimeOffset? lastUpdatedAtValue;
        private DateTimeOffset? initializedAtValue;

        private bool isInvalidating;

        public event Action<CacheType> CacheInvalidated;
        public event Action<CacheType, DateTimeOffset> CacheUpdated;

        protected ManagedCacheBase(CacheType type)
        {
            CacheType = type;
            Logger = LogHelper.GetLogger(GetType());
        }

        public bool ValidateData()
        {
            var isInitialized = IsInitialized;
            var timedOut = DateTimeOffset.Now - LastUpdatedAt > DataTimeout;
            var needsInvalidation = !isInitialized || timedOut;
            if (needsInvalidation)
            {
                Logger.Trace("needsInvalidation isInitialized:{0} timedOut:{1}", isInitialized, timedOut);
                InvalidateData();
            }
            return !needsInvalidation;
        }

        public void InvalidateData()
        {
            Logger.Trace("Invalidate");
            if (!isInvalidating)
            {
                isInvalidating = true;
                LastUpdatedAt = DateTimeOffset.MinValue;
                CacheInvalidated.SafeInvoke(CacheType);
            }
        }

        protected void SaveData(DateTimeOffset now, bool isChanged)
        {
            var isInitialized = IsInitialized;
            isChanged = isChanged || !isInitialized;

            InitializedAt = !isInitialized ? now : InitializedAt;
            LastUpdatedAt = isChanged ? now : LastUpdatedAt;

            Save(true);

            isInvalidating = false;

            if (isChanged)
            {
                Logger.Trace("Updated: {0}", now);
                CacheUpdated.SafeInvoke(CacheType, now);
            }
        }

        public abstract TimeSpan DataTimeout { get; }
        public string LastUpdatedAtString
        {
            get { return lastUpdatedAtString; }
            private set { lastUpdatedAtString = value; }
        }

        public string InitializedAtString
        {
            get { return initializedAtString; }
            private set { initializedAtString = value; }
        }

        public bool IsInitialized { get { return ApplicationCache.Instance.FirstRunAt <= InitializedAt; } }

        public DateTimeOffset LastUpdatedAt
        {
            get
            {
                if (!lastUpdatedAtValue.HasValue)
                {
                    DateTimeOffset result;
                    if (DateTimeOffset.TryParseExact(LastUpdatedAtString, Constants.Iso8601Format, CultureInfo.InvariantCulture, DateTimeStyles.None, out result))
                    {
                        lastUpdatedAtValue = result;
                    }
                    else
                    {
                        LastUpdatedAt = DateTimeOffset.MinValue;
                    }
                }

                return lastUpdatedAtValue.Value;
            }
            set
            {
                LastUpdatedAtString = value.ToString(Constants.Iso8601Format);
                lastUpdatedAtValue = value;
            }
        }

        public DateTimeOffset InitializedAt
        {
            get
            {
                if (!initializedAtValue.HasValue)
                {
                    DateTimeOffset result;
                    if (DateTimeOffset.TryParseExact(InitializedAtString, Constants.Iso8601Format, CultureInfo.InvariantCulture, DateTimeStyles.None, out result))
                    {
                        initializedAtValue = result;
                    }
                    else
                    {
                        InitializedAt = DateTimeOffset.MinValue;
                    }
                }

                return initializedAtValue.Value;
            }
            set
            {
                InitializedAtString = value.ToString(Constants.Iso8601Format);
                initializedAtValue = value;
            }
        }

        public CacheType CacheType { get; private set; }

        protected ILogging Logger { get; private set; }
    }

    class LocalConfigBranchDictionary : Dictionary<string, ConfigBranch>, ILocalConfigBranchDictionary
    {
        public LocalConfigBranchDictionary()
        { }

        public LocalConfigBranchDictionary(IDictionary<string, ConfigBranch> dictionary) : base()
        {
            foreach (var pair in dictionary)
            {
                this.Add(pair.Key, pair.Value);
            }
        }
    }

    public class ArrayContainer<T>
    {
        public T[] Values = new T[0];
    }

    public class StringArrayContainer : ArrayContainer<string>
    {}

    public class ConfigBranchArrayContainer : ArrayContainer<ConfigBranch>
    {}

    class RemoteConfigBranchDictionary : Dictionary<string, Dictionary<string, ConfigBranch>>, IRemoteConfigBranchDictionary
    {
        private string[] keys = new string[0];
        private StringArrayContainer[] subKeys = new StringArrayContainer[0];
        private ConfigBranchArrayContainer[] subKeyValues = new ConfigBranchArrayContainer[0];

        public RemoteConfigBranchDictionary()
        { }

        public RemoteConfigBranchDictionary(Dictionary<string, Dictionary<string, ConfigBranch>> dictionary)
        {
            foreach (var pair in dictionary)
            {
                Add(pair.Key, pair.Value.ToDictionary(valuePair => valuePair.Key, valuePair => valuePair.Value));
            }
        }

        // save the dictionary to lists
        public void OnBeforeSerialize()
        {
            var keyList = new List<string>();
            var subKeysList = new List<StringArrayContainer>();
            var subKeysValuesList = new List<ConfigBranchArrayContainer>();

            foreach (var pair in this)
            {
                var pairKey = pair.Key;
                keyList.Add(pairKey);

                var serializeSubKeys = new List<string>();
                var serializeSubKeyValues = new List<ConfigBranch>();

                var subDictionary = pair.Value;
                foreach (var subPair in subDictionary)
                {
                    serializeSubKeys.Add(subPair.Key);
                    serializeSubKeyValues.Add(subPair.Value);
                }

                subKeysList.Add(new StringArrayContainer { Values = serializeSubKeys.ToArray() });
                subKeysValuesList.Add(new ConfigBranchArrayContainer { Values = serializeSubKeyValues.ToArray() });
            }

            keys = keyList.ToArray();
            subKeys = subKeysList.ToArray();
            subKeyValues = subKeysValuesList.ToArray();
        }

        // load dictionary from lists
        public void OnAfterDeserialize()
        {
            Clear();

            if (keys.Length != subKeys.Length || subKeys.Length != subKeyValues.Length)
            {
                throw new SerializationException("Deserialization length mismatch");
            }

            for (var remoteIndex = 0; remoteIndex < keys.Length; remoteIndex++)
            {
                var remote = keys[remoteIndex];

                var subKeyContainer = subKeys[remoteIndex];
                var subKeyValueContainer = subKeyValues[remoteIndex];

                if (subKeyContainer.Values.Length != subKeyValueContainer.Values.Length)
                {
                    throw new SerializationException("Deserialization length mismatch");
                }

                var branchesDictionary = new Dictionary<string, ConfigBranch>();
                for (var branchIndex = 0; branchIndex < subKeyContainer.Values.Length; branchIndex++)
                {
                    var remoteBranchKey = subKeyContainer.Values[branchIndex];
                    var remoteBranch = subKeyValueContainer.Values[branchIndex];

                    branchesDictionary.Add(remoteBranchKey, remoteBranch);
                }

                Add(remote, branchesDictionary);
            }
        }
    }

    class ConfigRemoteDictionary : Dictionary<string, ConfigRemote>, IConfigRemoteDictionary
    {
        public ConfigRemoteDictionary()
        { }

        public ConfigRemoteDictionary(IDictionary<string, ConfigRemote> dictionary)
        {
            foreach (var pair in dictionary)
            {
                this.Add(pair.Key, pair.Value);
            }
        }
    }

    sealed class RepositoryInfoCache : ManagedCacheBase<RepositoryInfoCache>, IRepositoryInfoCache
    {
        private GitRemote currentGitRemote;
        private GitBranch currentGitBranch;
        private ConfigBranch currentConfigBranch;
        private ConfigRemote currentConfigRemote;

        public RepositoryInfoCache() : base(CacheType.RepositoryInfo)
        { }

        public void UpdateData(IRepositoryInfoCacheData data)
        {
            var now = DateTimeOffset.Now;
            var isUpdated = false;

            if (!Nullable.Equals(currentGitRemote, data.CurrentGitRemote))
            {
                currentGitRemote = data.CurrentGitRemote ?? GitRemote.Default;
                isUpdated = true;
            }

            if (!Nullable.Equals(currentGitBranch, data.CurrentGitBranch))
            {
                currentGitBranch = data.CurrentGitBranch ?? GitBranch.Default;
                isUpdated = true;
            }

            if (!Nullable.Equals(currentConfigRemote, data.CurrentConfigRemote))
            {
                currentConfigRemote = data.CurrentConfigRemote ?? ConfigRemote.Default;
                isUpdated = true;
            }

            if (!Nullable.Equals(currentConfigBranch, data.CurrentConfigBranch))
            {
                currentConfigBranch = data.CurrentConfigBranch ?? ConfigBranch.Default;
                isUpdated = true;
            }

            SaveData(now, isUpdated);
        }

        public GitRemote? CurrentGitRemote
        {
            get
            {
                ValidateData();
                return currentGitRemote.Equals(GitRemote.Default) ? (GitRemote?)null : currentGitRemote;
            }
        }

        public GitBranch? CurrentGitBranch
        {
            get
            {
                ValidateData();
                return currentGitBranch.Equals(GitBranch.Default) ? (GitBranch?)null : currentGitBranch;
            }
        }

        public ConfigRemote? CurrentConfigRemote
        {
            get
            {
                ValidateData();
                return currentConfigRemote.Equals(ConfigRemote.Default) ? (ConfigRemote?)null : currentConfigRemote;
            }
        }

        public ConfigBranch? CurrentConfigBranch
        {
            get
            {
                ValidateData();
                return currentConfigBranch.Equals(ConfigBranch.Default) ? (ConfigBranch?)null : currentConfigBranch;
            }
        }

        public override TimeSpan DataTimeout { get { return TimeSpan.FromDays(1); } }
    }

    sealed class BranchesCache : ManagedCacheBase<BranchesCache>, IBranchCache
    {
        private GitBranch[] localBranches = new GitBranch[0];
        private GitBranch[] remoteBranches = new GitBranch[0];
        private GitRemote[] remotes = new GitRemote[0];

        private LocalConfigBranchDictionary localConfigBranches = new LocalConfigBranchDictionary();
        private RemoteConfigBranchDictionary remoteConfigBranches = new RemoteConfigBranchDictionary();
        private ConfigRemoteDictionary configRemotes = new ConfigRemoteDictionary();

        public BranchesCache() : base(CacheType.Branches)
        { }

        public GitBranch[] LocalBranches
        {
            get { return localBranches; }
            set
            {
                var now = DateTimeOffset.Now;
                var isUpdated = false;

                Logger.Trace("{0} Updating LocalBranches: current:{1} new:{2}", now, localBranches, value);

                var localBranchesIsNull = localBranches == null;
                var valueIsNull = value == null;

                if (localBranchesIsNull != valueIsNull ||
                    !localBranchesIsNull && !localBranches.SequenceEqual(value))
                {
                    localBranches = value;
                    isUpdated = true;
                }

                SaveData(now, isUpdated);
            }
        }

        public GitBranch[] RemoteBranches
        {
            get { return remoteBranches; }
            set
            {
                var now = DateTimeOffset.Now;
                var isUpdated = false;

                Logger.Trace("{0} Updating RemoteBranches: current:{1} new:{2}", now, remoteBranches, value);

                var remoteBranchesIsNull = remoteBranches == null;
                var valueIsNull = value == null;

                if (remoteBranchesIsNull != valueIsNull ||
                    !remoteBranchesIsNull && !remoteBranches.SequenceEqual(value))
                {
                    remoteBranches = value;
                    isUpdated = true;
                }

                SaveData(now, isUpdated);
            }
        }

        public GitRemote[] Remotes
        {
            get { return remotes; }
            set
            {
                var now = DateTimeOffset.Now;
                var isUpdated = false;

                Logger.Trace("{0} Updating Remotes: current:{1} new:{2}", now, remotes, value);

                var remotesIsNull = remotes == null;
                var valueIsNull = value == null;

                if (remotesIsNull != valueIsNull ||
                    !remotesIsNull && !remotes.SequenceEqual(value))
                {
                    remotes = value;
                    isUpdated = true;
                }

                SaveData(now, isUpdated);
            }
        }

        public void RemoveLocalBranch(string branch)
        {
            if (LocalConfigBranches.ContainsKey(branch))
            {
                var now = DateTimeOffset.Now;
                LocalConfigBranches.Remove(branch);
                Logger.Trace("RemoveLocalBranch {0} branch:{1} ", now, branch);
                SaveData(now, true);
            }
            else
            {
                Logger.Warning("Branch {0} is not found", branch);
            }
        }

        public void AddLocalBranch(string branch)
        {
            if (!LocalConfigBranches.ContainsKey(branch))
            {
                var now = DateTimeOffset.Now;
                LocalConfigBranches.Add(branch, new ConfigBranch(branch));
                Logger.Trace("AddLocalBranch {0} branch:{1} ", now, branch);
                SaveData(now, true);
            }
            else
            {
                Logger.Warning("Branch {0} is already present", branch);
            }
        }

        public void AddRemoteBranch(string remote, string branch)
        {
            Dictionary<string, ConfigBranch> branchList;
            if (RemoteConfigBranches.TryGetValue(remote, out branchList))
            {
                if (!branchList.ContainsKey(branch))
                {
                    var now = DateTimeOffset.Now;
                    branchList.Add(branch, new ConfigBranch(branch, ConfigRemotes[remote], null));
                    Logger.Trace("AddRemoteBranch {0} remote:{1} branch:{2} ", now, remote, branch);
                    SaveData(now, true);
                }
                else
                {
                    Logger.Warning("Branch {0} is already present in Remote {1}", branch, remote);
                }
            }
            else
            {
                Logger.Warning("Remote {0} is not found", remote);
            }
        }

        public void RemoveRemoteBranch(string remote, string branch)
        {
            Dictionary<string, ConfigBranch> branchList;
            if (RemoteConfigBranches.TryGetValue(remote, out branchList))
            {
                if (branchList.ContainsKey(branch))
                {
                    var now = DateTimeOffset.Now;
                    branchList.Remove(branch);
                    Logger.Trace("RemoveRemoteBranch {0} remote:{1} branch:{2} ", now, remote, branch);
                    SaveData(now, true);
                }
                else
                {
                    Logger.Warning("Branch {0} is not found in Remote {1}", branch, remote);
                }
            }
            else
            {
                Logger.Warning("Remote {0} is not found", remote);
            }
        }

        public void SetRemotes(Dictionary<string, ConfigRemote> remoteDictionary, Dictionary<string, Dictionary<string, ConfigBranch>> branchDictionary)
        {
            var now = DateTimeOffset.Now;
            configRemotes = new ConfigRemoteDictionary(remoteDictionary);
            remoteConfigBranches = new RemoteConfigBranchDictionary(branchDictionary);
            Logger.Trace("SetRemotes {0}", now);
            SaveData(now, true);
        }

        public void SetLocals(Dictionary<string, ConfigBranch> branchDictionary)
        {
            var now = DateTimeOffset.Now;
            localConfigBranches = new LocalConfigBranchDictionary(branchDictionary);
            Logger.Trace("SetRemotes {0}", now);
            SaveData(now, true);
        }

        public ILocalConfigBranchDictionary LocalConfigBranches { get { return localConfigBranches; } }
        public IRemoteConfigBranchDictionary RemoteConfigBranches { get { return remoteConfigBranches; } }
        public IConfigRemoteDictionary ConfigRemotes { get { return configRemotes; } }
        public override TimeSpan DataTimeout { get { return TimeSpan.FromDays(1); } }
    }

    sealed class GitLogCache : ManagedCacheBase<GitLogCache>, IGitLogCache
    {
        private List<GitLogEntry> log = new List<GitLogEntry>();

        public GitLogCache() : base(CacheType.GitLog)
        { }

        public List<GitLogEntry> Log
        {
            get
            {
                ValidateData();
                return log;
            }
            set
            {
                var now = DateTimeOffset.Now;
                var isUpdated = false;

                Logger.Trace("{0} Updating Log: current:{1} new:{2}", now, log.Count, value.Count);

                if (!log.SequenceEqual(value))
                {
                    log = value;
                    isUpdated = true;
                }

                SaveData(now, isUpdated);
            }
        }

        public override TimeSpan DataTimeout { get { return TimeSpan.FromMinutes(1); } }
    }

    sealed class GitAheadBehindCache : ManagedCacheBase<GitAheadBehindCache>, IGitAheadBehindCache
    {
        private int ahead;
        private int behind;

        public GitAheadBehindCache() : base(CacheType.GitAheadBehind)
        { }

        public int Ahead
        {
            get
            {
                ValidateData();
                return ahead;
            }
            set
            {
                var now = DateTimeOffset.Now;
                var isUpdated = false;

                Logger.Trace("{0} Updating Ahead: current:{1} new:{2}", now, ahead, value);

                if (ahead != value)
                {
                    ahead = value;
                    isUpdated = true;
                }

                SaveData(now, isUpdated);
            }
        }

        public int Behind
        {
            get
            {
                ValidateData();
                return behind;
            }
            set
            {
                var now = DateTimeOffset.Now;
                var isUpdated = false;

                Logger.Trace("{0} Updating Behind: current:{1} new:{2}", now, behind, value);

                if (behind != value)
                {
                    behind = value;
                    isUpdated = true;
                }

                SaveData(now, isUpdated);
            }
        }

        public override TimeSpan DataTimeout { get { return TimeSpan.FromMinutes(1); } }
    }

    sealed class GitStatusCache : ManagedCacheBase<GitStatusCache>, IGitStatusCache
    {
        private List<GitStatusEntry> entries = new List<GitStatusEntry>();

        public GitStatusCache() : base(CacheType.GitStatus)
        { }

        public List<GitStatusEntry> Entries
        {
            get
            {
                ValidateData();
                return entries;
            }
            set
            {
                var now = DateTimeOffset.Now;
                var isUpdated = false;

                Logger.Trace("{0} Updating Entries: current:{1} new:{2}", now, entries.Count, value.Count);

                if (!entries.SequenceEqual(value))
                {
                    entries = value;
                    isUpdated = true;
                }

                SaveData(now, isUpdated);
            }
        }

        public override TimeSpan DataTimeout { get { return TimeSpan.FromMinutes(1); } }
    }

    sealed class GitLocksCache : ManagedCacheBase<GitLocksCache>, IGitLocksCache
    {
        private List<GitLock> gitLocks = new List<GitLock>();

        public GitLocksCache() : base(CacheType.GitLocks)
        { }

        public List<GitLock> GitLocks
        {
            get
            {
                ValidateData();
                return gitLocks;
            }
            set
            {
                var now = DateTimeOffset.Now;
                var isUpdated = false;

                Logger.Trace("{0} Updating GitLocks: current:{1} new:{2}", now, gitLocks.Count, value.Count);

                if (!gitLocks.SequenceEqual(value))
                {
                    gitLocks = value;
                    isUpdated = true;
                }

                SaveData(now, isUpdated);
            }
        }

        public override TimeSpan DataTimeout { get { return TimeSpan.FromMinutes(1); } }
    }

    sealed class GitUserCache : ManagedCacheBase<GitUserCache>, IGitUserCache
    {
        private string gitName;
        private string gitEmail;

        public GitUserCache() : base(CacheType.GitUser)
        {}

        public string Name
        {
            get
            {
                ValidateData();
                return gitName;
            }
            set
            {
                var now = DateTimeOffset.Now;
                var isUpdated = false;

                Logger.Trace("{0} Updating Name: current:{1} new:{2}", now, gitName, value);

                if (gitName != value)
                {
                    gitName = value;
                    isUpdated = true;
                }

                SaveData(now, isUpdated);
            }
        }

        public string Email
        {
            get
            {
                ValidateData();
                return gitEmail;
            }
            set
            {
                var now = DateTimeOffset.Now;
                var isUpdated = false;

                Logger.Trace("{0} Updating Email: current:{1} new:{2}", now, gitEmail, value);

                if (gitEmail != value)
                {
                    gitEmail = value;
                    isUpdated = true;
                }

                SaveData(now, isUpdated);
            }
        }

        public override TimeSpan DataTimeout { get { return TimeSpan.FromMinutes(10); } }
    }
}
