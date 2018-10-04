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
        private readonly GitInstallDetails installDetails;
        private readonly IZipHelper sharpZipLibHelper;

        public IProgress Progress { get; } = new Progress(TaskBase.Default);

        public GitInstaller(IEnvironment environment, IProcessManager processManager,
            CancellationToken token,
            GitInstallDetails installDetails = null)
        {
            this.environment = environment;
            this.processManager = processManager;
            this.sharpZipLibHelper = ZipHelper.Instance;
            this.cancellationToken = token;
            this.installDetails = installDetails ?? new GitInstallDetails(environment.UserCachePath, environment.IsWindows);
        }

        public GitInstallationState SetupGitIfNeeded(GitInstallationState state = null)
        {
            var skipSystemProbing = state != null;

            state = VerifyGitSettings(state);
            if (state.GitIsValid && state.GitLfsIsValid)
            {
                Logger.Trace("Using git install path from settings: {0}", state.GitExecutablePath);
                state.GitLastCheckTime = DateTimeOffset.Now;
                return state;
            }

            if (!skipSystemProbing)
            {
                if (environment.IsMac)
                    state = FindGit(state);
            }

            state = SetDefaultPaths(state);
            state = CheckForGitUpdates(state);

            if (state.GitIsValid && state.GitLfsIsValid)
            {
                state.GitLastCheckTime = DateTimeOffset.Now;
                return state;
            }

            state = VerifyZipFiles(state);
            // on developer builds, prefer local zips over downloading
#if DEVELOPER_BUILD
            state = GrabZipFromResourcesIfNeeded(state);
            state = GetZipsIfNeeded(state);
#else
            state = GetZipsIfNeeded(state);
            state = GrabZipFromResourcesIfNeeded(state);
#endif
            state = ExtractGit(state);

            // if installing from zip failed (internet down maybe?), try to find a usable system git
            if (!state.GitIsValid && state.GitInstallationPath == installDetails.GitInstallationPath)
                state = FindGit(state);
            if (!state.GitLfsIsValid && state.GitLfsInstallationPath == installDetails.GitLfsInstallationPath)
                state = FindGitLfs(state);
            state.GitLastCheckTime = DateTimeOffset.Now;
            return state;
        }

        public GitInstallationState VerifyGitSettings(GitInstallationState state = null)
        {
            state = state ?? environment.GitInstallationState;
            if (!state.GitExecutablePath.IsInitialized && !state.GitLfsExecutablePath.IsInitialized)
                return state;

            state = ValidateGitVersion(state);
            if (state.GitIsValid)
                state.GitInstallationPath = state.GitExecutablePath.Parent.Parent;

            if (!state.GitLfsExecutablePath.IsInitialized)
            {
                // look for it in the directory where we would install it from the bundle
                state.GitLfsExecutablePath = installDetails.GitLfsExecutablePath;
            }

            state = ValidateGitLfsVersion(state);

            if (state.GitLfsIsValid)
                state.GitLfsInstallationPath = state.GitLfsExecutablePath.Parent;

            return state;
        }

        public GitInstallationState FindSystemGit(GitInstallationState state)
        {
            state = FindGit(state);
            state = FindGitLfs(state);
            return state;
        }

        private GitInstallationState FindGit(GitInstallationState state)
        {
            if (!state.GitIsValid)
            {
                var gitPath = new FindExecTask("git", cancellationToken)
                    .Configure(processManager, dontSetupGit: true)
                    .Catch(e => true)
                    .RunSynchronously();
                state.GitExecutablePath = gitPath;
                state = ValidateGitVersion(state);
                if (state.GitIsValid)
                    state.GitInstallationPath = gitPath.Parent.Parent;
            }
            return state;
        }

        private GitInstallationState FindGitLfs(GitInstallationState state)
        {
            if (!state.GitLfsIsValid)
            {
                var gitLfsPath = new FindExecTask("git-lfs", cancellationToken)
                    .Configure(processManager, dontSetupGit: true)
                    .Catch(e => true)
                    .RunSynchronously();
                state.GitLfsExecutablePath = gitLfsPath;
                state = ValidateGitLfsVersion(state);
                if (state.GitLfsIsValid)
                    state.GitLfsInstallationPath = state.GitLfsExecutablePath.Parent;
            }
            return state;
        }

        public GitInstallationState SetDefaultPaths(GitInstallationState state)
        {
            if (!state.GitIsValid && environment.IsWindows)
            {
                state.GitInstallationPath = installDetails.GitInstallationPath;
                state.GitExecutablePath = installDetails.GitExecutablePath;
                state = ValidateGitVersion(state);
            }

            if (!state.GitLfsIsValid)
            {
                state.GitLfsExecutablePath = installDetails.GitLfsExecutablePath;
                state.GitLfsInstallationPath = state.GitLfsExecutablePath.Parent;
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
                .RunSynchronously();
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
            var version = new ProcessTask<TheVersion>(cancellationToken, "version", new LfsVersionOutputProcessor())
                    .Configure(processManager, state.GitLfsExecutablePath, dontSetupGit: true)
                    .Catch(e => true)
                    .RunSynchronously();
            state.GitLfsIsValid = version >= Constants.MinimumGitLfsVersion;
            state.GitLfsVersion = version;
            return state;
        }

        private GitInstallationState CheckForGitUpdates(GitInstallationState state)
        {
            if (state.GitInstallationPath == installDetails.GitInstallationPath)
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
            }

            if (state.GitLfsInstallationPath == installDetails.GitLfsInstallationPath)
            {
                state.GitLfsPackage = Package.Load(environment, installDetails.GitLfsPackageFeed);
                if (state.GitLfsPackage != null)
                {
                    state.GitLfsIsValid = state.GitLfsVersion >= state.GitLfsPackage.Version;
                    if (!state.GitLfsIsValid)
                    {
                        Logger.Trace($"{installDetails.GitLfsExecutablePath} is out of date");
                    }
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

            var downloader = new Downloader(environment.FileSystem);
            downloader.Catch(e =>
                {
                    LogHelper.Trace(e, "Failed to download");
                    return true;
                });
            downloader.Progress(p => Progress.UpdateProgress(20 + (long)(20 * p.Percentage), 100, downloader.Message));
            if (!state.GitZipExists && !state.GitIsValid && state.GitPackage != null)
                downloader.QueueDownload(state.GitPackage.Uri, installDetails.ZipPath);
            if (!state.GitLfsZipExists && !state.GitLfsIsValid && state.GitLfsPackage != null)
                downloader.QueueDownload(state.GitLfsPackage.Uri, installDetails.ZipPath);
            downloader.RunSynchronously();

            state.GitZipExists = installDetails.GitZipPath.FileExists();
            state.GitLfsZipExists = installDetails.GitLfsZipPath.FileExists();
            Progress.UpdateProgress(30, 100);

            return state;
        }

        private GitInstallationState GrabZipFromResourcesIfNeeded(GitInstallationState state)
        {
            if (!state.GitZipExists && !state.GitIsValid && state.GitInstallationPath == installDetails.GitInstallationPath)
                AssemblyResources.ToFile(ResourceType.Platform, "git.zip", installDetails.ZipPath, environment);
            state.GitZipExists = installDetails.GitZipPath.FileExists();

            if (state.GitLfsInstallationPath != installDetails.GitLfsInstallationPath)
                return state;

            if (!state.GitLfsZipExists && !state.GitLfsIsValid && state.GitLfsInstallationPath == installDetails.GitLfsInstallationPath)
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
                    .Catch(e =>
                    {
                        LogHelper.Trace(e, "Failed to unzip " + installDetails.GitZipPath);
                        return true;
                    });
                unzipTask.Progress(p => Progress.UpdateProgress(40 + (long)(20 * p.Percentage), 100, unzipTask.Message));
                var source = unzipTask.RunSynchronously();
                var target = state.GitInstallationPath;
                if (unzipTask.Successful)
                {
                    Logger.Info("Moving Git source:{0} target:{1}", source.ToString(), target.ToString());

                    target.DeleteIfExists();
                    target.EnsureParentDirectoryExists();

                    Logger.Info("target Exists: {0}", target.Exists());

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
                    .Catch(e =>
                    {
                        LogHelper.Trace(e, "Failed to unzip " + installDetails.GitLfsZipPath);
                        return true;
                    });
                unzipTask.Progress(p => Progress.UpdateProgress(60 + (long)(20 * p.Percentage), 100, unzipTask.Message));
                var source = unzipTask.RunSynchronously();
                var target = state.GitLfsInstallationPath;
                if (unzipTask.Successful)
                {
                    Logger.Info("Moving Git source:{0} target:{1}", source.ToString(), target.ToString());

                    target.DeleteIfExists();
                    target.EnsureParentDirectoryExists();

                    Logger.Info("target Exists: {0}", target.Exists());

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
            private const string packageFeed = "http://github-vs.s3.amazonaws.com/unity/git/";
#endif

            public const string GitDirectory = "git";
            public const string GitLfsDirectory = "git-lfs";

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

                GitInstallationPath = baseDataPath.Combine(GitDirectory);
                GitExecutablePath = GitInstallationPath.Combine(onWindows ? "cmd" : "bin", "git" + DefaultEnvironment.ExecutableExt);

                GitLfsInstallationPath = baseDataPath.Combine(GitLfsDirectory);
                GitLfsExecutablePath = GitLfsInstallationPath.Combine("git-lfs" + DefaultEnvironment.ExecutableExt);

                if (onWindows)
                {
                    GitPackageFeed = packageFeed + $"windows/{GitPackageName}";
                    GitLfsPackageFeed = packageFeed + $"windows/{GitLfsPackageName}";
                }
                else
                {
                    GitPackageFeed = packageFeed + $"mac/{GitPackageName}";
                    GitLfsPackageFeed = packageFeed + $"mac/{GitLfsPackageName}";
                }
            }

            public NPath ZipPath { get; }
            public NPath GitZipPath { get; }
            public NPath GitLfsZipPath { get; }
            public NPath GitInstallationPath { get; }
            public NPath GitLfsInstallationPath { get; }
            public NPath GitExecutablePath { get; }
            public NPath GitLfsExecutablePath { get; }
            public UriString GitPackageFeed { get; set; }
            public UriString GitLfsPackageFeed { get; set; }
        }
    }
}
