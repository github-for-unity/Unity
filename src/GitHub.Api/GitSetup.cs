using System;
using System.Threading;
using System.Threading.Tasks;

namespace GitHub.Unity
{
    class GitSetup
    {
        private readonly CancellationToken cancellationToken;
        private readonly IEnvironment environment;
        private readonly GitInstaller gitInstaller;
        private readonly IProcessManager processManager;

        public GitSetup(IEnvironment environment, IProcessManager processManager, IFileSystem fileSystem,
            CancellationToken cancellationToken)
        {
            this.environment = environment;
            this.processManager = processManager;
            this.cancellationToken = cancellationToken;
            gitInstaller = new GitInstaller(environment, fileSystem, cancellationToken);
            GitInstallationPath = gitInstaller.PackageDestinationDirectory;
            GitExecutablePath = gitInstaller.GitDestinationPath;
        }

        public async Task<bool> SetupIfNeeded(IProgress<float> percentage = null, IProgress<long> timeRemaining = null)
        {
            var setupIfNeeded = await gitInstaller.SetupIfNeeded(percentage, timeRemaining);

            var gitConfigGetTask = new GitConfigGetTask(environment, processManager,
                new TaskResultDispatcher<string>(s => {  }), "credential.helper",
                GitConfigSource.Global);

            return setupIfNeeded;
        }

        public NPath GitInstallationPath { get; private set; }
        public NPath GitExecutablePath { get; private set; }
    }
}
