using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GitHub.Unity
{
    public interface IRepositoryManager : IDisposable
    {
        event Action<ConfigBranch?> OnCurrentBranchUpdated;
        event Action<ConfigRemote?> OnCurrentRemoteUpdated;
        event Action<IUser> OnGitUserLoaded;
        event Action<bool> OnIsBusyChanged;
        event Action<string> OnLocalBranchAdded;
        event Action<Dictionary<string, ConfigBranch>> OnLocalBranchListUpdated;
        event Action<string> OnLocalBranchRemoved;
        event Action<string> OnLocalBranchUpdated;
        event Action<IList<GitLock>> OnLocksUpdated;
        event Action<string, string> OnRemoteBranchAdded;
        event Action<Dictionary<string, ConfigRemote>, Dictionary<string, Dictionary<string, ConfigBranch>>> OnRemoteBranchListUpdated;
        event Action<string, string> OnRemoteBranchRemoved;
        event Action<GitStatus> OnStatusUpdated;

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

        IGitConfig Config { get; }
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

    class RepositoryManager : IRepositoryManager
    {
        private readonly IGitConfig config;
        private readonly IGitClient gitClient;
        private readonly IPlatform platform;
        private readonly IRepositoryPathConfiguration repositoryPaths;
        private readonly ITaskManager taskManager;
        private readonly IRepositoryWatcher watcher;

        private bool isBusy;

        public event Action<ConfigBranch?> OnCurrentBranchUpdated;
        public event Action<ConfigRemote?> OnCurrentRemoteUpdated;
        public event Action<IUser> OnGitUserLoaded;
        public event Action<bool> OnIsBusyChanged;
        public event Action<string> OnLocalBranchAdded;
        public event Action<Dictionary<string, ConfigBranch>> OnLocalBranchListUpdated;
        public event Action<string> OnLocalBranchRemoved;
        public event Action<string> OnLocalBranchUpdated;
        public event Action<IList<GitLock>> OnLocksUpdated;
        public event Action<string, string> OnRemoteBranchAdded;
        public event Action<Dictionary<string, ConfigRemote>, Dictionary<string, Dictionary<string, ConfigBranch>>> OnRemoteBranchListUpdated;
        public event Action<string, string> OnRemoteBranchRemoved;
        public event Action<GitStatus> OnStatusUpdated;

        public RepositoryManager(IPlatform platform, ITaskManager taskManager, IGitConfig gitConfig,
            IRepositoryWatcher repositoryWatcher, IGitClient gitClient,
            IRepositoryPathConfiguration repositoryPaths)
        {
            this.repositoryPaths = repositoryPaths;
            this.platform = platform;
            this.taskManager = taskManager;
            this.gitClient = gitClient;
            this.watcher = repositoryWatcher;
            this.config = gitConfig;

            SetupWatcher();
        }

        public static RepositoryManager CreateInstance(IPlatform platform, ITaskManager taskManager,
            IGitClient gitClient, NPath repositoryRoot)
        {
            var repositoryPathConfiguration = new RepositoryPathConfiguration(repositoryRoot);
            string filePath = repositoryPathConfiguration.DotGitConfig;
            var gitConfig = new GitConfig(filePath);

            var repositoryWatcher = new RepositoryWatcher(platform, repositoryPathConfiguration, taskManager.Token);

            return new RepositoryManager(platform, taskManager, gitConfig, repositoryWatcher,
                gitClient, repositoryPathConfiguration);
        }

        public void Initialize()
        {
            Logger.Trace("Initialize");
            watcher.Initialize();
        }

        public void Start()
        {
            Logger.Trace("Start");

            UpdateConfigData();
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
                    UpdateConfigData(true);
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
                    UpdateConfigData(true);
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
                .Then((success, locks) =>
                {
                    if (success)
                    {
                        Logger.Trace("OnLocksUpdated");
                        OnLocksUpdated?.Invoke(locks);
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
            GitClient.GetConfigUserAndEmail()
                .Then((success, strings) => {
                    var username = strings[0];
                    var email = strings[1];

                    var user = new User {
                        Name = username,
                        Email = email
                    };

                    Logger.Trace("OnGitUserLoaded: {0}", user);
                    OnGitUserLoaded?.Invoke(user);

                }).Start();
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

        private void UpdateHead()
        {
            var head = repositoryPaths.DotGitHead.ReadAllLines().FirstOrDefault();
            Logger.Trace("UpdateHead: {0}", head ?? "[NULL]");
            UpdateCurrentBranchAndRemote(head);
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
            OnRemoteBranchRemoved?.Invoke(remote, name);
        }

        private void Watcher_OnRemoteBranchCreated(string remote, string name)
        {
            OnRemoteBranchAdded?.Invoke(remote, name);
        }

        private void Watcher_OnRepositoryChanged()
        {
            Logger.Trace("OnRepositoryChanged");
            UpdateGitStatus();
        }

        private void UpdateGitStatus()
        {
            Logger.Trace("Updating Git Status");

            var task = GitClient.Status()
                .Finally((success, ex, data) =>
                {
                    Logger.Trace($"GitStatus update: {success} {(data.HasValue ? data.Value.ToString() : "[null]")}");
                    if (success && data.HasValue)
                    {
                        OnStatusUpdated?.Invoke(data.Value);
                        Logger.Trace("Updated Git Status");
                    }
                });

            HookupHandlers(task).Start();
        }

        private void Watcher_OnConfigChanged()
        {
            UpdateConfigData(true);
        }

        private void Watcher_OnHeadChanged()
        {
            Logger.Trace("Watcher_OnHeadChanged");
            UpdateHead();
            UpdateGitStatus();
        }

        private void UpdateCurrentBranchAndRemote(string head)
        {
            ConfigBranch? branch = null;

            if (head.StartsWith("ref:"))
            {
                var branchName = head.Substring(head.IndexOf("refs/heads/") + "refs/heads/".Length);
                branch = config.GetBranch(branchName);

                if (!branch.HasValue)
                {
                    branch = new ConfigBranch { Name = branchName };
                }
            }

            var defaultRemote = "origin";
            ConfigRemote? remote = null;

            if (branch.HasValue && branch.Value.IsTracking)
            {
                remote = branch.Value.Remote;
            }

            if (!remote.HasValue)
            {
                remote = config.GetRemote(defaultRemote);
            }

            if (!remote.HasValue)
            {
                var configRemotes = config.GetRemotes().ToArray();
                if (configRemotes.Any())
                {
                    remote = configRemotes.FirstOrDefault();
                }
            }

            new ActionTask(taskManager.Token, () => {
                Logger.Trace("OnCurrentBranchUpdated: {0}", branch.HasValue ? branch.Value.ToString() : "[NULL]");
                OnCurrentBranchUpdated?.Invoke(branch);

                Logger.Trace("OnCurrentRemoteUpdated: {0}", remote.HasValue ? remote.Value.ToString() : "[NULL]");
                OnCurrentRemoteUpdated?.Invoke(remote);
            })
            {
                Affinity = TaskAffinity.UI
            }.Start();
        }

        private void Watcher_OnIndexChanged()
        {}

        private void Watcher_OnLocalBranchCreated(string name)
        {
            OnLocalBranchAdded?.Invoke(name);
        }

        private void Watcher_OnLocalBranchDeleted(string name)
        {
            OnLocalBranchRemoved?.Invoke(name);
        }

        private void Watcher_OnLocalBranchChanged(string name)
        {
            OnLocalBranchUpdated?.Invoke(name);
        }

        private void UpdateConfigData(bool resetConfig = false)
        {
            Logger.Trace("UpdateConfigData reset:{0}", resetConfig);

            if (resetConfig)
            {
                config.Reset();
            }

            LoadBranchesFromConfig();
            LoadRemotesFromConfig();
            UpdateHead();
        }

        private void LoadBranchesFromConfig()
        {
            Logger.Trace("LoadBranchesFromConfig");

            var branches = new Dictionary<string, ConfigBranch>();
            LoadBranchesFromConfig(branches, repositoryPaths.BranchesPath, config.GetBranches().Where(x => x.IsTracking), "");

            Logger.Trace("OnLocalBranchListUpdated {0} branches", branches.Count);
            OnLocalBranchListUpdated?.Invoke(branches);
        }

        private void LoadBranchesFromConfig(Dictionary<string, ConfigBranch> branches, NPath path, IEnumerable<ConfigBranch> configBranches, string prefix)
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
                LoadBranchesFromConfig(branches, dir, configBranches, prefix + dir.FileName + "/");
            }
        }

        private void LoadRemotesFromConfig()
        {
            Logger.Trace("LoadRemotesFromConfig");

            var remotes = config.GetRemotes().ToArray().ToDictionary(x => x.Name, x => x);
            var remoteBranches = new Dictionary<string, Dictionary<string, ConfigBranch>>();

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

            Logger.Trace("OnRemoteBranchListUpdated {0} remotes", remotes.Count);
            OnRemoteBranchListUpdated?.Invoke(remotes, remoteBranches);
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
