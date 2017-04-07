using System;
using System.Threading;
using System.Threading.Tasks;

namespace GitHub.Unity
{
    class GitInstaller : IGitInstaller
    {
        public const string GitLfsExecutableMD5 = "CD59072535B9DFF416676C8A84571B0C";

        private const string PortableGitExpectedVersion = "f02737a78695063deace08e96d5042710d3e32db";
        private const string PackageName = "PortableGit";
        private const string TempPathPrefix = "github-unity-portable";
        private const string GitZipFile = "git.zip";
        private const string GitLfsZipFile = "git-lfs.zip";
        private NPath gitConfigDestinationPath;

        private readonly CancellationToken cancellationToken;
        private readonly IEnvironment environment;
        private readonly ILogging logger;
        private readonly IFileSystem fileSystem;

        private delegate void ExtractZipFile(string archive, string outFolder, CancellationToken cancellationToken,
            IProgress<float> zipFileProgress = null, IProgress<long> estimatedDurationProgress = null);
        private ExtractZipFile extractCallback;

        public GitInstaller(IEnvironment environment, IZipHelper sharpZipLibHelper, IFileSystem fileSystem)
            : this(environment, sharpZipLibHelper, fileSystem, CancellationToken.None)
        {
        }

        public GitInstaller(IEnvironment environment, IFileSystem fileSystem, CancellationToken cancellationToken)
            : this(environment, null, fileSystem, CancellationToken.None)
        {
        }

        public GitInstaller(IEnvironment environment, IZipHelper sharpZipLibHelper, IFileSystem fileSystem, CancellationToken cancellationToken)
        {
            Guard.ArgumentNotNull(environment, nameof(environment));

            logger = Logging.GetLogger(GetType());
            this.cancellationToken = cancellationToken;

            this.environment = environment;
            this.fileSystem = fileSystem;
            this.extractCallback = sharpZipLibHelper != null
                 ? (ExtractZipFile)sharpZipLibHelper.Extract
                 : ZipHelper.ExtractZipFile;


            PackageDestinationDirectory = environment.GetSpecialFolder(Environment.SpecialFolder.LocalApplicationData)
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

            GitDestinationPath = PackageDestinationDirectory;
            if (DefaultEnvironment.OnWindows)
                GitDestinationPath = GitDestinationPath.Combine("cmd");
            else
                GitDestinationPath = GitDestinationPath.Combine("bin");
            GitDestinationPath = GitDestinationPath.Combine(GitExecutable);

            GitLfsDestinationPath = PackageDestinationDirectory;
            gitConfigDestinationPath = PackageDestinationDirectory;
            if (DefaultEnvironment.OnWindows)
            {
                GitLfsDestinationPath = GitLfsDestinationPath.Combine("mingw32");
                gitConfigDestinationPath = gitConfigDestinationPath.Combine("mingw32");
            }
            GitLfsDestinationPath = GitLfsDestinationPath.Combine("libexec", "git-core", GitLfsExecutable);
            gitConfigDestinationPath = gitConfigDestinationPath.Combine("etc", "gitconfig");

        }

        public bool IsExtracted()
        {
            return IsPortableGitExtracted() && IsGitLfsExtracted();
        }

        private bool IsPortableGitExtracted()
        {
            if (!GitDestinationPath.FileExists())
            {
                logger.Trace("{0} not installed yet", GitDestinationPath);
                return false;
            }

            return true;
        }

        public bool IsGitLfsExtracted()
        {
            if (!GitLfsDestinationPath.FileExists())
            {
                logger.Trace("{0} not installed yet", GitLfsDestinationPath);
                return false;
            }

            if (fileSystem.CalculateMD5(GitLfsDestinationPath) == GitLfsExecutableMD5)
            {
                logger.Trace("{0} has incorrect MD5", GitDestinationPath);
                return false;
            }

            return true;
        }

