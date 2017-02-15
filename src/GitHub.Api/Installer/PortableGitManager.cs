using System;
using System.IO;
using System.Threading;
using GitHub.Unity;

namespace GitHub.Api
{
    class PortableGitManager : IPortableGitManager
    {
        private const string WindowsPortableGitZip = @"resources\windows\PortableGit.zip";
        private const string WindowsGitLfsZip = @"resources\windows\git-lfs-windows-386-2.0-pre-d9833cd.zip";
        private const string TemporaryFolderSuffix = ".deleteme";
        private const string PortableGitExpectedVersion = "f02737a78695063deace08e96d5042710d3e32db";
        private const string PackageName = "PortableGit";

        private readonly CancellationToken? cancellationToken;
        private readonly IEnvironment environment;
        private readonly IFileSystem fileSystem;
        private readonly ILogging logger;
        private readonly IZipHelper sharpZipLibHelper;

        public PortableGitManager(IEnvironment environment, IFileSystem fileSystem, IZipHelper sharpZipLibHelper,
            CancellationToken? cancellationToken = null)
        {
            Guard.ArgumentNotNull(environment, nameof(environment));
            Guard.ArgumentNotNull(fileSystem, nameof(fileSystem));
            Guard.ArgumentNotNull(sharpZipLibHelper, nameof(sharpZipLibHelper));

            logger = Logging.GetLogger(GetType());

            this.environment = environment;
            this.fileSystem = fileSystem;
            this.sharpZipLibHelper = sharpZipLibHelper;
            this.cancellationToken = cancellationToken;
        }

        public bool IsExtracted()
        {
            return IsPortableGitExtracted() && IsGitLfsExtracted();
        }

        private bool IsPortableGitExtracted()
        {
            var target = PackageDestinationDirectory;

            var git = fileSystem.Combine(target, "cmd", "git.exe");
            if (!fileSystem.FileExists(git))
            {
                logger.Debug("git.exe not found");
                return false;
            }

            return true;
        }

        public bool IsGitLfsExtracted()
        {
            logger.Debug("PackageDestinationDirectory: {0}", PackageDestinationDirectory);

            var gitLfs = fileSystem.Combine(PackageDestinationDirectory, @"mingw32\libexec\git-core\git-lfs.exe");
            if (!fileSystem.FileExists(gitLfs))
            {
                logger.Warning("git-lfs.exe not found");
                return false;
            }

            return true;
        }

        public void ExtractGitIfNeeded(IProgress<float> zipFileProgress = null,
            IProgress<long> estimatedDurationProgress = null)
        {
            if (IsPortableGitExtracted())
            {
                logger.Debug("Already extracted {0}, returning", WindowsPortableGitZip);
                return;
            }

            var tempPath = GetTemporaryPath();

            var archiveFilePath = fileSystem.Combine(environment.ExtensionInstallPath, WindowsPortableGitZip);

            try
            {
                fileSystem.CreateDirectory(tempPath);
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Couldn't create temp dir: " + tempPath);
                throw;
            }

            if (!fileSystem.FileExists(archiveFilePath))
            {
                var exception = new FileNotFoundException("Could not find file", archiveFilePath);
                logger.Error(exception, "Trying to extract {0}, but it doesn't exist", archiveFilePath);
                throw exception;
            }

            try
            {
                sharpZipLibHelper.ExtractZipFile(archiveFilePath, tempPath, cancellationToken, zipFileProgress,
                    estimatedDurationProgress);
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error Extracting Archive:\"{0}\" OutDir:\"{1}\"", archiveFilePath, tempPath);
                throw;
            }

            try
            {
                var nPath = new NPath(tempPath);
                nPath.CopyFiles(new NPath(PackageDestinationDirectory), true);
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error Extracting Archive:\"{0}\" OutDir:\"{1}\"", archiveFilePath, tempPath);
                throw;
            }
        }

        public void ExtractGitLfsIfNeeded(IProgress<float> zipFileProgress = null,
            IProgress<long> estimatedDurationProgress = null)
        {
            if (IsGitLfsExtracted())
            {
                logger.Debug("Already extracted {0}, returning", WindowsGitLfsZip);
                return;
            }

            var tempPath = GetTemporaryPath();

            var archiveFilePath = fileSystem.Combine(environment.ExtensionInstallPath, WindowsGitLfsZip);

            try
            {
                fileSystem.CreateDirectory(tempPath);
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Couldn't create temp dir: " + tempPath);
                throw;
            }

            if (!fileSystem.FileExists(archiveFilePath))
            {
                var exception = new FileNotFoundException("Could not find file", archiveFilePath);
                logger.Error(exception, "Trying to extract {0}, but it doesn't exist", archiveFilePath);
                throw exception;
            }

            try
            {
                sharpZipLibHelper.ExtractZipFile(archiveFilePath, tempPath, cancellationToken, zipFileProgress,
                    estimatedDurationProgress);
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error Extracting Archive:\"{0}\" OutDir:\"{1}\"", archiveFilePath, tempPath);
                throw;
            }
        }

        private string GetTemporaryPath()
        {
            return fileSystem.Combine(fileSystem.GetTempPath(), fileSystem.GetRandomFileName() + TemporaryFolderSuffix);
        }

        public string PackageDestinationDirectory => fileSystem.Combine(environment.UserProfilePath, "GitHubUnity", PackageNameWithVersion);

        public string GitLfsDestinationDirectory
            => fileSystem.Combine(PackageDestinationDirectory, @"mingw32\libexec\git-core\git-lfs.exe");

        public string PackageNameWithVersion => PackageName + "_" + PortableGitExpectedVersion;
    }
}
