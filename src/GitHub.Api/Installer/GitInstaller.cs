using System;
using System.Threading;

namespace GitHub.Unity
{
    class GitInstallDetails
    {
        public NPath GitInstallPath { get; }
        public string GitExec { get; }
        public NPath GitExecPath { get; }
        public string GitLfsExec { get; }
        public NPath GitLfsExecPath { get; }

        public const string ExtractedMD5 = "65fd0575d3b47d8207b9e19d02faca4f";
        public const string FileListMD5 = "a152a216b2e76f6c127053251187a278";

        private const string PackageVersion = "f02737a78695063deace08e96d5042710d3e32db";
        private const string PackageName = "PortableGit";
        private const string PackageNameWithVersion = PackageName + "_" + PackageVersion;

        private readonly bool onWindows;

        public GitInstallDetails(NPath targetInstallPath, bool onWindows)
        {
            this.onWindows = onWindows;
            var gitInstallPath = targetInstallPath.Combine(ApplicationInfo.ApplicationName, PackageNameWithVersion);
            GitInstallPath = gitInstallPath;

            if (onWindows)
            {
                GitExec += "git.exe";
                GitLfsExec += "git-lfs.exe";

                GitExecPath = gitInstallPath.Combine("cmd", GitExec);
            }
            else
            {
                GitExec = "git";
                GitLfsExec = "git-lfs";

                GitExecPath = gitInstallPath.Combine("bin", GitExec);
            }

            GitLfsExecPath = GetGitLfsExecPath(gitInstallPath);
        }

        public NPath GetGitLfsExecPath(NPath gitInstallRoot)
        {
            return onWindows
                ? gitInstallRoot.Combine("mingw32", "libexec", "git-core", GitLfsExec)
                : gitInstallRoot.Combine("libexec", "git-core", GitLfsExec);
        }
    }

    class GitInstaller
    {
        private static ILogging Logger = Logging.GetLogger<GitInstaller>();

        private readonly IEnvironment environment;
        private readonly IZipHelper sharpZipLibHelper;
        private readonly CancellationToken cancellationToken;
        private readonly GitInstallDetails installDetails;

        public GitInstaller(IEnvironment environment, CancellationToken cancellationToken, GitInstallDetails installDetails)
            : this(environment, ZipHelper.Instance, cancellationToken, installDetails)
        {
        }

        public GitInstaller(IEnvironment environment, IZipHelper sharpZipLibHelper, CancellationToken cancellationToken, GitInstallDetails installDetails)
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

            new FuncTask<bool>(cancellationToken, IsGitExtracted)
                .Then((success, isPortableGitExtracted) => {

                    Logger.Trace("IsPortableGitExtracted: {0}", isPortableGitExtracted);

                    if (isPortableGitExtracted)
                    {
                        Logger.Trace("SetupGitIfNeeded: Skipped");

                        new FuncTask<NPath>(cancellationToken, () => installDetails.GitExecPath)
                            .Then(onSuccess)
                            .Start();
                    }
                    else
                    {
                        var tempZipPath = NPath.CreateTempDirectory("git_zip_paths");
                        var gitArchivePath = tempZipPath.Combine("git.zip");
                        var gitLfsArchivePath = tempZipPath.Combine("git-lfs.zip");
                        var gitExtractPath = tempZipPath.Combine("git").CreateDirectory();
                        var gitLfsExtractPath = tempZipPath.Combine("git-lfs").CreateDirectory();

                        var downloadGitMd5Task = new DownloadTextTask(CancellationToken.None,
                            "https://ghfvs-installer.github.com/unity/portable_git/git.zip.MD5.txt");

                        var downloadGitTask = new DownloadTask(CancellationToken.None, environment.FileSystem,
                            "https://ghfvs-installer.github.com/unity/portable_git/git.zip", gitArchivePath, retryCount: 1);

                        var downloadGitLfsMd5Task = new DownloadTextTask(CancellationToken.None,
                            "https://ghfvs-installer.github.com/unity/portable_git/git-lfs.zip.MD5.txt");

                        var downloadGitLfsTask = new DownloadTask(CancellationToken.None, environment.FileSystem,
                            "https://ghfvs-installer.github.com/unity/portable_git/git-lfs.zip", gitLfsArchivePath, retryCount: 1);

                        downloadGitMd5Task
                            .Then((b, s) =>
                            {
                                downloadGitTask.ValidationHash = s;
                            })
                            .Then(downloadGitTask)
                            .Then(downloadGitLfsMd5Task)
                            .Then((b, s) =>
                            {
                                downloadGitLfsTask.ValidationHash = s;
                            })
                            .Then(downloadGitLfsTask)
                            .Then(new UnzipTask(cancellationToken, gitArchivePath, gitExtractPath, sharpZipLibHelper))
                            .Then(new UnzipTask(cancellationToken, gitLfsArchivePath, gitLfsExtractPath, sharpZipLibHelper))
                            .Then(() => {
                                var targetGitLfsExecPath = installDetails.GetGitLfsExecPath(gitExtractPath);
                                var extractGitLfsExePath = gitLfsExtractPath.Combine(installDetails.GitLfsExec);
                                extractGitLfsExePath.Move(targetGitLfsExecPath);

                                var extractedMD5 = environment.FileSystem.CalculateFolderMD5(gitExtractPath);
                                if (!extractedMD5.Equals(GitInstallDetails.ExtractedMD5, StringComparison.InvariantCultureIgnoreCase))
                                {
                                    Logger.Warning("MD5 {0} does not match expected {1}", extractedMD5, GitInstallDetails.ExtractedMD5);
                                    Logger.Warning("Failed PortableGitInstallTask");
                                    throw new Exception();
                                }

                                Logger.Trace("Moving tempDirectory:\"{0}\" to extractTarget:\"{1}\"", gitExtractPath,
                                    installDetails.GitInstallPath);

                                gitExtractPath.Move(installDetails.GitInstallPath);

                                Logger.Trace("Deleting tempZipPath:\"{0}\"", tempZipPath);
                                tempZipPath.DeleteIfExists();

                            }).Finally((b, exception) => {
                                if (b)
                                {
                                    Logger.Trace("SetupGitIfNeeded: Success");

                                    new FuncTask<NPath>(cancellationToken, () => installDetails.GitExecPath)
                                        .Then(onSuccess)
                                        .Start();
                                }
                                else
                                {
                                    Logger.Trace("SetupGitIfNeeded: Failed");

                                    onFailure.Start();
                                }
                            }).Start();
                    }

                }).Start();
        }

        private bool IsGitExtracted()
        {
            if (!installDetails.GitInstallPath.DirectoryExists())
            {
                Logger.Trace("{0} does not exist", installDetails.GitInstallPath);
                return false;
            }

            var fileListMD5 = environment.FileSystem.CalculateFolderMD5(installDetails.GitInstallPath, false);
            if (!fileListMD5.Equals(GitInstallDetails.FileListMD5, StringComparison.InvariantCultureIgnoreCase))
            {
                Logger.Trace("MD5 {0} does not match expected {1}", fileListMD5, GitInstallDetails.FileListMD5);
                return false;
            }

            Logger.Trace("Git Present");
            return true;
        }
    }
}
