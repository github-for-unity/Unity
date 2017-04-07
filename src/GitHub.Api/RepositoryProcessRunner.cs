using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace GitHub.Unity
{
    interface IRepositoryProcessRunner
    {
        ITask<GitStatus?> PrepareGitStatus(ITaskResultDispatcher<GitStatus> resultDispatcher);
        Task<bool> RunGitConfigGet(ITaskResultDispatcher<string> resultDispatcher, string key, GitConfigSource configSource);
        ITask<IEnumerable<GitLock>> PrepareGitListLocks(ITaskResultDispatcher<IEnumerable<GitLock>> resultDispatcher);
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

        public ITask<GitStatus?> PrepareGitStatus(ITaskResultDispatcher<GitStatus> resultDispatcher)
        {
            return new GitStatusTask(environment, processManager, resultDispatcher, new GitObjectFactory(environment));
        }

        public Task<bool> RunGitConfigGet(ITaskResultDispatcher<string> resultDispatcher, string key, GitConfigSource configSource)
        {
            var task = new GitConfigGetTask(environment, processManager, resultDispatcher, key, configSource);
            return task.RunAsync(cancellationToken);
        }

        public ITask<IEnumerable<GitLock>> PrepareGitListLocks(ITaskResultDispatcher<IEnumerable<GitLock>> resultDispatcher)
        {
            var gitObjectFactory = new GitObjectFactory(environment);
            return new GitListLocksTask(environment, processManager, resultDispatcher, gitObjectFactory);
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
