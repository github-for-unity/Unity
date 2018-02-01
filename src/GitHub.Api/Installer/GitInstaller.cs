using System;
using System.Threading;
using System.Threading.Tasks;

namespace GitHub.Unity
{
    class GitInstaller : IGitInstaller
    {
        public const string WindowsGitLfsExecutableMD5 = "177bb14d0c08f665a24f0d5516c3b080";
        public const string MacGitLfsExecutableMD5 = "f81a1a065a26a4123193e8fd96c561ad";

        private const string PortableGitExpectedVersion = "f02737a78695063deace08e96d5042710d3e32db";
        private const string PackageName = "PortableGit";
        private const string TempPathPrefix = "github-unity-portable";
        private const string GitZipFile = "git.zip";
        private const string GitLfsZipFile = "git-lfs.zip";

        private readonly CancellationToken cancellationToken;
        private readonly IEnvironment environment;
        private readonly ILogging logger;

        private delegate void ExtractZipFile(string archive, string outFolder, CancellationToken cancellationToken,
            IProgress<float> zipFileProgress = null, IProgress<long> estimatedDurationProgress = null);
        private ExtractZipFile extractCallback;

        public GitInstaller(IEnvironment environment, CancellationToken cancellationToken)
            : this(environment, null, cancellationToken)
        {
        }

        public GitInstaller(IEnvironment environment, IZipHelper sharpZipLibHelper, CancellationToken cancellationToken)
        {
            Guard.ArgumentNotNull(environment, nameof(environment));

            logger = Logging.GetLogger(GetType());
            this.cancellationToken = cancellationToken;

            this.environment = environment;
            this.extractCallback = sharpZipLibHelper != null
                 ? (ExtractZipFile)sharpZipLibHelper.Extract
                 : ZipHelper.ExtractZipFile;


            GitInstallationPath = environment.GetSpecialFolder(Environment.SpecialFolder.LocalApplicationData)
                .ToNPath().Combine(ApplicationInfo.ApplicationName, PackageNameWithVersion);
            var gitExecutable = "git";
            var gitLfsExecutable = "git-lfs";
            if (DefaultEnvironment.OnWindows)
            {
                gitExecutable += ".exe";
                gitLfsExecutable += ".exe";
            }
            GitLfsExecutable = gitLfsExecutable;
            GitExecutable = gitExecutable;

            GitExecutablePath = GitInstallationPath;
            if (DefaultEnvironment.OnWindows)
                GitExecutablePath = GitExecutablePath.Combine("cmd");
            else
                GitExecutablePath = GitExecutablePath.Combine("bin");
            GitExecutablePath = GitExecutablePath.Combine(GitExecutable);

            GitLfsExecutablePath = GitInstallationPath;

            if (DefaultEnvironment.OnWindows)
            {
                GitLfsExecutablePath = GitLfsExecutablePath.Combine("mingw32");
            }

            GitLfsExecutablePath = GitLfsExecutablePath.Combine("libexec", "git-core", GitLfsExecutable);
        }

        public bool IsExtracted()
        {
            return IsPortableGitExtracted() && IsGitLfsExtracted();
        }

        private bool IsPortableGitExtracted()
        {
            if (!GitExecutablePath.FileExists())
            {
                logger.Trace("{0} not installed yet", GitExecutablePath);
                return false;
            }

            logger.Trace("Git Present");

            return true;
        }

        public bool IsGitLfsExtracted()
        {
            if (!GitLfsExecutablePath.FileExists())
            {
                logger.Trace("{0} not installed yet", GitLfsExecutablePath);
                return false;
            }

            var calculateMd5 = environment.FileSystem.CalculateMD5(GitLfsExecutablePath);
            logger.Trace("GitLFS MD5: {0}", calculateMd5);
            var md5 = environment.IsWindows ? WindowsGitLfsExecutableMD5 : MacGitLfsExecutableMD5;
            if (md5.Equals(calculateMd5, StringComparison.OrdinalIgnoreCase))
            {
                logger.Trace("{0} has incorrect MD5", GitExecutablePath);
                return false;
            }

            logger.Trace("GitLFS Present");

            return true;
        }

        public async Task<bool> SetupIfNeeded(IProgress<float> zipFileProgress = null, IProgress<long> estimatedDurationProgress = null)
        {
            logger.Trace("SetupIfNeeded");

            cancellationToken.ThrowIfCancellationRequested();

            NPath tempPath = null;
            try
            {
                tempPath = NPath.CreateTempDirectory(TempPathPrefix);

                cancellationToken.ThrowIfCancellationRequested();

                var ret = await SetupGitIfNeeded(tempPath, zipFileProgress, estimatedDurationProgress);

                cancellationToken.ThrowIfCancellationRequested();

                ret &= await SetupGitLfsIfNeeded(tempPath, zipFileProgress, estimatedDurationProgress);

                tempPath.Delete();
                return ret;
            }
            catch (Exception ex)
            {
                logger.Trace(ex);
                return false;
            }
            finally
            {
                try
                {
                    if (tempPath != null)
                        tempPath.DeleteIfExists();
                }
                catch {}
            }
        }

