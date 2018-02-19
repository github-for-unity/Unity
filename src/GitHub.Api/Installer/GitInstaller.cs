using System;
using System.Threading;
using GitHub.Logging;

namespace GitHub.Unity
{
    class GitInstallDetails
    {
        public const string DefaultGitZipMd5Url = "https://ghfvs-installer.github.com/unity/portable_git/git.zip.MD5.txt";
        public const string DefaultGitZipUrl = "https://ghfvs-installer.github.com/unity/portable_git/git.zip";
        public const string DefaultGitLfsZipMd5Url = "https://ghfvs-installer.github.com/unity/portable_git/git-lfs.zip.MD5.txt";
        public const string DefaultGitLfsZipUrl = "https://ghfvs-installer.github.com/unity/portable_git/git-lfs.zip";

        public const string GitExtractedMD5 = "e6cfc0c294a2312042f27f893dfc9c0a";
        public const string GitLfsExtractedMD5 = "36e3ae968b69fbf42dff72311040d24a";

        public const string WindowsGitLfsExecutableMD5 = "177bb14d0c08f665a24f0d5516c3b080";
        public const string MacGitLfsExecutableMD5 = "f81a1a065a26a4123193e8fd96c561ad";

        public const string FileListMD5 = "a152a216b2e76f6c127053251187a278";

        private const string PackageVersion = "f02737a78695063deace08e96d5042710d3e32db";
        private const string PackageName = "PortableGit";

        private readonly bool onWindows;

        public GitInstallDetails(NPath targetInstallPath, bool onWindows)
        {
            this.onWindows = onWindows;
            var gitInstallPath = targetInstallPath.Combine(ApplicationInfo.ApplicationName, PackageNameWithVersion);
            GitInstallationPath = gitInstallPath;

            if (onWindows)
            {
                GitExecutable += "git.exe";
                GitLfsExecutable += "git-lfs.exe";

                GitExecutablePath = gitInstallPath.Combine("cmd", GitExecutable);
            }
            else
            {
                GitExecutable = "git";
                GitLfsExecutable = "git-lfs";

                GitExecutablePath = gitInstallPath.Combine("bin", GitExecutable);
            }

            GitLfsExecutablePath = GetGitLfsExecutablePath(gitInstallPath);
        }

        public NPath GetGitLfsExecutablePath(NPath gitInstallRoot)
        {
            return onWindows
                ? gitInstallRoot.Combine("mingw32", "libexec", "git-core", GitLfsExecutable)
                : gitInstallRoot.Combine("libexec", "git-core", GitLfsExecutable);
        }

        public NPath GitInstallationPath { get; }
        public string GitExecutable { get; }
        public NPath GitExecutablePath { get; }
        public string GitLfsExecutable { get; }
        public NPath GitLfsExecutablePath { get; }
        public UriString GitZipMd5Url { get; set; } = DefaultGitZipMd5Url;
        public UriString GitZipUrl { get; set; } = DefaultGitZipUrl;
        public UriString GitLfsZipMd5Url { get; set; } = DefaultGitLfsZipMd5Url;
        public UriString GitLfsZipUrl { get; set; } = DefaultGitLfsZipUrl;
        public string PackageNameWithVersion => PackageName + "_" + PackageVersion;
    }

    class GitInstaller
    {
        private static readonly ILogging Logger = LogHelper.GetLogger<GitInstaller>();
        private readonly CancellationToken cancellationToken;

        private readonly IEnvironment environment;
        private readonly GitInstallDetails installDetails;
        private readonly IZipHelper sharpZipLibHelper;
        private NPath gitArchiveFilePath;
        private NPath gitLfsArchivePath;
        private Progress progress = new Progress();
        public event Action<IProgress> OnProgress;

        public GitInstaller(IEnvironment environment, CancellationToken cancellationToken,
            GitInstallDetails installDetails)
            : this(environment, ZipHelper.Instance, cancellationToken, installDetails, null, null)
        {}

        public GitInstaller(IEnvironment environment, CancellationToken cancellationToken,
            GitInstallDetails installDetails, NPath gitArchiveFilePath, NPath gitLfsArchivePath)
            : this(
                environment, ZipHelper.Instance, cancellationToken, installDetails, gitArchiveFilePath,
                gitLfsArchivePath)
        {}

        public GitInstaller(IEnvironment environment, IZipHelper sharpZipLibHelper, CancellationToken cancellationToken,
            GitInstallDetails installDetails, NPath gitArchiveFilePath, NPath gitLfsArchivePath)
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

            new ActionTask(cancellationToken, () => {
                if (IsGitExtracted())
                {
                    Logger.Trace("SetupGitIfNeeded: Skipped");
                    progress.Total = 100;
                    progress.Value = 100;
                    OnProgress?.Invoke(progress);
                    onSuccess.PreviousResult = installDetails.GitExecutablePath;
                    onSuccess.Start();
                }
                else
                {
                    ExtractPortableGit(onSuccess, onFailure);
                }
            }).Start();
        }

