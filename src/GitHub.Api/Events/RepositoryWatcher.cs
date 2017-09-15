using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using sfw.net;

namespace GitHub.Unity
{
    interface IRepositoryWatcher : IDisposable
    {
        void Start();
        void Stop();
        event Action<string> HeadChanged;
        event Action IndexChanged;
        event Action ConfigChanged;
        event Action<string> LocalBranchChanged;
        event Action<string> LocalBranchCreated;
        event Action<string> LocalBranchDeleted;
        event Action RepositoryChanged;
        event Action<string, string> RemoteBranchCreated;
        event Action<string, string> RemoteBranchDeleted;
        void Initialize();
        int CheckAndProcessEvents();
    }

    class RepositoryWatcher : IRepositoryWatcher
    {
        private readonly RepositoryPathConfiguration paths;
        private readonly CancellationToken cancellationToken;
        private readonly NPath[] ignoredPaths;
        private readonly ManualResetEventSlim pauseEvent;
        private readonly bool disableNative;
        private NativeInterface nativeInterface;
        private bool running;
        private Task task;
        private int lastCountOfProcessedEvents = 0;
        private bool processingEvents;
        private readonly ManualResetEventSlim signalProcessingEventsDone = new ManualResetEventSlim(false);

        public event Action<string> HeadChanged;
        public event Action IndexChanged;
        public event Action ConfigChanged;
        public event Action<string> LocalBranchChanged;
        public event Action<string> LocalBranchCreated;
        public event Action<string> LocalBranchDeleted;
        public event Action RepositoryChanged;
        public event Action<string, string> RemoteBranchCreated;
        public event Action<string, string> RemoteBranchDeleted;

        public RepositoryWatcher(IPlatform platform, RepositoryPathConfiguration paths, CancellationToken cancellationToken)
        {
            this.paths = paths;
            this.cancellationToken = cancellationToken;

            ignoredPaths = new[] {
                platform.Environment.UnityProjectPath.Combine("Library"),
                platform.Environment.UnityProjectPath.Combine("Temp")
            };

            pauseEvent = new ManualResetEventSlim();
            //disableNative = !platform.Environment.IsWindows;
        }

        public void Initialize()
        {
            var pathsRepositoryPath = paths.RepositoryPath.ToString();

            try
            {
                if (!disableNative)
                    nativeInterface = new NativeInterface(pathsRepositoryPath);
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
        }

        public void Start()
        {
            if (disableNative)
            {
                Logger.Trace("Native interface is disabled");
                return;
            }

            if (nativeInterface == null)
            {
                Logger.Warning("NativeInterface is null");
                throw new InvalidOperationException("NativeInterface is null");
            }

            Logger.Trace("Watching Path: \"{0}\"", paths.RepositoryPath.ToString());

            running = true;
            pauseEvent.Reset();
            task = Task.Factory.StartNew(WatcherLoop, cancellationToken, TaskCreationOptions.None, TaskScheduler.Default);
        }

        public void Stop()
        {
            if (!running)
            {
                Logger.Warning("Watcher already stopped");
                return;
            }

            Logger.Trace("Stopping watcher");

            running = false;
            pauseEvent.Set();
        }

        private void WatcherLoop()
        {
            Logger.Trace("Starting watcher");

            while (running)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    Stop();
                    break;
                }

                CheckAndProcessEvents();

                if (pauseEvent.Wait(1000))
                {
                    break;
                }
            }
        }

        public int CheckAndProcessEvents()
        {
            if (processingEvents)
            {
                signalProcessingEventsDone.Wait(cancellationToken);
                return lastCountOfProcessedEvents;
            }

            signalProcessingEventsDone.Reset();
            processingEvents = true;
            lastCountOfProcessedEvents = 0;
            var fileEvents = nativeInterface.GetEvents();

            if (fileEvents.Length > 0)
            {
                Logger.Trace("Handling {0} Events", fileEvents.Length);
                var processedEventCount = ProcessEvents(fileEvents);
                lastCountOfProcessedEvents = processedEventCount;
                Logger.Trace("Processed {0} Events", processedEventCount);
            }

            processingEvents = false;
            signalProcessingEventsDone.Set();
            return lastCountOfProcessedEvents;
        }

