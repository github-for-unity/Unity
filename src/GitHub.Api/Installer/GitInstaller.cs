using System;
using System.Threading;
using GitHub.Logging;

namespace GitHub.Unity
{
    public class GitInstaller
    {
        private static readonly ILogging Logger = LogHelper.GetLogger<GitInstaller>();
        private readonly CancellationToken cancellationToken;

        private readonly IEnvironment environment;
        private readonly IProcessManager processManager;
        private readonly ISettings systemSettings;
        private readonly GitInstallDetails installDetails;
        private readonly IZipHelper sharpZipLibHelper;

        public IProgress Progress { get; set; }

        public GitInstaller(IEnvironment environment, IProcessManager processManager,
            CancellationToken token,
            ISettings systemSettings,
            GitInstallDetails installDetails = null)
        {
            this.environment = environment;
            this.processManager = processManager;
            this.systemSettings = systemSettings;
            this.sharpZipLibHelper = ZipHelper.Instance;
            this.cancellationToken = token;
            this.installDetails = installDetails ?? new GitInstallDetails(environment.UserCachePath, environment.IsWindows);
        }

        public GitInstallationState SetupGitIfNeeded()
        {
            var state = new GitInstallationState();
            state = VerifyGitFromSettings(state);
            if (state.GitIsValid && state.GitLfsIsValid)
            {
                Logger.Trace("Using git install path from settings: {0}", state.GitExecutablePath);
                state.GitLastCheckTime = DateTimeOffset.Now;
                systemSettings?.Set(Constants.GitInstallationState, state);
                return state;
            }

            if (environment.IsWindows)
                state = FindWindowsGit(state);
            else
                state = FindMacGit(state);

            state = CheckForUpdates(state);

            if (state.GitIsValid && state.GitLfsIsValid)
            {
                state.GitLastCheckTime = DateTimeOffset.Now;
                systemSettings?.Set(Constants.GitInstallationState, state);
                return state;
            }

            state = VerifyZipFiles(state);
            state = GetZipsIfNeeded(state);
            state = GrabZipFromResourcesIfNeeded(state);
            state = ExtractGit(state);
            state.GitLastCheckTime = DateTimeOffset.Now;
            systemSettings?.Set(Constants.GitInstallationState, state);
            return state;
        }

        public GitInstallationState VerifyGitFromSettings(GitInstallationState state)
        {
            if (systemSettings == null)
                return state;

            NPath gitExecutablePath = systemSettings.Get(Constants.GitInstallPathKey).ToNPath();
            if (!gitExecutablePath.IsInitialized || !gitExecutablePath.FileExists())
                return state;

            state.GitExecutablePath = gitExecutablePath;
            state = ValidateGitVersion(state);
            if (state.GitIsValid)
                state.GitInstallationPath = state.GitExecutablePath.Parent.Parent;
            state.GitLfsExecutablePath = ProcessManager.FindExecutableInPath(installDetails.GitLfsExecutable, true, state.GitInstallationPath);
            state = ValidateGitLfsVersion(state);
            if (state.GitLfsIsValid)
                state.GitLfsInstallationPath = state.GitInstallationPath;
            return state;
        }

        private GitInstallationState FindMacGit(GitInstallationState state)
        {
            if (!state.GitIsValid)
            {
                var gitPath = new FindExecTask("git", cancellationToken).Configure(processManager, dontSetupGit: true)
                    .Catch(e => true)
                    .RunWithReturn(true);
                state.GitExecutablePath = gitPath;
                state = ValidateGitVersion(state);
                if (state.GitIsValid)
                    state.GitInstallationPath = gitPath.Parent.Parent;
            }

            if (!state.GitLfsIsValid)
            {
                var gitLfsPath = new FindExecTask("git-lfs", cancellationToken)
                    .Configure(processManager, dontSetupGit: true)
                    .Catch(e => true)
                    .RunWithReturn(true);
                state.GitLfsExecutablePath = gitLfsPath;
                state = ValidateGitLfsVersion(state);
                if (state.GitLfsIsValid)
                    state.GitLfsInstallationPath = gitLfsPath.Parent.Parent;
                else
                {
                    state.GitLfsInstallationPath = installDetails.GitInstallationPath;
                    state.GitLfsExecutablePath = installDetails.GitLfsExecutablePath;
                }
            }
            return state;
        }

