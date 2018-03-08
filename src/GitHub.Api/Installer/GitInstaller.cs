using System;
using System.Threading;
using System.Threading.Tasks;
using GitHub.Logging;

namespace GitHub.Unity
{
    class GitInstaller
    {
        private static readonly ILogging Logger = LogHelper.GetLogger<GitInstaller>();
        private readonly CancellationToken cancellationToken;

        private readonly IEnvironment environment;
        private readonly GitInstallDetails installDetails;
        private readonly IZipHelper sharpZipLibHelper;

        ITask<NPath> installationTask;

        public GitInstaller(IEnvironment environment, CancellationToken cancellationToken,
            GitInstallDetails installDetails = null)
        {
            this.environment = environment;
            this.sharpZipLibHelper = ZipHelper.Instance;
            this.cancellationToken = cancellationToken;
            this.installDetails = installDetails ?? new GitInstallDetails(environment.UserCachePath, environment.IsWindows);
        }

        public ITask<NPath> SetupGitIfNeeded()
        {
            //Logger.Trace("SetupGitIfNeeded");

            installationTask = new FuncTask<GitInstallationState, NPath>(cancellationToken, (_, r) => installDetails.GitExecutablePath)
                { Name = "Git Installation - Complete" };
            installationTask.OnStart += thisTask => thisTask.UpdateProgress(0, 100);
            installationTask.OnEnd += (thisTask, result, success, exception) => thisTask.UpdateProgress(100, 100);

            if (!environment.IsWindows)
                return installationTask;

            var startTask = new FuncTask<GitInstallationState>(cancellationToken, () =>
                {
                    var state = VerifyGitInstallation();
                    if (!state.GitIsValid && !state.GitLfsIsValid)
                        state = GrabZipFromResources(state);
                    else
                        Logger.Trace("SetupGitIfNeeded: Skipped");
                    return state;
                })
                { Name = "Git Installation - Extract" };


            startTask.OnEnd += (thisTask, state, success, exception) =>
            {
                if (!state.GitIsValid && !state.GitLfsIsValid)
                {
                    if (!state.GitZipExists || !state.GitLfsZipExists)
                        thisTask = thisTask.Then(CreateDownloadTask(state));
                    thisTask = thisTask.Then(ExtractPortableGit(state));
                }
                thisTask.Then(installationTask);
            };

            // we want to start the startTask and not the installationTask because the latter only gets
            // appended to the task chain when startTask ends, so calling Start() on it wouldn't work
            startTask.Start();
            return installationTask;
        }