        private int ProcessEvents(Event[] fileEvents)
        {
            var eventsProcessed = 0;
            var configChanged = false;
            var headChanged = false;
            var repositoryChanged = false;
            var indexChanged = false;

            string headContent = null;

            foreach (var fileEvent in fileEvents)
            {
                if (!running)
                {
                    break;
                }

                if (cancellationToken.IsCancellationRequested)
                {
                    Stop();
                    break;
                }

                //Logger.Trace(fileEvent.Describe());

                var eventDirectory = new NPath(fileEvent.Directory);
                var fileA = eventDirectory.Combine(fileEvent.FileA);

                NPath fileB = null;
                if (fileEvent.FileB != null)
                {
                    fileB = eventDirectory.Combine(fileEvent.FileB);
                }

                // handling events in .git/*
                if (fileA.IsChildOf(paths.DotGitPath))
                {
                    if (!configChanged && fileA.Equals(paths.DotGitConfig))
                    {
                        configChanged = true;
                    }
                    else if (!headChanged && fileA.Equals(paths.DotGitHead))
                    {
                        if (fileEvent.Type != EventType.DELETED)
                        {
                            headContent = paths.DotGitHead.ReadAllLines().FirstOrDefault();
                        }

                        headChanged = true;
                    }
                    else if (!indexChanged && fileA.Equals(paths.DotGitIndex))
                    {
                        indexChanged = true;
                    }
                    else if (fileA.IsChildOf(paths.RemotesPath))
                    {
                        var relativePath = fileA.RelativeTo(paths.RemotesPath);
                        var relativePathElements = relativePath.Elements.ToArray();

                        if (!relativePathElements.Any())
                        {
                            continue;
                        }

                        var origin = relativePathElements[0];

                        if (fileEvent.Type == EventType.DELETED)
                        {
                            if (fileA.ExtensionWithDot == ".lock")
                            {
                                continue;
                            }

                            var branch = string.Join(@"/", relativePathElements.Skip(1).ToArray());

                            Logger.Trace("RemoteBranchDeleted: {0}/{1}", origin, branch);
                            RemoteBranchDeleted?.Invoke(origin, branch);
                            eventsProcessed++;
                        }
                        else if (fileEvent.Type == EventType.RENAMED)
                        {
                            if (fileA.ExtensionWithDot != ".lock")
                            {
                                continue;
                            }

                            if (fileB != null && fileB.FileExists())
                            {
                                if (fileA.FileNameWithoutExtension == fileB.FileNameWithoutExtension)
                                {
                                    var branchPathElement = relativePathElements
                                        .Skip(1).Take(relativePathElements.Length - 2)
                                        .Union(new[] { fileA.FileNameWithoutExtension }).ToArray();

                                    var branch = string.Join(@"/", branchPathElement);

                                    Logger.Trace("RemoteBranchCreated: {0}/{1}", origin, branch);
                                    RemoteBranchCreated?.Invoke(origin, branch);
                                    eventsProcessed++;
                                }
                            }
                        }
                    }
                    else if (fileA.IsChildOf(paths.BranchesPath))
                    {
                        if (fileEvent.Type == EventType.MODIFIED)
                        {
                            if (fileA.DirectoryExists())
                            {
                                continue;
                            }

                            if (fileA.ExtensionWithDot == ".lock")
                            {
                                continue;
                            }

                            var relativePath = fileA.RelativeTo(paths.BranchesPath);
                            var relativePathElements = relativePath.Elements.ToArray();

                            if (!relativePathElements.Any())
                            {
                                continue;
                            }

                            var branch = string.Join(@"/", relativePathElements.ToArray());

                            Logger.Trace("LocalBranchChanged: {0}", branch);
                            LocalBranchChanged?.Invoke(branch);
                            eventsProcessed++;
                        }
                        else if (fileEvent.Type == EventType.DELETED)
                        {
                            if (fileA.ExtensionWithDot == ".lock")
                            {
                                continue;
                            }

                            var relativePath = fileA.RelativeTo(paths.BranchesPath);
                            var relativePathElements = relativePath.Elements.ToArray();

                            if (!relativePathElements.Any())
                            {
                                continue;
                            }

                            var branch = string.Join(@"/", relativePathElements.ToArray());

                            Logger.Trace("LocalBranchDeleted: {0}", branch);
                            LocalBranchDeleted?.Invoke(branch);
                            eventsProcessed++;
                        }
                        else if (fileEvent.Type == EventType.RENAMED)
                        {
                            if (fileA.ExtensionWithDot != ".lock")
                            {
                                continue;
                            }

                            if (fileB != null && fileB.FileExists())
                            {
                                if (fileA.FileNameWithoutExtension == fileB.FileNameWithoutExtension)
                                {
                                    var relativePath = fileB.RelativeTo(paths.BranchesPath);
                                    var relativePathElements = relativePath.Elements.ToArray();

                                    if (!relativePathElements.Any())
                                    {
                                        continue;
                                    }

                                    var branch = string.Join(@"/", relativePathElements.ToArray());

                                    Logger.Trace("LocalBranchCreated: {0}", branch);
                                    LocalBranchCreated?.Invoke(branch);
                                    eventsProcessed++;
                                }
                            }
                        }
                    }
                }
                else
                {
                    if (repositoryChanged || ignoredPaths.Any(ignoredPath => fileA.IsChildOf(ignoredPath)))
                    {
                        continue;
                    }

                    repositoryChanged = true;
                }
            }

            if (configChanged)
            {
                Logger.Trace("ConfigChanged");
                ConfigChanged?.Invoke();
                eventsProcessed++;
            }

            if (headChanged)
            {
                Logger.Trace("HeadChanged: {0}", headContent ?? "[null]");
                HeadChanged?.Invoke(headContent);
                eventsProcessed++;
            }

            if (indexChanged)
            {
                Logger.Trace("IndexChanged");
                IndexChanged?.Invoke();
                eventsProcessed++;
            }

            if (repositoryChanged)
            {
                Logger.Trace("RepositoryChanged");
                RepositoryChanged?.Invoke();
                eventsProcessed++;
            }

            return eventsProcessed;
        }

        private bool disposed;
        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (!disposed)
                {
                    disposed = true;
                    Stop();
                    if (nativeInterface != null)
                    {
                        nativeInterface.Dispose();
                        nativeInterface = null;
                    }
                }
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }

        protected static ILogging Logger { get; } = Logging.GetLogger<RepositoryWatcher>();
    }
}
