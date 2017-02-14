using System;
using System.Collections.Concurrent;
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
        protected readonly ConcurrentDictionary<string, bool> extractResults = new ConcurrentDictionary<string, bool>();

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
            var extractResult = extractResults.GetOrAdd(WindowsPortableGitZip, false);
            if (extractResult)
            {
                return;
            }

            // First, check to see if we're already done
            if (IsPackageExtracted())
            {
                Logger.Info("Already extracted {0}, returning", WindowsPortableGitZip);
                return;
            }

            if ((Action)null != null)
            {
                ((Action)null)();
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

                extractResults.TryRemove(WindowsPortableGitZip, out extractResult);

                throw;
            }

            if (!FileSystem.FileExists(archiveFilePath))
            {
                var exception = new FileNotFoundException("Could not find file", archiveFilePath);
                Logger.Error(exception, "Trying to extract {0}, but it doesn't exist", archiveFilePath);

                extractResults.TryRemove(WindowsPortableGitZip, out extractResult);

                throw exception;
            }

            try
            {
                SharpZipLibHelper.ExtractZipFile(archiveFilePath, tempPath);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error Extracting Archive:\"{0}\" OutDir:\"{1}\"", archiveFilePath, tempPath);

                extractResults.TryRemove(WindowsPortableGitZip, out extractResult);

                throw;
            }
        }

        public bool IsExtracted()
        {
            return IsPackageExtracted();
        }

        public string GetPortableGitDestinationDirectory(bool createIfNeeded = false)
        {
            return GetPackageDestinationDirectory(createIfNeeded);
        }

        public bool IsPackageExtracted()
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
