using System;
using System.Threading;
using System.Threading.Tasks;

namespace GitHub.Unity
{
    class GitSetup
    {
        private readonly IEnvironment environment;
        private readonly CancellationToken cancellationToken;
        private readonly GitInstaller gitInstaller;
        public NPath GitInstallationPath { get; private set; }
        public NPath GitExecutablePath { get; private set; }

        public GitSetup(IEnvironment environment, CancellationToken cancellationToken)
        {
            this.environment = environment;
            this.cancellationToken = cancellationToken;
            gitInstaller = new GitInstaller(environment, cancellationToken: cancellationToken);
            GitInstallationPath = gitInstaller.PackageDestinationDirectory;
            GitExecutablePath = gitInstaller.GitDestinationPath;
        }
        public Task<bool> SetupIfNeeded(IProgress<float> percentage = null, IProgress<long> timeRemaining = null)
        {
            return gitInstaller.Setup(percentage, timeRemaining);
        }
    }
}