        private GitInstallationState FindWindowsGit(GitInstallationState state)
        {
            if (!state.GitIsValid)
            {
                state.GitInstallationPath = installDetails.GitInstallationPath;
                state.GitExecutablePath = installDetails.GitExecutablePath;
                state = ValidateGitVersion(state);
            }

            if (!state.GitLfsIsValid)
            {
                state.GitLfsInstallationPath = installDetails.GitInstallationPath;
                state.GitLfsExecutablePath = installDetails.GitLfsExecutablePath;
                state = ValidateGitLfsVersion(state);
            }
            return state;
        }

        public GitInstallationState ValidateGitVersion(GitInstallationState state)
        {
            if (!state.GitExecutablePath.IsInitialized || !state.GitExecutablePath.FileExists())
            {
                state.GitIsValid = false;
                return state;
            }
            var version = new GitVersionTask(cancellationToken)
                .Configure(processManager, state.GitExecutablePath, dontSetupGit: true)
                .Catch(e => true)
                .RunWithReturn(true);
            state.GitIsValid = version >= Constants.MinimumGitVersion;
            state.GitVersion = version;
            return state;
        }

        public GitInstallationState ValidateGitLfsVersion(GitInstallationState state)
        {
            if (!state.GitLfsExecutablePath.IsInitialized || !state.GitLfsExecutablePath.FileExists())
            {
                state.GitLfsIsValid = false;
                return state;
            }
            var version =
                new ProcessTask<TheVersion>(cancellationToken, "version", new LfsVersionOutputProcessor())
                    .Configure(processManager, state.GitLfsExecutablePath, dontSetupGit: true)
                    .Catch(e => true)
                    .RunWithReturn(true);
            state.GitLfsIsValid = version >= Constants.MinimumGitLfsVersion;
            state.GitLfsVersion = version;
            return state;
        }

        private GitInstallationState CheckForUpdates(GitInstallationState state)
        {
            state.GitPackage = Package.Load(environment, installDetails.GitPackageFeed);
            if (state.GitPackage != null)
            {
                state.GitIsValid = state.GitVersion >= state.GitPackage.Version;
                if (state.GitIsValid)
                {
                    state.IsCustomGitPath = state.GitExecutablePath != installDetails.GitExecutablePath;
                }
                else
                {
                    Logger.Trace($"{installDetails.GitExecutablePath} is out of date");
                }
            }

            state.GitLfsPackage = Package.Load(environment, installDetails.GitLfsPackageFeed);
            if (state.GitLfsPackage != null)
            {
                state.GitLfsIsValid = state.GitLfsVersion >= state.GitLfsPackage.Version;
                if (!state.GitLfsIsValid)
                {
                    Logger.Trace($"{installDetails.GitLfsExecutablePath} is out of date");
                }
            }
            return state;
        }

        private GitInstallationState VerifyZipFiles(GitInstallationState state)
        {
            if (!state.GitIsValid && state.GitPackage != null)
            {
                state.GitZipExists = installDetails.GitZipPath.FileExists();
                if (!Utils.VerifyFileIntegrity(installDetails.GitZipPath, state.GitPackage.Md5))
                {
                    installDetails.GitZipPath.DeleteIfExists();
                }
                state.GitZipExists = installDetails.GitZipPath.FileExists();
            }

            if (!state.GitLfsIsValid && state.GitLfsPackage != null)
            {
                state.GitLfsZipExists = installDetails.GitLfsZipPath.FileExists();
                if (!Utils.VerifyFileIntegrity(installDetails.GitLfsZipPath, state.GitLfsPackage.Md5))
                {
                    installDetails.GitLfsZipPath.DeleteIfExists();
                }
                state.GitLfsZipExists = installDetails.GitLfsZipPath.FileExists();
            }
            return state;
        }

        private GitInstallationState GetZipsIfNeeded(GitInstallationState state)
        {
            if (state.GitZipExists && state.GitLfsZipExists)
                return state;

            var downloader = new Downloader();
            downloader.Catch(e => true);
            downloader.Progress(p => ((Progress)Progress)?.UpdateProgress(20 + (long)(20 * p.Percentage), 100, downloader.Name));
            if (!state.GitZipExists && !state.GitIsValid && state.GitPackage != null)
                downloader.QueueDownload(state.GitPackage.Uri, installDetails.ZipPath);
            if (!state.GitLfsZipExists && !state.GitLfsIsValid && state.GitLfsPackage != null)
                downloader.QueueDownload(state.GitLfsPackage.Uri, installDetails.ZipPath);
            downloader.RunWithReturn(true);

            state.GitZipExists = installDetails.GitZipPath.FileExists();
            state.GitLfsZipExists = installDetails.GitLfsZipPath.FileExists();
            ((Progress)Progress)?.UpdateProgress(30, 100);

            return state;
        }

