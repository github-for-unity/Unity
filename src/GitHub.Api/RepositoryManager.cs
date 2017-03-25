using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace GitHub.Unity
{
    interface IRepositoryManager : IDisposable
    {
        void Refresh();

        IGitConfig Config { get; }
        ConfigBranch? ActiveBranch { get; set; }
        ConfigRemote? ActiveRemote { get; set; }
        IRepositoryProcessRunner ProcessRunner { get; }
        Dictionary<string, ConfigBranch> LocalBranches { get; }
        Dictionary<string, Dictionary<string, ConfigBranch>> RemoteBranches { get; }
        event Action OnActiveBranchChanged;
        event Action OnActiveRemoteChanged;
        event Action OnRemoteBranchListChanged;
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
        public RepositoryManager CreateRepositoryManager(IPlatform platform, NPath repositoryRoot,
            CancellationToken cancellationToken)
        {
            var repositoryRepositoryPathConfiguration = new RepositoryPathConfiguration(repositoryRoot);
            var gitConfig = new GitConfig(repositoryRepositoryPathConfiguration.DotGitConfig);
            var repositoryWatcher = new RepositoryWatcher(platform, repositoryRepositoryPathConfiguration);
            var repositoryProcessRunner = new RepositoryProcessRunner(platform.Environment, platform.ProcessManager,
                platform.CredentialManager, platform.UIDispatcher, cancellationToken);

            return new RepositoryManager(repositoryRepositoryPathConfiguration, platform, gitConfig, repositoryWatcher,
                repositoryProcessRunner, cancellationToken);
        }
    }

    class RepositoryManager : IRepositoryManager
    {
        private readonly Dictionary<string, ConfigBranch> branches = new Dictionary<string, ConfigBranch>();
        private readonly CancellationToken cancellationToken;
        private readonly IGitConfig config;
        private readonly IPlatform platform;
        private readonly IRepository repository;
        private readonly IRepositoryPathConfiguration repositoryPaths;
        private readonly IRepositoryProcessRunner repositoryProcessRunner;
        private readonly IRepositoryWatcher watcher;
        private ConfigBranch? activeBranch;
        private ConfigRemote? activeRemote;
        private bool disposed;

        private string head;
        private DateTime lastLocksUpdate;
        private DateTime lastStatusUpdate;
        private Dictionary<string, Dictionary<string, ConfigBranch>> remoteBranches =
            new Dictionary<string, Dictionary<string, ConfigBranch>>();
        private Dictionary<string, ConfigRemote> remotes;
        private bool statusUpdateRequested;

        public RepositoryManager(IRepositoryPathConfiguration repositoryRepositoryPathConfiguration, IPlatform platform,
            IGitConfig gitConfig, IRepositoryWatcher repositoryWatcher, IRepositoryProcessRunner repositoryProcessRunner,
            CancellationToken cancellationToken)
        {
            repositoryPaths = repositoryRepositoryPathConfiguration;

            this.platform = platform;
            this.cancellationToken = cancellationToken;
            this.repositoryProcessRunner = repositoryProcessRunner;

            config = gitConfig;
            repository = InitializeRepository();

            watcher = repositoryWatcher;

            watcher.ConfigChanged += OnConfigChanged;
            watcher.HeadChanged += HeadChanged;
            watcher.IndexChanged += OnIndexChanged;
            watcher.LocalBranchCreated += OnLocalBranchCreated;
            watcher.LocalBranchDeleted += OnLocalBranchDeleted;
            watcher.RepositoryChanged += OnRepositoryUpdated;
            watcher.RemoteBranchCreated += OnRemoteBranchCreated;
            watcher.RemoteBranchDeleted += OnRemoteBranchDeleted;
        }

        public void Start()
        {
            watcher.Start();
            OnRepositoryUpdated();
        }

        public void Stop()
        {
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
        public event Action OnRemoteOrTrackingChanged;

        private void OnRemoteBranchRenamed(string remote, string oldName, string name)
        {
            RemoveRemoteBranch(remote, oldName);
            AddRemoteBranch(remote, name);
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
            Logger.Trace("Starting OnRepositoryUpdated");

            if (statusUpdateRequested)
            {
                Logger.Trace("Exiting OnRepositoryUpdated");
                return;
            }

            statusUpdateRequested = true;

            GitStatus? gitStatus = null;
            var runGitStatus = repositoryProcessRunner.RunGitStatus(new TaskResultDispatcher<GitStatus>(s => {
                Logger.Debug("RunGitStatus Success: {0}", s);
                gitStatus = s;
            }, () => {
                Logger.Warning("RunGitStatus Failed");
            }));

            GitLock[] gitLocks = null;
            var runGitListLocks =
                repositoryProcessRunner.RunGitListLocks(new TaskResultDispatcher<IEnumerable<GitLock>>(s => {
                    var resultArray = s.ToArray();
                    Logger.Debug("RunGitListLocks Success: {0}", resultArray.Count());
                    gitLocks = resultArray;

                }, () => {
                    Logger.Warning("RunGitListLocks Failed");
                }));

            runGitStatus.Wait(cancellationToken);
            runGitListLocks.Wait(cancellationToken);

            Logger.Trace("OnRepositoryUpdated Processing Results");
            statusUpdateRequested = false;

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

            Logger.Trace("Ending OnRepositoryUpdated lastStatusUpdate:{0} lastLocksUpdate:{1}", lastStatusUpdate, lastLocksUpdate);
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
            OnRepositoryUpdated();
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
            set
            {
                activeBranch = value;
                OnActiveBranchChanged?.Invoke();
            }
        }

        public ConfigRemote? ActiveRemote
        {
            get { return activeRemote; }
            set
            {
                activeRemote = value;
                OnActiveRemoteChanged?.Invoke();
            }
        }

        public IRepositoryProcessRunner ProcessRunner => repositoryProcessRunner;
        protected static ILogging Logger { get; } = Logging.GetLogger<RepositoryManager>();
    }
}