        private GitInstallationState VerifyGitInstallation()
        {
            var state = new GitInstallationState();
            state.GitExists = installDetails.GitExecutablePath.FileExists() ?? false;
            state.GitLfsExists = installDetails.GitLfsExecutablePath.FileExists() ?? false;
            state.GitZipExists = installDetails.GitZipPath.FileExists();
            state.GitLfsZipExists = installDetails.GitLfsZipPath.FileExists();

            if (state.GitExists)
            {
                var actualmd5 = installDetails.GitExecutablePath.CalculateMD5();
                var expectedmd5 = environment.IsWindows ? GitInstallDetails.WindowsGitExecutableMD5 : GitInstallDetails.MacGitExecutableMD5;
                state.GitIsValid = expectedmd5.Equals(actualmd5, StringComparison.InvariantCultureIgnoreCase);
                if (!state.GitIsValid)
                    Logger.Trace($"Path {installDetails.GitExecutablePath} has MD5 {actualmd5} expected {expectedmd5}");
            }
            else
                Logger.Trace($"{installDetails.GitExecutablePath} does not exist");

            if (state.GitLfsExists)
            {
                var actualmd5 = installDetails.GitLfsExecutablePath.CalculateMD5();
                var expectedmd5 = environment.IsWindows ? GitInstallDetails.WindowsGitLfsExecutableMD5 : GitInstallDetails.MacGitLfsExecutableMD5;
                state.GitLfsIsValid = expectedmd5.Equals(actualmd5, StringComparison.InvariantCultureIgnoreCase);
                if (!state.GitLfsIsValid)
                    Logger.Trace($"Path {installDetails.GitLfsExecutablePath} has MD5 {actualmd5} expected {expectedmd5}");
            }
            else
                Logger.Trace($"{installDetails.GitLfsExecutablePath} does not exist");

            if (!state.GitZipExists)
                Logger.Trace($"{installDetails.GitZipPath} does not exist");
            if (!state.GitLfsZipExists)
                Logger.Trace($"{installDetails.GitLfsZipPath} does not exist");
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
            downloader.QueueDownload(installDetails.GitZipUrl, installDetails.GitZipMd5Url, installDetails.ZipPath);
            downloader.QueueDownload(installDetails.GitLfsZipUrl, installDetails.GitLfsZipMd5Url, installDetails.ZipPath);
            return downloader.Then((_, data) =>
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
            var gitExtractPath = tempZipExtractPath.Combine("git").CreateDirectory();

            if (!state.GitIsValid)
            {
                ITask<NPath> unzipTask = new UnzipTask(cancellationToken, installDetails.GitZipPath, gitExtractPath, sharpZipLibHelper,
                    environment.FileSystem, GitInstallDetails.GitExtractedMD5);
                unzipTask.Progress(p => installationTask.UpdateProgress(40 + (long)(20 * p.Percentage), 100, unzipTask.Name));

                unzipTask = unzipTask.Then((s, path) =>
                {
                    var source = path;
                    var target = installDetails.GitInstallationPath;
                    target.DeleteIfExists();
                    target.EnsureParentDirectoryExists();
                    Logger.Trace($"Moving '{source}' to '{target}'");
                    source.Move(target);
                    state.GitExists = installDetails.GitExecutablePath.FileExists();
                    state.GitIsValid = s;
                    return path;
                });
                task = unzipTask;
            }

            var gitLfsExtractPath = tempZipExtractPath.Combine("git-lfs").CreateDirectory();

            if (!state.GitLfsIsValid)
            {
                ITask<NPath> unzipTask = new UnzipTask(cancellationToken, installDetails.GitLfsZipPath, gitLfsExtractPath, sharpZipLibHelper,
                    environment.FileSystem, GitInstallDetails.GitLfsExtractedMD5);
                unzipTask.Progress(p => installationTask.UpdateProgress(60 + (long)(20 * p.Percentage), 100, unzipTask.Name));

                unzipTask = unzipTask.Then((s, path) =>
                {
                    var source = path.Combine(installDetails.GitLfsExecutable);
                    var target = installDetails.GetGitLfsExecutablePath(installDetails.GitInstallationPath);
                    target.DeleteIfExists();
                    target.EnsureParentDirectoryExists();
                    Logger.Trace($"Moving '{source}' to '{target}'");
                    source.Move(target);
                    state.GitExists = target.FileExists();
                    state.GitIsValid = s;
                    return path;
                });
                task = task?.Then(unzipTask) ?? unzipTask;
            }

            return task.Finally(new FuncTask<GitInstallationState>(cancellationToken, (success) =>
            {
                tempZipExtractPath.DeleteIfExists();
                return state;
            }));
        }

        class GitInstallationState
        {
            public bool GitExists { get; set; }
            public bool GitLfsExists { get; set; }
            public bool GitIsValid { get; set; }
            public bool GitLfsIsValid { get; set; }
            public bool GitZipExists { get; set; }
            public bool GitLfsZipExists { get; set; }
        }

        public class GitInstallDetails
        {
            public const string DefaultGitZipMd5Url = "https://ghfvs-installer.github.com/unity/portable_git/git.zip.md5";
            public const string DefaultGitZipUrl = "https://ghfvs-installer.github.com/unity/portable_git/git.zip";
            public const string DefaultGitLfsZipMd5Url = "https://ghfvs-installer.github.com/unity/portable_git/git-lfs.zip.md5";
            public const string DefaultGitLfsZipUrl = "https://ghfvs-installer.github.com/unity/portable_git/git-lfs.zip";

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
