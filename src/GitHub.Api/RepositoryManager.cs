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
            var gitConfig = new GitConfig(repositoryPathConfiguration.DotGitConfig);

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
        private Dictionary<string, Dictionary<string, ConfigBranch>> remoteBranches =
            new Dictionary<string, Dictionary<string, ConfigBranch>>();
        private Dictionary<string, ConfigRemote> remotes;

        public RepositoryManager(IPlatform platform, ITaskRunner taskRunner, IGitConfig gitConfig, IRepositoryWatcher repositoryWatcher, IRepositoryProcessRunner repositoryProcessRunner, IRepositoryPathConfiguration repositoryPathConfiguration, CancellationToken cancellationToken)
        {
            repositoryPaths = repositoryPathConfiguration;

            this.platform = platform;
            this.taskRunner = taskRunner;
            this.cancellationToken = cancellationToken;
            this.repositoryProcessRunner = repositoryProcessRunner;

            config = gitConfig;
            repository = InitializeRepository();

            watcher = repositoryWatcher;

            watcher.HeadChanged += HeadChanged;
            watcher.IndexChanged += OnIndexChanged;
            watcher.LocalBranchChanged += OnLocalBranchChanged;
            watcher.LocalBranchCreated += OnLocalBranchCreated;
            watcher.LocalBranchDeleted += OnLocalBranchDeleted;
            watcher.RepositoryChanged += OnRepositoryUpdated;
            watcher.RemoteBranchCreated += OnRemoteBranchCreated;
            watcher.RemoteBranchChanged += OnRemoteBranchChanged;
            watcher.RemoteBranchDeleted += OnRemoteBranchDeleted;

            repositoryUpdateCallback = TaskExtensions.Debounce(OnRepositoryUpdatedHandler, 500);
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
        public void Dispose()
        {
            Dispose(true);
        }

        public event Action OnActiveBranchChanged;
        public event Action OnActiveRemoteChanged;
        public event Action OnRemoteBranchListChanged;
        public event Action OnLocalBranchListChanged;
        public event Action<GitStatus> OnRepositoryChanged;
        public event Action OnHeadChanged;
        public event Action<bool> OnIsBusyChanged;
        public event Action OnRemoteOrTrackingChanged;

        public void CommitFiles(TaskResultDispatcher<string> resultDispatcher, List<string> files, string message, string body)
        {
            var task = ProcessRunner.PrepareGitCommitFileTask(resultDispatcher, files, message, body);

            PrepareTask(task, "Git CommitFiles");

            taskRunner.AddTask(task);
        }

        public void Fetch(ITaskResultDispatcher<string> resultDispatcher, string remote)
        {
            var task = ProcessRunner.PrepareGitFetch(resultDispatcher, remote);

            PrepareTask(task, "Git Fetch");

            taskRunner.AddTask(task);
        }

        public void Pull(ITaskResultDispatcher<string> resultDispatcher, string remote, string branch)
        {
            var task = ProcessRunner.PrepareGitPull(resultDispatcher, remote, branch);

            PrepareTask(task, "Git Pull", true);

            taskRunner.AddTask(task);
        }

        public void Push(ITaskResultDispatcher<string> resultDispatcher, string remote, string branch)
        {
            var task = ProcessRunner.PrepareGitPush(resultDispatcher, remote, branch);

            PrepareTask(task, "Git Push");

            taskRunner.AddTask(task);
        }

        public void RemoteAdd(ITaskResultDispatcher<string> resultDispatcher, string remote, string url)
        {
            var task = ProcessRunner.PrepareGitRemoteAdd(resultDispatcher, remote, url);

            PrepareTask(task, "Git RemoteAdd");

            taskRunner.AddTask(task);
        }

        public void RemoteRemove(ITaskResultDispatcher<string> resultDispatcher, string remote)
        {
            var task = ProcessRunner.PrepareGitRemoteRemove(resultDispatcher, remote);

            PrepareTask(task, "Git RemoteRemove");

            taskRunner.AddTask(task);
        }

        public void SwitchBranch(ITaskResultDispatcher<string> resultDispatcher, string branch)
        {
            var task = ProcessRunner.PrepareSwitchBranch(resultDispatcher, branch);

            PrepareTask(task, "Git SwitchBranch", true);

            taskRunner.AddTask(task);
        }

        public void DeleteBranch(ITaskResultDispatcher<string> resultDispatcher, string branch, bool deleteUnmerged = false)
        {
            var task = ProcessRunner.PrepareDeleteBranch(resultDispatcher, branch, deleteUnmerged);

            PrepareTask(task, "Git DeleteBranch");

            taskRunner.AddTask(task);
        }

        public void CreateBranch(ITaskResultDispatcher<string> resultDispatcher, string branch, string baseBranch)
        {
            var task = ProcessRunner.PrepareCreateBranch(resultDispatcher, branch, baseBranch);

            PrepareTask(task, "Git CreateBranch");

            taskRunner.AddTask(task);
        }

        private void PrepareTask(ITask task, string operation, bool disableWatcher = false)
        {
            task.OnBegin = t => {
                Logger.Trace("Start " + operation);

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

            task.OnEnd = t => {
                if (disableWatcher)
                {
                    watcher.Start();
                }

                IsBusy = false;

                Logger.Trace("Finish " + operation);
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

            if (handlingRepositoryUpdate)
            {
                Logger.Trace("Exiting OnRepositoryUpdatedHandler");
                return;
            }

            handlingRepositoryUpdate = true;

            GitStatus? gitStatus = null;
            var runGitStatus = repositoryProcessRunner.RunGitStatus(new TaskResultDispatcher<GitStatus>(s => {
                Logger.Debug("RunGitStatus Success: {0}", s);
                gitStatus = s;
            }, () => { Logger.Warning("RunGitStatus Failed"); }));

            GitLock[] gitLocks = null;
            var runGitListLocks =
                repositoryProcessRunner.RunGitListLocks(new TaskResultDispatcher<IEnumerable<GitLock>>(s => {
                    var resultArray = s.ToArray();
                    Logger.Debug("RunGitListLocks Success: {0}", resultArray.Count());
                    gitLocks = resultArray;
                }, () => { Logger.Warning("RunGitListLocks Failed"); }));

            runGitStatus.Wait(cancellationToken);
            runGitListLocks.Wait(cancellationToken);

            Logger.Trace("OnRepositoryUpdatedHandler Processing Results");
            handlingRepositoryUpdate = false;

            if (gitStatus.HasValue)
            {
                Debug.Assert(gitStatus != null, "gitStatus != null");
                var gitStatusValue = gitStatus.Value;

                if (gitStatusValue.Entries.Any())
                {
                    lastStatusUpdate = DateTime.Now;
                }

                if (gitLocks != null)
                {
                    lastLocksUpdate = DateTime.Now;

                    var gitLockDictionary = gitLocks.ToDictionary(gitLock => gitLock.Path);

                    gitStatusValue.Entries = gitStatusValue.Entries.Select(entry => {
                        GitLock gitLock;
                        if (gitLockDictionary.TryGetValue(entry.Path, out gitLock))
                        {
                            entry.Lock = gitLock;
                        }

                        return entry;
                    }).ToArray();
                }

                OnRepositoryChanged?.Invoke(gitStatusValue);
            }

            Logger.Trace("Ending OnRepositoryUpdatedHandler lastStatusUpdate:{0} lastLocksUpdate:{1}", lastStatusUpdate,
                lastLocksUpdate);
        }

        private void OnConfigChanged()
        {
            config.Reset();
            RefreshConfigData();
            OnRemoteOrTrackingChanged?.Invoke();
        }

        private void HeadChanged(string contents)
        {
            head = contents;
            ActiveBranch = GetActiveBranch();
            ActiveRemote = GetActiveRemote();
            OnHeadChanged?.Invoke();
        }

        private void OnIndexChanged()
        {
            Logger.Trace("OnIndexChanged Trigger OnRepositoryUpdatedHandler");
            repositoryUpdateCallback.Invoke();
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
            LoadBranchesFromConfig();
            LoadRemotesFromConfig();

            ActiveBranch = GetActiveBranch();
            ActiveRemote = GetActiveRemote();
        }

        private void LoadBranchesFromConfig()
        {
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
            if (branch.HasValue)
            {
                return branch.Value.Remote;
            }

            var remote = config.GetRemote(defaultRemote);
            if (remote.HasValue)
            {
                return remote;
            }

            return config.GetRemotes().FirstOrDefault();
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
            if (disposing)
            {
                if (disposed)
                {
                    return;
                }

                disposed = true;
                Stop();
                watcher.Dispose();
            }
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
                activeBranch = value;
                OnActiveBranchChanged?.Invoke();
            }
        }

        public ConfigRemote? ActiveRemote
        {
            get { return activeRemote; }
            private set
            {
                activeRemote = value;
                OnActiveRemoteChanged?.Invoke();
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
                    isBusy = value;
                    OnIsBusyChanged?.Invoke(isBusy);
                }
            }
        }

        protected static ILogging Logger { get; } = Logging.GetLogger<RepositoryManager>();
    }
}