        private void ExtractPortableGit(ActionTask<NPath> onSuccess, ITask onFailure)
        {
            Logger.Trace("ExtractPortableGit");
            ITask downloadFilesTask = null;
            if ((gitArchiveFilePath == null) || (gitLfsArchivePath == null))
            {
                downloadFilesTask = CreateDownloadTask();
            }

            var tempZipExtractPath = NPath.CreateTempDirectory("git_zip_extract_zip_paths");
            var gitExtractPath = tempZipExtractPath.Combine("git").CreateDirectory();
            var gitLfsExtractPath = tempZipExtractPath.Combine("git-lfs").CreateDirectory();

            var resultTask = new UnzipTask(cancellationToken, gitArchiveFilePath, gitExtractPath, sharpZipLibHelper, environment.FileSystem, GitInstallDetails.GitExtractedMD5)
                    .Progress(p =>
                    {
                        progress.Task = p.Task;
                        var pt = p.Value / p.Total;
                        progress.Value = 40 + 20 * pt;
                    })
                .Then(new UnzipTask(cancellationToken, gitLfsArchivePath, gitLfsExtractPath, sharpZipLibHelper, environment.FileSystem, GitInstallDetails .GitLfsExtractedMD5))
                    .Progress(p =>
                    {
                        progress.Task = p.Task;
                        var pt = p.Value / p.Total;
                        progress.Value = 60 + 20 * pt;
                    })
                .Then(s => MoveGitAndLfs(gitExtractPath, gitLfsExtractPath, tempZipExtractPath));

            resultTask.Then(onFailure, TaskRunOptions.OnFailure);
            resultTask.Then(onSuccess, TaskRunOptions.OnSuccess);

            if (downloadFilesTask != null)
            {
                resultTask = downloadFilesTask.Then(resultTask);
            }

            resultTask.Start();
        }

        private NPath MoveGitAndLfs(NPath gitExtractPath, NPath gitLfsExtractPath, NPath tempZipExtractPath)
        {
            progress.Value = 80;
            OnProgress?.Invoke(progress);

            var targetGitLfsExecPath = installDetails.GetGitLfsExecutablePath(gitExtractPath);
            var extractGitLfsExePath = gitLfsExtractPath.Combine(installDetails.GitLfsExecutable);

            Logger.Trace($"Moving Git LFS Exe:'{extractGitLfsExePath}' to target in tempDirectory:'{targetGitLfsExecPath}'");

            extractGitLfsExePath.Move(targetGitLfsExecPath);

            Logger.Trace($"Moving tempDirectory:'{gitExtractPath}' to extractTarget:'{installDetails.GitInstallationPath}'");

            progress.Value = 90;
            OnProgress?.Invoke(progress);

            installDetails.GitInstallationPath.EnsureParentDirectoryExists();
            gitExtractPath.Move(installDetails.GitInstallationPath);

            Logger.Trace($"Deleting targetGitLfsExecPath:'{targetGitLfsExecPath}'");

            targetGitLfsExecPath.DeleteIfExists();

            Logger.Trace($"Deleting tempZipPath:'{tempZipExtractPath}'");
            tempZipExtractPath.DeleteIfExists();
            return installDetails.GitExecutablePath;
        }

        private ITask CreateDownloadTask()
        {
            var tempZipPath = NPath.CreateTempDirectory("git_zip_paths");
            gitArchiveFilePath = tempZipPath.Combine("git.zip");
            gitLfsArchivePath = tempZipPath.Combine("git-lfs.zip");

            var downloadGitMd5Task = new DownloadTextTask(TaskManager.Instance.Token, environment.FileSystem,
                installDetails.GitZipMd5Url, tempZipPath)
                .Progress(p =>
                {
                    progress.Task = p.Task;
                    var pt = p.Value / p.Total;
                    progress.Value = 10 * pt;
                });

            var downloadGitTask = new DownloadTask(TaskManager.Instance.Token, environment.FileSystem,
                installDetails.GitZipUrl, tempZipPath)
                .Progress(p =>
                {
                    progress.Task = p.Task;
                    var pt = p.Value / p.Total;
                    progress.Value = 10 + 10 * pt;
                });

            var downloadGitLfsMd5Task = new DownloadTextTask(TaskManager.Instance.Token, environment.FileSystem,
                installDetails.GitLfsZipMd5Url, tempZipPath)
                .Progress(p =>
                {
                    progress.Task = p.Task;
                    var pt = p.Value / p.Total;
                    progress.Value = 20 + 10 * pt;
                });

            var downloadGitLfsTask = new DownloadTask(TaskManager.Instance.Token, environment.FileSystem,
                installDetails.GitLfsZipUrl, tempZipPath)
                .Progress(p =>
                {
                    progress.Task = p.Task;
                    var pt = p.Value / p.Total;
                    progress.Value = 30 + 10 * pt;
                });

            return
                downloadGitMd5Task.Then((b, s) => { ((DownloadTask)downloadGitTask).ValidationHash = s; })
                                  .Then(downloadGitTask)
                                  .Then(downloadGitLfsMd5Task)
                                  .Then((b, s) => { ((DownloadTask)downloadGitLfsTask).ValidationHash = s; })
                                  .Then(downloadGitLfsTask);
        }

        private bool IsGitExtracted()
        {
            if (!installDetails.GitInstallationPath.DirectoryExists())
            {
                Logger.Warning($"{installDetails.GitInstallationPath} does not exist");
                return false;
            }

            var fileListMD5 = environment.FileSystem.CalculateFolderMD5(installDetails.GitInstallationPath, false);
            if (!fileListMD5.Equals(GitInstallDetails.FileListMD5, StringComparison.InvariantCultureIgnoreCase))
            {
                Logger.Warning($"Path {installDetails.GitInstallationPath} has MD5 {fileListMD5} expected {GitInstallDetails.FileListMD5}");
                return false;
            }

            var calculateMd5 = environment.FileSystem.CalculateFileMD5(installDetails.GitLfsExecutablePath);
            var md5 = environment.IsWindows ? GitInstallDetails.WindowsGitLfsExecutableMD5 : GitInstallDetails.MacGitLfsExecutableMD5;

            if (!md5.Equals(calculateMd5, StringComparison.InvariantCultureIgnoreCase))
            {
                Logger.Warning($"Path {installDetails.GitLfsExecutablePath} has MD5 {calculateMd5} expected {md5}");
                return false;
            }

            Logger.Trace("Git Present");
            return true;
        }
    }
}
