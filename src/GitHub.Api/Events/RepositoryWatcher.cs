using System;
using System.Collections.Generic;
using System.Linq;

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
        event Action<string, string> LocalBranchMoved;
        event Action RepositoryChanged;
        event Action<string, string> RemoteBranchCreated;
        event Action<string, string> RemoteBranchDeleted;
        event Action<string, string> RemoteBranchChanged;
        event Action<string, string, string> RemoteBranchRenamed;
    }

    class CompositeDisposable : List<IDisposable>, IDisposable
    {
        public new void Remove(IDisposable item)
        {
            base.Remove(item);
            try
            {
                item.Dispose();
            }
            catch
            {}
        }

        public new void Clear()
        {
            foreach (var item in ToArray())
            {
                Remove(item);
            }
        }

        private bool disposed = false;
        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (disposed)
                {
                    throw new ObjectDisposedException(nameof(CompositeDisposable));
                }

                disposed = true;
                Clear();
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
    }

    class RepositoryWatcher : IRepositoryWatcher
    {
        private readonly CompositeDisposable disposables = new CompositeDisposable();
        //private readonly IFileSystemWatch fileHierarchyWatcher;
        private readonly IFileSystemWatch gitConfigWatcher;
        private readonly IFileSystemWatch gitHeadWatcher;
        private readonly IFileSystemWatch gitIndexWatcher;
        private readonly IFileSystemWatch localBranchesWatcher;
        private readonly Dictionary<string, IFileSystemWatch> remoteBranchesWatchers = new Dictionary<string, IFileSystemWatch>();
        private readonly IFileSystemWatch remotesDirWatcher;

        private bool running = false;

        public event Action<string> HeadChanged;
        public event Action IndexChanged;
        public event Action ConfigChanged;
        public event Action<string> LocalBranchCreated;
        public event Action<string> LocalBranchDeleted;
        public event Action<string, string> LocalBranchMoved;
        public event Action RepositoryChanged;
        public event Action<string, string> RemoteBranchCreated;
        public event Action<string, string> RemoteBranchDeleted;
        public event Action<string, string> RemoteBranchChanged;
        public event Action<string, string, string> RemoteBranchRenamed;

        public RepositoryWatcher(IPlatform platform, IRepositoryPathConfiguration repositoryPath)
        {
            //fileHierarchyWatcher = platform.FileSystemWatchFactory.GetOrCreate(repositoryPath, true);
            //fileHierarchyWatcher.Changed += f => {
            //    if (!ignore.Any(f.IsChildOf))
            //    {
            //        RepositoryChanged?.Invoke();
            //    }
            //};
            //fileHierarchyWatcher.Created += f => {
            //    if (!ignore.Any(f.IsChildOf))
            //    {
            //        RepositoryChanged?.Invoke();
            //    }
            //};
            //fileHierarchyWatcher.Deleted += f => {
            //    if (!ignore.Any(f.IsChildOf))
            //    {
            //        RepositoryChanged?.Invoke();
            //    }
            //};
            //fileHierarchyWatcher.Renamed += (f, __) => {
            //    if (!ignore.Any(f.IsChildOf))
            //    {
            //        RepositoryChanged?.Invoke();
            //    }
            //};

            gitConfigWatcher = platform.FileSystemWatchFactory.GetOrCreate(repositoryPath.DotGitConfig, false);
            gitConfigWatcher.Changed += _ => ConfigChanged?.Invoke();

            gitHeadWatcher = platform.FileSystemWatchFactory.GetOrCreate(repositoryPath.DotGitHead, false);
            gitHeadWatcher.Changed += s => HeadChanged?.Invoke(s.ReadAllLines().FirstOrDefault());

            gitIndexWatcher = platform.FileSystemWatchFactory.GetOrCreate(repositoryPath.DotGitIndex, false);
            gitIndexWatcher.Changed += _ => IndexChanged?.Invoke();

            localBranchesWatcher = platform.FileSystemWatchFactory.GetOrCreate(repositoryPath.BranchesPath, true);
            localBranchesWatcher.Created += s => LocalBranchCreated?.Invoke(s.RelativeTo(repositoryPath.BranchesPath).ToString(SlashMode.Forward));
            localBranchesWatcher.Deleted += s => LocalBranchDeleted?.Invoke(s.RelativeTo(repositoryPath.BranchesPath).ToString(SlashMode.Forward));

            localBranchesWatcher.Renamed += (o, n) => LocalBranchMoved?.Invoke(
                    o.RelativeTo(repositoryPath.BranchesPath).ToString(SlashMode.Forward),
                    n.RelativeTo(repositoryPath.BranchesPath).ToString(SlashMode.Forward));

            if (repositoryPath.RemotesPath.DirectoryExists())
            {
                foreach (var dir in repositoryPath.RemotesPath.Directories())
                {
                    var remote = dir.FileName;
                    AddRemoteBranchesWatcher(platform, dir, remote);
                }
            }
            else
            {
                remotesDirWatcher = platform.FileSystemWatchFactory.GetOrCreate(repositoryPath.RemotesPath.Parent, false);
                disposables.Add(remotesDirWatcher);
                remotesDirWatcher.Created += s =>
                {
                    if (s.RelativeTo(repositoryPath.RemotesPath.Parent) == "remotes")
                    {
                        remotesDirWatcher.Enable = false;
                        disposables.Remove(remotesDirWatcher);

                        foreach (var dir in repositoryPath.RemotesPath.Directories())
                        {
                            var remote = dir.FileName;
                            var watcher = AddRemoteBranchesWatcher(platform, dir, remote);
                            if (running)
                            {
                                watcher.Enable = true;
                            }
                        }
                    }
                };
            }

            //disposables.Add(fileHierarchyWatcher);
            disposables.Add(gitConfigWatcher);
            disposables.Add(gitHeadWatcher);
            disposables.Add(gitIndexWatcher);
            disposables.Add(localBranchesWatcher);
        }

        public void Start()
        {
            ToggleWatchers(true);
        }

        public void Stop()
        {
            ToggleWatchers(false);
        }

        private IFileSystemWatch AddRemoteBranchesWatcher(IPlatform platform, NPath dir, string remote)
        {
            var watcher = platform.FileSystemWatchFactory.GetOrCreate(dir, true, true);
            watcher.Created += f =>
            {
                RemoteBranchCreated?.Invoke(remote, f.RelativeTo(dir).ToString(SlashMode.Forward));
            };

            watcher.Deleted += f =>
            {
                RemoteBranchDeleted?.Invoke(remote, f.RelativeTo(dir).ToString(SlashMode.Forward));
            };

            watcher.Changed += f =>
            {
                var name = f.RelativeTo(dir).ToString(SlashMode.Forward);
                if (name != "HEAD")
                {
                    RemoteBranchChanged?.Invoke(remote, name);
                }
            };

            watcher.Renamed += (o, n) =>
            {
                RemoteBranchRenamed?.Invoke(remote, o.RelativeTo(dir).ToString(SlashMode.Forward),
                    n.RelativeTo(dir).ToString(SlashMode.Forward));
            };

            remoteBranchesWatchers.Add(dir, watcher);
            disposables.Add(watcher);
            return watcher;
        }

        private void RemoveRemoteBranchesWatcher(string remote)
        {
            IFileSystemWatch watcher = null;
            if (remoteBranchesWatchers.TryGetValue(remote, out watcher))
            {
                remoteBranchesWatchers.Remove(remote);
                watcher.Enable = false;
                disposables.Remove(watcher);
            }
        }

        private void ToggleWatchers(bool enable)
        {
            //fileHierarchyWatcher.Enable = enable;
            gitConfigWatcher.Enable = enable;
            gitHeadWatcher.Enable = enable;
            gitIndexWatcher.Enable = enable;
            localBranchesWatcher.Enable = enable;
            if (remotesDirWatcher != null)
            {
                remotesDirWatcher.Enable = enable;
            }

            foreach (var watcher in remoteBranchesWatchers.Values)
            {
                watcher.Enable = enable;
            }
        }

        private bool disposed = false;
        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (disposed)
                {
                    return;
                }

                disposed = true;
                ToggleWatchers(false);
                disposables.Dispose();
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }

        protected static ILogging Logger { get; } = Logging.GetLogger<RepositoryWatcher>();
    }
}
