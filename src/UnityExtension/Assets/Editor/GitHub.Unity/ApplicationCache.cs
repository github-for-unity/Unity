using System;
using System.Collections.Generic;
using System.Linq;
using Octokit;
using UnityEditor;
using UnityEngine;
using Application = UnityEngine.Application;

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
        private static readonly TimeSpan DataTimeout = TimeSpan.MaxValue;

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
            SaveData(DateTimeOffset.Now, true);
        }

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

        public RemoteConfigBranchDictionary(IDictionary<string, IDictionary<string, ConfigBranch>> dictionary)
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
                throw new Exception("Deserialization length mismatch");
            }

            for (var remoteIndex = 0; remoteIndex < keys.Length; remoteIndex++)
            {
                var remote = keys[remoteIndex];

                var subKeyContainer = subKeys[remoteIndex];
                var subKeyValueContainer = subKeyValues[remoteIndex];

                if (subKeyContainer.Values.Length != subKeyValueContainer.Values.Length)
                {
                    throw new Exception("Deserialization length mismatch");
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

        IEnumerator<KeyValuePair<string, IDictionary<string, ConfigBranch>>> IEnumerable<KeyValuePair<string, IDictionary<string, ConfigBranch>>>.GetEnumerator()
        {
            throw new NotImplementedException();
            //return AsDictionary
            //    .Select(pair => new KeyValuePair<string, IDictionary<string, ConfigBranch>>(pair.Key, pair.Value.AsDictionary))
            //    .GetEnumerator();
        }

        void ICollection<KeyValuePair<string, IDictionary<string, ConfigBranch>>>.Add(KeyValuePair<string, IDictionary<string, ConfigBranch>> item)
        {
            throw new NotImplementedException();
            //Guard.ArgumentNotNull(item, "item");
            //Guard.ArgumentNotNull(item.Value, "item.Value");
            //
            //var serializableDictionary = item.Value as SerializableDictionary<string, ConfigBranch>;
            //if (serializableDictionary == null)
            //{
            //    serializableDictionary = new SerializableDictionary<string, ConfigBranch>(item.Value);
            //}
            //
            //Add(item.Key, serializableDictionary);
        }

        bool ICollection<KeyValuePair<string, IDictionary<string, ConfigBranch>>>.Contains(KeyValuePair<string, IDictionary<string, ConfigBranch>> item)
        {
            throw new NotImplementedException();
        }

        void ICollection<KeyValuePair<string, IDictionary<string, ConfigBranch>>>.CopyTo(KeyValuePair<string, IDictionary<string, ConfigBranch>>[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        bool ICollection<KeyValuePair<string, IDictionary<string, ConfigBranch>>>.Remove(KeyValuePair<string, IDictionary<string, ConfigBranch>> item)
        {
            throw new NotImplementedException();
        }

        bool ICollection<KeyValuePair<string, IDictionary<string, ConfigBranch>>>.IsReadOnly
        {
            get { throw new NotImplementedException(); }
        }

        void IDictionary<string, IDictionary<string, ConfigBranch>>.Add(string key, IDictionary<string, ConfigBranch> value)
        {
            throw new NotImplementedException();
        }

        bool IDictionary<string, IDictionary<string, ConfigBranch>>.TryGetValue(string key, out IDictionary<string, ConfigBranch> value)
        {
            value = null;
                                    
            Dictionary<string, ConfigBranch> branches;
            if (TryGetValue(key, out branches))
            {
                value = branches;
                return true;
            }
                                    
            return false;
        }

        IDictionary<string, ConfigBranch> IDictionary<string, IDictionary<string, ConfigBranch>>.this[string key]
        {
            get
            {
                throw new NotImplementedException();
                //var dictionary = (IDictionary<string, IDictionary<string, ConfigBranch>>)this;
                //IDictionary<string, ConfigBranch> value;
                //if (!dictionary.TryGetValue(key, out value))
                //{
                //    throw new KeyNotFoundException();
                //}
                //
                //return value;
            }
            set
            {
                throw new NotImplementedException();
                //var dictionary = (IDictionary<string, IDictionary<string, ConfigBranch>>)this;
                //dictionary.Add(key, value);
            }
        }

        ICollection<string> IDictionary<string, IDictionary<string, ConfigBranch>>.Keys
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        ICollection<IDictionary<string, ConfigBranch>> IDictionary<string, IDictionary<string, ConfigBranch>>.Values
        {
            get
            {
                return Values.Cast<IDictionary<string,ConfigBranch>>().ToArray();
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
        public static readonly GitRemote DefaultGitRemote = new GitRemote();
        public static readonly GitBranch DefaultGitBranch = new GitBranch();

        [SerializeField] private string lastUpdatedAtString = DateTimeOffset.MinValue.ToString();
        [SerializeField] private string lastVerifiedAtString = DateTimeOffset.MinValue.ToString();
        [SerializeField] private GitRemote gitRemote;
        [SerializeField] private GitBranch gitBranch;

        public GitRemote? CurrentGitRemote
        {
            get
            {
                ValidateData();
                return gitRemote.Equals(DefaultGitRemote) ? (GitRemote?)null : gitRemote;
            }
            set
            {
                var now = DateTimeOffset.Now;
                var isUpdated = false;

                Logger.Trace("Updating: {0} gitRemote:{1}", now, value);

                if (!Nullable.Equals(gitRemote, value))
                {
                    gitRemote = value ?? DefaultGitRemote;
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
                return gitBranch.Equals(DefaultGitBranch) ? (GitBranch?)null : gitBranch;
            }
            set
            {
                var now = DateTimeOffset.Now;
                var isUpdated = false;

                Logger.Trace("Updating: {0} gitBranch:{1}", now, value);

                if (!Nullable.Equals(gitBranch, value))
                {
                    gitBranch = value ?? DefaultGitBranch;
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
    }

    [Location("cache/branches.yaml", LocationAttribute.Location.LibraryFolder)]
    sealed class BranchCache : ManagedCacheBase<BranchCache>, IBranchCache
    {
        public static readonly ConfigBranch DefaultConfigBranch = new ConfigBranch();
        public static readonly ConfigRemote DefaultConfigRemote = new ConfigRemote();

        [SerializeField] private string lastUpdatedAtString = DateTimeOffset.MinValue.ToString();
        [SerializeField] private string lastVerifiedAtString = DateTimeOffset.MinValue.ToString();

        [SerializeField] private ConfigBranch gitConfigBranch;
        [SerializeField] private ConfigRemote gitConfigRemote;

        [SerializeField] private GitBranch[] localBranches = new GitBranch[0];
        [SerializeField] private GitBranch[] remoteBranches = new GitBranch[0];
        [SerializeField] private GitRemote[] remotes = new GitRemote[0];

        [SerializeField] private LocalConfigBranchDictionary localConfigBranches = new LocalConfigBranchDictionary();
        [SerializeField] private RemoteConfigBranchDictionary remoteConfigBranches = new RemoteConfigBranchDictionary();
        [SerializeField] private ConfigRemoteDictionary configRemotes = new ConfigRemoteDictionary();

        public ConfigRemote? CurrentConfigRemote
        {
            get
            {
                ValidateData();
                return gitConfigRemote.Equals(DefaultConfigRemote) ? (ConfigRemote?)null : gitConfigRemote;
            }
            set
            {
                var now = DateTimeOffset.Now;
                var isUpdated = false;

                Logger.Trace("Updating: {0} gitConfigRemote:{1}", now, value);

                if (!Nullable.Equals(gitConfigRemote, value))
                {
                    gitConfigRemote = value ?? DefaultConfigRemote;
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
                return gitConfigBranch.Equals(DefaultConfigBranch) ? (ConfigBranch?)null : gitConfigBranch;
            }
            set
            {
                var now = DateTimeOffset.Now;
                var isUpdated = false;

                Logger.Trace("Updating: {0} gitConfigBranch:{1}", now, value);

                if (!Nullable.Equals(gitConfigBranch, value))
                {
                    gitConfigBranch = value ?? DefaultConfigBranch;
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
                LocalConfigBranches.Add(branch, new ConfigBranch { Name = branch });
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
            IDictionary<string, ConfigBranch> branchList;
            if (RemoteConfigBranches.TryGetValue(remote, out branchList))
            {
                if (!branchList.ContainsKey(branch))
                {
                    var now = DateTimeOffset.Now;
                    branchList.Add(branch, new ConfigBranch { Name = branch, Remote = ConfigRemotes[remote] });
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
            IDictionary<string, ConfigBranch> branchList;
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

        public void SetRemotes(IDictionary<string, ConfigRemote> remoteDictionary, IDictionary<string, IDictionary<string, ConfigBranch>> branchDictionary)
        {
            var now = DateTimeOffset.Now;
            configRemotes = new ConfigRemoteDictionary(remoteDictionary);
            remoteConfigBranches = new RemoteConfigBranchDictionary(branchDictionary);
            Logger.Trace("SetRemotes {0}", now);
            SaveData(now, true);
        }

        public void SetLocals(IDictionary<string, ConfigBranch> branchDictionary)
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
            set
            {
                var now = DateTimeOffset.Now;
                var isUpdated = false;

                Logger.Trace("Updating: {0} gitStatus:{1}", now, value);

                if (!status.Equals(value))
                {
                    status = value;
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
    }

    [Location("cache/gitlocks.yaml", LocationAttribute.Location.LibraryFolder)]
    sealed class GitLocksCache : ManagedCacheBase<GitLocksCache>, IGitLocksCache
    {
        [SerializeField] private string lastUpdatedAtString = DateTimeOffset.MinValue.ToString();
        [SerializeField] private string lastVerifiedAtString = DateTimeOffset.MinValue.ToString();
        [SerializeField] private List<GitLock> gitLocks = new List<GitLock>();

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
