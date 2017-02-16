using System;
using System.IO;
using System.Threading;
using GitHub.Unity;

namespace GitHub.Unity
{
    class PortableGitManager : IPortableGitManager
    {
        private const string WindowsPortableGitZip = @"resources\windows\PortableGit-2.11.1-32-bit.zip";
        private const string WindowsGitLfsZip = @"resources\windows\git-lfs-windows-386-2.0-pre-d9833cd.zip";
        private const string PortableGitExpectedVersion = "f02737a78695063deace08e96d5042710d3e32db";
        private const string PackageName = "PortableGit";

        private readonly CancellationToken? cancellationToken;
        private readonly IEnvironment environment;
        private readonly ILogging logger;
        private readonly IZipHelper sharpZipLibHelper;

        public PortableGitManager(IEnvironment environment, IZipHelper sharpZipLibHelper,
            CancellationToken? cancellationToken = null)
        {
            Guard.ArgumentNotNull(environment, nameof(environment));
            Guard.ArgumentNotNull(sharpZipLibHelper, nameof(sharpZipLibHelper));

            logger = Logging.GetLogger(GetType());

            this.environment = environment;
            this.sharpZipLibHelper = sharpZipLibHelper;
            this.cancellationToken = cancellationToken;

            PackageDestinationDirectory = environment.UserProfilePath.ToNPath().Combine(ApplicationInfo.ApplicationName, PackageNameWithVersion);
            GitLfsDestinationPath = PackageDestinationDirectory.Combine(@"mingw32", "libexec", "git-core", "git-lfs.exe");
        }

        public bool IsExtracted()
        {
            return IsPortableGitExtracted() && IsGitLfsExtracted();
        }

        private bool IsPortableGitExtracted()
        {
            var target = PackageDestinationDirectory;

            var git = target.Combine("cmd", "git.exe");
            if (!git.FileExists())
            {
                logger.Debug("git.exe not found");
                return false;
            }

            return true;
        }

        public bool IsGitLfsExtracted()
        {
            logger.Debug("PackageDestinationDirectory: {0}", PackageDestinationDirectory);

            if (!GitLfsDestinationPath.FileExists())
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

            var archiveFilePath = environment.ExtensionInstallPath.ToNPath().Combine(WindowsPortableGitZip);

            if (!archiveFilePath.FileExists())
            {
                var exception = new FileNotFoundException("Could not find file", archiveFilePath);
                logger.Error(exception, "Trying to extract {0}, but it doesn't exist", archiveFilePath);
                throw exception;
            }

            var tempPath = GetTemporaryPath();

            try
            {
                sharpZipLibHelper.ExtractZipFile(archiveFilePath, tempPath, cancellationToken, zipFileProgress,
                    estimatedDurationProgress);
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error ExtractingArchive Source:\"{0}\" OutDir:\"{1}\"", archiveFilePath, tempPath);
                throw;
            }

            try
            {
                tempPath.Copy(PackageDestinationDirectory);
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error CopyingArchive Source:\"{0}\" OutDir:\"{1}\"", tempPath, PackageDestinationDirectory);
                throw;
            }
            tempPath.DeleteIfExists();
        }

        public void ExtractGitLfsIfNeeded(IProgress<float> zipFileProgress = null,
            IProgress<long> estimatedDurationProgress = null)
        {
            if (IsGitLfsExtracted())
            {
                logger.Debug("Already extracted {0}, returning", WindowsGitLfsZip);
                return;
            }

            var archiveFilePath = environment.ExtensionInstallPath.ToNPath().Combine(WindowsGitLfsZip);

            if (!archiveFilePath.FileExists())
            {
                var exception = new FileNotFoundException("Could not find file", archiveFilePath);
                logger.Error(exception, "Trying to extract {0}, but it doesn't exist", archiveFilePath);
                throw exception;
            }

            var tempPath = GetTemporaryPath();
            var tempGitLfsPath = tempPath.Combine("git-lfs.exe");

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
                tempGitLfsPath.Copy(GitLfsDestinationPath);
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error Copying git-lfs Source:\"{0}\" Destination:\"{1}\"", tempGitLfsPath, GitLfsDestinationPath);
                throw;
            }
            tempPath.DeleteIfExists();
        }

        private NPath GetTemporaryPath()
        {
            return NPath.CreateTempDirectory("github-unity-portable");
        }

        public NPath PackageDestinationDirectory { get; private set; }

        public NPath GitLfsDestinationPath { get; private set; }

        public string PackageNameWithVersion => PackageName + "_" + PortableGitExpectedVersion;
    }
}
