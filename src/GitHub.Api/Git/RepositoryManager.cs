using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Octokit;
using GitHub.Logging;

namespace GitHub.Unity
{
    public interface IRepositoryManager : IDisposable
    {
        event Action<bool> IsBusyChanged;
        event Action<ConfigBranch?, ConfigRemote?> CurrentBranchUpdated;
        event Action<GitStatus> GitStatusUpdated;
        event Action<List<GitLock>> GitLocksUpdated;
        event Action<List<GitLogEntry>> GitLogUpdated;
        event Action<Dictionary<string, ConfigBranch>> LocalBranchesUpdated;
        event Action<Dictionary<string, ConfigRemote>, Dictionary<string, Dictionary<string, ConfigBranch>>> RemoteBranchesUpdated;
        event Action<GitAheadBehindStatus> GitAheadBehindStatusUpdated;

        void Initialize();
        void Start();
        void Stop();
        ITask CommitAllFiles(string message, string body);
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
        ITask LockFile(string file);
        ITask UnlockFile(string file, bool force);
        ITask DiscardChanges(GitStatusEntry[] gitStatusEntries);
        void UpdateGitLog();
        void UpdateGitStatus();
        void UpdateGitAheadBehindStatus();
        void UpdateLocks();
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
            DotGitCommitEditMsg = DotGitPath.Combine("COMMIT_EDITMSG");
        }

