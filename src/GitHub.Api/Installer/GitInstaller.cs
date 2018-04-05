using System;
using System.Threading;
using GitHub.Logging;

namespace GitHub.Unity
{
    class GitInstaller
    {
        private static readonly ILogging Logger = LogHelper.GetLogger<GitInstaller>();
        private readonly CancellationToken cancellationToken;

        private readonly IEnvironment environment;
        private readonly IProcessManager processManager;
        private readonly GitInstallDetails installDetails;
        private readonly IZipHelper sharpZipLibHelper;

        ITask<GitInstallationState> installationTask;

        public GitInstaller(IEnvironment environment, IProcessManager processManager,
            ITaskManager taskManager,
            GitInstallDetails installDetails = null)
        {
            this.environment = environment;
            this.processManager = processManager;
            this.sharpZipLibHelper = ZipHelper.Instance;
            this.cancellationToken = taskManager.Token;
            this.installDetails = installDetails ?? new GitInstallDetails(environment.UserCachePath, environment.IsWindows);
        }

        public ITask<GitInstallationState> SetupGitIfNeeded()
        {
            //Logger.Trace("SetupGitIfNeeded");

            installationTask = new FuncTask<GitInstallationState, GitInstallationState>(cancellationToken, (success, path) => path)
                { Name = "Git Installation - Complete" };
            installationTask.OnStart += thisTask => thisTask.UpdateProgress(0, 100);
            installationTask.OnEnd += (thisTask, result, success, exception) => thisTask.UpdateProgress(100, 100);

            ITask<GitInstallationState> startTask = null;
            GitInstallationState installationState = new GitInstallationState();
            if (!environment.IsWindows)
            {
                var findTask = new FindExecTask("git", cancellationToken)
                    .Configure(processManager, dontSetupGit: true)
                    .Catch(e => true);
                findTask.OnEnd += (thisTask, path, success, exception) =>
                {
                    // we should doublecheck that system git is usable here
                    installationState.GitIsValid = success;
                    if (success)
                    {
                        installationState.GitExecutablePath = path;
                        installationState.GitInstallationPath = path.Resolve().Parent.Parent;
                    }
                };
                findTask.Then(new FindExecTask("git-lfs", cancellationToken)
                    .Configure(processManager, dontSetupGit: true))
                    .Catch(e => true);
                findTask.OnEnd += (thisTask, path, success, exception) =>
                {
                    installationState.GitLfsIsValid = success;
                    if (success)
                    {
                        // we should doublecheck that system git is usable here
                        installationState.GitLfsExecutablePath = path;
                        installationState.GitLfsInstallationPath = path.Resolve().Parent.Parent;
                    }
                };
                startTask = findTask.Then(s => installationState);
            }
            else
            {
                startTask = new FuncTask<GitInstallationState>(cancellationToken, () =>
                    {
                        installationState = VerifyGitInstallation();
                        if (!installationState.GitIsValid && !installationState.GitLfsIsValid)
                            installationState = GrabZipFromResources(installationState);
                        else
                            Logger.Trace("SetupGitIfNeeded: Skipped");
                        return installationState;
                    })
                { Name = "Git Installation - Extract" };
            }

            startTask.OnEnd += (thisTask, installState, success, exception) =>
            {
                if (!installState.GitIsValid && !installState.GitLfsIsValid)
                {
                    if (!installState.GitZipExists || !installState.GitLfsZipExists)
                        thisTask = thisTask.Then(CreateDownloadTask(installState));
                    thisTask = thisTask.Then(ExtractPortableGit(installState));
                }
                thisTask = thisTask.Then(installationTask);
            };

            return startTask;
        }

