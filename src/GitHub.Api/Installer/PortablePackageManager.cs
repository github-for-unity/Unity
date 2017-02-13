using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using GitHub.Extensions;
using GitHub.IO;
using NLog;
using LogManager = NLog.LogManager;

namespace GitHub.Helpers
{
    public abstract class PortablePackageManager : ICleanupService
    {
        static readonly Logger log = LogManager.GetCurrentClassLogger();

        readonly ConcurrentDictionary<string, ReplaySubject<ProgressResult>> extractResults =
            new ConcurrentDictionary<string, ReplaySubject<ProgressResult>>();

        readonly IOperatingSystem operatingSystem;
        readonly IProgram program;
        readonly IZipArchive zipArchive;

        protected PortablePackageManager(IOperatingSystem operatingSystem, IProgram program, IZipArchive zipArchive)
        {
            Ensure.ArgumentNotNull(operatingSystem, "operatingSystem");
            Ensure.ArgumentNotNull(program, "program");
            Ensure.ArgumentNotNull(zipArchive, "zipArchive");

            this.operatingSystem = operatingSystem;
            this.program = program;
            this.zipArchive = zipArchive;
        }

        protected IOperatingSystem OperatingSystem { get { return operatingSystem; } }

        public bool IsPackageExtracted()
        {
            var target = GetPackageDestinationDirectory();
            var canaryFile = operatingSystem.GetFile(GetPathToCanary(target));
            if (!canaryFile.Exists) return false;

            var versionFile = operatingSystem.GetFile(Path.Combine(target, "VERSION"));
            if (!versionFile.Exists) return false;

            if (versionFile.ReadAllText().Trim() != GetExpectedVersion())
            {
                versionFile.Directory.FastDelete()
                    .Subscribe(
                        _ => log.Warn("Package '{0}' out of date, wanted {1}", target, GetExpectedVersion()),
                        ex => log.Warn("Failed to remove " + target, ex));

                return false;
            }

            return true;
        }

        public string GetPackageDestinationDirectory(bool createIfNeeded = false, bool includeExpectedVersion = true)
        {
            string packageName = includeExpectedVersion
                ? GetPackageNameWithVersion()
                : GetPackageName();
            var packageDestinationPath = RootPackageDirectory.Combine(packageName);
            if (createIfNeeded)
            {
                var destination = operatingSystem.GetDirectory(packageDestinationPath);
                FuncExtensions.Retry(destination.Create);
            }

            return packageDestinationPath;
        }

        public string GetPackageNameWithVersion()
        {
            return GetPackageName() + "_" + GetExpectedVersion();
        }

        protected PathString RootPackageDirectory
        {
            get { return operatingSystem.Environment.LocalGitHubApplicationDataPath; }
        }

