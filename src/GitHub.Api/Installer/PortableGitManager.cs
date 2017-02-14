using System;
using System.IO;
using GitHub.Unity;

namespace GitHub.Api
{
    class PortableGitManager : IPortableGitManager
    {
        private const string WindowsPortableGitZip = @"resources\windows\PortableGit.zip";
        protected const string TemporaryFolderSuffix = ".deleteme";

        private const string ExpectedVersion = "f02737a78695063deace08e96d5042710d3e32db";

        private const string PackageName = "PortableGit";

        public PortableGitManager(IEnvironment environment, IFileSystem fileSystem, ISharpZipLibHelper sharpZipLibHelper)
        {
            Guard.ArgumentNotNull(environment, nameof(environment));
            Guard.ArgumentNotNull(fileSystem, nameof(fileSystem));
            Guard.ArgumentNotNull(sharpZipLibHelper, nameof(sharpZipLibHelper));

            Logger = Logging.GetLogger(GetType());

            Environment = environment;
            FileSystem = fileSystem;
            SharpZipLibHelper = sharpZipLibHelper;
        }

        public void ExtractGitIfNeeded()
        {
            if (IsExtracted())
            {
                Logger.Info("Already extracted {0}, returning", WindowsPortableGitZip);
                return;
            }

            var environmentPath = Environment.ExtensionInstallPath;
            var tempPath = Path.Combine(environmentPath, FileSystem.GetRandomFileName() + TemporaryFolderSuffix);
            var archiveFilePath = Path.Combine(environmentPath, WindowsPortableGitZip);

            try
            {
                FileSystem.CreateDirectory(tempPath);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Couldn't create temp dir: " + tempPath);
                throw;
            }

            if (!FileSystem.FileExists(archiveFilePath))
            {
                var exception = new FileNotFoundException("Could not find file", archiveFilePath);
                Logger.Error(exception, "Trying to extract {0}, but it doesn't exist", archiveFilePath);
                throw exception;
            }

            try
            {
                SharpZipLibHelper.ExtractZipFile(archiveFilePath, tempPath);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error Extracting Archive:\"{0}\" OutDir:\"{1}\"", archiveFilePath, tempPath);
                throw;
            }
        }

        public bool IsExtracted()
        {
            var target = GetPackageDestinationDirectory();

            var git = FileSystem.Combine(target, "cmd", "git.exe");
            if (!FileSystem.FileExists(git))
            {
                return false;
            }

            var versionFile = FileSystem.Combine(target, "VERSION");
            if (!FileSystem.FileExists(versionFile))
            {
                return false;
            }

            var expectedVersion = ExpectedVersion;
            if (FileSystem.ReadAllText(versionFile).Trim() != expectedVersion)
            {
                Logger.Warning("Package '{0}' out of date, wanted {1}", target, expectedVersion);

                try
                {
                    var parentDirectory = FileSystem.GetParentDirectory(versionFile);
                    FileSystem.DeleteAllFiles(parentDirectory);
                }
                catch (Exception ex)
                {
                    Logger.Warning(ex, "Failed to remove {0}", target);
                }

                return false;
            }

            return true;
        }

        public string GetPortableGitDestinationDirectory(bool createIfNeeded = false)
        {
            return GetPackageDestinationDirectory(createIfNeeded);
        }

        public string GetPackageDestinationDirectory(bool createIfNeeded = false, bool includeExpectedVersion = true)
        {
            var packageName = includeExpectedVersion ? PackageNameWithVersion : PackageName;

            var packageDestinationPath = FileSystem.Combine(Environment.ExtensionInstallPath, packageName);
            if (createIfNeeded)
            {
                FileSystem.CreateDirectory(packageDestinationPath);
            }

            return packageDestinationPath;
        }

        public string PackageNameWithVersion => PackageName + "_" + ExpectedVersion;

        protected IEnvironment Environment { get; }
        protected IFileSystem FileSystem { get; }
        protected ISharpZipLibHelper SharpZipLibHelper { get; }

        protected ILogging Logger { get; }
    }
}
