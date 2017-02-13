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

        public override Task<bool> RunAsync(CancellationToken cancel)
        {
            if (string.IsNullOrEmpty(environment.GitInstallPath))
            {
                RaiseOnFailure(Localization.NoGitError);
                Abort();
            }

            return base.RunAsync(cancel);
        }

        protected override string ProcessName
        {
            get { return environment.GitInstallPath; }
        }
        protected IEnvironment Environment => environment;
    }
}
