using GitHub.Api;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace GitHub.Unity
{
    class GitTask : ProcessTask
    {
        private readonly IEnvironment environment;

        public GitTask(IEnvironment environment, IProcessManager processManager, ITaskResultDispatcher resultDispatcher,
            Action<string> onSuccess, Action onFailure)
            : base(environment, processManager, resultDispatcher, onSuccess, onFailure)
        {
            this.environment = environment;
        }

        public static Task<bool> Run(IEnvironment environment, IProcessManager processManager, string arguments,
            Action<string> onSuccess = null, Action onFailure = null)
        {
            var task = new GitTask(environment, processManager, null, onSuccess, onFailure);
            task.SetArguments(arguments);
            return task.RunAsync(processManager.CancellationToken);
        }

        public override Task<bool> RunAsync(CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(environment.GitExecutablePath))
            {
                RaiseOnFailure(Localization.NoGitError);
                Abort();
            }

            return base.RunAsync(cancellationToken);
        }

        protected override string ProcessName
        {
            get { return environment.GitExecutablePath; }
        }
    }
}
