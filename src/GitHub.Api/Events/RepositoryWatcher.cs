using System;
using System.Linq;
using System.Threading;
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
        private readonly NPath branchesPath;
        private readonly NPath dotGitConfig;
        private readonly NPath dotGitHead;
        private readonly NPath dotGitIndex;
        private readonly NPath dotGitPath;
        private readonly NPath[] ignoredPaths;
        private readonly NPath remotesPath;

        private bool disposed;
        private NativeInterface nativeInterface;
        private bool running;
        private Thread thread;

        public RepositoryWatcher(IPlatform platform, NPath repositoryPath, NPath dotGitPath,
            NPath dotGitIndex, NPath dotGitHead, NPath branchesPath, NPath remotesPath, NPath dotGitConfig)
        {
            this.dotGitPath = dotGitPath;
            this.dotGitIndex = dotGitIndex;
            this.dotGitHead = dotGitHead;
            this.branchesPath = branchesPath;
            this.remotesPath = remotesPath;
            this.dotGitConfig = dotGitConfig;

            ignoredPaths = new[] {
                platform.Environment.UnityProjectPath.ToNPath().Combine("Library"),
                platform.Environment.UnityProjectPath.ToNPath().Combine("Temp")
            };

            nativeInterface = new NativeInterface(repositoryPath);
            thread = new Thread(ThreadLoop);
        }

        public void Start()
        {
            running = true;
            thread.Start();
        }

        public void Stop()
        {
            running = false;
        }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            disposed = true;

            nativeInterface.Dispose();
            nativeInterface = null;
        }

        public event Action<string> HeadChanged;
        public event Action IndexChanged;
        public event Action ConfigChanged;
        public event Action<string> LocalBranchCreated;
        public event Action<string> LocalBranchDeleted;
        public event Action RepositoryChanged;
        public event Action<string, string> RemoteBranchCreated;
        public event Action<string, string> RemoteBranchDeleted;

        private void ThreadLoop()
        {
            while (running)
            {
                foreach (var fileEvent in nativeInterface.GetEvents())
                {
                    var file = new NPath(fileEvent.Directory).Combine(fileEvent.FileA);
                    if (file.IsChildOf(dotGitPath))
                    {
                        if (file.Equals(dotGitConfig))
                        {
                            ConfigChanged?.Invoke();
                        }
                        else if (file.Equals(dotGitHead))
                        {
                            string headContent = null;
                            if (fileEvent.Type != EventType.DELETED)
                            {
                                headContent = dotGitHead.ReadAllLines().FirstOrDefault();
                            }

                            HeadChanged?.Invoke(headContent);
                        }
                        else if (file.Equals(dotGitIndex))
                        {
                            IndexChanged?.Invoke();
                        }
                        else if (file.IsChildOf(remotesPath))
                        {
                            if (file.ExtensionWithDot == ".lock")
                            {
                                continue; 
                            }

                            var relativePath = file.RelativeTo(remotesPath);
                            var relativePathElements = relativePath.Elements.ToArray();

                            if (relativePathElements.Length > 0)
                            {
                                var origin = relativePathElements[0];
                                var branch = string.Join(@"/", relativePathElements.Skip(1).ToArray());

                                switch (fileEvent.Type)
                                {
                                    case EventType.DELETED:
                                        RemoteBranchDeleted?.Invoke(origin, branch);
                                        break;

                                    case EventType.CREATED:

                                        break;

                                    case EventType.MODIFIED:

                                        break;

                                    case EventType.RENAMED:

                                        break;

                                    default:
                                        throw new ArgumentOutOfRangeException();
                                }
                            }
                        }
                        else if (file.IsChildOf(branchesPath))
                        {
                            if (file.ExtensionWithDot == ".lock")
                            {
                                continue;
                            }

                            var relativePath = file.RelativeTo(branchesPath);
                            var relativePathElements = relativePath.Elements.ToArray();

                            var branch = string.Join(@"/", relativePathElements.ToArray());

                            switch (fileEvent.Type)
                            {
                                case EventType.DELETED:
                                    LocalBranchDeleted?.Invoke(branch);
                                    break;

                                case EventType.CREATED:

                                    break;

                                case EventType.MODIFIED:

                                    break;

                                case EventType.RENAMED:

                                    break;

                                default:
                                    throw new ArgumentOutOfRangeException();
                            }
                        }
                    }
                    else
                    {
                        if (ignoredPaths.Any(ignoredPath => file.IsChildOf(ignoredPath)))
                        {
                            continue;
                        }

                        RepositoryChanged?.Invoke();
                    }
                }

                Thread.Sleep(200);
            }
        }

        protected static ILogging Logger { get; } = Logging.GetLogger<RepositoryWatcher>();
    }
}