        private GitInstallationState VerifyGitInstallation()
        {
            var state = new GitInstallationState();
            var gitExists = installDetails.GitExecutablePath.IsInitialized && installDetails.GitExecutablePath.FileExists();
            var gitLfsExists = installDetails.GitLfsExecutablePath.IsInitialized && installDetails.GitLfsExecutablePath.FileExists();
            state.GitZipExists = installDetails.GitZipPath.FileExists();
            state.GitLfsZipExists = installDetails.GitLfsZipPath.FileExists();

            if (gitExists)
            {
                var actualmd5 = installDetails.GitExecutablePath.CalculateMD5();
                var expectedmd5 = environment.IsWindows ? GitInstallDetails.WindowsGitExecutableMD5 : GitInstallDetails.MacGitExecutableMD5;
                state.GitIsValid = expectedmd5.Equals(actualmd5, StringComparison.InvariantCultureIgnoreCase);
                if (state.GitIsValid)
                {
                    state.GitInstallationPath = installDetails.GitInstallationPath;
                    state.GitExecutablePath = installDetails.GitExecutablePath;
                }
                else
                {
                    Logger.Trace($"Path {installDetails.GitExecutablePath} has MD5 {actualmd5} expected {expectedmd5}");
                }
            }
            else
                Logger.Trace($"{installDetails.GitExecutablePath} does not exist");

            if (gitLfsExists)
            {
                var actualmd5 = installDetails.GitLfsExecutablePath.CalculateMD5();
                var expectedmd5 = environment.IsWindows ? GitInstallDetails.WindowsGitLfsExecutableMD5 : GitInstallDetails.MacGitLfsExecutableMD5;
                state.GitLfsIsValid = expectedmd5.Equals(actualmd5, StringComparison.InvariantCultureIgnoreCase);
                if (state.GitLfsIsValid)
                {
                    state.GitLfsInstallationPath = installDetails.GitInstallationPath;
                    state.GitLfsExecutablePath = installDetails.GitLfsExecutablePath;
                }
                else
                {
                    Logger.Trace($"Path {installDetails.GitLfsExecutablePath} has MD5 {actualmd5} expected {expectedmd5}");
                }
            }
            else
                Logger.Trace($"{installDetails.GitLfsExecutablePath} does not exist");

            installationTask.UpdateProgress(10, 100);
            return state;
        }

        private GitInstallationState GrabZipFromResources(GitInstallationState state)
        {
            if (!state.GitZipExists)
            {
                AssemblyResources.ToFile(ResourceType.Platform, "git.zip", installDetails.ZipPath, environment);
                AssemblyResources.ToFile(ResourceType.Platform, "git.zip.md5", installDetails.ZipPath, environment);
            }
            state.GitZipExists = installDetails.GitZipPath.FileExists();

            if (!state.GitLfsZipExists)
            {
                AssemblyResources.ToFile(ResourceType.Platform, "git-lfs.zip", installDetails.ZipPath, environment);
                AssemblyResources.ToFile(ResourceType.Platform, "git-lfs.zip.md5", installDetails.ZipPath, environment);
            }
            state.GitLfsZipExists = installDetails.GitLfsZipPath.FileExists();
            installationTask.UpdateProgress(20, 100);
            return state;
        }

        private ITask<GitInstallationState> CreateDownloadTask(GitInstallationState state)
        {
            var downloader = new Downloader();
            downloader.Catch(e => true);
            if (!state.GitIsValid)
                downloader.QueueDownload(installDetails.GitZipUrl, installDetails.GitZipMd5Url, installDetails.ZipPath);
            if (!state.GitLfsIsValid)
                downloader.QueueDownload(installDetails.GitLfsZipUrl, installDetails.GitLfsZipMd5Url, installDetails.ZipPath);
            return downloader.Then((success, data) =>
            {
                state.GitZipExists = installDetails.GitZipPath.FileExists();
                state.GitLfsZipExists = installDetails.GitLfsZipPath.FileExists();
                installationTask.UpdateProgress(40, 100);
                return state;
            });
        }

