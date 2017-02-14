using System;
using System.Collections.Concurrent;
using System.IO;
using GitHub.Unity;

namespace GitHub.Api
{
    abstract class PortablePackageManager : ICleanupService
    {
        protected IEnvironment Environment { get; }
        protected IFileSystem FileSystem { get; }
        protected ISharpZipLibHelper SharpZipLibHelper { get; }
        protected ILogging Logger { get; }

        private const string TemporaryFolderSuffix = ".deleteme";
        private const string WindowsPortableGitZip = @"resources\windows\PortableGit.zip";

        readonly ConcurrentDictionary<string, bool> extractResults =
            new ConcurrentDictionary<string, bool>();

        readonly IProgram program;

        protected PortablePackageManager(IEnvironment environment, IFileSystem fileSystem, ISharpZipLibHelper sharpZipLibHelper)
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
            if (!FileSystem.FileExists(canaryFile)) return false;
            
            var versionFile = FileSystem.Combine(target, "VERSION");
            if (!FileSystem.FileExists(versionFile)) return false;

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
            string packageName = includeExpectedVersion
                ? GetPackageNameWithVersion()
                : GetPackageName();

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

        protected string RootPackageDirectory
        {
            get { return Environment.ExtensionInstallPath;}
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
            var archiveFilePath = Path.Combine(environmentPath, WindowsPortableGitZip);
                
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

//        IObservable<ProgressResult> MoveTemporaryPackageToFinalDestination(IDirectory temporaryDirectory)
//        {
//            try
//            {
//                var destinationPath = GetPackageDestinationDirectory();
//                var destination = operatingSystem.GetDirectory(destinationPath);
//                destination.FastDelete();
//                temporaryDirectory.MoveToWithRetryAndCopyFallback(destination);
//            }
//            catch (Exception e)
//            {
//                log.Error(string.Format(CultureInfo.InvariantCulture, 
//                    "Couldn't rename temporary directory to {0}", GetPackageDestinationDirectory()), e);
//                return Observable.Throw<ProgressResult>(e);
//            }
//            return Observable.Return(new ProgressResult(100));
//        }

        protected abstract string GetPathToCanary(string rootDir);

        protected abstract string GetExpectedVersion();

        protected abstract string GetPackageName();

        // Deletes older versions of the package.
//        IObservable<Unit> ICleanupService.Clean()
//        {
//            return Observable.Defer(() =>
//            {
//                var pathPrefix = GetPackageDestinationDirectory(createIfNeeded: false, includeExpectedVersion: false);
//                var currentPackageDir = GetPackageDestinationDirectory(); // The package dir we do NOT want to delete.
//
//                var root = operatingSystem.GetDirectory(RootPackageDirectory);
//                if (!root.Exists) return Observable.Return(Unit.Default);
//
//                return root.EnumerateDirectories()
//                    .ToObservable()
//                    .Do(x => log.Debug(CultureInfo.InvariantCulture, "Examining package dir {0}", x.FullName))
//                    .Where(x => x.FullName.StartsWith(pathPrefix, StringComparison.OrdinalIgnoreCase) && x.FullName != currentPackageDir)
//                    .Do(x =>
//                    {
//                        log.Info("About to cleanup old package '{0}', current version is '{1}'", x.FullName, currentPackageDir);
//                        x.FastDelete();
//                    })
//                    .AsCompletion();
//            });
//        }
    }
}
