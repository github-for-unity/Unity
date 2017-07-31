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
            disableNative = !platform.Environment.IsWindows;
        }

        public void Initialize()
        {
            var pathsRepositoryPath = paths.RepositoryPath.ToString();
            Logger.Trace("Watching Path: \"{0}\"", pathsRepositoryPath);

            if (!disableNative)
                nativeInterface = new NativeInterface(pathsRepositoryPath);
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
                throw new Exception("Not initialized");
            }

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

                var fileEvents = nativeInterface.GetEvents();

                if (fileEvents.Any())
                {
                    Logger.Trace("Processing {0} Events", fileEvents.Length);
                }

                var repositoryChanged = false;

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
                        HandleEventInDotGit(fileEvent, fileA, fileB);
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

                if (repositoryChanged)
                {
                    Logger.Trace("RepositoryChanged");
                    RepositoryChanged?.Invoke();
                }

                if (pauseEvent.Wait(200))
                {
                    break;
                }
            }
        }

        private void HandleEventInDotGit(Event fileEvent, NPath fileA, NPath fileB = null)
        {
            if (fileA.Equals(paths.DotGitConfig))
            {
                Logger.Trace("ConfigChanged");

                ConfigChanged?.Invoke();
            }
            else if (fileA.Equals(paths.DotGitHead))
            {
                string headContent = null;
                if (fileEvent.Type != EventType.DELETED)
                {
                    headContent = paths.DotGitHead.ReadAllLines().FirstOrDefault();
                }

                Logger.Trace("HeadChanged: {0}", headContent ?? "[null]");
                HeadChanged?.Invoke(headContent);
            }
            else if (fileA.Equals(paths.DotGitIndex))
            {
                Logger.Trace("IndexChanged");
                IndexChanged?.Invoke();
            }
            else if (fileA.IsChildOf(paths.RemotesPath))
            {
                var relativePath = fileA.RelativeTo(paths.RemotesPath);
                var relativePathElements = relativePath.Elements.ToArray();

                if (!relativePathElements.Any())
                {
                    return;
                }

                var origin = relativePathElements[0];

                if (fileEvent.Type == EventType.DELETED)
                {
                    if (fileA.ExtensionWithDot == ".lock")
                    {
                        return;
                    }

                    var branch = string.Join(@"/", relativePathElements.Skip(1).ToArray());

                    Logger.Trace("RemoteBranchDeleted: {0}/{1}", origin, branch);
                    RemoteBranchDeleted?.Invoke(origin, branch);
                }
                else if (fileEvent.Type == EventType.RENAMED)
                {
                    if (fileA.ExtensionWithDot != ".lock")
                    {
                        return;
                    }

                    if (fileB != null && fileB.FileExists())
                    {
                        if (fileA.FileNameWithoutExtension == fileB.FileNameWithoutExtension)
                        {
                            var branchPathElement = relativePathElements.Skip(1)
                                                              .Take(relativePathElements.Length-2)
                                                              .Union(new [] { fileA.FileNameWithoutExtension }).ToArray();

                            var branch = string.Join(@"/", branchPathElement);

                            Logger.Trace("RemoteBranchCreated: {0}/{1}", origin, branch);
                            RemoteBranchCreated?.Invoke(origin, branch);
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
                        return;
                    }

                    if (fileA.ExtensionWithDot == ".lock")
                    {
                        return;
                    }

                    var relativePath = fileA.RelativeTo(paths.BranchesPath);
                    var relativePathElements = relativePath.Elements.ToArray();

                    if (!relativePathElements.Any())
                    {
                        return;
                    }

                    var branch = string.Join(@"/", relativePathElements.ToArray());

                    Logger.Trace("LocalBranchChanged: {0}", branch);
                    LocalBranchChanged?.Invoke(branch);
                }
                else if (fileEvent.Type == EventType.DELETED)
                {
                    if (fileA.ExtensionWithDot == ".lock")
                    {
                        return;
                    }

                    var relativePath = fileA.RelativeTo(paths.BranchesPath);
                    var relativePathElements = relativePath.Elements.ToArray();

                    if (!relativePathElements.Any())
                    {
                        return;
                    }

                    var branch = string.Join(@"/", relativePathElements.ToArray());

                    Logger.Trace("LocalBranchDeleted: {0}", branch);
                    LocalBranchDeleted?.Invoke(branch);
                }
                else if (fileEvent.Type == EventType.RENAMED)
                {
                    if (fileA.ExtensionWithDot != ".lock")
                    {
                        return;
                    }

                    if (fileB != null && fileB.FileExists())
                    {
                        if (fileA.FileNameWithoutExtension == fileB.FileNameWithoutExtension)
                        {
                            var relativePath = fileB.RelativeTo(paths.BranchesPath);
                            var relativePathElements = relativePath.Elements.ToArray();

                            if (!relativePathElements.Any())
                            {
                                return;
                            }

                            var branch = string.Join(@"/", relativePathElements.ToArray());

                            Logger.Trace("LocalBranchCreated: {0}", branch);
                            LocalBranchCreated?.Invoke(branch);
                        }
                    }
                }
            }
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
