using System;
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
        event Action<string> LocalBranchCreated;
        event Action<string> LocalBranchDeleted;
        event Action RepositoryChanged;
        event Action<string, string> RemoteBranchCreated;
        event Action<string, string> RemoteBranchDeleted;
    }

    class RepositoryWatcher : IRepositoryWatcher
    {
        private readonly RepositoryPathConfiguration paths;
        private readonly CancellationToken cancellationToken;
        private readonly NPath[] ignoredPaths;
        private readonly Task task;
        private readonly AutoResetEvent autoResetEvent;

        private NativeInterface nativeInterface;
        private bool running;

        public event Action<string> HeadChanged;
        public event Action IndexChanged;
        public event Action ConfigChanged;
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
                platform.Environment.UnityProjectPath.ToNPath().Combine("Library"),
                platform.Environment.UnityProjectPath.ToNPath().Combine("Temp")
            };

            var pathsRepositoryPath = paths.RepositoryPath.ToString();
            Logger.Trace("Watching Path: \"{0}\"", pathsRepositoryPath);

            nativeInterface = new NativeInterface(pathsRepositoryPath);
            task = new Task(WatcherLoop);
            autoResetEvent = new AutoResetEvent(false);
        }

        public void Start()
        {
            running = true;
            task.Start(TaskScheduler.Current);
        }

        public void Stop()
        {
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

                foreach (var fileEvent in nativeInterface.GetEvents())
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

                    var eventDirectory = new NPath(fileEvent.Directory);
                    var fileA = eventDirectory.Combine(fileEvent.FileA);

                    NPath fileB = null;
                    if (fileEvent.FileB != null)
                    {
                        fileB = eventDirectory.Combine(fileEvent.FileB);
                    }

                    //Logger.Trace("FileEvent: {0} \"{1}\"", fileEvent.Type.ToString(), fileA.ToString(),
                    //  fileB?.ToString() != null ? "\"" + fileB + "\"" : "[NULL]");

                    // handling events in .git/*
                    if (fileA.IsChildOf(paths.DotGitPath))
                    {
                        HandleEventInDotGit(fileEvent, fileA, fileB);
                    }
                    else
                    {
                        if (ignoredPaths.Any(ignoredPath => fileA.IsChildOf(ignoredPath)))
                        {
                            continue;
                        }

                        Logger.Debug("RepositoryChanged {0}: {1} {2}", fileEvent.Type, fileA.ToString(),
                            fileB?.ToString() ?? "[NULL]");

                        RepositoryChanged?.Invoke();
                    }
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
                if (fileA.ExtensionWithDot == ".lock")
                {
                    return;
                }

                var relativePath = fileA.RelativeTo(paths.RemotesPath);
                var relativePathElements = relativePath.Elements.ToArray();

                if (fileEvent.Type == EventType.DELETED && relativePathElements.Length > 1)
                {
                    var origin = relativePathElements[0];
                    var branch = string.Join(@"/", relativePathElements.Skip(1).ToArray());

                    Logger.Debug("RemoteBranchDeleted: {0}", branch);
                    RemoteBranchDeleted?.Invoke(origin, branch);
                }
            }
            else if (fileA.IsChildOf(paths.BranchesPath))
            {
                // when a branch is created, we detect it by looking for a rename event
                // from a branch-name.lock file to a branch-name file
                if (fileA.ExtensionWithDot == ".lock" && fileEvent.Type != EventType.RENAMED)
                {
                    return;
                }

                if (fileEvent.Type == EventType.DELETED)
                {
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
