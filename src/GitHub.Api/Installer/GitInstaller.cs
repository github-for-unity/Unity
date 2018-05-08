using System;
using System.Threading;
using GitHub.Logging;
using System.Threading.Tasks;

namespace GitHub.Unity
{
    class GitInstaller
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
            ITaskManager taskManager,
            ISettings systemSettings = null,
            GitInstallDetails installDetails = null)
        {
            this.environment = environment;
            this.processManager = processManager;
            this.systemSettings = systemSettings;
            this.sharpZipLibHelper = ZipHelper.Instance;
            this.cancellationToken = taskManager.Token;
            this.installDetails = installDetails ?? new GitInstallDetails(environment.UserCachePath, environment.IsWindows);
        }

        public GitInstallationState SetupGitIfNeeded()
        {
            //Logger.Trace("SetupGitIfNeeded");
            var state = new GitInstallationState();

            if (systemSettings != null)
            {
                NPath? gitExecutablePath = systemSettings.Get(Constants.GitInstallPathKey)?.ToNPath();
                if (gitExecutablePath.HasValue && gitExecutablePath.Value.FileExists()) // we have a git path
                {
                    Logger.Trace("Using git install path from settings: {0}", gitExecutablePath);
                    state.GitExecutablePath = gitExecutablePath.Value;
                    state.GitIsValid = true;

                    var findTask = new FindExecTask("git-lfs", cancellationToken).Configure(processManager, dontSetupGit: true).Catch(e => true);
                    var gitLfsPath = findTask.RunWithReturn(true);
                    state.GitLfsIsValid = findTask.Successful;
                    if (state.GitLfsIsValid)
                    {
                        // we should doublecheck that system git is usable here
                        state.GitLfsExecutablePath = gitLfsPath;
                        state.GitLfsInstallationPath = gitLfsPath.Resolve().Parent.Parent;
                    }

                    if (state.GitIsValid && state.GitLfsIsValid)
                        return state;
                }
            }

            if (!environment.IsWindows)
            {
                return VerifyMacGit(state);
            }

            state = VerifyPortableGitInstallation(state);
            if (state.GitIsValid && state.GitLfsIsValid)
                return state;

            state = VerifyZipFiles(state);
            state = GrabZipFromResourcesIfNeeded(state);
            state = GetZipsIfNeeded(state);
            state = ExtractPortableGit(state);
            return state;
        }

        private GitInstallationState VerifyMacGit(GitInstallationState state)
        {
            var findTask = new FindExecTask("git", cancellationToken).Configure(processManager, dontSetupGit: true).Catch(e => true);
            var gitPath = findTask.RunWithReturn(true);
            state.GitIsValid = findTask.Successful;
            if (state.GitIsValid)
            {
                state.GitExecutablePath = gitPath;
                state.GitInstallationPath = gitPath.Resolve().Parent.Parent;
            }

            findTask = new FindExecTask("git-lfs", cancellationToken).Configure(processManager, dontSetupGit: true).Catch(e => true);
            var gitLfsPath = findTask.RunWithReturn(true);
            state.GitLfsIsValid = findTask.Successful;
            if (state.GitLfsIsValid)
            {
                // we should doublecheck that system git is usable here
                state.GitLfsExecutablePath = gitLfsPath;
                state.GitLfsInstallationPath = gitLfsPath.Resolve().Parent.Parent;
            }
            return state;
        }

        private GitInstallationState VerifyPortableGitInstallation(GitInstallationState state)
        {
            state.GitPackage = Package.Load(environment, installDetails.GitPackageFeed);
            state.GitIsValid = VerifyExecutableMd5(installDetails.GitExecutablePath, state.GitPackage);
            if (state.GitIsValid)
            {
                state.GitInstallationPath = installDetails.GitInstallationPath;
                state.GitExecutablePath = installDetails.GitExecutablePath;
            }
            else
            {
                Logger.Trace($"{installDetails.GitExecutablePath} is out of date");
            }

            state.GitLfsPackage = Package.Load(environment, installDetails.GitLfsPackageFeed);
            state.GitLfsIsValid = VerifyExecutableMd5(installDetails.GitLfsExecutablePath, state.GitLfsPackage);
            if (state.GitLfsIsValid)
            {
                state.GitLfsInstallationPath = installDetails.GitInstallationPath;
                state.GitLfsExecutablePath = installDetails.GitLfsExecutablePath;
            }
            else
            {
                Logger.Trace($"{installDetails.GitLfsExecutablePath} is out of date");
            }
            return state;
        }

        private bool VerifyExecutableMd5(NPath executablePath, Package package)
        {
            if (package == null || !executablePath.FileExists())
                return false;
            var actualmd5 = executablePath.CalculateMD5();
            var expectedmd5 = package.ExecutableMd5;
            return expectedmd5.Equals(actualmd5, StringComparison.InvariantCultureIgnoreCase);
        }

        private GitInstallationState VerifyZipFiles(GitInstallationState state)
        {
            state.GitZipExists = installDetails.GitZipPath.FileExists();
            if (!Utils.VerifyFileIntegrity(installDetails.GitZipPath, state.GitPackage.Md5))
            {
                installDetails.GitZipPath.DeleteIfExists();
            }
            state.GitZipExists = installDetails.GitZipPath.FileExists();
            if (state.GitZipExists)
            {
                state.GitInstallationPath = installDetails.GitInstallationPath;
                state.GitExecutablePath = installDetails.GitExecutablePath;
            }

            state.GitLfsZipExists = installDetails.GitLfsZipPath.FileExists();
            if (!Utils.VerifyFileIntegrity(installDetails.GitLfsZipPath, state.GitLfsPackage.Md5))
            {
                installDetails.GitLfsZipPath.DeleteIfExists();
            }
            state.GitLfsZipExists = installDetails.GitLfsZipPath.FileExists();
            if (state.GitLfsZipExists)
            {
                state.GitLfsInstallationPath = installDetails.GitInstallationPath;
                state.GitLfsExecutablePath = installDetails.GitLfsExecutablePath;
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
            if (!state.GitZipExists && !state.GitIsValid)
                downloader.QueueDownload(state.GitPackage.Uri, installDetails.ZipPath);
            if (!state.GitLfsZipExists && !state.GitLfsIsValid)
                downloader.QueueDownload(state.GitLfsPackage.Uri, installDetails.ZipPath);
            downloader.RunWithReturn(true);

            state.GitZipExists = installDetails.GitZipPath.FileExists();
            state.GitLfsZipExists = installDetails.GitLfsZipPath.FileExists();
            ((Progress)Progress)?.UpdateProgress(30, 100);

            state = GrabZipFromResourcesIfNeeded(state);
            ((Progress)Progress)?.UpdateProgress(40, 100);
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

        private GitInstallationState ExtractPortableGit(GitInstallationState state)
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
                var target = installDetails.GitInstallationPath;
                if (unzipTask.Successful)
                {
                    var source = path;
                    target.DeleteIfExists();
                    target.EnsureParentDirectoryExists();
                    Logger.Trace($"Moving '{source}' to '{target}'");
                    source.Move(target);
                    state.GitInstallationPath = installDetails.GitInstallationPath;
                    state.GitExecutablePath = installDetails.GitExecutablePath;
                    state.GitIsValid = true;
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
                var target = installDetails.GetGitLfsExecutablePath(state.GitInstallationPath);
                if (unzipTask.Successful)
                {
                    var source = path.Combine(installDetails.GitLfsExecutable);
                    target.DeleteIfExists();
                    target.EnsureParentDirectoryExists();
                    Logger.Trace($"Moving '{source}' to '{target}'");
                    source.Move(target);
                    state.GitLfsInstallationPath = state.GitInstallationPath;
                    state.GitLfsExecutablePath = target;
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
