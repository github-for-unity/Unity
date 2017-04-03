using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace GitHub.Unity
{
    interface IRepositoryProcessRunner
    {
        Task<bool> RunGitStatus(ITaskResultDispatcher<GitStatus> resultDispatcher);
        Task<bool> RunGitConfigGet(ITaskResultDispatcher<string> resultDispatcher, string key, GitConfigSource configSource);
        Task<bool> RunGitListLocks(ITaskResultDispatcher<IEnumerable<GitLock>> resultDispatcher);
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
        ITask PrepareGitCommitFileTask(TaskResultDispatcher<string> resultDispatcher, List<string> files, string message, string body);
    }

    class RepositoryProcessRunner: IRepositoryProcessRunner
    {
        private readonly IEnvironment environment;
        private readonly IProcessManager processManager;
        private readonly ICredentialManager credentialManager;
        private readonly IUIDispatcher uiDispatcher;
        private readonly CancellationToken cancellationToken;

        public RepositoryProcessRunner(IEnvironment environment, IProcessManager processManager,
            ICredentialManager credentialManager, IUIDispatcher uiDispatcher,
            CancellationToken cancellationToken)
        {
            this.environment = environment;
            this.processManager = processManager;
            this.credentialManager = credentialManager;
            this.uiDispatcher = uiDispatcher;
            this.cancellationToken = cancellationToken;
        }

        public Task<bool> RunGitStatus(ITaskResultDispatcher<GitStatus> resultDispatcher)
        {
            var task = new GitStatusTask(environment, processManager, resultDispatcher, new GitObjectFactory(environment));
            return task.RunAsync(cancellationToken);
        }

        public Task<bool> RunGitConfigGet(ITaskResultDispatcher<string> resultDispatcher, string key, GitConfigSource configSource)
        {
            var task = new GitConfigGetTask(environment, processManager, resultDispatcher, key, configSource);
            return task.RunAsync(cancellationToken);
        }

        public Task<bool> RunGitListLocks(ITaskResultDispatcher<IEnumerable<GitLock>> resultDispatcher)
        {
            var gitObjectFactory = new GitObjectFactory(environment);
            var task = new GitListLocksTask(environment, processManager, resultDispatcher, gitObjectFactory);
            return task.RunAsync(cancellationToken);
        }

        public ITask PrepareGitPull(ITaskResultDispatcher<string> resultDispatcher, string remote, string branch)
        {
            return new GitPullTask(environment, processManager, resultDispatcher,
                credentialManager, uiDispatcher, remote, branch);
        }

        public ITask PrepareGitPush(ITaskResultDispatcher<string> resultDispatcher, string remote, string branch)
        {
            return new GitPushTask(environment, processManager, resultDispatcher,
                credentialManager, uiDispatcher, remote, branch, true);
        }

        public ITask PrepareGitFetch(ITaskResultDispatcher<string> resultDispatcher, string remote)
        {
            return new GitFetchTask(environment, processManager, resultDispatcher, credentialManager, uiDispatcher,
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

        public ITask PrepareGitCommit(ITaskResultDispatcher<string> resultDispatcher, string message, string body)
        {
            return new GitCommitTask(environment, processManager, resultDispatcher, message, body);
        }

        public ITask PrepareGitAdd(ITaskResultDispatcher<string> resultDispatcher, List<string> files)
        {
            return new GitAddTask(environment, processManager, resultDispatcher, files);
        }

        public ITask PrepareGitCommitFileTask(TaskResultDispatcher<string> resultDispatcher, List<string> files, string message, string body)
        {
            return new GitCommitFilesTask(environment, processManager, resultDispatcher, files, message, body);
        }

        protected static ILogging Logger { get; } = Logging.GetLogger<RepositoryProcessRunner>();
    }
}