        [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "Automatically disposed when the observable completes.")]
        protected IObservable<ProgressResult> ExtractPackageIfNeeded(string fileName, Action preExtract = null, Action postExtract = null, int? estimatedFileCount = null)
        {
            var newResult = new ReplaySubject<ProgressResult>(bufferSize: 1);

            var extractResult = extractResults.GetOrAdd(fileName, newResult);
            if (extractResult != newResult)
            {
                return extractResult;
            }

            // First, check to see if we're already done
            if (IsPackageExtracted())
            {
                log.Info(CultureInfo.InvariantCulture, "Already extracted {0}, returning 100%", fileName);
                extractResult.OnNext(new ProgressResult(100));
                extractResult.OnCompleted();

                return extractResult;
            }

            return Observable.Defer(() =>
            {
                if (preExtract != null) 
                { 
                    preExtract(); 
                }
                var tempPath = RootPackageDirectory.Combine(Path.GetRandomFileName() + GitHubDirectory.TemporaryFolderSuffix);
                IDirectory temporaryDirectory;
                try
                {
                    temporaryDirectory = operatingSystem.GetDirectory(tempPath);
                    temporaryDirectory.Create();
                }
                catch (Exception ex)
                {
                    log.Error("Couldn't create temp dir: " + tempPath, ex);
                    extractResult.OnError(ex);
                    return extractResult;
                }

                var archiveFilePath = Path.Combine(program.ExecutingAssemblyDirectory, fileName);
                var archiveFile = operatingSystem.GetFile(archiveFilePath);
                if (!archiveFile.Exists)
                {
                    var exception = new FileNotFoundException("Could not find file", archiveFilePath);
                    log.Error(String.Format(CultureInfo.InvariantCulture, "Trying to extract {0}, but it doesn't exist", archiveFilePath), exception);
                    extractResult.OnError(exception);
                    return extractResult;
                }

                var proc = zipArchive.ExtractToDirectory(archiveFilePath, temporaryDirectory.FullName);

                IObservable<ProgressResult> progress;
                if (estimatedFileCount != null)
                {
                    progress = proc.CombinedOutput
                        .TakeUntil(proc)
                        .Scan(0, (acc, _) => acc + 1)
                        .Select(x => (int)(((double)x / estimatedFileCount) * 95.0))
                        .Select(x => new ProgressResult(x));
                }
                else
                {
                    progress = Observable.Timer(DateTimeOffset.MinValue, TimeSpan.FromSeconds(0.5), RxApp.TaskpoolScheduler)
                        .TakeUntil(proc)
                        .Select(x => (int)Math.Min(x * 5, 95))
                        .Select(x => new ProgressResult(x));
                }

                progress = progress
                    .Concat(Observable.Return(new ProgressResult(95)))
                    .Concat(Observable.Defer(() => 
                        // It's conceivable that during the time we've been extracting the
                        // package another process of GHfW has completed the extract so
                        // right before we do the final move we'll do another check to see
                        // if it's already extracted. If it is we'll bail and let the scavenger
                        // take care of cleaning us up on the next run.
                        IsPackageExtracted() 
                            ? Observable.Return(new ProgressResult(100))
                            : MoveTemporaryPackageToFinalDestination(temporaryDirectory)));

                var ret = progress
                    .Multicast(extractResult);

                // We Multicast so if someone subscribes later, they'll just see 
                // "100, Finished" as fast as possible (since the op already finished)
                log.Info(CultureInfo.InvariantCulture, "Extracting {0} is (so far) successful", fileName);
                ret.Connect();

                return extractResult.Do(_ => {},
                    ex =>
                    {
                        log.Warn("Failed to extract package successfully: " + fileName, ex); 
                        ReplaySubject<ProgressResult> res;
                        extractResults.TryRemove(fileName, out res);
                    }, () =>
                    {
                        ReplaySubject<ProgressResult> res;
                        extractResults.TryRemove(fileName, out res);
                        log.Info("Extracted package successfully: " + fileName);

                        if (postExtract != null)
                        {
                            postExtract();
                        }
                    });
            });
        }

        IObservable<ProgressResult> MoveTemporaryPackageToFinalDestination(IDirectory temporaryDirectory)
        {
            try
            {
                var destinationPath = GetPackageDestinationDirectory();
                var destination = operatingSystem.GetDirectory(destinationPath);
                destination.FastDelete();
                temporaryDirectory.MoveToWithRetryAndCopyFallback(destination);
            }
            catch (Exception e)
            {
                log.Error(string.Format(CultureInfo.InvariantCulture, 
                    "Couldn't rename temporary directory to {0}", GetPackageDestinationDirectory()), e);
                return Observable.Throw<ProgressResult>(e);
            }
            return Observable.Return(new ProgressResult(100));
        }

        protected abstract string GetPathToCanary(string rootDir);

        protected abstract string GetExpectedVersion();

        protected abstract string GetPackageName();

        // Deletes older versions of the package.
        IObservable<Unit> ICleanupService.Clean()
        {
            return Observable.Defer(() =>
            {
                var pathPrefix = GetPackageDestinationDirectory(createIfNeeded: false, includeExpectedVersion: false);
                var currentPackageDir = GetPackageDestinationDirectory(); // The package dir we do NOT want to delete.

                var root = operatingSystem.GetDirectory(RootPackageDirectory);
                if (!root.Exists) return Observable.Return(Unit.Default);

                return root.EnumerateDirectories()
                    .ToObservable()
                    .Do(x => log.Debug(CultureInfo.InvariantCulture, "Examining package dir {0}", x.FullName))
                    .Where(x => x.FullName.StartsWith(pathPrefix, StringComparison.OrdinalIgnoreCase) && x.FullName != currentPackageDir)
                    .Do(x =>
                    {
                        log.Info("About to cleanup old package '{0}', current version is '{1}'", x.FullName, currentPackageDir);
                        x.FastDelete();
                    })
                    .AsCompletion();
            });
        }
    }
}
