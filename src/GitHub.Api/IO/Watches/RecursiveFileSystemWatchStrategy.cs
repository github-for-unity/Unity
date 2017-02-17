using System;
using System.Collections.Generic;
using System.IO;

namespace GitHub.Api
{
    class RecursiveFileSystemWatchStrategy : FileSystemWatchStrategyBase
    {
        private readonly IFileSystem fileSystem;
        private readonly object watchesLock = new object();
        private readonly IFileSystemWatchFactory watchFactory;

        private Dictionary<string, IFileSystemWatch> watches = new Dictionary<string, IFileSystemWatch>();

        public RecursiveFileSystemWatchStrategy(IFileSystemWatchFactory watchFactory, IFileSystem fileSystem)
        {
            this.watchFactory = watchFactory;
            this.fileSystem = fileSystem;
        }

        public override void Watch(string path, string filter = null)
        {
            Guard.ArgumentNotNull(path, nameof(path));

            var key = new WatchArguments { Path = path, Filter = filter };

            IFileSystemWatch watch;
            lock(watchesLock)
            {
                if (watches.ContainsKey(path))
                {
                    throw new Exception("path and filter combination already watched");
                }

                Logger.Debug("Watching Path:{0} Filter:{1}", path, filter == null ? "[NONE]" : filter);

                if (filter != null)
                {
                    watch = watchFactory.CreateWatch(path, filter);
                }
                else
                {
                    watch = watchFactory.CreateWatch(path);
                }

                watches.Add(path, watch);
            }

            watch.AddListener(this);
            if (fileSystem.ExistingPathIsDirectory(path))
            {
                var directories = fileSystem.GetDirectories(path);
                foreach (var directory in directories)
                {
                    Watch(directory, filter);
                }
            }
        }

        public override bool ClearWatch(string path)
        {
            lock(watchesLock)
            {
                IFileSystemWatch value;
                if (!watches.TryGetValue(path, out value))
                {
                    return false;
                }

                Logger.Debug("Clearing Watch:{0}", path);

                watches.Remove(path);

                value.Enable = false;
                value.RemoveListener(this);
                value.Dispose();
                return true;
            }
        }

        public override void OnCreate(object sender, FileSystemEventArgs e)
        {
            base.OnCreate(sender, e);
            if (!fileSystem.ExistingPathIsDirectory(e.FullPath))
            {
                return;
            }

            Logger.Debug("Added Child Directory: {0}", e.FullPath);
            Watch(e.FullPath);
        }

        public override void OnDelete(object sender, FileSystemEventArgs e)
        {
            base.OnDelete(sender, e);

            Logger.Debug("Path Deleted: {0}", e.FullPath);

            ClearWatch(e.FullPath);
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
    }
}
