using System;
using System.Collections.Concurrent;
using System.IO;
using GitHub.Unity;

namespace GitHub.Api
{
    abstract class PortablePackageManager : IPortablePackageManager
    {
        private const string TemporaryFolderSuffix = ".deleteme";

        private readonly ConcurrentDictionary<string, bool> extractResults = new ConcurrentDictionary<string, bool>();

        private readonly IProgram program;

        protected PortablePackageManager(IEnvironment environment, IFileSystem fileSystem,
            ISharpZipLibHelper sharpZipLibHelper)
        {
            Guard.ArgumentNotNull(environment, nameof(environment));
            Guard.ArgumentNotNull(fileSystem, nameof(fileSystem));
            Guard.ArgumentNotNull(sharpZipLibHelper, nameof(sharpZipLibHelper));

            Logger = Logging.GetLogger(GetType());

            Environment = environment;
            FileSystem = fileSystem;
            SharpZipLibHelper = sharpZipLibHelper;
        }

        public bool IsPackageExtracted()
        {
            var target = GetPackageDestinationDirectory();

            var canaryFile = GetPathToCanary(target);
            if (!FileSystem.FileExists(canaryFile))
            {
                return false;
            }

            var versionFile = FileSystem.Combine(target, "VERSION");
            if (!FileSystem.FileExists(versionFile))
            {
                return false;
            }

            var expectedVersion = GetExpectedVersion();
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
            var packageName = includeExpectedVersion ? GetPackageNameWithVersion() : GetPackageName();

            var packageDestinationPath = FileSystem.Combine(RootPackageDirectory, packageName);
            if (createIfNeeded)
            {
                FileSystem.CreateDirectory(packageDestinationPath);
            }

            return packageDestinationPath;
        }

        public string GetPackageNameWithVersion()
        {
            return GetPackageName() + "_" + GetExpectedVersion();
        }

        public void Clean()
        {
            throw new NotImplementedException();
        }

        protected void ExtractPackageIfNeeded(string fileName, Action preExtract = null, Action postExtract = null)
        {
            var extractResult = extractResults.GetOrAdd(fileName, false);
            if (extractResult)
            {
                return;
            }

            // First, check to see if we're already done
            if (IsPackageExtracted())
            {
                Logger.Info("Already extracted {0}, returning", fileName);
                return;
            }

            if (preExtract != null)
            {
                preExtract();
            }

            var environmentPath = Environment.ExtensionInstallPath;
            var tempPath = Path.Combine(environmentPath, FileSystem.GetRandomFileName() + TemporaryFolderSuffix);
            var archiveFilePath = Path.Combine(environmentPath, fileName);

            try
            {
                FileSystem.CreateDirectory(tempPath);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Couldn't create temp dir: " + tempPath);

                extractResults.TryRemove(fileName, out extractResult);

                throw;
            }

            if (!FileSystem.FileExists(archiveFilePath))
            {
                var exception = new FileNotFoundException("Could not find file", archiveFilePath);
                Logger.Error(exception, "Trying to extract {0}, but it doesn't exist", archiveFilePath);

                extractResults.TryRemove(fileName, out extractResult);

                throw exception;
            }

            try
            {
                SharpZipLibHelper.ExtractZipFile(archiveFilePath, tempPath);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error Extracting Archive:\"{0}\" OutDir:\"{1}\"", archiveFilePath, tempPath);

                extractResults.TryRemove(fileName, out extractResult);

                throw;
            }
        }

        protected abstract string GetPathToCanary(string rootDir);

        protected abstract string GetExpectedVersion();

        protected abstract string GetPackageName();
        protected IEnvironment Environment { get; }
        protected IFileSystem FileSystem { get; }
        protected ISharpZipLibHelper SharpZipLibHelper { get; }
        protected ILogging Logger { get; }

        protected string RootPackageDirectory
        {
            get { return Environment.ExtensionInstallPath; }
        }
    }
}
