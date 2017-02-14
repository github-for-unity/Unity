using System;
using System.Collections.Concurrent;
using System.IO;
using GitHub.Unity;

namespace GitHub.Api
{
    abstract class PortablePackageManager : ICleanupService
    {
        private readonly IEnvironment environment;
        private readonly IFileSystem fileSystem;
        private readonly ISharpZipLibHelper sharpZipLibHelper;
        private readonly ILogging logger;

        private const string TemporaryFolderSuffix = ".deleteme";
        private const string WindowsPortableGitZip = @"resources\windows\PortableGit.zip";

        readonly ConcurrentDictionary<string, bool> extractResults =
            new ConcurrentDictionary<string, bool>();

//        readonly IOperatingSystem operatingSystem;
        readonly IProgram program;
//        readonly IZipArchive zipArchive;

//        protected PortablePackageManager(IOperatingSystem operatingSystem, IProgram program, IZipArchive zipArchive)
        protected PortablePackageManager(IEnvironment environment, IFileSystem fileSystem, ISharpZipLibHelper sharpZipLibHelper)
        {
            Guard.ArgumentNotNull(environment, nameof(environment));
            Guard.ArgumentNotNull(fileSystem, nameof(fileSystem));
            Guard.ArgumentNotNull(sharpZipLibHelper, nameof(sharpZipLibHelper));

            logger = Logging.GetLogger(GetType());

            this.environment = environment;
            this.fileSystem = fileSystem;
            this.sharpZipLibHelper = sharpZipLibHelper;
        }

//        protected IOperatingSystem OperatingSystem { get { return operatingSystem; } }

        public bool IsPackageExtracted()
        {
            var target = GetPackageDestinationDirectory();

            throw new NotImplementedException();

//            var canaryFile = operatingSystem.GetFile(GetPathToCanary(target));
//            if (!canaryFile.Exists) return false;
//
//            var versionFile = operatingSystem.GetFile(Path.Combine(target, "VERSION"));
//            if (!versionFile.Exists) return false;
//
//            if (versionFile.ReadAllText().Trim() != GetExpectedVersion())
//            {
//                versionFile.Directory.FastDelete()
//                    .Subscribe(
//                        _ => log.Warn("Package '{0}' out of date, wanted {1}", target, GetExpectedVersion()),
//                        ex => log.Warn("Failed to remove " + target, ex));
//
//                return false;
//            }
//
//            return true;
        }

        public string GetPackageDestinationDirectory(bool createIfNeeded = false, bool includeExpectedVersion = true)
        {
            throw new NotImplementedException();

//            string packageName = includeExpectedVersion
//                ? GetPackageNameWithVersion()
//                : GetPackageName();
//            var packageDestinationPath = RootPackageDirectory.Combine(packageName);
//            if (createIfNeeded)
//            {
//                var destination = operatingSystem.GetDirectory(packageDestinationPath);
//                FuncExtensions.Retry(destination.Create);
//            }
//
//            return packageDestinationPath;
        }

        public string GetPackageNameWithVersion()
        {
            throw new NotImplementedException();

//            return GetPackageName() + "_" + GetExpectedVersion();
        }

        protected string RootPackageDirectory
        {
            get { throw new NotImplementedException();}
        }

        protected void ExtractPackageIfNeeded(string fileName, Action preExtract = null, Action postExtract = null)
        {
            var extractResult = extractResults.GetOrAdd(fileName, false);
            if (!extractResult)
            {
                return;
            }

            // First, check to see if we're already done
            if (IsPackageExtracted())
            {
                logger.Info("Already extracted {0}, returning", fileName);
                return;
            }

            if (preExtract != null)
            {
                preExtract();
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

                extractResults.TryRemove(fileName, out extractResult);

                throw;
            }

            if (!fileSystem.FileExists(archiveFilePath))
            {
                var exception = new FileNotFoundException("Could not find file", archiveFilePath);
                logger.Error(exception, "Trying to extract {0}, but it doesn't exist", archiveFilePath);

                extractResults.TryRemove(fileName, out extractResult);

                throw exception;
            }

            try
            {
                sharpZipLibHelper.ExtractZipFile(archiveFilePath, tempPath);
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error Extracting Archive:\"{0}\" OutDir:\"{1}\"", archiveFilePath, tempPath);

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
