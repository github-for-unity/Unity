using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace GitHub.Unity
{
    interface IRepositoryManager : IDisposable
    {
        void Refresh();
        void Fetch(ITaskResultDispatcher<string> resultDispatcher, string remote);
        void Pull(ITaskResultDispatcher<string> resultDispatcher, string remote, string branch);
        void Push(ITaskResultDispatcher<string> resultDispatcher, string remote, string branch);
        void SwitchBranch(ITaskResultDispatcher<string> resultDispatcher, string branch);
        void DeleteBranch(ITaskResultDispatcher<string> resultDispatcher, string branch, bool deleteUnmerged = false);
        void CreateBranch(ITaskResultDispatcher<string> resultDispatcher, string branch, string baseBranch);
        void RemoteRemove(ITaskResultDispatcher<string> resultDispatcher, string remote);
        void RemoteAdd(ITaskResultDispatcher<string> resultDispatcher, string remote, string url);
        void CommitFiles(TaskResultDispatcher<string> resultDispatcher, List<string> files, string message, string body);
        void ListLocks(bool local);
        void LockFile(ITaskResultDispatcher<string> resultDispatcher, string file);
        void UnlockFile(ITaskResultDispatcher<string> resultDispatcher, string file, bool force);
        void Initialize();
        void Start();

        IGitConfig Config { get; }
        bool IsBusy { get; }
        ConfigBranch? ActiveBranch { get; }
        ConfigRemote? ActiveRemote { get; }
        IRepositoryProcessRunner ProcessRunner { get; }
        Dictionary<string, ConfigBranch> LocalBranches { get; }
        Dictionary<string, Dictionary<string, ConfigBranch>> RemoteBranches { get; }

        event Action OnActiveBranchChanged;
        event Action OnActiveRemoteChanged;
        event Action OnRemoteBranchListChanged;
        event Action<bool> OnIsBusyChanged;
        event Action OnLocalBranchListChanged;
        event Action<GitStatus> OnRepositoryChanged;
        event Action OnHeadChanged;
        event Action OnRemoteOrTrackingChanged;
        event Action<IEnumerable<GitLock>> OnLocksUpdated;
        void RemoteChange(ITaskResultDispatcher<string> resultDispatcher, string remote, string url);
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
                              .Select(x => x.Substring(7).Trim())
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
        public RepositoryManager CreateRepositoryManager(IPlatform platform, ITaskRunner taskRunner, NPath repositoryRoot,
            CancellationToken cancellationToken)
        {
            var repositoryPathConfiguration = new RepositoryPathConfiguration(repositoryRoot);
            string filePath = repositoryPathConfiguration.DotGitConfig;
            var gitConfig = new GitConfig(new GitConfigFileManager(filePath));

            var repositoryWatcher = new RepositoryWatcher(platform, repositoryPathConfiguration, cancellationToken);

            var repositoryProcessRunner = new RepositoryProcessRunner(platform.Environment, platform.ProcessManager,
                platform.CredentialManager, platform.UIDispatcher, cancellationToken);

            return new RepositoryManager(platform, taskRunner, gitConfig, repositoryWatcher,
                repositoryProcessRunner, repositoryPathConfiguration, cancellationToken);
        }
    }

    class RepositoryManager : IRepositoryManager
    {
        private readonly Dictionary<string, ConfigBranch> branches = new Dictionary<string, ConfigBranch>();
        private readonly CancellationToken cancellationToken;
        private readonly IGitConfig config;
        private readonly IPlatform platform;
        private readonly ITaskRunner taskRunner;
        private readonly IRepository repository;
        private readonly IRepositoryPathConfiguration repositoryPaths;
        private readonly IRepositoryProcessRunner repositoryProcessRunner;
        private readonly IRepositoryWatcher watcher;

        private ConfigBranch? activeBranch;
        private ConfigRemote? activeRemote;
        private Action repositoryUpdateCallback;
        private bool handlingRepositoryUpdate;
        private string head;
        private bool isBusy;
        private DateTime lastLocksUpdate;
        private DateTime lastStatusUpdate;
        private Dictionary<string, Dictionary<string, ConfigBranch>> remoteBranches = new Dictionary<string, Dictionary<string, ConfigBranch>>();
        private Dictionary<string, ConfigRemote> remotes;
        private IEnumerable<GitLock> locks;

        public event Action OnActiveBranchChanged;
        public event Action OnActiveRemoteChanged;
        public event Action OnRemoteBranchListChanged;
        public event Action OnLocalBranchListChanged;
        public event Action<GitStatus> OnRepositoryChanged;
        public event Action OnHeadChanged;
        public event Action<bool> OnIsBusyChanged;
        public event Action OnRemoteOrTrackingChanged;
        public event Action<IEnumerable<GitLock>> OnLocksUpdated;

        public RepositoryManager(IPlatform platform, ITaskRunner taskRunner, IGitConfig gitConfig,
            IRepositoryWatcher repositoryWatcher, IRepositoryProcessRunner repositoryProcessRunner,
            IRepositoryPathConfiguration repositoryPaths, CancellationToken cancellationToken)
        {
            this.repositoryPaths = repositoryPaths;
            this.platform = platform;
            this.taskRunner = taskRunner;
            this.cancellationToken = cancellationToken;
            this.repositoryProcessRunner = repositoryProcessRunner;

            config = gitConfig;
            repository = InitializeRepository();

            watcher = repositoryWatcher;

            watcher.HeadChanged += HeadChanged;
            watcher.IndexChanged += OnIndexChanged;
            watcher.ConfigChanged += OnConfigChanged;
            watcher.LocalBranchChanged += OnLocalBranchChanged;
            watcher.LocalBranchCreated += OnLocalBranchCreated;
            watcher.LocalBranchDeleted += OnLocalBranchDeleted;
            watcher.RepositoryChanged += OnRepositoryUpdated;
            watcher.RemoteBranchCreated += OnRemoteBranchCreated;
            watcher.RemoteBranchChanged += OnRemoteBranchChanged;
            watcher.RemoteBranchDeleted += OnRemoteBranchDeleted;

            const int debounceTimeout = 0;

            repositoryUpdateCallback = debounceTimeout == 0 ?
                OnRepositoryUpdatedHandler 
                : TaskExtensions.Debounce(OnRepositoryUpdatedHandler, debounceTimeout);
        }

        public void Initialize()
        {
            Logger.Trace("Initialize");
            watcher.Initialize();
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
            OnRepositoryUpdated();
        }

        public void CommitFiles(TaskResultDispatcher<string> resultDispatcher, List<string> files, string message, string body)
        {
            var task = ProcessRunner.PrepareGitAdd(resultDispatcher, files);
            PrepareTask(task);
            var taskCommit = ProcessRunner.PrepareGitCommit(resultDispatcher, message, body);
            PrepareTask(taskCommit);
            taskRunner.AddTask(task);
            taskRunner.AddTask(taskCommit);
        }

        public void Fetch(ITaskResultDispatcher<string> resultDispatcher, string remote)
        {
            var task = ProcessRunner.PrepareGitFetch(resultDispatcher, remote);
            PrepareTask(task);
            taskRunner.AddTask(task);
        }

        public void Pull(ITaskResultDispatcher<string> resultDispatcher, string remote, string branch)
        {
            var task = ProcessRunner.PrepareGitPull(resultDispatcher, remote, branch);
            PrepareTask(task, true);
            taskRunner.AddTask(task);
        }

        public void Push(ITaskResultDispatcher<string> resultDispatcher, string remote, string branch)
        {
            var task = ProcessRunner.PrepareGitPush(resultDispatcher, remote, branch);
            PrepareTask(task);
            taskRunner.AddTask(task);
        }

        public void RemoteAdd(ITaskResultDispatcher<string> resultDispatcher, string remote, string url)
        {
            var task = ProcessRunner.PrepareGitRemoteAdd(new TaskResultDispatcher<string>(s => {
                resultDispatcher.ReportSuccess(s);
                if (!platform.Environment.IsWindows)
                    OnConfigChanged();
            }, resultDispatcher.ReportFailure), remote, url);

            PrepareTask(task);
            taskRunner.AddTask(task);
        }

        public void RemoteRemove(ITaskResultDispatcher<string> resultDispatcher, string remote)
        {
            var task = ProcessRunner.PrepareGitRemoteRemove(new TaskResultDispatcher<string>(s => {
                resultDispatcher.ReportSuccess(s);
                if (!platform.Environment.IsWindows)
                    OnConfigChanged();
            }, resultDispatcher.ReportFailure), remote);

            PrepareTask(task);
            taskRunner.AddTask(task);
        }

        public void RemoteChange(ITaskResultDispatcher<string> resultDispatcher, string remote, string url)
        {
            var task = ProcessRunner.PrepareGitRemoteChange(resultDispatcher, remote, url);
            PrepareTask(task);
            taskRunner.AddTask(task);
        }

        public void SwitchBranch(ITaskResultDispatcher<string> resultDispatcher, string branch)
        {
            var task = ProcessRunner.PrepareSwitchBranch(resultDispatcher, branch);
            PrepareTask(task, true);
            taskRunner.AddTask(task);
        }

        public void DeleteBranch(ITaskResultDispatcher<string> resultDispatcher, string branch, bool deleteUnmerged = false)
        {
            var task = ProcessRunner.PrepareDeleteBranch(resultDispatcher, branch, deleteUnmerged);
            PrepareTask(task);
            taskRunner.AddTask(task);
        }

        public void CreateBranch(ITaskResultDispatcher<string> resultDispatcher, string branch, string baseBranch)
        {
            var task = ProcessRunner.PrepareCreateBranch(resultDispatcher, branch, baseBranch);
            PrepareTask(task);
            taskRunner.AddTask(task);
        }

        public void ListLocks(bool local)
        {
            var dispatcher = new TaskResultDispatcher<IEnumerable<GitLock>>(l =>
            {
                if (locks == null || !locks.SequenceEqual(l))
                {
                    locks = l;

                    Logger.Trace("OnLocksUpdated");
                    OnLocksUpdated(locks);
                }
            });

            var task = ProcessRunner.PrepareGitListLocks(dispatcher, local);
            PrepareTask(task);
            taskRunner.AddTask(task);
        }

        public void LockFile(ITaskResultDispatcher<string> resultDispatcher, string file)
        {
            var task = ProcessRunner.PrepareGitLockFile(resultDispatcher, file);
            PrepareTask(task);
            taskRunner.AddTask(task);
            ListLocks(false);
        }

        public void UnlockFile(ITaskResultDispatcher<string> resultDispatcher, string file, bool force)
        {
            var task = ProcessRunner.PrepareGitUnlockFile(resultDispatcher, file, force);
            PrepareTask(task);
            taskRunner.AddTask(task);
            ListLocks(false);
        }

        private void PrepareTask(ITask task, bool disableWatcher = false)
        {
            task.OnBegin += t => {
                Logger.Trace("Start " + task.Label);

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

                Logger.Trace("Finish " + task.Label);
            };
        }

        private void OnRemoteBranchDeleted(string remote, string name)
        {
            RemoveRemoteBranch(remote, name);
        }

        private void OnRemoteBranchChanged(string remote, string name)
        {}

        private void OnRemoteBranchCreated(string remote, string name)
        {
            AddRemoteBranch(remote, name);
        }

        private void OnRepositoryUpdated()
        {
            Logger.Trace("OnRepositoryUpdated Trigger OnRepositoryUpdatedHandler");
            repositoryUpdateCallback.Invoke();
        }

        private void OnRepositoryUpdatedHandler()
        {
            Logger.Trace("Starting OnRepositoryUpdatedHandler");

            var taskStatus = ProcessRunner.PrepareGitStatus(new TaskResultDispatcher<GitStatus>(gitStatus => {
                OnRepositoryChanged?.Invoke(gitStatus);
                Logger.Trace("Ending OnRepositoryUpdatedHandler");
            }));
            PrepareTask(taskStatus);
            taskRunner.AddTask(taskStatus);
        }

        private void OnConfigChanged()
        {
            config.Reset();
            RefreshConfigData();

            Logger.Trace("OnRemoteOrTrackingChanged");
            OnRemoteOrTrackingChanged?.Invoke();
        }

        private void HeadChanged(string contents)
        {
            Logger.Trace("HeadChanged");
            head = contents;
            ActiveBranch = GetActiveBranch();
            ActiveRemote = GetActiveRemote();
            OnHeadChanged?.Invoke();
            OnRepositoryUpdatedHandler();
        }

        private void OnIndexChanged()
        {
            //Logger.Trace("OnIndexChanged Trigger OnRepositoryUpdatedHandler");
            //repositoryUpdateCallback.Invoke();
        }

        private void OnLocalBranchCreated(string name)
        {
            AddLocalBranch(name);
        }

        private void OnLocalBranchDeleted(string name)
        {
            RemoveLocalBranch(name);
        }

        private void OnLocalBranchChanged(string name)
        {
            if (name == this.Repository.CurrentBranch)
            {
                OnActiveBranchChanged?.Invoke();
                OnRepositoryUpdatedHandler();
            }
        }

        private IRepository InitializeRepository()
        {
            head = repositoryPaths.DotGitHead.ReadAllLines().FirstOrDefault();

            RefreshConfigData();

            var remote =
                config.GetRemotes()
                      .Where(x => HostAddress.Create(new UriString(x.Url).ToRepositoryUri()).IsGitHubDotCom())
                      .FirstOrDefault();
            UriString cloneUrl = "";
            if (remote.Url != null)
            {
                cloneUrl = new UriString(remote.Url).ToRepositoryUrl();
            }

            var user = new User();

            repositoryProcessRunner.RunGitConfigGet(new TaskResultDispatcher<string>(value => { user.Name = value; }),
                "user.name", GitConfigSource.User).Wait(cancellationToken);

            repositoryProcessRunner.RunGitConfigGet(new TaskResultDispatcher<string>(value => { user.Email = value; }),
                "user.email", GitConfigSource.User).Wait(cancellationToken);

            return new Repository(this, repositoryPaths.RepositoryPath.FileName, cloneUrl,
                repositoryPaths.RepositoryPath, user);
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
                if (activeBranch.HasValue != value.HasValue || (activeBranch.HasValue && !activeBranch.Value.Equals(value.Value)))
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
                if (activeRemote.HasValue != value.HasValue || (activeRemote.HasValue && !activeRemote.Value.Equals(value.Value)))
                {
                    activeRemote = value;
                    Logger.Trace("OnActiveRemoteChanged: {0}", value?.ToString() ?? "NULL");
                    OnActiveRemoteChanged?.Invoke();
                }
            }
        }

        public IRepositoryProcessRunner ProcessRunner => repositoryProcessRunner;

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