        public async Task<bool> SetupIfNeeded(IProgress<float> zipFileProgress = null, IProgress<long> estimatedDurationProgress = null)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (InstalledGitIsValid())
            {
                return true;
            }

            NPath tempPath = null;
            try
            {
                tempPath = NPath.CreateTempDirectory(TempPathPrefix);

                cancellationToken.ThrowIfCancellationRequested();

                var ret = await SetupGitIfNeeded(tempPath, zipFileProgress, estimatedDurationProgress);

                cancellationToken.ThrowIfCancellationRequested();

                ret &= await SetupGitLfsIfNeeded(tempPath, zipFileProgress, estimatedDurationProgress);

                var archiveFilePath = AssemblyResources.ToFile(ResourceType.Platform, "gitconfig", tempPath);
                if (archiveFilePath.FileExists())
                {
                    archiveFilePath.Copy(gitConfigDestinationPath);
                }

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

        private bool InstalledGitIsValid()
        {
            return false;
        }

        public Task<bool> SetupGitIfNeeded(NPath tempPath, IProgress<float> zipFileProgress = null,
            IProgress<long> estimatedDurationProgress = null)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (IsPortableGitExtracted())
            {
                logger.Trace("Already extracted {0}, returning", PackageDestinationDirectory);
                return TaskEx.FromResult(true);
            }

            cancellationToken.ThrowIfCancellationRequested();

            var archiveFilePath = AssemblyResources.ToFile(ResourceType.Platform, GitZipFile, tempPath);
            if (!archiveFilePath.FileExists())
            {
                logger.Warning("Archive \"{0}\" missing", archiveFilePath.ToString());

                archiveFilePath = environment.ExtensionInstallPath.ToNPath().Combine(archiveFilePath);
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
                logger.Trace("Copying \"{0}\" to \"{1}\"", unzipPath, PackageDestinationDirectory);

                unzipPath.Copy(PackageDestinationDirectory);
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error CopyingArchive Source:\"{0}\" OutDir:\"{1}\"", tempPath, PackageDestinationDirectory);
                return TaskEx.FromResult(false);
            }
            unzipPath.DeleteIfExists();
            return TaskEx.FromResult(true);
        }

        public Task<bool> SetupGitLfsIfNeeded(NPath tempPath, IProgress<float> zipFileProgress = null,
            IProgress<long> estimatedDurationProgress = null)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (IsGitLfsExtracted())
            {
                logger.Trace("Already extracted {0}, returning", GitLfsDestinationPath);
                return TaskEx.FromResult(false);
            }

            cancellationToken.ThrowIfCancellationRequested();

            var archiveFilePath = AssemblyResources.ToFile(ResourceType.Platform, GitLfsZipFile, tempPath);
            if (!archiveFilePath.FileExists())
            {
                logger.Warning("Archive \"{0}\" missing", archiveFilePath.ToString());

                archiveFilePath = environment.ExtensionInstallPath.ToNPath().Combine(archiveFilePath);
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
                logger.Trace("Copying \"{0}\" to \"{1}\"", unzippedGitLfsExecutablePath, GitLfsDestinationPath);

                unzippedGitLfsExecutablePath.Copy(GitLfsDestinationPath);
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error Copying git-lfs Source:\"{0}\" Destination:\"{1}\"", unzipPath, GitLfsDestinationPath);
                return TaskEx.FromResult(false);
            }
            unzipPath.DeleteIfExists();
            return TaskEx.FromResult(true);
        }

        private NPath GetTemporaryPath()
        {
            return NPath.CreateTempDirectory(TempPathPrefix);
        }

        public NPath PackageDestinationDirectory { get; private set; }

        public NPath GitLfsDestinationPath { get; private set; }

        public NPath GitDestinationPath { get; private set; }
        public string PackageNameWithVersion => PackageName + "_" + PortableGitExpectedVersion;

        private string GitLfsExecutable { get; set; }
        private string GitExecutable { get; set; }
    }
}
