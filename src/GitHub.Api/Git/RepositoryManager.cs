using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace GitHub.Unity
{
    interface IRepositoryManager : IDisposable
    {
        event Action<bool> OnIsBusyChanged;

        event Action OnCommitChanged;
        event Action OnLocalBranchListChanged;
        event Action OnRemoteBranchListChanged;
        event Action<GitStatus> OnStatusUpdated;
        event Action<ConfigBranch?> OnActiveBranchChanged;
        event Action<ConfigRemote?> OnActiveRemoteChanged;
        event Action<IUser> OnGitUserLoaded;

        event Action<IEnumerable<GitLock>> OnLocksUpdated;
        Dictionary<string, ConfigBranch> LocalBranches { get; }
        Dictionary<string, Dictionary<string, ConfigBranch>> RemoteBranches { get; }
        IGitConfig Config { get; }
        IGitClient GitClient { get; }
        bool IsBusy { get; }
        void Initialize();
        void Start();
        void Stop();
        void Refresh();
        ITask CommitAllFiles(string message, string body);
        ITask CommitFiles(List<string> files, string message, string body);
        ITask<List<GitLogEntry>> Log();
        ITask Fetch(string remote);
        ITask Pull(string remote, string branch);
        ITask Push(string remote, string branch);
        ITask Revert(string changeset);
        ITask RemoteAdd(string remote, string url);
        ITask RemoteRemove(string remote);
        ITask RemoteChange(string remote, string url);
        ITask SwitchBranch(string branch);
        ITask DeleteBranch(string branch, bool deleteUnmerged = false);
        ITask CreateBranch(string branch, string baseBranch);
        ITask ListLocks(bool local);
        ITask LockFile(string file);
        ITask UnlockFile(string file, bool force);
        int WaitForEvents();
    }

    interface IRepositoryPathConfiguration
    {
        NPath RepositoryPath { get; }
        NPath DotGitPath { get; }
        NPath BranchesPath { get; }
        NPath RemotesPath { get; }
        NPath DotGitIndex { get; }
        NPath DotGitHead { get; }
        NPath DotGitConfig { get; }
    }

    class RepositoryPathConfiguration : IRepositoryPathConfiguration
    {
        public RepositoryPathConfiguration(NPath repositoryPath)
        {
            RepositoryPath = repositoryPath;

            DotGitPath = repositoryPath.Combine(".git");
            if (DotGitPath.FileExists())
            {
                DotGitPath =
                    DotGitPath.ReadAllLines()
                              .Where(x => x.StartsWith("gitdir:"))
                              .Select(x => x.Substring(7).Trim().ToNPath())
                              .First();
            }

            BranchesPath = DotGitPath.Combine("refs", "heads");
            RemotesPath = DotGitPath.Combine("refs", "remotes");
            DotGitIndex = DotGitPath.Combine("index");
            DotGitHead = DotGitPath.Combine("HEAD");
            DotGitConfig = DotGitPath.Combine("config");
        }

        public NPath RepositoryPath { get; }
        public NPath DotGitPath { get; }
        public NPath BranchesPath { get; }
        public NPath RemotesPath { get; }
        public NPath DotGitIndex { get; }
        public NPath DotGitHead { get; }
        public NPath DotGitConfig { get; }
    }

    class RepositoryManager : IRepositoryManager
    {
        private readonly Dictionary<string, ConfigBranch> branches = new Dictionary<string, ConfigBranch>();
        private readonly CancellationToken cancellationToken;
        private readonly IGitConfig config;
        private readonly IGitClient gitClient;
        private readonly IPlatform platform;
        private readonly IRepositoryPathConfiguration repositoryPaths;
        private readonly ITaskManager taskManager;
        private readonly IUsageTracker usageTracker;
        private readonly IRepositoryWatcher watcher;

        private string head;
        private bool isBusy;
        private IEnumerable<GitLock> locks;
        private Dictionary<string, Dictionary<string, ConfigBranch>> remoteBranches = new Dictionary<string, Dictionary<string, ConfigBranch>>();
        private Dictionary<string, ConfigRemote> remotes;
        private Action repositoryUpdateCallback;
        private ConfigBranch? activeBranch;

        // internal busy flag signal
        public event Action<bool> OnIsBusyChanged;

        // branches loaded from config file
        public event Action OnLocalBranchListChanged;
        public event Action OnRemoteBranchListChanged;

        public event Action<IEnumerable<GitLock>> OnLocksUpdated;

        public event Action OnCommitChanged;
        public event Action<GitStatus> OnStatusUpdated;
        public event Action<ConfigBranch?> OnActiveBranchChanged;
        public event Action<ConfigRemote?> OnActiveRemoteChanged;
        public event Action<IUser> OnGitUserLoaded;

        public static RepositoryManager CreateInstance(IPlatform platform, ITaskManager taskManager, IUsageTracker usageTracker,
            IGitClient gitClient, NPath repositoryRoot)
        {
            var repositoryPathConfiguration = new RepositoryPathConfiguration(repositoryRoot);
            string filePath = repositoryPathConfiguration.DotGitConfig;
            var gitConfig = new GitConfig(filePath);

            var repositoryWatcher = new RepositoryWatcher(platform, repositoryPathConfiguration, taskManager.Token);

            return new RepositoryManager(platform, taskManager, usageTracker, gitConfig, repositoryWatcher,
                gitClient, repositoryPathConfiguration, taskManager.Token);
        }

        public RepositoryManager(IPlatform platform, ITaskManager taskManager, IUsageTracker usageTracker, IGitConfig gitConfig,
            IRepositoryWatcher repositoryWatcher, IGitClient gitClient,
            IRepositoryPathConfiguration repositoryPaths, CancellationToken cancellationToken)
        {
            this.repositoryPaths = repositoryPaths;
            this.platform = platform;
            this.taskManager = taskManager;
            this.usageTracker = usageTracker;
            this.cancellationToken = cancellationToken;
            this.gitClient = gitClient;
            this.watcher = repositoryWatcher;
            this.config = gitConfig;

            SetupWatcher();
        }

        public void Initialize()
        {
            Logger.Trace("Initialize");
            watcher.Initialize();
        }

        public void Start()
        {
            Logger.Trace("Start");

            ReadHead();
            RefreshConfigData();
            LoadGitUser();
            watcher.Start();
        }

        public void Stop()
        {
            Logger.Trace("Stop");
            watcher.Stop();
        }

        /// <summary>
        /// Never ever call this from any callback that might be triggered by events
        /// raised here. This is not reentrancy safe and will deadlock if you do.
        /// Call this only from a non-callback main thread or preferably only for tests
        /// </summary>
        /// <returns></returns>
        public int WaitForEvents()
        {
            return watcher.CheckAndProcessEvents();
        }

        public void Refresh()
        {
            Logger.Trace("Refresh");
            UpdateGitStatus();
        }

        public ITask CommitAllFiles(string message, string body)
        {
            var add = GitClient.AddAll();
            add.OnStart += t => IsBusy = true;
            return add
                .Then(GitClient.Commit(message, body))
                .Finally(() => IsBusy = false);
        }

        public ITask CommitFiles(List<string> files, string message, string body)
        {
            var add = GitClient.Add(files);
            add.OnStart += t => IsBusy = true;
            return add
                .Then(GitClient.Commit(message, body))
                .Finally(() => IsBusy = false);
        }

        public ITask<List<GitLogEntry>> Log()
        {
            var task = GitClient.Log();
            HookupHandlers(task);
            return task;
        }

        public ITask Fetch(string remote)
        {
            var task = GitClient.Fetch(remote);
            return HookupHandlers(task);
        }

        public ITask Pull(string remote, string branch)
        {
            var task = GitClient.Pull(remote, branch);
            return HookupHandlers(task, true);
        }

        public ITask Push(string remote, string branch)
        {
            var task = GitClient.Push(remote, branch);
            return HookupHandlers(task);
        }

        public ITask Revert(string changeset)
        {
            var task = GitClient.Revert(changeset);
            return HookupHandlers(task);
        }

        public ITask RemoteAdd(string remote, string url)
        {
            var task = GitClient.RemoteAdd(remote, url);
            HookupHandlers(task);
            if (!platform.Environment.IsWindows)
            {
                task.Then(_ => {
                    RefreshConfigData(true);
                });
            }
            return task;
        }

        public ITask RemoteRemove(string remote)
        {
            var task = GitClient.RemoteRemove(remote);
            HookupHandlers(task);
            if (!platform.Environment.IsWindows)
            {
                task.Then(_ => {
                    RefreshConfigData(true);
                });
            }
            return task;
        }

        public ITask RemoteChange(string remote, string url)
        {
            var task = GitClient.RemoteChange(remote, url);
            return HookupHandlers(task);
        }

        public ITask SwitchBranch(string branch)
        {
            var task = GitClient.SwitchBranch(branch);
            return HookupHandlers(task, true);
        }

        public ITask DeleteBranch(string branch, bool deleteUnmerged = false)
        {
            var task = GitClient.DeleteBranch(branch, deleteUnmerged);
            return HookupHandlers(task);
        }

        public ITask CreateBranch(string branch, string baseBranch)
        {
            var task = GitClient.CreateBranch(branch, baseBranch);
            return HookupHandlers(task);
        }

        public ITask ListLocks(bool local)
        {
            var task = GitClient
                .ListLocks(local)
                .Then((s, t) =>
                {
                    if (locks == null || !locks.SequenceEqual(t))
                    {
                        locks = t;
                        Logger.Trace("OnLocksUpdated");
                        OnLocksUpdated(locks);
                    }
                });
            return HookupHandlers(task);
        }

        public ITask LockFile(string file)
        {
            var task = GitClient.Lock(file);
            HookupHandlers(task);
            
            return task.Then(ListLocks(false));
        }

        public ITask UnlockFile(string file, bool force)
        {
            var task = GitClient.Unlock(file, force);
            HookupHandlers(task).Schedule(taskManager);

            return task.Then(ListLocks(false));
        }

        private void LoadGitUser()
        {
            var user = new User();
            GitClient.GetConfig("user.name", GitConfigSource.User)
                .Then((success, value) => user.Name = value).Then(
            GitClient.GetConfig("user.email", GitConfigSource.User)
                .Then((success, value) => user.Email = value))
            .Then(() => OnGitUserLoaded?.Invoke(user))
            .Start();
        }

        private void SetupWatcher()
        {
            watcher.HeadChanged += Watcher_OnHeadChanged;
            watcher.IndexChanged += Watcher_OnIndexChanged;
            watcher.ConfigChanged += Watcher_OnConfigChanged;
            watcher.LocalBranchChanged += Watcher_OnLocalBranchChanged;
            watcher.LocalBranchCreated += Watcher_OnLocalBranchCreated;
            watcher.LocalBranchDeleted += Watcher_OnLocalBranchDeleted;
            watcher.RepositoryChanged += Watcher_OnRepositoryChanged;
            watcher.RemoteBranchCreated += Watcher_OnRemoteBranchCreated;
            watcher.RemoteBranchDeleted += Watcher_OnRemoteBranchDeleted;
        }

        private void ReadHead()
        {
            head = repositoryPaths.DotGitHead.ReadAllLines().FirstOrDefault();
        }

        private ITask HookupHandlers(ITask task, bool disableWatcher = false)
        {
            task.OnStart += t => {
                Logger.Trace("Start " + task.Name);
                IsBusy = true;

                if (disableWatcher)
                {
                    watcher.Stop();
                }
            };

            task.OnEnd += t => {
                if (disableWatcher)
                {
                    watcher.Start();
                }

                IsBusy = false;

                Logger.Trace("Finish " + task.Name);
            };
            return task;
        }

        private void Watcher_OnRemoteBranchDeleted(string remote, string name)
        {
            RemoveRemoteBranch(remote, name);
        }

        private void Watcher_OnRemoteBranchCreated(string remote, string name)
        {
            AddRemoteBranch(remote, name);
        }

        private void Watcher_OnRepositoryChanged()
        {
            UpdateGitStatus();
        }

        private void UpdateGitStatus()
        {
            Logger.Trace("Updating Git Status");

            var task = GitClient.Status()
                .Finally((success, ex, data) =>
                {
                    Logger.Trace($"GitStatus update: {success} {(data.HasValue ? data.Value.ToString() : "null")}");
                    if (success && data.HasValue)
                    {
                        OnStatusUpdated?.Invoke(data.Value);
                    }
                    Logger.Trace("Updated Git Status");
                });

            HookupHandlers(task).Start();
        }

        private void Watcher_OnConfigChanged()
        {
            RefreshConfigData(true);
        }

        private void Watcher_OnHeadChanged(string contents)
        {
            Logger.Trace("Watcher_OnHeadChanged");
            head = contents;
            OnActiveBranchChanged?.Invoke(GetActiveBranch());
            OnActiveRemoteChanged?.Invoke(GetActiveRemote());
            UpdateGitStatus();
        }

        private void Watcher_OnIndexChanged()
        {}

        private void Watcher_OnLocalBranchCreated(string name)
        {
            AddLocalBranch(name);
        }

        private void Watcher_OnLocalBranchDeleted(string name)
        {
            RemoveLocalBranch(name);
        }

        private void Watcher_OnLocalBranchChanged(string name)
        {
            if (name == activeBranch?.Name)
            {
                // commit of current branch changed, trigger OnHeadChanged
                OnCommitChanged?.Invoke();
                UpdateGitStatus();
            }
        }

        private void RefreshConfigData(bool resetConfig = false)
        {
            if (resetConfig)
            {
                config.Reset();
            }

            Logger.Trace("RefreshConfigData");

            LoadBranchesFromConfig();
            LoadRemotesFromConfig();

            OnActiveBranchChanged?.Invoke(GetActiveBranch());
            OnActiveRemoteChanged?.Invoke(GetActiveRemote());
        }

        private void LoadBranchesFromConfig()
        {
            branches.Clear();
            LoadBranchesFromConfig(repositoryPaths.BranchesPath, config.GetBranches().Where(x => x.IsTracking), "");
        }

        private void LoadBranchesFromConfig(NPath path, IEnumerable<ConfigBranch> configBranches, string prefix)
        {
            Logger.Trace("LoadBranchesFromConfig");

            foreach (var file in path.Files())
            {
                var branchName = prefix + file.FileName;
                var branch =
                    configBranches.Where(x => x.Name == branchName).Select(x => x as ConfigBranch?).FirstOrDefault();
                if (!branch.HasValue)
                {
                    branch = new ConfigBranch { Name = branchName };
                }
                branches.Add(branchName, branch.Value);
            }

            foreach (var dir in path.Directories())
            {
                LoadBranchesFromConfig(dir, configBranches, prefix + dir.FileName + "/");
            }
        }

        private void LoadRemotesFromConfig()
        {
            Logger.Trace("LoadRemotesFromConfig");

            remotes = config.GetRemotes().ToDictionary(x => x.Name, x => x);
            remoteBranches = new Dictionary<string, Dictionary<string, ConfigBranch>>();

            foreach (var remote in remotes.Keys)
            {
                var branchList = new Dictionary<string, ConfigBranch>();
                var basedir = repositoryPaths.RemotesPath.Combine(remote);
                if (basedir.Exists())
                {
                    foreach (var branch in
                        basedir.Files(true)
                               .Select(x => x.RelativeTo(basedir))
                               .Select(x => x.ToString(SlashMode.Forward)))
                    {
                        branchList.Add(branch, new ConfigBranch { Name = branch, Remote = remotes[remote] });
                    }

                    remoteBranches.Add(remote, branchList);
                }
            }
        }

        private ConfigRemote? GetActiveRemote(string defaultRemote = "origin")
        {
            if (activeBranch.HasValue && activeBranch.Value.IsTracking)
            {
                return activeBranch.Value.Remote;
            }

            var remote = config.GetRemote(defaultRemote);
            if (remote.HasValue)
            {
                return remote;
            }

            using (var remoteEnumerator = config.GetRemotes().GetEnumerator())
            {
                if (remoteEnumerator.MoveNext())
                {
                    return remoteEnumerator.Current;
                }
            }

            return null;
        }

        private ConfigBranch? GetActiveBranch()
        {
            if (head.StartsWith("ref:"))
            {
                var branch = head.Substring(head.IndexOf("refs/heads/") + "refs/heads/".Length);
                activeBranch = GetBranch(branch);
            }
            else
            {
                activeBranch = null;
            }
            return activeBranch;
        }

        private ConfigBranch? GetBranch(string name)
        {
            if (branches.ContainsKey(name))
            {
                return branches[name];
            }

            return null;
        }

        private void AddLocalBranch(string name)
        {
            if (!branches.ContainsKey(name))
            {
                var branch = config.GetBranch(name);
                if (!branch.HasValue)
                {
                    branch = new ConfigBranch { Name = name };
                }
                branches.Add(name, branch.Value);
                OnLocalBranchListChanged?.Invoke();
            }
        }

        private void RemoveLocalBranch(string oldName)
        {
            if (branches.ContainsKey(oldName))
            {
                branches.Remove(oldName);
                OnLocalBranchListChanged?.Invoke();
            }
        }

        private void AddRemoteBranch(string remote, string name)
        {
            Dictionary<string, ConfigBranch> branchList = null;
            if (remoteBranches.TryGetValue(remote, out branchList))
            {
                if (!branchList.ContainsKey(name))
                {
                    branchList.Add(name, new ConfigBranch { Name = name, Remote = remotes[remote] });
                    OnRemoteBranchListChanged?.Invoke();
                }
            }
        }

        private void RemoveRemoteBranch(string remote, string name)
        {
            Dictionary<string, ConfigBranch> branchList = null;
            if (remoteBranches.TryGetValue(remote, out branchList))
            {
                if (branches.ContainsKey(name))
                {
                    branches.Remove(name);
                    OnRemoteBranchListChanged?.Invoke();
                }
            }
        }

        private bool disposed;

        private void Dispose(bool disposing)
        {
            if (disposed) return;
            disposed = true;

            if (disposing)
            {
                Stop();
                watcher.Dispose();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public Dictionary<string, ConfigBranch> LocalBranches => branches;
        public Dictionary<string, Dictionary<string, ConfigBranch>> RemoteBranches => remoteBranches;

        public IGitConfig Config => config;

        public IGitClient GitClient => gitClient;

        public bool IsBusy
        {
            get { return isBusy; }
            private set
            {
                if (isBusy != value)
                {
                    Logger.Trace("IsBusyChanged Value:{0}", value);
                    isBusy = value;
                    OnIsBusyChanged?.Invoke(isBusy);
                }
            }
        }

        protected static ILogging Logger { get; } = Logging.GetLogger<RepositoryManager>();
    }
}