        public NPath RepositoryPath { get; }
        public NPath DotGitPath { get; }
        public NPath BranchesPath { get; }
        public NPath RemotesPath { get; }
        public NPath DotGitIndex { get; }
        public NPath DotGitHead { get; }
        public NPath DotGitConfig { get; }
        public NPath DotGitCommitEditMsg { get; }
    }

    class RepositoryManager : IRepositoryManager
    {
        private readonly IGitConfig config;
        private readonly IGitClient gitClient;
        private readonly IProcessManager processManager;
        private readonly IRepositoryPathConfiguration repositoryPaths;
        private readonly IFileSystem fileSystem;
        private readonly CancellationToken token;
        private readonly IRepositoryWatcher watcher;

        private bool isBusy;

        public event Action<ConfigBranch?, ConfigRemote?> CurrentBranchUpdated;
        public event Action<bool> IsBusyChanged;
        public event Action<GitStatus> GitStatusUpdated;
        public event Action<GitAheadBehindStatus> GitAheadBehindStatusUpdated;
        public event Action<List<GitLock>> GitLocksUpdated;
        public event Action<List<GitLogEntry>> GitLogUpdated;
        public event Action<Dictionary<string, ConfigBranch>> LocalBranchesUpdated;
        public event Action<Dictionary<string, ConfigRemote>, Dictionary<string, Dictionary<string, ConfigBranch>>> RemoteBranchesUpdated;

        public RepositoryManager(IGitConfig gitConfig,
            IRepositoryWatcher repositoryWatcher, IGitClient gitClient,
            IProcessManager processManager,
            IFileSystem fileSystem,
            CancellationToken token,
            IRepositoryPathConfiguration repositoryPaths)
        {
            this.repositoryPaths = repositoryPaths;
            this.fileSystem = fileSystem;
            this.token = token;
            this.gitClient = gitClient;
            this.processManager = processManager;
            this.watcher = repositoryWatcher;
            this.config = gitConfig;

            SetupWatcher();
        }

        public static RepositoryManager CreateInstance(IPlatform platform, ITaskManager taskManager, IGitClient gitClient,
            IProcessManager processManager, IFileSystem fileSystem, NPath repositoryRoot)
        {
            var repositoryPathConfiguration = new RepositoryPathConfiguration(repositoryRoot);
            string filePath = repositoryPathConfiguration.DotGitConfig;
            var gitConfig = new GitConfig(filePath);

            var repositoryWatcher = new RepositoryWatcher(platform, repositoryPathConfiguration, taskManager.Token);

            return new RepositoryManager(gitConfig, repositoryWatcher,
                gitClient, processManager, fileSystem,
                taskManager.Token, repositoryPathConfiguration);
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

        public ITask CommitAllFiles(string message, string body)
        {
            var task = GitClient
                .AddAll()
                .Then(GitClient.Commit(message, body));

            return HookupHandlers(task, true, true);
        }

        public ITask CommitFiles(List<string> files, string message, string body)
        {
            var task = GitClient
                .Add(files)
                .Then(GitClient.Commit(message, body));

            return HookupHandlers(task, true, true);
        }

        public ITask Fetch(string remote)
        {
            var task = GitClient.Fetch(remote);
            return HookupHandlers(task, true, false);
        }

        public ITask Pull(string remote, string branch)
        {
            var task = GitClient.Pull(remote, branch);
            return HookupHandlers(task, true, true);
        }

        public ITask Push(string remote, string branch)
        {
            var task = GitClient.Push(remote, branch);
            return HookupHandlers(task, true, false);
        }

        public ITask Revert(string changeset)
        {
            var task = GitClient.Revert(changeset);
            return HookupHandlers(task, true, true);
        }

        public ITask RemoteAdd(string remote, string url)
        {
            var task = GitClient.RemoteAdd(remote, url);
            return HookupHandlers(task, true, false);
        }

        public ITask RemoteRemove(string remote)
        {
            var task = GitClient.RemoteRemove(remote);
            return HookupHandlers(task, true, false);
        }

        public ITask RemoteChange(string remote, string url)
        {
            var task = GitClient.RemoteChange(remote, url);
            return HookupHandlers(task, true, false);
        }

        public ITask SwitchBranch(string branch)
        {
            var task = GitClient.SwitchBranch(branch);
            return HookupHandlers(task, true, true);
        }

        public ITask DeleteBranch(string branch, bool deleteUnmerged = false)
        {
            var task = GitClient.DeleteBranch(branch, deleteUnmerged);
            return HookupHandlers(task, true, false);
        }

        public ITask CreateBranch(string branch, string baseBranch)
        {
            var task = GitClient.CreateBranch(branch, baseBranch);
            return HookupHandlers(task, true, false);
        }

        public ITask LockFile(string file)
        {
            var task = GitClient.Lock(file);
            return HookupHandlers(task, true, false).Then(UpdateLocks);
        }

        public ITask UnlockFile(string file, bool force)
        {
            var task = GitClient.Unlock(file, force);
            return HookupHandlers(task, true, false).Then(UpdateLocks);
        }

        public void UpdateGitLog()
        {
            var task = GitClient
                .Log()
                .Then((success, logEntries) =>
                {
                    if (success)
                    {
                        GitLogUpdated?.Invoke(logEntries);
                    }
                });
            task = HookupHandlers(task, false, false);
            task.Start();
        }

        public void UpdateGitStatus()
        {
            var task = GitClient
                .Status()
                .Then((success, status) =>
                {
                    if (success)
                    {
                        GitStatusUpdated?.Invoke(status);
                    }
                });
            task = HookupHandlers(task, true, false);
            task.Start();
        }

        public ITask DiscardChanges(GitStatusEntry[] gitStatusEntries)
        {
            Guard.ArgumentNotNullOrEmpty(gitStatusEntries, "gitStatusEntries");

            ActionTask<GitStatusEntry[]> task = null;
            task = new ActionTask<GitStatusEntry[]>(token, (_, entries) =>
                {
                    var itemsToDelete = new List<string>();
                    var itemsToRevert = new List<string>();

                    foreach (var gitStatusEntry in gitStatusEntries)
                    {
                        if (gitStatusEntry.status == GitFileStatus.Added || gitStatusEntry.status == GitFileStatus.Untracked)
                        {
                            itemsToDelete.Add(gitStatusEntry.path);
                        }
                        else
                        {
                            itemsToRevert.Add(gitStatusEntry.path);
                        }
                    }

                    if (itemsToDelete.Any())
                    {
                        foreach (var itemToDelete in itemsToDelete)
                        {
                            fileSystem.FileDelete(itemToDelete);
                        }
                    }

                    ITask<string> gitDiscardTask = null;
                    if (itemsToRevert.Any())
                    {
                        gitDiscardTask = GitClient.Discard(itemsToRevert);
                        task.Then(gitDiscardTask);
                    }
                }
                , () => gitStatusEntries);


            return HookupHandlers(task, true, true);
        }

        public void UpdateGitAheadBehindStatus()
        {
            ConfigBranch? configBranch;
            ConfigRemote? configRemote;
            GetCurrentBranchAndRemote(out configBranch, out configRemote);

            if (configBranch.HasValue && configBranch.Value.Remote.HasValue)
            {
                var name = configBranch.Value.Name;
                var trackingName = configBranch.Value.IsTracking ? configBranch.Value.Remote.Value.Name + "/" + name : "[None]";

                var task = GitClient
                    .AheadBehindStatus(name, trackingName)
                    .Then((success, status) =>
                    {
                        if (success)
                        {
                            GitAheadBehindStatusUpdated?.Invoke(status);
                        }
                    });
                task = HookupHandlers(task, true, false);
                task.Start();
            }
            else
            {
                GitAheadBehindStatusUpdated?.Invoke(GitAheadBehindStatus.Default);
            }
        }

        public void UpdateLocks()
        {
            var task = GitClient.ListLocks(false);
            HookupHandlers(task, false, false);
            task.Then((success, locks) =>
            {
                if (success)
                {
                    GitLocksUpdated?.Invoke(locks);
                }
            }).Start();
        }

        private ITask HookupHandlers(ITask task, bool isExclusive, bool filesystemChangesExpected)
        {
            return new ActionTask(token, () => {
                    if (isExclusive)
                    {
                        Logger.Trace("Starting Operation - Setting Busy Flag");
                        IsBusy = true;
                    }

                    if (filesystemChangesExpected)
                    {
                        Logger.Trace("Starting Operation - Disable Watcher");
                        watcher.Stop();
                    }
                })
                .Then(task)
                .Finally((success, exception) => {
                    if (filesystemChangesExpected)
                    {
                        Logger.Trace("Ended Operation - Enable Watcher");
                        watcher.Start();
                    }

                    if (isExclusive)
                    {
                        Logger.Trace("Ended Operation - Clearing Busy Flag");
                        IsBusy = false;
                    }

                    if (!success)
                    {
                        throw exception;
                    }
                });
        }

        private void SetupWatcher()
        {
            watcher.HeadChanged += WatcherOnHeadChanged;
            watcher.IndexChanged += WatcherOnIndexChanged;
            watcher.ConfigChanged += WatcherOnConfigChanged;
            watcher.RepositoryCommitted += WatcherOnRepositoryCommitted;
            watcher.RepositoryChanged += WatcherOnRepositoryChanged;
            watcher.LocalBranchesChanged += WatcherOnLocalBranchesChanged;
            watcher.RemoteBranchesChanged += WatcherOnRemoteBranchesChanged;
        }

        private void UpdateHead()
        {
            Logger.Trace("UpdateHead");
            UpdateCurrentBranchAndRemote();
            UpdateGitLog();
        }

        private string GetCurrentHead()
        {
            return repositoryPaths.DotGitHead.ReadAllLines().FirstOrDefault();
        }

        private void UpdateCurrentBranchAndRemote()
        {
            ConfigBranch? branch;
            ConfigRemote? remote;
            GetCurrentBranchAndRemote(out branch, out remote);

            Logger.Trace("CurrentBranch: {0}", branch.HasValue ? branch.Value.ToString() : "[NULL]");
            Logger.Trace("CurrentRemote: {0}", remote.HasValue ? remote.Value.ToString() : "[NULL]");
            CurrentBranchUpdated?.Invoke(branch, remote);
        }

        private void GetCurrentBranchAndRemote(out ConfigBranch? branch, out ConfigRemote? remote)
        {
            branch = null;
            remote = null;

            var head = GetCurrentHead();
            if (head.StartsWith("ref:"))
            {
                var branchName = head.Substring(head.IndexOf("refs/heads/") + "refs/heads/".Length);
                branch = config.GetBranch(branchName);

                if (!branch.HasValue)
                {
                    branch = new ConfigBranch(branchName);
                }
            }

            var defaultRemote = "origin";

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
        }

        private void WatcherOnRemoteBranchesChanged()
        {
            Logger.Trace("WatcherOnRemoteBranchesChanged");
            UpdateRemoteBranches();
        }

        private void WatcherOnLocalBranchesChanged()
        {
            Logger.Trace("WatcherOnLocalBranchesChanged");
            UpdateLocalBranches();
            UpdateGitLog();
        }

        private void WatcherOnRepositoryCommitted()
        {
            Logger.Trace("WatcherOnRepositoryCommitted");
            UpdateGitLog();
            UpdateGitStatus();
        }

        private void WatcherOnRepositoryChanged()
        {
            Logger.Trace("WatcherOnRepositoryChanged");
            UpdateGitStatus();
        }

        private void WatcherOnConfigChanged()
        {
            Logger.Trace("WatcherOnConfigChanged");
            UpdateConfigData(true);
        }

        private void WatcherOnHeadChanged()
        {
            Logger.Trace("WatcherOnHeadChanged");
            UpdateHead();
        }

        private void WatcherOnIndexChanged()
        {
            Logger.Trace("WatcherOnIndexChanged");
            UpdateGitStatus();
        }

        private void UpdateConfigData(bool resetConfig = false)
        {
            Logger.Trace("UpdateConfigData reset:{0}", resetConfig);

            if (resetConfig)
            {
                config.Reset();
            }

            UpdateLocalBranches();
            UpdateRemoteBranches();
            UpdateHead();
        }

        private void UpdateLocalBranches()
        {
            Logger.Trace("UpdateLocalBranches");

            var branches = new Dictionary<string, ConfigBranch>();
            UpdateLocalBranches(branches, repositoryPaths.BranchesPath, config.GetBranches().Where(x => x.IsTracking), "");

            Logger.Trace("OnLocalBranchListUpdated {0} branches", branches.Count);
            LocalBranchesUpdated?.Invoke(branches);
        }

        private void UpdateLocalBranches(Dictionary<string, ConfigBranch> branches, NPath path, IEnumerable<ConfigBranch> configBranches, string prefix)
        {
            foreach (var file in path.Files())
            {
                var branchName = prefix + file.FileName;
                var branch =
                    configBranches.Where(x => x.Name == branchName).Select(x => x as ConfigBranch?).FirstOrDefault();
                if (!branch.HasValue)
                {
                    branch = new ConfigBranch(branchName);
                }
                branches.Add(branchName, branch.Value);
            }

            foreach (var dir in path.Directories())
            {
                UpdateLocalBranches(branches, dir, configBranches, prefix + dir.FileName + "/");
            }
        }

        private void UpdateRemoteBranches()
        {
            Logger.Trace("UpdateRemoteBranches");

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
                        branchList.Add(branch, new ConfigBranch(branch, remotes[remote]));
                    }

                    remoteBranches.Add(remote, branchList);
                }
            }

            Logger.Trace("OnRemoteBranchListUpdated {0} remotes", remotes.Count);
            RemoteBranchesUpdated?.Invoke(remotes, remoteBranches);

            UpdateGitAheadBehindStatus();
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
                    IsBusyChanged?.Invoke(isBusy);
                }
            }
        }

        protected static ILogging Logger { get; } = LogHelper.GetLogger<RepositoryManager>();
    }
}
