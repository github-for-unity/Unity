using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace GitHub.Unity
{
    interface IRepositoryManager : IDisposable
    {
        event Action OnActiveBranchChanged;
        event Action OnActiveRemoteChanged;
        event Action<bool> OnIsBusyChanged;
        event Action OnLocalBranchListChanged;
        event Action OnHeadChanged;
        event Action<GitStatus> OnStatusUpdated;
        event Action<IEnumerable<GitLock>> OnLocksUpdated;
        event Action OnRemoteBranchListChanged;
        event Action OnRemoteOrTrackingChanged;
        Task Initialize();
        void Start();
        void Stop();
        void Refresh();
        ITask CommitFiles(List<string> files, string message, string body);
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
        Dictionary<string, ConfigBranch> LocalBranches { get; }
        Dictionary<string, Dictionary<string, ConfigBranch>> RemoteBranches { get; }
        IRepository Repository { get; }
        IGitConfig Config { get; }
        ConfigBranch? ActiveBranch { get; }
        ConfigRemote? ActiveRemote { get; }
        IGitClient GitClient { get; }
        bool IsBusy { get; }
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

    class RepositoryManagerFactory
    {
        public RepositoryManager CreateRepositoryManager(IPlatform platform, ITaskManager taskManager,
            IUsageTracker usageTracker, IGitClient gitClient, NPath repositoryRoot)
        {
            var repositoryPathConfiguration = new RepositoryPathConfiguration(repositoryRoot);
            string filePath = repositoryPathConfiguration.DotGitConfig;
            var gitConfig = new GitConfig(filePath);

            var repositoryWatcher = new RepositoryWatcher(platform, repositoryPathConfiguration, taskManager.Token);

            return new RepositoryManager(platform, taskManager, usageTracker, gitConfig, repositoryWatcher, gitClient,
                repositoryPathConfiguration, taskManager.Token);
        }
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

        private ConfigBranch? activeBranch;
        private ConfigRemote? activeRemote;
        private string head;
        private bool isBusy;
        private IEnumerable<GitLock> locks;
        private Dictionary<string, Dictionary<string, ConfigBranch>> remoteBranches = new Dictionary<string, Dictionary<string, ConfigBranch>>();
        private Dictionary<string, ConfigRemote> remotes;
        private IRepository repository;

        public event Action OnActiveBranchChanged;
        public event Action OnActiveRemoteChanged;
        public event Action OnHeadChanged;
        public event Action<bool> OnIsBusyChanged;
        public event Action OnLocalBranchListChanged;
        public event Action<IEnumerable<GitLock>> OnLocksUpdated;
        public event Action OnRemoteBranchListChanged;
        public event Action OnRemoteOrTrackingChanged;
        public event Action<GitStatus> OnStatusUpdated;

        public RepositoryManager(IPlatform platform, ITaskManager taskManager, IUsageTracker usageTracker,
            IGitConfig gitConfig, IRepositoryWatcher repositoryWatcher, IGitClient gitClient,
            IRepositoryPathConfiguration repositoryPaths, CancellationToken cancellationToken)
        {
            this.repositoryPaths = repositoryPaths;
            this.platform = platform;
            this.taskManager = taskManager;
            this.usageTracker = usageTracker;
            this.cancellationToken = cancellationToken;
            this.gitClient = gitClient;

            config = gitConfig;

            watcher = repositoryWatcher;

            watcher.HeadChanged += Watcher_OnHeadChanged;
            watcher.IndexChanged += Watcher_OnIndexChanged;
            watcher.ConfigChanged += Watcher_OnConfigChanged;
            watcher.LocalBranchChanged += Watcher_OnLocalBranchChanged;
            watcher.LocalBranchCreated += Watcher_OnLocalBranchCreated;
            watcher.LocalBranchDeleted += Watcher_OnLocalBranchDeleted;
            watcher.RepositoryChanged += Watcher_OnRepositoryChanged;
            watcher.RemoteBranchCreated += Watcher_OnRemoteBranchCreated;
            watcher.RemoteBranchDeleted += Watcher_OnRemoteBranchDeleted;

            var remote = config.GetRemote("origin");
            if (!remote.HasValue)
            {
                remote = config.GetRemotes().Where(x => HostAddress
                    .Create(new UriString(x.Url).ToRepositoryUri()).IsGitHubDotCom()).FirstOrDefault();
            }
            UriString cloneUrl = "";
            if (remote.Value.Url != null)
            {
                cloneUrl = new UriString(remote.Value.Url).ToRepositoryUrl();
            }

            repository = new Repository(gitClient, this, repositoryPaths.RepositoryPath.FileName, cloneUrl,
                repositoryPaths.RepositoryPath);
        }

        public async Task Initialize()
        {
            Logger.Trace("Initialize");
            watcher.Initialize();
            repository = await InitializeRepository().SafeAwait();
            Logger.Trace($"Initialize done {Repository}");
        }

        public void Start()
        {
            Logger.Trace("Start");
            watcher.Start();
        }

        public void Stop()
        {
            Logger.Trace("Stop");
            watcher.Stop();
        }

        public void Refresh()
        {
            Logger.Trace("Refresh");
            UpdateGitStatus();
        }

        public ITask CommitFiles(List<string> files, string message, string body)
        {
            var add = GitClient.Add(files);
            add.OnStart += t => IsBusy = true;
            return add
                .Then(GitClient.Commit(message, body))
                .Finally(() => IsBusy = false);
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
                task.Then(_ => UpdateConfigData());
            }
            return task;
        }

        public ITask RemoteRemove(string remote)
        {
            var task = GitClient.RemoteRemove(remote);
            HookupHandlers(task);
            if (!platform.Environment.IsWindows)
            {
                task.Then(_ => UpdateConfigData());
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
            ListLocks(false);
            return task;
        }

        public ITask UnlockFile(string file, bool force)
        {
            var task = GitClient.Unlock(file, force);
            HookupHandlers(task).Schedule(taskManager);
            ListLocks(false);
            return task;
        }

        private ITask HookupHandlers(ITask task, bool disableWatcher = false)
        {
            task.OnStart += t => {
                Logger.Trace("Start " + task.Name);

                if (IsBusy)
                {
                    throw new Exception("System Busy");
                }

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
            UpdateConfigData();
        }

        private void UpdateConfigData()
        {
            config.Reset();
            RefreshConfigData();
        }

        private void Watcher_OnHeadChanged(string contents)
        {
            Logger.Trace("Watcher_OnHeadChanged");
            head = contents;
            ActiveBranch = GetActiveBranch();
            ActiveRemote = GetActiveRemote();
            OnHeadChanged?.Invoke();
            UpdateGitStatus();
        }

        private void Watcher_OnIndexChanged()
        { }

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
            if (name == ActiveBranch?.Name)
            {
                OnActiveBranchChanged?.Invoke();
                UpdateGitStatus();
            }
        }

        private async Task<IRepository> InitializeRepository()
        {
            head = repositoryPaths.DotGitHead.ReadAllLines().FirstOrDefault();

            RefreshConfigData();

            var user = new User();

            var res = await GitClient.GetConfig("user.name", GitConfigSource.User).StartAwait();
            user.Name = res;

            res = await gitClient.GetConfig("user.email", GitConfigSource.User).StartAwait();
            if (res == null)
            {
                throw new InvalidOperationException("No user configured");
            }

            user.Email = res;
            repository.User = user;
            return repository;
        }

        private void RefreshConfigData()
        {
            Logger.Trace("RefreshConfigData");
            LoadBranchesFromConfig();
            LoadRemotesFromConfig();

            ActiveBranch = GetActiveBranch();
            ActiveRemote = GetActiveRemote();
        }

        private void LoadBranchesFromConfig()
        {
            branches.Clear();
            LoadBranchesFromConfig(repositoryPaths.BranchesPath, config.GetBranches().Where(x => x.IsTracking), "");
        }

        private void LoadBranchesFromConfig(NPath path, IEnumerable<ConfigBranch> configBranches, string prefix)
        {
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
            var branch = ActiveBranch;
            if (branch.HasValue && branch.Value.IsTracking)
            {
                return branch.Value.Remote;
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
                return GetBranch(branch);
            }
            else
            {
                return null;
            }
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

        public IRepository Repository => repository;
        public IGitConfig Config => config;

        public ConfigBranch? ActiveBranch
        {
            get { return activeBranch; }
            private set
            {
                if (activeBranch.HasValue != value.HasValue ||
                    activeBranch.HasValue && !activeBranch.Value.Equals(value.Value))
                {
                    activeBranch = value;
                    Logger.Trace("OnActiveBranchChanged: {0}", value?.ToString() ?? "NULL");
                    OnActiveBranchChanged?.Invoke();
                }
            }
        }

        public ConfigRemote? ActiveRemote
        {
            get { return activeRemote; }
            private set
            {
                if (activeRemote.HasValue != value.HasValue ||
                    activeRemote.HasValue && !activeRemote.Value.Equals(value.Value))
                {
                    activeRemote = value;
                    Logger.Trace("OnActiveRemoteChanged: {0}", value?.ToString() ?? "NULL");
                    OnActiveRemoteChanged?.Invoke();
                }
            }
        }

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
