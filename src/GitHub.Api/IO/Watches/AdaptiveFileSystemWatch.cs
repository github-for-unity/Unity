using System.Collections.Generic;
using System.IO;

namespace GitHub.Api
{
    class AdaptiveFileSystemWatch : FileSystemWatch
    {
        private readonly IFileSystem fileSystem;
        private readonly bool recursive;
        private readonly object lk = new object();
        private readonly IFileSystemWatcherWrapper watch;
        private readonly IFileSystemWatchWrapperFactory watchWrapperFactory;

        private bool enabled;

        private Dictionary<string, IFileSystemWatcherWrapper> watches =
            new Dictionary<string, IFileSystemWatcherWrapper>();

        public AdaptiveFileSystemWatch(IFileSystemWatchWrapperFactory watchWrapperFactory, IFileSystem fileSystem,
            string path, bool recursive = false, string filter = null)
        {
            this.watchWrapperFactory = watchWrapperFactory;
            this.fileSystem = fileSystem;
            this.recursive = recursive;
            watch = watchWrapperFactory.CreateWatch(path, false, filter);
            watch.AddListener(this);

            if (recursive)
            {
                ScanChildPaths(path);
            }
        }

        public override void OnCreate(object sender, FileSystemEventArgs e)
        {
            base.OnCreate(sender, e);
            if (!recursive)
            {
                return;
            }

            if (!fileSystem.ExistingPathIsDirectory(e.FullPath))
            {
                return;
            }

            Logger.Debug("Added Child Directory: {0}", e.FullPath);
            lock(lk)
            {
                var childWatch = watchWrapperFactory.CreateWatch(e.FullPath, false, watch.Filter);
                watches.Add(e.FullPath, childWatch);
                childWatch.AddListener(this);
            }
        }

        public override void OnDelete(object sender, FileSystemEventArgs e)
        {
            base.OnDelete(sender, e);

            lock(lk)
            {
                IFileSystemWatcherWrapper childWatch;
                if (!watches.TryGetValue(e.FullPath, out childWatch))
                {
                    return;
                }

                watches.Remove(e.FullPath);

                watch.RemoveListener(this);
                watch.Dispose();
            }

            Logger.Debug("Child Directory Deleted: {0}", e.FullPath);
        }

        public override void Dispose()
        {
            base.Dispose();

            if (watches == null)
            {
                return;
            }

            var watchList = watches;
            watches = null;

            foreach (var watcher in watchList.Values)
            {
                watcher.Dispose();
            }
        }

        private void ScanChildPaths(string path)
        {
            if (fileSystem.ExistingPathIsDirectory(path))
            {
                var directories = fileSystem.GetDirectories(path);
                foreach (var directory in directories)
                {
                    var childWatch = watchWrapperFactory.CreateWatch(directory, false, watch.Filter);
                    childWatch.AddListener(this);
                    watches.Add(directory, childWatch);
                    ScanChildPaths(directory);
                }
            }
        }

        public override bool Enable
        {
            get
            {
                lock(lk)
                {
                    return enabled;
                }
            }
            set
            {
                lock(lk)
                {
                    watch.Enable = value;
                    foreach (var watchesValue in watches.Values)
                    {
                        watchesValue.Enable = value;
                    }

                    enabled = value;
                }
            }
        }
    }
}