        public Task<bool> SetupGitIfNeeded(NPath tempPath, IProgress<float> zipFileProgress = null,
            IProgress<long> estimatedDurationProgress = null)
        {
            logger.Trace("SetupGitIfNeeded");

            cancellationToken.ThrowIfCancellationRequested();

            if (IsPortableGitExtracted())
            {
                logger.Trace("Already extracted {0}, returning", GitInstallationPath);
                return TaskEx.FromResult(true);
            }

            cancellationToken.ThrowIfCancellationRequested();

            var archiveFilePath = AssemblyResources.ToFile(ResourceType.Platform, GitZipFile, tempPath, environment);
            if (!archiveFilePath.FileExists())
            {
                logger.Warning("Archive \"{0}\" missing", archiveFilePath.ToString());

                archiveFilePath = environment.ExtensionInstallPath.Combine(archiveFilePath);
                if (!archiveFilePath.FileExists())
                {
                    logger.Warning("Archive \"{0}\" missing, returning", archiveFilePath.ToString());
                    return TaskEx.FromResult(false);
                }
            }

            cancellationToken.ThrowIfCancellationRequested();

            var unzipPath = tempPath.Combine("git");

            try
            {
                logger.Trace("Extracting \"{0}\" to \"{1}\"", archiveFilePath, unzipPath);

                extractCallback(archiveFilePath, unzipPath, cancellationToken, zipFileProgress,
                    estimatedDurationProgress);
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error ExtractingArchive Source:\"{0}\" OutDir:\"{1}\"", archiveFilePath, tempPath);
                return TaskEx.FromResult(false);
            }

            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                GitInstallationPath.DeleteIfExists();
                GitInstallationPath.EnsureParentDirectoryExists();

                logger.Trace("Moving \"{0}\" to \"{1}\"", unzipPath, GitInstallationPath);

                unzipPath.Move(GitInstallationPath);
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error Moving \"{0}\" to \"{1}\"", tempPath, GitInstallationPath);
                return TaskEx.FromResult(false);
            }
            unzipPath.DeleteIfExists();
            return TaskEx.FromResult(true);
        }

        public Task<bool> SetupGitLfsIfNeeded(NPath tempPath, IProgress<float> zipFileProgress = null,
            IProgress<long> estimatedDurationProgress = null)
        {
            logger.Trace("SetupGitLfsIfNeeded");

            cancellationToken.ThrowIfCancellationRequested();

            if (IsGitLfsExtracted())
            {
                logger.Trace("Already extracted {0}, returning", GitLfsExecutablePath);
                return TaskEx.FromResult(false);
            }

            cancellationToken.ThrowIfCancellationRequested();

            var archiveFilePath = AssemblyResources.ToFile(ResourceType.Platform, GitLfsZipFile, tempPath, environment);
            if (!archiveFilePath.FileExists())
            {
                logger.Warning("Archive \"{0}\" missing", archiveFilePath.ToString());

                archiveFilePath = environment.ExtensionInstallPath.Combine(archiveFilePath);
                if (!archiveFilePath.FileExists())
                {
                    logger.Warning("Archive \"{0}\" missing, returning", archiveFilePath.ToString());
                    return TaskEx.FromResult(false);
                }
            }

            cancellationToken.ThrowIfCancellationRequested();

            var unzipPath = tempPath.Combine("git-lfs");

            try
            {
                logger.Trace("Extracting \"{0}\" to \"{1}\"", archiveFilePath, unzipPath);

                extractCallback(archiveFilePath, unzipPath, cancellationToken, zipFileProgress,
                    estimatedDurationProgress);
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error Extracting Archive:\"{0}\" OutDir:\"{1}\"", archiveFilePath, tempPath);
                return TaskEx.FromResult(false);
            }

            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var unzippedGitLfsExecutablePath = unzipPath.Combine(GitLfsExecutable);
                logger.Trace("Copying \"{0}\" to \"{1}\"", unzippedGitLfsExecutablePath, GitLfsExecutablePath);

                unzippedGitLfsExecutablePath.Copy(GitLfsExecutablePath);
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error Copying git-lfs Source:\"{0}\" Destination:\"{1}\"", unzipPath, GitLfsExecutablePath);
                return TaskEx.FromResult(false);
            }
            unzipPath.DeleteIfExists();
            return TaskEx.FromResult(true);
        }

        private NPath GetTemporaryPath()
        {
            return NPath.CreateTempDirectory(TempPathPrefix);
        }

        public NPath GitInstallationPath { get; private set; }

        public NPath GitLfsExecutablePath { get; private set; }

        public NPath GitExecutablePath { get; private set; }
        public string PackageNameWithVersion => PackageName + "_" + PortableGitExpectedVersion;

        private string GitLfsExecutable { get; set; }
        private string GitExecutable { get; set; }
    }
}
