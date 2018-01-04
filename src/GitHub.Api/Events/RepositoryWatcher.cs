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
        event Action HeadChanged;
        event Action IndexChanged;
        event Action ConfigChanged;
        event Action RepositoryCommitted;
        event Action RepositoryChanged;
        event Action LocalBranchesChanged;
        event Action RemoteBranchesChanged;
        void Initialize();
        int CheckAndProcessEvents();
    }

    class RepositoryWatcher : IRepositoryWatcher
    {
        private readonly RepositoryPathConfiguration paths;
        private readonly CancellationToken cancellationToken;
        private readonly NPath[] ignoredPaths;
        private readonly ManualResetEventSlim pauseEvent;
        private NativeInterface nativeInterface;
        private bool running;
        private int lastCountOfProcessedEvents = 0;
        private bool processingEvents;
        private readonly ManualResetEventSlim signalProcessingEventsDone = new ManualResetEventSlim(false);

        public event Action HeadChanged;
        public event Action IndexChanged;
        public event Action ConfigChanged;
        public event Action RepositoryCommitted;
        public event Action RepositoryChanged;
        public event Action LocalBranchesChanged;
        public event Action RemoteBranchesChanged;

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
                nativeInterface = new NativeInterface(pathsRepositoryPath);
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
        }

        public void Start()
        {
            if (nativeInterface == null)
            {
                Logger.Warning("NativeInterface is null");
                throw new InvalidOperationException("NativeInterface is null");
            }

            Logger.Trace("Watching Path: \"{0}\"", paths.RepositoryPath.ToString());

            running = true;
            pauseEvent.Reset();
            Task.Factory.StartNew(WatcherLoop, cancellationToken, TaskCreationOptions.None, TaskScheduler.Default);
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
            var processedEventCount = 0;

            var fileEvents = nativeInterface.GetEvents();
            if (fileEvents.Length > 0)
            {
                Logger.Trace("Handling {0} Events", fileEvents.Length);
                processedEventCount = ProcessEvents(fileEvents);
                Logger.Trace("Processed {0} Events", processedEventCount);
            }

            lastCountOfProcessedEvents = processedEventCount;
            processingEvents = false;
            signalProcessingEventsDone.Set();

            return processedEventCount;
        }

        private int ProcessEvents(Event[] fileEvents)
        {
            var events = new HashSet<EventType>();
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
                    if (!events.Contains(EventType.ConfigChanged) && fileA.Equals(paths.DotGitConfig))
                    {
                        events.Add(EventType.ConfigChanged);
                    }
                    else if (!events.Contains(EventType.HeadChanged) && fileA.Equals(paths.DotGitHead))
                    {
                        events.Add(EventType.HeadChanged);
                    }
                    else if (!events.Contains(EventType.IndexChanged) && fileA.Equals(paths.DotGitIndex))
                    {
                        events.Add(EventType.IndexChanged);
                    }
                    else if (!events.Contains(EventType.RemoteBranchesChanged) && fileA.IsChildOf(paths.RemotesPath))
                    {
                        events.Add(EventType.RemoteBranchesChanged);
                    }
                    else if (!events.Contains(EventType.LocalBranchesChanged) && fileA.IsChildOf(paths.BranchesPath))
                    {
                        events.Add(EventType.LocalBranchesChanged);
                    }
                    else if (!events.Contains(EventType.RepositoryCommitted) && fileA.IsChildOf(paths.DotGitCommitEditMsg))
                    {
                        events.Add(EventType.RepositoryCommitted);
                    }
                }
                else
                {
                    if (events.Contains(EventType.RepositoryChanged) || ignoredPaths.Any(ignoredPath => fileA.IsChildOf(ignoredPath)))
                    {
                        continue;
                    }
                    events.Add(EventType.RepositoryChanged);
                }
            }

            return FireEvents(events);
        }

        private int FireEvents(HashSet<EventType> events)
        {
            int eventsProcessed = 0;
            if (events.Contains(EventType.ConfigChanged))
            {
                Logger.Trace("ConfigChanged");
                ConfigChanged?.Invoke();
                eventsProcessed++;
            }

            if (events.Contains(EventType.HeadChanged))
            {
                Logger.Trace("HeadChanged");
                HeadChanged?.Invoke();
                eventsProcessed++;
            }

            if (events.Contains(EventType.LocalBranchesChanged))
            {
                Logger.Trace("LocalBranchesChanged");
                LocalBranchesChanged?.Invoke();
                eventsProcessed++;
            }

            if (events.Contains(EventType.RemoteBranchesChanged))
            {
                Logger.Trace("RemoteBranchesChanged");
                RemoteBranchesChanged?.Invoke();
                eventsProcessed++;
            }

            if (events.Contains(EventType.IndexChanged))
            {
                Logger.Trace("IndexChanged");
                IndexChanged?.Invoke();
                eventsProcessed++;
            }

            if (events.Contains(EventType.RepositoryChanged))
            {
                Logger.Trace("RepositoryChanged");
                RepositoryChanged?.Invoke();
                eventsProcessed++;
            }

            if (events.Contains(EventType.RepositoryCommitted))
            {
                Logger.Trace("RepositoryCommitted");
                RepositoryCommitted?.Invoke();
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

        private enum EventType
        {
            None,
            ConfigChanged,
            HeadChanged,
            IndexChanged,
            LocalBranchesChanged,
            RemoteBranchesChanged,
            RepositoryChanged,
            RepositoryCommitted
        }
    }
}
