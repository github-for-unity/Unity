using GitHub.Unity;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace GitHub.Unity
{
    class GitTask : ProcessTask
    {
        private readonly IEnvironment environment;

        public GitTask(IEnvironment environment, IProcessManager processManager, ITaskResultDispatcher<string> resultDispatcher = null)
            : base(environment, processManager, resultDispatcher)
        {
            this.environment = environment;
        }

        public static Task<bool> Run(IEnvironment environment, IProcessManager processManager,
            ITaskResultDispatcher<string> resultDispatcher, string arguments)
        {
            var task = new GitTask(environment, processManager);
            task.SetArguments(arguments);
            return task.RunAsync(processManager.CancellationToken);
        }

        public override Task<bool> RunAsync(CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(environment.GitExecutablePath))
            {
                RaiseOnFailure();
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