        private FuncTask<GitInstallationState> ExtractPortableGit(GitInstallationState state)
        {
            ITask<NPath> task = null;
            var tempZipExtractPath = NPath.CreateTempDirectory("git_zip_extract_zip_paths");

            if (state.GitZipExists && !state.GitIsValid)
            {
                var gitExtractPath = tempZipExtractPath.Combine("git").CreateDirectory();
                var unzipTask = new UnzipTask(cancellationToken, installDetails.GitZipPath,
                        gitExtractPath, sharpZipLibHelper,
                        environment.FileSystem)
                    .Catch(e => true);
                unzipTask.Progress(p => installationTask.UpdateProgress(40 + (long)(20 * p.Percentage), 100, unzipTask.Name));

                unzipTask = unzipTask.Then((success, path) =>
                {
                    var target = installDetails.GitInstallationPath;
                    if (success)
                    {
                        var source = path;
                        target.DeleteIfExists();
                        target.EnsureParentDirectoryExists();
                        Logger.Trace($"Moving '{source}' to '{target}'");
                        source.Move(target);
                        state.GitInstallationPath = installDetails.GitInstallationPath;
                        state.GitExecutablePath = installDetails.GitExecutablePath;
                        state.GitIsValid = success;
                    }
                    return target;
                });
                task = unzipTask;
            }

            if (state.GitLfsZipExists && !state.GitLfsIsValid)
            {
                var gitLfsExtractPath = tempZipExtractPath.Combine("git-lfs").CreateDirectory();
                var unzipTask = new UnzipTask(cancellationToken, installDetails.GitLfsZipPath,
                        gitLfsExtractPath, sharpZipLibHelper,
                        environment.FileSystem)
                    .Catch(e => true);
                unzipTask.Progress(p => installationTask.UpdateProgress(60 + (long)(20 * p.Percentage), 100, unzipTask.Name));

                unzipTask = unzipTask.Then((success, path) =>
                {
                    var target = installDetails.GetGitLfsExecutablePath(state.GitInstallationPath);
                    if (success)
                    {
                        var source = path.Combine(installDetails.GitLfsExecutable);
                        target.DeleteIfExists();
                        target.EnsureParentDirectoryExists();
                        Logger.Trace($"Moving '{source}' to '{target}'");
                        source.Move(target);
                        state.GitLfsInstallationPath = state.GitInstallationPath;
                        state.GitLfsExecutablePath = target;
                        state.GitLfsIsValid = success;
                    }
                    return target;
                });
                task = task?.Then(unzipTask) ?? unzipTask;
            }

            var endTask = new FuncTask<GitInstallationState>(cancellationToken, (success) =>
            {
                tempZipExtractPath.DeleteIfExists();
                return state;
            });

            if (task != null)
            {
                endTask = task.Then(endTask);
            }

            return endTask;
        }

        public class GitInstallationState
        {
            public bool GitIsValid { get; set; }
            public bool GitLfsIsValid { get; set; }
            public bool GitZipExists { get; set; }
            public bool GitLfsZipExists { get; set; }
            public NPath GitInstallationPath { get; set; }
            public NPath GitExecutablePath { get; set; }
            public NPath GitLfsInstallationPath { get; set; }
            public NPath GitLfsExecutablePath { get; set; }
        }

        public class GitInstallDetails
        {
            public const string DefaultGitZipMd5Url = "https://ghfvs-installer.github.com/unity/git/windows/git.zip.md5";
            public const string DefaultGitZipUrl = "https://ghfvs-installer.github.com/unity/git/windows/git.zip";
            public const string DefaultGitLfsZipMd5Url = "https://ghfvs-installer.github.com/unity/git/windows/git-lfs.zip.md5";
            public const string DefaultGitLfsZipUrl = "https://ghfvs-installer.github.com/unity/git/windows/git-lfs.zip";

            public const string GitExtractedMD5 = "e6cfc0c294a2312042f27f893dfc9c0a";
            public const string GitLfsExtractedMD5 = "36e3ae968b69fbf42dff72311040d24a";

            public const string WindowsGitExecutableMD5 = "50570ed932559f294d1a1361801740b9";
            public const string MacGitExecutableMD5 = "";

            public const string WindowsGitLfsExecutableMD5 = "177bb14d0c08f665a24f0d5516c3b080";
            public const string MacGitLfsExecutableMD5 = "f81a1a065a26a4123193e8fd96c561ad";

            private const string PackageVersion = "f02737a78695063deace08e96d5042710d3e32db";
            private const string PackageName = "PortableGit";

            private const string gitZip = "git.zip";
            private const string gitLfsZip = "git-lfs.zip";

            private readonly bool onWindows;

            public GitInstallDetails(NPath baseDataPath, bool onWindows)
            {
                this.onWindows = onWindows;

                ZipPath = baseDataPath.Combine("downloads");
                ZipPath.EnsureDirectoryExists();
                GitZipPath = ZipPath.Combine(gitZip);
                GitLfsZipPath = ZipPath.Combine(gitLfsZip);

                var gitInstallPath = baseDataPath.Combine(PackageNameWithVersion);
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

            public NPath ZipPath { get; }
            public NPath GitZipPath { get; }
            public NPath GitLfsZipPath { get; }
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
    }
}
