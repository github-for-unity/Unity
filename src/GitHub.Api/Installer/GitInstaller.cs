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

        public const string GitExtractedMD5 = "e6cfc0c294a2312042f27f893dfc9c0a";
        public const string GitLfsExtractedMD5 = "36e3ae968b69fbf42dff72311040d24a";

        public const string WindowsGitLfsExecutableMD5 = "177bb14d0c08f665a24f0d5516c3b080";
        public const string MacGitLfsExecutableMD5 = "f81a1a065a26a4123193e8fd96c561ad";

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
        private NPath gitArchiveFilePath;
        private NPath gitLfsArchivePath;

        public GitInstaller(IEnvironment environment, CancellationToken cancellationToken, GitInstallDetails installDetails)
            : this(environment, ZipHelper.Instance, cancellationToken, installDetails, null, null)
        {
        }

        public GitInstaller(IEnvironment environment, CancellationToken cancellationToken, GitInstallDetails installDetails, NPath gitArchiveFilePath, NPath gitLfsArchivePath)
            : this(environment, ZipHelper.Instance, cancellationToken, installDetails, gitArchiveFilePath, gitLfsArchivePath)
        {
        }

        public GitInstaller(IEnvironment environment, IZipHelper sharpZipLibHelper, CancellationToken cancellationToken, GitInstallDetails installDetails, NPath gitArchiveFilePath, NPath gitLfsArchivePath)
        {
            this.environment = environment;
            this.sharpZipLibHelper = sharpZipLibHelper;
            this.cancellationToken = cancellationToken;
            this.installDetails = installDetails;
            this.gitArchiveFilePath = gitArchiveFilePath;
            this.gitLfsArchivePath = gitLfsArchivePath;
        }

        public void SetupGitIfNeeded(ActionTask<NPath> onSuccess, ITask onFailure)
        {
            Logger.Trace("SetupGitIfNeeded");

            if (!environment.IsWindows)
            {
                onFailure.Start();
                return;
            }

            new FuncTask<bool>(cancellationToken, IsGitExtracted)
                .Finally((success, ex, isPortableGitExtracted) => {
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
                        ITask downloadFilesTask = null;
                        if (gitArchiveFilePath == null || gitLfsArchivePath == null)
                        {
                            downloadFilesTask = CreateDownloadTask();
                        }

                        var tempZipExtractPath = NPath.CreateTempDirectory("git_zip_extract_zip_paths");
                        var gitExtractPath = tempZipExtractPath.Combine("git").CreateDirectory();
                        var gitLfsExtractPath = tempZipExtractPath.Combine("git-lfs").CreateDirectory();

                        var resultTask = new UnzipTask(cancellationToken, gitArchiveFilePath, gitExtractPath, sharpZipLibHelper, environment.FileSystem, GitInstallDetails.GitExtractedMD5)
                            .Then(new UnzipTask(cancellationToken, gitLfsArchivePath, gitLfsExtractPath, sharpZipLibHelper, environment.FileSystem, GitInstallDetails.GitLfsExtractedMD5))
                            .Then(() => {
                                var targetGitLfsExecPath = installDetails.GetGitLfsExecPath(gitExtractPath);
                                var extractGitLfsExePath = gitLfsExtractPath.Combine(installDetails.GitLfsExec);

                                Logger.Trace("Moving Git LFS Exe:\"{0}\" to target in tempDirectory:\"{1}\" ", extractGitLfsExePath,
                                    targetGitLfsExecPath);

                                extractGitLfsExePath.Move(targetGitLfsExecPath);

                                Logger.Trace("Moving tempDirectory:\"{0}\" to extractTarget:\"{1}\"", gitExtractPath,
                                    installDetails.GitInstallPath);

                                installDetails.GitInstallPath.EnsureParentDirectoryExists();
                                gitExtractPath.Move(installDetails.GitInstallPath);

                                Logger.Trace("Deleting targetGitLfsExecPath:\"{0}\"", targetGitLfsExecPath);
                                targetGitLfsExecPath.DeleteIfExists();

                                Logger.Trace("Deleting tempZipPath:\"{0}\"", tempZipExtractPath);
                                tempZipExtractPath.DeleteIfExists();
                            })
                            .Finally((b, exception) => {
                                if (b)
                                {
                                    Logger.Trace("SetupGitIfNeeded: Success");

                                    new FuncTask<NPath>(cancellationToken, () => installDetails.GitExecPath)
                                        .Then(onSuccess)
                                        .Start();
                                }
                                else
                                {
                                    Logger.Warning("SetupGitIfNeeded: Failed");

                                    onFailure.Start();
                                }
                            });

                        if (downloadFilesTask != null)
                        {
                            resultTask = downloadFilesTask.Then(resultTask);
                        }

                        resultTask.Start();
                    }
                }).Start();
        }

        private ITask CreateDownloadTask()
        {
            var tempZipPath = NPath.CreateTempDirectory("git_zip_paths");
            gitArchiveFilePath = tempZipPath.Combine("git");
            gitLfsArchivePath = tempZipPath.Combine("git-lfs");

            var downloadGitMd5Task = new DownloadTextTask(TaskManager.Instance.Token,
                environment.FileSystem,
                "https://ghfvs-installer.github.com/unity/portable_git/git.zip.MD5.txt",
                gitArchiveFilePath);

            var downloadGitTask = new DownloadTask(TaskManager.Instance.Token, environment.FileSystem,
                "https://ghfvs-installer.github.com/unity/portable_git/git.zip",
                gitArchiveFilePath, retryCount: 1);

            var downloadGitLfsMd5Task = new DownloadTextTask(TaskManager.Instance.Token, environment.FileSystem,
                "https://ghfvs-installer.github.com/unity/portable_git/git-lfs.zip.MD5.txt",
                gitLfsArchivePath);

            var downloadGitLfsTask = new DownloadTask(TaskManager.Instance.Token, environment.FileSystem,
                "https://ghfvs-installer.github.com/unity/portable_git/git-lfs.zip",
                gitLfsArchivePath, retryCount: 1);

            return downloadGitMd5Task
                .Then((b, s) => {
                    downloadGitTask.ValidationHash = s;
                })
                .Then(downloadGitTask)
                .Then(downloadGitLfsMd5Task)
                .Then((b, s) => {
                    downloadGitLfsTask.ValidationHash = s;
                })
                .Then(downloadGitLfsTask);
        }

        private bool IsGitExtracted()
        {
            if (!installDetails.GitInstallPath.DirectoryExists())
            {
                Logger.Warning("{0} does not exist", installDetails.GitInstallPath);
                return false;
            }

            var fileListMD5 = environment.FileSystem.CalculateFolderMD5(installDetails.GitInstallPath, false);
            if (!fileListMD5.Equals(GitInstallDetails.FileListMD5, StringComparison.InvariantCultureIgnoreCase))
            {
                Logger.Warning("Path {0} has MD5 {1} expected {2}", installDetails.GitInstallPath, fileListMD5, GitInstallDetails.FileListMD5);
                return false;
            }

            var calculateMd5 = environment.FileSystem.CalculateFileMD5(installDetails.GitLfsExecPath);
            var md5 = environment.IsWindows ? GitInstallDetails.WindowsGitLfsExecutableMD5 : GitInstallDetails.MacGitLfsExecutableMD5;
            if (!md5.Equals(calculateMd5, StringComparison.InvariantCultureIgnoreCase))
            {
                Logger.Warning("Path {0} has MD5 {1} expected {2}", installDetails.GitLfsExecPath, calculateMd5, md5);
                return false;
            }

            Logger.Trace("Git Present");
            return true;
        }
    }
}
