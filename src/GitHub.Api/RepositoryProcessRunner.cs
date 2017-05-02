using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace GitHub.Unity
{
    interface IRepositoryProcessRunner
    {
        ITask<GitStatus?> PrepareGitStatus(ITaskResultDispatcher<GitStatus> resultDispatcher);
        Task<bool> RunGitConfigGet(ITaskResultDispatcher<string> resultDispatcher, string key, GitConfigSource configSource);
        Task<bool> RunGitConfigSet(ITaskResultDispatcher<string> resultDispatcher, string key, string value, GitConfigSource configSource);
        ITask<IEnumerable<GitLock>> PrepareGitListLocks(ITaskResultDispatcher<IEnumerable<GitLock>> resultDispatcher, bool local);
        ITask PrepareGitPull(ITaskResultDispatcher<string> resultDispatcher, string remote, string branch);
        ITask PrepareGitPush(ITaskResultDispatcher<string> resultDispatcher, string remote, string branch);
        ITask PrepareSwitchBranch(ITaskResultDispatcher<string> resultDispatcher, string branch);
        ITask PrepareGitFetch(ITaskResultDispatcher<string> resultDispatcher, string remote);
        ITask PrepareDeleteBranch(ITaskResultDispatcher<string> resultDispatcher, string branch, bool deleteUnmerged = false);
        ITask PrepareCreateBranch(ITaskResultDispatcher<string> resultDispatcher, string branch, string baseBranch);
        ITask PrepareGitRemoteAdd(ITaskResultDispatcher<string> resultDispatcher, string remote, string url);
        ITask PrepareGitRemoteRemove(ITaskResultDispatcher<string> resultDispatcher, string remote);
        ITask PrepareGitCommit(ITaskResultDispatcher<string> resultDispatcher, string message, string body);
        ITask PrepareGitAdd(ITaskResultDispatcher<string> resultDispatcher, List<string> files);
        ITask PrepareGitCommitFileTask(ITaskResultDispatcher<string> resultDispatcher, List<string> files, string message, string body);
        ITask PrepareGitLockFile(ITaskResultDispatcher<string> resultDispatcher, string file);
        ITask PrepareGitUnlockFile(ITaskResultDispatcher<string> resultDispatcher, string file, bool force);
        ITask PrepareGitRemoteChange(ITaskResultDispatcher<string> resultDispatcher, string remote, string url);
    }

    class RepositoryProcessRunner: IRepositoryProcessRunner
    {
        private readonly IEnvironment environment;
        private readonly IProcessManager processManager;
        private readonly IKeychainManager keychainManager;
        private readonly IUIDispatcher uiDispatcher;
        private readonly CancellationToken cancellationToken;

        public RepositoryProcessRunner(IEnvironment environment, IProcessManager processManager,
            IKeychainManager keychainManager, IUIDispatcher uiDispatcher,
            CancellationToken cancellationToken)
        {
            this.environment = environment;
            this.processManager = processManager;
            this.keychainManager = keychainManager;
            this.uiDispatcher = uiDispatcher;
            this.cancellationToken = cancellationToken;
        }

        public ITask<GitStatus?> PrepareGitStatus(ITaskResultDispatcher<GitStatus> resultDispatcher)
        {
            return new GitStatusTask(environment, processManager, resultDispatcher, new GitObjectFactory(environment));
        }

        public Task<bool> RunGitConfigGet(ITaskResultDispatcher<string> resultDispatcher, string key, GitConfigSource configSource)
        {
            var task = new GitConfigGetTask(environment, processManager, resultDispatcher, key, configSource);
            return task.RunAsync(cancellationToken);
        }

        public Task<bool> RunGitConfigSet(ITaskResultDispatcher<string> resultDispatcher, string key, string value, GitConfigSource configSource)
        {
            var task = new GitConfigSetTask(environment, processManager, resultDispatcher, key, value, configSource);
            return task.RunAsync(cancellationToken);
        }

        public ITask<IEnumerable<GitLock>> PrepareGitListLocks(ITaskResultDispatcher<IEnumerable<GitLock>> resultDispatcher,
            bool local)
        {
            var gitObjectFactory = new GitObjectFactory(environment);
            return new GitListLocksTask(environment, processManager, resultDispatcher, gitObjectFactory, local);
        }

        public ITask PrepareGitPull(ITaskResultDispatcher<string> resultDispatcher, string remote, string branch)
        {
            return new GitPullTask(environment, processManager, resultDispatcher,
                keychainManager, uiDispatcher, remote, branch);
        }

        public ITask PrepareGitPush(ITaskResultDispatcher<string> resultDispatcher, string remote, string branch)
        {
            return new GitPushTask(environment, processManager, resultDispatcher,
                keychainManager, uiDispatcher, remote, branch, true);
        }

        public ITask PrepareGitFetch(ITaskResultDispatcher<string> resultDispatcher, string remote)
        {
            return new GitFetchTask(environment, processManager, resultDispatcher, keychainManager, uiDispatcher,
                remote);
        }

        public ITask PrepareSwitchBranch(ITaskResultDispatcher<string> resultDispatcher, string branch)
        {
            return new GitSwitchBranchesTask(environment, processManager, resultDispatcher, branch);
        }

        public ITask PrepareDeleteBranch(ITaskResultDispatcher<string> resultDispatcher, string branch,
            bool deleteUnmerged = false)
        {
            return new GitBranchDeleteTask(environment, processManager, resultDispatcher, branch, deleteUnmerged);
        }

        public ITask PrepareCreateBranch(ITaskResultDispatcher<string> resultDispatcher, string branch,
            string baseBranch)
        {
            return new GitBranchCreateTask(environment, processManager, resultDispatcher, branch, baseBranch);
        }

        public ITask PrepareGitRemoteAdd(ITaskResultDispatcher<string> resultDispatcher, string remote, string url)
        {
            return new GitRemoteAddTask(environment, processManager, resultDispatcher, remote, url);
        }

        public ITask PrepareGitRemoteRemove(ITaskResultDispatcher<string> resultDispatcher, string remote)
        {
            return new GitRemoteRemoveTask(environment, processManager, resultDispatcher, remote);
        }

        public ITask PrepareGitRemoteChange(ITaskResultDispatcher<string> resultDispatcher, string remote, string url)
        {
            return new GitRemoteChangeTask(environment, processManager, resultDispatcher, remote, url);
        }

        public ITask PrepareGitCommit(ITaskResultDispatcher<string> resultDispatcher, string message, string body)
        {
            return new GitCommitTask(environment, processManager, resultDispatcher, message, body);
        }

        public ITask PrepareGitAdd(ITaskResultDispatcher<string> resultDispatcher, List<string> files)
        {
            return new GitAddTask(environment, processManager, resultDispatcher, files);
        }

        public ITask PrepareGitCommitFileTask(ITaskResultDispatcher<string> resultDispatcher, List<string> files, string message, string body)
        {
            return new GitCommitFilesTask(environment, processManager, resultDispatcher, files, message, body);
        }

        public ITask PrepareGitLockFile(ITaskResultDispatcher<string> resultDispatcher, string file)
        {
            return new GitLockTask(environment, processManager, resultDispatcher, file);
        }

        public ITask PrepareGitUnlockFile(ITaskResultDispatcher<string> resultDispatcher, string file, bool force)
        {
            return new GitUnlockTask(environment, processManager, resultDispatcher, file, force);
        }

        protected static ILogging Logger { get; } = Logging.GetLogger<RepositoryProcessRunner>();
    }
}
