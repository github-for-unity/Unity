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
        event Action<string, string> RemoteBranchChanged;
        event Action<string, string> RemoteBranchCreated;
        event Action<string, string> RemoteBranchDeleted;
        void Initialize();
    }

    class RepositoryWatcher : IRepositoryWatcher
    {
        private readonly RepositoryPathConfiguration paths;
        private readonly CancellationToken cancellationToken;
        private readonly NPath[] ignoredPaths;
        private readonly AutoResetEvent autoResetEvent;

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
        public event Action<string, string> RemoteBranchChanged;
        public event Action<string, string> RemoteBranchCreated;
        public event Action<string, string> RemoteBranchDeleted;

        public RepositoryWatcher(IPlatform platform, RepositoryPathConfiguration paths, CancellationToken cancellationToken)
        {
            this.paths = paths;
            this.cancellationToken = cancellationToken;

            ignoredPaths = new[] {
                platform.Environment.UnityProjectPath.ToNPath().Combine("Library"),
                platform.Environment.UnityProjectPath.ToNPath().Combine("Temp")
            };

            autoResetEvent = new AutoResetEvent(false);
        }

        public void Initialize()
        {
            var pathsRepositoryPath = paths.RepositoryPath.ToString();
            Logger.Trace("Watching Path: \"{0}\"", pathsRepositoryPath);

            nativeInterface = new NativeInterface(pathsRepositoryPath);
        }

        public void Start()
        {
            Logger.Trace("Starting watcher");

            if (nativeInterface == null)
            {
                Logger.Warning("NativeInterface is null");
                throw new Exception("Not initialized");
            }

            running = true;
            task = new Task(WatcherLoop);
            task.Start(TaskScheduler.Current);
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
            autoResetEvent.Set();
        }

        private void WatcherLoop()
        {
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
                    Logger.Debug("RepositoryChanged");
                    RepositoryChanged?.Invoke();
                }

                if (autoResetEvent.WaitOne(200))
                {
                    break;
                }
            }
        }

        private void HandleEventInDotGit(Event fileEvent, NPath fileA, NPath fileB = null)
        {
            if (fileA.Equals(paths.DotGitConfig))
            {
                Logger.Debug("ConfigChanged");

                ConfigChanged?.Invoke();
            }
            else if (fileA.Equals(paths.DotGitHead))
            {
                string headContent = null;
                if (fileEvent.Type != EventType.DELETED)
                {
                    headContent = paths.DotGitHead.ReadAllLines().FirstOrDefault();
                }

                Logger.Debug("HeadChanged: {0}", headContent ?? "[null]");
                HeadChanged?.Invoke(headContent);
            }
            else if (fileA.Equals(paths.DotGitIndex))
            {
                Logger.Debug("IndexChanged");
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
                    var branch = string.Join(@"/", relativePathElements.Skip(1).ToArray());

                    Logger.Debug("RemoteBranchDeleted: {0}/{1}", origin, branch);
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

                            Logger.Debug("RemoteBranchCreated: {0}/{1}", origin, branch);
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

                    Logger.Debug("LocalBranchChanged: {0}", branch);
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

                    Logger.Debug("LocalBranchDeleted: {0}", branch);
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

                            Logger.Debug("LocalBranchCreated: {0}", branch);
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
                    nativeInterface.Dispose();
                    nativeInterface = null;
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
