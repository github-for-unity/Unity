using System;
using System.Threading;
using System.Threading.Tasks;

namespace GitHub.Unity
{
    interface IRepositoryProcessRunner
    {
        Task<bool> RunGitStatus(ITaskResultDispatcher<GitStatus> resultDispatcher);
        Task<bool> RunGitTrackedFileList(ITaskResultDispatcher<GitStatus> resultDispatcher);
        ITask PrepareGitPull(ITaskResultDispatcher<string> resultDispatcher, string remote, string branch);
        ITask PrepareGitPush(ITaskResultDispatcher<string> resultDispatcher, string remote, string branch);
    }

    class RepositoryProcessRunner : IRepositoryProcessRunner
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

        public Task<bool> RunGitTrackedFileList(ITaskResultDispatcher<GitStatus> resultDispatcher)
        {
            throw new NotImplementedException();
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

        protected static ILogging Logger { get; } = Logging.GetLogger<RepositoryProcessRunner>();
    }
}