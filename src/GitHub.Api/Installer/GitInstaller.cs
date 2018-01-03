using System;
using System.Threading;

namespace GitHub.Unity
{
    class GitInstaller
    {
        private static ILogging Logger = Logging.GetLogger<GitInstaller>();

        private readonly IEnvironment environment;
        private readonly IZipHelper sharpZipLibHelper;
        private readonly CancellationToken cancellationToken;
        private readonly PortableGitInstallDetails installDetails;

        public GitInstaller(IEnvironment environment, CancellationToken cancellationToken, PortableGitInstallDetails installDetails)
            : this(environment, null, cancellationToken, installDetails)
        {
        }

        public GitInstaller(IEnvironment environment, IZipHelper sharpZipLibHelper, CancellationToken cancellationToken, PortableGitInstallDetails installDetails)
        {
            this.environment = environment;
            this.sharpZipLibHelper = sharpZipLibHelper;
            this.cancellationToken = cancellationToken;
            this.installDetails = installDetails;
        }

        public void SetupGitIfNeeded(ActionTask<NPath> onSuccess, ITask onFailure)
        {
            Logger.Trace("SetupGitIfNeeded");

            if (!environment.IsWindows)
            {
                onFailure.Start();
            }

            new FuncTask<bool>(cancellationToken, IsPortableGitExtracted)
                .Then((success, isPortableGitExtracted) => {

                    Logger.Trace("IsPortableGitExtracted: {0}", isPortableGitExtracted);

                    if (isPortableGitExtracted)
                    {
                        new FuncTask<NPath>(cancellationToken, () => installDetails.GitExecPath)
                            .Then(onSuccess)
                            .Start();
                    }
                    else
                    {
                        new PortableGitInstallTask(cancellationToken, environment, installDetails).Then((b, path) => {
                            if (b && path != null)
                            {
                                new FuncTask<NPath>(cancellationToken, () => path)
                                    .Then(onSuccess)
                                    .Start();
                            }
                            else
                            {
                                onFailure.Start();
                            }
                        }).Start();
                        
                    }

                }).Start();
        }

        private bool IsPortableGitExtracted()
        {
            if (!installDetails.GitInstallPath.DirectoryExists())
            {
                Logger.Trace("{0} does not exist", installDetails.GitInstallPath);
                return false;
            }

            var fileListMD5 = environment.FileSystem.CalculateFolderMD5(installDetails.GitInstallPath, false);
            if (!fileListMD5.Equals(PortableGitInstallDetails.FileListMD5, StringComparison.InvariantCultureIgnoreCase))
            {
                Logger.Trace("MD5 {0} does not match expected {1}", fileListMD5, PortableGitInstallDetails.FileListMD5);
                return false;
            }

            Logger.Trace("Git Present");
            return true;
        }
    }
}
