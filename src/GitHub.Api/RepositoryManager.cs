using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Octokit_Extensions35;

namespace GitHub.Unity
{
    interface IRepositoryManager : IDisposable
    {
        event Action OnActiveBranchChanged;
        event Action OnActiveRemoteChanged;
        event Action OnRemoteBranchListChanged;
        event Action OnLocalBranchListChanged;
        event Action<GitStatus> OnRepositoryChanged;
        event Action<GitStatus> OnRefreshTrackedFileList;
        event Action OnHeadChanged;
        event Action OnRemoteOrTrackingChanged;

        void Refresh();

        IGitConfig Config { get;}
        ConfigBranch? ActiveBranch { get; set; }
        ConfigRemote? ActiveRemote { get; set; }
        IRepositoryProcessRunner ProcessRunner { get; }
        Dictionary<string, ConfigBranch> LocalBranches { get; }
        Dictionary<string, Dictionary<string, ConfigBranch>> RemoteBranches { get; }
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
        public NPath RepositoryPath { get; }
        public NPath DotGitPath { get; }
        public NPath BranchesPath { get; }
        public NPath RemotesPath { get; }
        public NPath DotGitIndex { get; }
        public NPath DotGitHead { get; }
        public NPath DotGitConfig { get; }

        public RepositoryPathConfiguration(NPath repositoryPath)
        {
            RepositoryPath = repositoryPath;

            DotGitPath = repositoryPath.Combine(".git");
            if (DotGitPath.FileExists())
            {
                DotGitPath = DotGitPath.ReadAllLines()
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
    }

    class RepositoryManagerFactory
    {
        public RepositoryManager CreateRepositoryManager(IPlatform platform, NPath repositoryRoot, CancellationToken cancellationToken)
        {
            var repositoryRepositoryPathConfiguration = new RepositoryPathConfiguration(repositoryRoot);
            var gitConfig = new GitConfig(repositoryRepositoryPathConfiguration.DotGitConfig);
            var repositoryWatcher = new RepositoryWatcher(platform, repositoryRepositoryPathConfiguration);
            var repositoryProcessRunner = new RepositoryProcessRunner(platform.Environment, platform.ProcessManager,
                platform.CredentialManager, platform.UIDispatcher,
                cancellationToken);

            return new RepositoryManager(repositoryRepositoryPathConfiguration, platform, gitConfig, repositoryWatcher, repositoryProcessRunner, cancellationToken);
        }
    }

    class RepositoryManager : IRepositoryManager
    {
        private readonly IRepositoryWatcher watcher;
        private readonly IRepositoryProcessRunner processRunner;
        private readonly IRepository repository;
        private readonly IPlatform platform;
        private readonly CancellationToken cancellationToken;
        private readonly IGitConfig config;
        private readonly IRepositoryPathConfiguration repositoryPaths;
        private readonly Dictionary<string, ConfigBranch> branches = new Dictionary<string, ConfigBranch>();

        public Dictionary<string, ConfigBranch> LocalBranches => branches;

        private Dictionary<string, ConfigRemote> remotes;
        private Dictionary<string, Dictionary<string, ConfigBranch>> remoteBranches = new Dictionary<string, Dictionary<string, ConfigBranch>>();
        public Dictionary<string, Dictionary<string, ConfigBranch>> RemoteBranches => remoteBranches;

        private string head;
        private ConfigBranch? activeBranch;
        private ConfigRemote? activeRemote;
        private bool disposed;
        private DateTime lastStatusUpdate;
        private DateTime lastLocksUpdate;

        public event Action OnActiveBranchChanged;
        public event Action OnActiveRemoteChanged;
        public event Action OnRemoteBranchListChanged;
        public event Action OnLocalBranchListChanged;
        public event Action<GitStatus> OnRepositoryChanged;
        public event Action<GitStatus> OnRefreshTrackedFileList;
        public event Action OnHeadChanged;
        public event Action OnRemoteOrTrackingChanged;

        public RepositoryManager(IRepositoryPathConfiguration repositoryRepositoryPathConfiguration, IPlatform platform, IGitConfig gitConfig, IRepositoryWatcher repositoryWatcher, IRepositoryProcessRunner repositoryProcessRunner, CancellationToken cancellationToken)
        {
            repositoryPaths = repositoryRepositoryPathConfiguration;

            this.platform = platform;
            this.cancellationToken = cancellationToken;

            config = gitConfig;
            repository = InitializeRepository();

            watcher = repositoryWatcher;

            watcher.ConfigChanged += OnConfigChanged;
            watcher.HeadChanged += HeadChanged;
            watcher.IndexChanged += OnIndexChanged;
            watcher.LocalBranchCreated += OnLocalBranchCreated;
            watcher.LocalBranchDeleted += OnLocalBranchDeleted;
            watcher.LocalBranchMoved += OnLocalBranchMoved;
            watcher.RepositoryChanged += OnRepositoryUpdated;
            watcher.RemoteBranchCreated += OnRemoteBranchCreated;
            watcher.RemoteBranchChanged += OnRemoteBranchChanged;
            watcher.RemoteBranchDeleted += OnRemoteBranchDeleted;
            watcher.RemoteBranchRenamed += OnRemoteBranchRenamed;

            processRunner = repositoryProcessRunner;
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
        {
        }

        private void OnRemoteBranchCreated(string remote, string name)
        {
            AddRemoteBranch(remote, name);
        }

        private void OnRepositoryUpdated()
        {
//            if (!statusUpdateRequested)
//            {
//                statusUpdateRequested = true;
//                run git status
//                var result = new TaskResultDispatcher<GitStatus>(
//                    status =>
//                    {
//                        lastStatusUpdate = DateTime.Now;
//                        statusUpdateRequested = false;
//                        OnRepositoryChanged?.Invoke(status);
//                    },
//                    () => {});
//
//            
//            
//
////                TaskEx.Delay(2, cancellationToken)
////                    .ContinueWith(_ => 
////                    {
//                      processRunner.RunGitStatus(result);
////                  }
////                    );
//            //}
//                result = new TaskResultDispatcher<GitStatus>(
//                    status =>
//                    {
//                        OnRefreshTrackedFileList?.Invoke(status);
//                    },
//                    () => {});
//                processRunner.RunGitTrackedFileList(result);
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

        private void OnLocalBranchMoved(string oldName, string name)
        {
            RemoveLocalBranch(oldName);
            AddLocalBranch(name);
        }

        private IRepository InitializeRepository()
        {
            head = repositoryPaths.DotGitHead
                .ReadAllLines()
                .FirstOrDefault();

            RefreshConfigData();

            var remote = config.GetRemotes()
                               .Where(x => HostAddress.Create(new UriString(x.Url).ToRepositoryUri()).IsGitHubDotCom())
                               .FirstOrDefault();
            UriString cloneUrl = "";
            if (remote.Url != null)
                cloneUrl = new UriString(remote.Url).ToRepositoryUrl();

            var user = new User();
            ProcessTask task = new GitConfigGetTask(platform.Environment, platform.ProcessManager,
                        new TaskResultDispatcher<string>(value =>
                        {
                            user.Name = value;
                        }), "user.name", GitConfigSource.User);
            task.RunAsync(cancellationToken).Wait();
            task = new GitConfigGetTask(platform.Environment, platform.ProcessManager,
                        new TaskResultDispatcher<string>(value =>
                        {
                            user.Email = value;
                        }), "user.email", GitConfigSource.User);
            task.RunAsync(cancellationToken).Wait();

            return new Repository(this, repositoryPaths.RepositoryPath.FileName, cloneUrl, repositoryPaths.RepositoryPath, user);
        }

        private void RefreshConfigData()
        {
            LoadBranchesFromConfig();
            LoadRemotesFromConfig();

            ActiveBranch = GetActiveBranch();
            ActiveRemote = GetActiveRemote();

            //Logger.Debug("Active remote {0}", ActiveRemote);
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
                var branch = configBranches.Where(x => x.Name == branchName).Select(x => x as ConfigBranch?).FirstOrDefault();
                if (!branch.HasValue)
                    branch = new ConfigBranch { Name = branchName };
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
                    foreach (var branch in basedir.Files(true).Select(x => x.RelativeTo(basedir)).Select(x => x.ToString(SlashMode.Forward)))
                    {
                        branchList.Add(branch, new ConfigBranch
                        {
                            Name = branch,
                            Remote = remotes[remote]
                        });
                    }
                    remoteBranches.Add(remote, branchList);
                }
            }
        }

        private ConfigRemote? GetActiveRemote(string defaultRemote = "origin")
        {
            var branch = ActiveBranch;
            if (branch.HasValue)
                return branch.Value.Remote;
            var remote = config.GetRemote(defaultRemote);
            if (remote.HasValue)
                return remote;
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
                return null;
        }

        private ConfigBranch? GetBranch(string name)
        {
            if (branches.ContainsKey(name))
                return branches[name];
            return null;
        }


        private void AddLocalBranch(string name)
        {
            if (!branches.ContainsKey(name))
            {
                var branch = config.GetBranch(name);
                if (!branch.HasValue)
                    branch = new ConfigBranch { Name = name };
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
                if (disposed) return;
                disposed = true;
                Stop();
                watcher.Dispose();
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }

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

        public IRepositoryProcessRunner ProcessRunner => processRunner;
        protected static ILogging Logger { get; } = Logging.GetLogger<RepositoryManager>();
    }
}