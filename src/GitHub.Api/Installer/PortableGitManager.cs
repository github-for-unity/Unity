using System;
using System.IO;
using System.Threading;
using GitHub.Unity;

namespace GitHub.Api
{
    class PortableGitManager : IPortableGitManager
    {
        private const string WindowsPortableGitZip = @"resources\windows\PortableGit.zip";
        private const string TemporaryFolderSuffix = ".deleteme";
        private const string ExpectedVersion = "f02737a78695063deace08e96d5042710d3e32db";
        private const string PackageName = "PortableGit";

        private readonly ILogging logger;
        private readonly IEnvironment environment;
        private readonly IFileSystem fileSystem;
        private readonly ISharpZipLibHelper sharpZipLibHelper;
        private readonly CancellationToken? cancellationToken;

        public PortableGitManager(IEnvironment environment, IFileSystem fileSystem, ISharpZipLibHelper sharpZipLibHelper,
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

        public void ExtractGitIfNeeded()
        {
            if (IsExtracted())
            {
                logger.Info("Already extracted {0}, returning", WindowsPortableGitZip);
                return;
            }

            var environmentPath = environment.ExtensionInstallPath;
            var tempPath = Path.Combine(environmentPath, fileSystem.GetRandomFileName() + TemporaryFolderSuffix);
            var archiveFilePath = Path.Combine(environmentPath, WindowsPortableGitZip);

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
                sharpZipLibHelper.ExtractZipFile(archiveFilePath, tempPath, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error Extracting Archive:\"{0}\" OutDir:\"{1}\"", archiveFilePath, tempPath);
                throw;
            }
        }

        public bool IsExtracted()
        {
            var target = PackageDestinationDirectory;

            var git = fileSystem.Combine(target, "cmd", "git.exe");
            if (!fileSystem.FileExists(git))
            {
                return false;
            }

            var versionFile = fileSystem.Combine(target, "VERSION");
            if (!fileSystem.FileExists(versionFile))
            {
                return false;
            }

            var expectedVersion = ExpectedVersion;
            if (fileSystem.ReadAllText(versionFile).Trim() != expectedVersion)
            {
                logger.Warning("Package '{0}' out of date, wanted {1}", target, expectedVersion);

                try
                {
                    var parentDirectory = fileSystem.GetParentDirectory(versionFile);
                    fileSystem.DeleteAllFiles(parentDirectory);
                }
                catch (Exception ex)
                {
                    logger.Warning(ex, "Failed to remove {0}", target);
                }

                return false;
            }

            return true;
        }

        public string PackageDestinationDirectory
            => fileSystem.Combine(environment.ExtensionInstallPath, PackageNameWithVersion);

        public string PackageNameWithVersion => PackageName + "_" + ExpectedVersion;
    }
}