        private GitInstallationState GrabZipFromResourcesIfNeeded(GitInstallationState state)
        {
            if (!state.GitZipExists && !state.GitIsValid)
                AssemblyResources.ToFile(ResourceType.Platform, "git.zip", installDetails.ZipPath, environment);
            state.GitZipExists = installDetails.GitZipPath.FileExists();

            if (!state.GitLfsZipExists && !state.GitLfsIsValid)
                AssemblyResources.ToFile(ResourceType.Platform, "git-lfs.zip", installDetails.ZipPath, environment);
            state.GitLfsZipExists = installDetails.GitLfsZipPath.FileExists();
            return state;
        }

        private GitInstallationState ExtractGit(GitInstallationState state)
        {
            var tempZipExtractPath = NPath.CreateTempDirectory("git_zip_extract_zip_paths");

            if (state.GitZipExists && !state.GitIsValid)
            {
                var gitExtractPath = tempZipExtractPath.Combine("git").CreateDirectory();
                var unzipTask = new UnzipTask(cancellationToken, installDetails.GitZipPath,
                        gitExtractPath, sharpZipLibHelper,
                        environment.FileSystem)
                    .Catch(e => true);
                unzipTask.Progress(p => ((Progress)Progress)?.UpdateProgress(40 + (long)(20 * p.Percentage), 100, unzipTask.Name));
                var path = unzipTask.RunWithReturn(true);
                var target = state.GitInstallationPath;
                if (unzipTask.Successful)
                {
                    var source = path;
                    target.DeleteIfExists();
                    target.EnsureParentDirectoryExists();
                    source.Move(target);
                    state.GitIsValid = true;
                    state.IsCustomGitPath = state.GitExecutablePath != installDetails.GitExecutablePath;
                }
            }

            if (state.GitLfsZipExists && !state.GitLfsIsValid)
            {
                var gitLfsExtractPath = tempZipExtractPath.Combine("git-lfs").CreateDirectory();
                var unzipTask = new UnzipTask(cancellationToken, installDetails.GitLfsZipPath,
                        gitLfsExtractPath, sharpZipLibHelper,
                        environment.FileSystem)
                    .Catch(e => true);
                unzipTask.Progress(p => ((Progress)Progress)?.UpdateProgress(60 + (long)(20 * p.Percentage), 100, unzipTask.Name));
                var path = unzipTask.RunWithReturn(true);
                var target = state.GitLfsExecutablePath;
                if (unzipTask.Successful)
                {
                    var source = path.Combine(installDetails.GitLfsExecutable);
                    target.DeleteIfExists();
                    target.EnsureParentDirectoryExists();
                    source.Move(target);
                    state.GitLfsIsValid = true;
                }
            }

            tempZipExtractPath.DeleteIfExists();
            return state;
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
            public Package GitPackage { get; set; }
            public Package GitLfsPackage { get; set; }
            public DateTimeOffset GitLastCheckTime { get; set; }
            public bool IsCustomGitPath { get; set; }
            public TheVersion GitVersion { get; set; }
            public TheVersion GitLfsVersion { get; set; }
        }

        public class GitInstallDetails
        {
            public const string GitPackageName = "git.json";
            public const string GitLfsPackageName = "git-lfs.json";
#if DEBUG
            private const string packageFeed = "http://localhost:50000/unity/git/";
#else
            private const string packageFeed = "https://ghfvs-installer.github.com/unity/git/";
#endif

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
                    GitPackageFeed = packageFeed + $"windows/{GitPackageName}";
                    GitLfsPackageFeed = packageFeed + $"windows/{GitLfsPackageName}";
                }
                else
                {
                    GitExecutable = "git";
                    GitLfsExecutable = "git-lfs";
                    GitExecutablePath = gitInstallPath.Combine("bin", GitExecutable);
                    GitPackageFeed = packageFeed + $"mac/{GitPackageName}";
                    GitLfsPackageFeed = packageFeed + $"mac/{GitLfsPackageName}";
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
            public UriString GitPackageFeed { get; set; }
            public UriString GitLfsPackageFeed { get; set; }
            public string PackageNameWithVersion => PackageName + "_" + PackageVersion;
        }
    }
}
