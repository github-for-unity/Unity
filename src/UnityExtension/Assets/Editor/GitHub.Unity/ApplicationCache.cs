using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.Serialization;
using UnityEditor;
using UnityEngine;
using Application = UnityEngine.Application;

namespace GitHub.Unity
{
    [Serializable]
    public class SerializationException : Exception
    {
        public SerializationException() : base()
        { }
        public SerializationException(string message) : base(message)
        { }
        public SerializationException(string message, Exception innerException) : base(message, innerException)
        { }
        protected SerializationException(SerializationInfo info, StreamingContext context) : base(info, context)
        { }
    }

    sealed class ApplicationCache : ScriptObjectSingleton<ApplicationCache>
    {
        [SerializeField] private bool firstRun = true;
        [SerializeField] public string firstRunAtString;
        [NonSerialized] private bool? firstRunValue;
        [NonSerialized] public DateTimeOffset? firstRunAtValue;

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
                    environment = new DefaultEnvironment(new CacheContainer());
                    if (unityApplication == null)
                    {
                        unityAssetsPath = Application.dataPath;
                        unityApplication = EditorApplication.applicationPath;
                        extensionInstallPath = DetermineInstallationPath();
                        unityVersion = Application.unityVersion;
                    }
                    environment.Initialize(unityVersion, extensionInstallPath.ToNPath(), unityApplication.ToNPath(),
                        unityAssetsPath.ToNPath());
                    environment.InitializeRepository(!String.IsNullOrEmpty(repositoryPath)
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
        [NonSerialized] private DateTimeOffset? lastUpdatedAtValue;
        [NonSerialized] private DateTimeOffset? lastVerifiedAtValue;
        [NonSerialized] private DateTimeOffset? initializedAtValue;
        [NonSerialized] private readonly bool invalidOnFirstRun;

        public event Action CacheInvalidated;
        public event Action<DateTimeOffset> CacheUpdated;

        protected ManagedCacheBase(bool invalidOnFirstRun)
        {
            this.invalidOnFirstRun = invalidOnFirstRun;
            Logger = Logging.GetLogger(GetType());
        }

        public void ValidateData()
        {
            var initialized = ValidateInitialized();
            if (initialized)
            {
                if (DateTimeOffset.Now - LastUpdatedAt > DataTimeout)
                {
                    Logger.Trace("Timeout Invalidation");
                    InvalidateData();
                }
            }
        }

        private bool ValidateInitialized()
        {
            var isInitialized = IsInitialized;
            if (!isInitialized)
            {
                Logger.Trace("Not Initialized");

                if (invalidOnFirstRun)
                {
                    InvalidateData();
                }
            }

            return isInitialized;
        }

        public void InvalidateData()
        {
            Logger.Trace("Invalidate");
            CacheInvalidated.SafeInvoke();
        }

        protected void SaveData(DateTimeOffset now, bool isUpdated)
        {
            if (!IsInitialized)
            {
                InitializedAt = now;
            }

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

        public abstract TimeSpan DataTimeout { get; }
        public abstract string LastUpdatedAtString { get; protected set; }
        public abstract string LastVerifiedAtString { get; protected set; }
        public abstract string InitializedAtString { get; protected set; }

        public bool IsInitialized
        {
            get { return ApplicationCache.Instance.FirstRunAt <= InitializedAt; }
        }

        public DateTimeOffset LastUpdatedAt
        {
            get
            {
                if (!lastUpdatedAtValue.HasValue)
                {
                    ValidateInitialized();

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

        public DateTimeOffset LastVerifiedAt
        {
            get
            {
                if (!lastVerifiedAtValue.HasValue)
                {
                    ValidateInitialized();

                    DateTimeOffset result;
                    if (DateTimeOffset.TryParseExact(LastVerifiedAtString, Constants.Iso8601Format, CultureInfo.InvariantCulture, DateTimeStyles.None, out result))
                    {
                        lastVerifiedAtValue = result;
                    }
                    else
                    {
                        LastVerifiedAt = DateTimeOffset.MinValue;
                    }
                }

                return lastVerifiedAtValue.Value;
            }
            set
            {
                LastVerifiedAtString = value.ToString(Constants.Iso8601Format);
                lastVerifiedAtValue = value;
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

        protected ILogging Logger { get; private set; }
    }

    [Serializable]
    class LocalConfigBranchDictionary : SerializableDictionary<string, ConfigBranch>, ILocalConfigBranchDictionary
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

    [Serializable]
    public class ArrayContainer<T>
    {
        [SerializeField] public T[] Values = new T[0];
    }

    [Serializable]
    public class StringArrayContainer: ArrayContainer<string>
    {
    }

    [Serializable]
    public class ConfigBranchArrayContainer : ArrayContainer<ConfigBranch>
    {
    }

    [Serializable]
    class RemoteConfigBranchDictionary : Dictionary<string, Dictionary<string, ConfigBranch>>, ISerializationCallbackReceiver, IRemoteConfigBranchDictionary
    {
        [SerializeField] private string[] keys = new string[0];
        [SerializeField] private StringArrayContainer[] subKeys = new StringArrayContainer[0];
        [SerializeField] private ConfigBranchArrayContainer[] subKeyValues = new ConfigBranchArrayContainer[0];

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

    [Serializable]
    class ConfigRemoteDictionary : SerializableDictionary<string, ConfigRemote>, IConfigRemoteDictionary
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

    [Location("cache/repoinfo.yaml", LocationAttribute.Location.LibraryFolder)]
    sealed class RepositoryInfoCache : ManagedCacheBase<RepositoryInfoCache>, IRepositoryInfoCache
    {
        [SerializeField] private string lastUpdatedAtString = DateTimeOffset.MinValue.ToString(Constants.Iso8601Format);
        [SerializeField] private string lastVerifiedAtString = DateTimeOffset.MinValue.ToString(Constants.Iso8601Format);
        [SerializeField] private string firstInitializedAtString = DateTimeOffset.MinValue.ToString(Constants.Iso8601Format);
        [SerializeField] private GitRemote gitRemote;
        [SerializeField] private GitBranch gitBranch;

        public RepositoryInfoCache() : base(false)
        { }

        public GitRemote? CurrentGitRemote
        {
            get
            {
                ValidateData();
                return gitRemote.Equals(GitRemote.Default) ? (GitRemote?)null : gitRemote;
            }
            set
            {
                var now = DateTimeOffset.Now;
                var isUpdated = false;

                Logger.Trace("Updating: {0} gitRemote:{1}", now, value);

                if (!Nullable.Equals(gitRemote, value))
                {
                    gitRemote = value ?? GitRemote.Default;
                    isUpdated = true;
                }

                SaveData(now, isUpdated);
            }
        }

        public GitBranch? CurentGitBranch
        {
            get
            {
                ValidateData();
                return gitBranch.Equals(GitBranch.Default) ? (GitBranch?)null : gitBranch;
            }
            set
            {
                var now = DateTimeOffset.Now;
                var isUpdated = false;

                Logger.Trace("Updating: {0} gitBranch:{1}", now, value);

                if (!Nullable.Equals(gitBranch, value))
                {
                    gitBranch = value ?? GitBranch.Default;
                    isUpdated = true;
                }

                SaveData(now, isUpdated);
            }
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

        public override string InitializedAtString
        {
            get { return firstInitializedAtString; }
            protected set { firstInitializedAtString = value; }
        }

        public override TimeSpan DataTimeout
        {
            get { return TimeSpan.MaxValue; }
        }
    }

    [Location("cache/branches.yaml", LocationAttribute.Location.LibraryFolder)]
    sealed class BranchCache : ManagedCacheBase<BranchCache>, IBranchCache
    {
        [SerializeField] private string lastUpdatedAtString = DateTimeOffset.MinValue.ToString(Constants.Iso8601Format);
        [SerializeField] private string lastVerifiedAtString = DateTimeOffset.MinValue.ToString(Constants.Iso8601Format);
        [SerializeField] private string initializedAtString = DateTimeOffset.MinValue.ToString(Constants.Iso8601Format);

        [SerializeField] private ConfigBranch gitConfigBranch;
        [SerializeField] private ConfigRemote gitConfigRemote;

        [SerializeField] private GitBranch[] localBranches = new GitBranch[0];
        [SerializeField] private GitBranch[] remoteBranches = new GitBranch[0];
        [SerializeField] private GitRemote[] remotes = new GitRemote[0];

        [SerializeField] private LocalConfigBranchDictionary localConfigBranches = new LocalConfigBranchDictionary();
        [SerializeField] private RemoteConfigBranchDictionary remoteConfigBranches = new RemoteConfigBranchDictionary();
        [SerializeField] private ConfigRemoteDictionary configRemotes = new ConfigRemoteDictionary();

        public BranchCache() : base(false)
        { }

        public ConfigRemote? CurrentConfigRemote
        {
            get
            {
                ValidateData();
                return gitConfigRemote.Equals(ConfigRemote.Default) ? (ConfigRemote?)null : gitConfigRemote;
            }
            set
            {
                var now = DateTimeOffset.Now;
                var isUpdated = false;

                Logger.Trace("Updating: {0} gitConfigRemote:{1}", now, value);

                if (!Nullable.Equals(gitConfigRemote, value))
                {
                    gitConfigRemote = value ?? ConfigRemote.Default;
                    isUpdated = true;
                }

                SaveData(now, isUpdated);
            }
        }

        public ConfigBranch? CurentConfigBranch
        {
            get
            {
                ValidateData();
                return gitConfigBranch.Equals(ConfigBranch.Default) ? (ConfigBranch?)null : gitConfigBranch;
            }
            set
            {
                var now = DateTimeOffset.Now;
                var isUpdated = false;

                Logger.Trace("Updating: {0} gitConfigBranch:{1}", now, value);

                if (!Nullable.Equals(gitConfigBranch, value))
                {
                    gitConfigBranch = value ?? ConfigBranch.Default;
                    isUpdated = true;
                }

                SaveData(now, isUpdated);
            }
        }

        public GitBranch[] LocalBranches
        {
            get { return localBranches; }
            set
            {
                var now = DateTimeOffset.Now;
                var isUpdated = false;

                Logger.Trace("Updating: {0} localBranches:{1}", now, value);

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

        public ILocalConfigBranchDictionary LocalConfigBranches
        {
            get { return localConfigBranches; }
        }

        public GitBranch[] RemoteBranches
        {
            get { return remoteBranches; }
            set
            {
                var now = DateTimeOffset.Now;
                var isUpdated = false;

                Logger.Trace("Updating: {0} remoteBranches:{1}", now, value);

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

        public IRemoteConfigBranchDictionary RemoteConfigBranches
        {
            get { return remoteConfigBranches; }
        }

        public GitRemote[] Remotes
        {
            get { return remotes; }
            set
            {
                var now = DateTimeOffset.Now;
                var isUpdated = false;

                Logger.Trace("Updating: {0} remotes:{1}", now, value);

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

        public IConfigRemoteDictionary ConfigRemotes
        {
            get { return configRemotes; }
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
                    branchList.Add(branch, new ConfigBranch(branch,ConfigRemotes[remote]));
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

        public override string InitializedAtString
        {
            get { return initializedAtString; }
            protected set { initializedAtString = value; }
        }

        public override TimeSpan DataTimeout
        {
            get { return TimeSpan.MaxValue; }
        }
    }

    [Location("cache/gitlog.yaml", LocationAttribute.Location.LibraryFolder)]
    sealed class GitLogCache : ManagedCacheBase<GitLogCache>, IGitLogCache
    {
        [SerializeField] private string lastUpdatedAtString = DateTimeOffset.MinValue.ToString(Constants.Iso8601Format);
        [SerializeField] private string lastVerifiedAtString = DateTimeOffset.MinValue.ToString(Constants.Iso8601Format);
        [SerializeField] private string initializedAtString = DateTimeOffset.MinValue.ToString(Constants.Iso8601Format);
        [SerializeField] private List<GitLogEntry> log = new List<GitLogEntry>();

        public GitLogCache() : base(true)
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

                Logger.Trace("Updating: {0} gitLog:{1}", now, value);

                if (!log.SequenceEqual(value))
                {
                    log = value;
                    isUpdated = true;
                }

                SaveData(now, isUpdated);
            }
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

        public override string InitializedAtString
        {
            get { return initializedAtString; }
            protected set { initializedAtString = value; }
        }

        public override TimeSpan DataTimeout
        {
            get { return TimeSpan.FromMinutes(1); }
        }
    }

    [Location("cache/gittrackingstatus.yaml", LocationAttribute.Location.LibraryFolder)]
    sealed class GitTrackingStatusCache : ManagedCacheBase<GitTrackingStatusCache>, IGitTrackingStatusCache
    {
        [SerializeField] private string lastUpdatedAtString = DateTimeOffset.MinValue.ToString(Constants.Iso8601Format);
        [SerializeField] private string lastVerifiedAtString = DateTimeOffset.MinValue.ToString(Constants.Iso8601Format);
        [SerializeField] private string initializedAtString = DateTimeOffset.MinValue.ToString(Constants.Iso8601Format);
        [SerializeField] private int ahead;
        [SerializeField] private int behind;

        public GitTrackingStatusCache() : base(true)
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

                Logger.Trace("Updating: {0} ahead:{1}", now, value);

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

                Logger.Trace("Updating: {0} behind:{1}", now, value);

                if (behind != value)
                {
                    behind = value;
                    isUpdated = true;
                }

                SaveData(now, isUpdated);
            }
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

        public override string InitializedAtString
        {
            get { return initializedAtString; }
            protected set { initializedAtString = value; }
        }

        public override TimeSpan DataTimeout
        {
            get { return TimeSpan.FromMinutes(1); }
        }
    }

     [Location("cache/gitstatusentries.yaml", LocationAttribute.Location.LibraryFolder)]
    sealed class GitStatusEntriesCache : ManagedCacheBase<GitStatusEntriesCache>, IGitStatusEntriesCache
    {
        [SerializeField] private string lastUpdatedAtString = DateTimeOffset.MinValue.ToString(Constants.Iso8601Format);
        [SerializeField] private string lastVerifiedAtString = DateTimeOffset.MinValue.ToString(Constants.Iso8601Format);
        [SerializeField] private string initializedAtString = DateTimeOffset.MinValue.ToString(Constants.Iso8601Format);
        [SerializeField] private List<GitStatusEntry> entries = new List<GitStatusEntry>();

        public GitStatusEntriesCache() : base(true)
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

                Logger.Trace("Updating: {0} entries:{1}", now, value.Count);

                if (!entries.SequenceEqual(value))
                {
                    entries = value;
                    isUpdated = true;
                }

                SaveData(now, isUpdated);
            }
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

        public override string InitializedAtString
        {
            get { return initializedAtString; }
            protected set { initializedAtString = value; }
        }

        public override TimeSpan DataTimeout
        {
            get { return TimeSpan.FromMinutes(1); }
        }
    }

    [Location("cache/gitlocks.yaml", LocationAttribute.Location.LibraryFolder)]
    sealed class GitLocksCache : ManagedCacheBase<GitLocksCache>, IGitLocksCache
    {
        [SerializeField] private string lastUpdatedAtString = DateTimeOffset.MinValue.ToString(Constants.Iso8601Format);
        [SerializeField] private string lastVerifiedAtString = DateTimeOffset.MinValue.ToString(Constants.Iso8601Format);
        [SerializeField] private string initializedAtString = DateTimeOffset.MinValue.ToString(Constants.Iso8601Format);
        [SerializeField] private List<GitLock> gitLocks = new List<GitLock>();

        public GitLocksCache() : base(true)
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

                Logger.Trace("Updating: {0} gitLocks:{1}", now, value);

                if (!gitLocks.SequenceEqual(value))
                {
                    gitLocks = value;
                    isUpdated = true;
                }

                SaveData(now, isUpdated);
            }
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

        public override string InitializedAtString
        {
            get { return initializedAtString; }
            protected set { initializedAtString = value; }
        }

        public override TimeSpan DataTimeout
        {
            get { return TimeSpan.FromMinutes(1); }
        }
    }

    [Location("cache/gituser.yaml", LocationAttribute.Location.LibraryFolder)]
    sealed class GitUserCache : ManagedCacheBase<GitUserCache>, IGitUserCache
    {
        [SerializeField] private string lastUpdatedAtString = DateTimeOffset.MinValue.ToString(Constants.Iso8601Format);
        [SerializeField] private string lastVerifiedAtString = DateTimeOffset.MinValue.ToString(Constants.Iso8601Format);
        [SerializeField] private string initializedAtString = DateTimeOffset.MinValue.ToString(Constants.Iso8601Format);
        [SerializeField] private string gitName;
        [SerializeField] private string gitEmail;

        public GitUserCache() : base(true)
        { }

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

                Logger.Trace("Updating: {0} Name:{1}", now, value);

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

                Logger.Trace("Updating: {0} Email:{1}", now, value);

                if (gitEmail != value)
                {
                    gitEmail = value;
                    isUpdated = true;
                }

                SaveData(now, isUpdated);
            }
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

        public override string InitializedAtString
        {
            get { return initializedAtString; }
            protected set { initializedAtString = value; }
        }

        public override TimeSpan DataTimeout
        {
            get { return TimeSpan.FromMinutes(10); }
        }
    }
}
