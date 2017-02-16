using System;
using System.Collections.Generic;
using System.IO;
using GitHub.Unity;

namespace GitHub.Api.IO
{
    class DefaultFileSystemWatchStrategy : IFileSystemWatchStrategy, IFileSystemWatchListener, IDisposable
    {
        private static ILogging logger = Logging.GetLogger<DefaultFileSystemWatchStrategy>();

        private readonly List<IFileSystemWatchListener> listeners = new List<IFileSystemWatchListener>();
        private Dictionary<PathAndFilter, IFileSystemWatch> watches =
            new Dictionary<PathAndFilter, IFileSystemWatch>();

        private readonly IFileSystemWatchFactory watchFactory;
        private object listenerLock = new object();
        private object watchesLock = new object();

        public DefaultFileSystemWatchStrategy(IFileSystemWatchFactory watchFactory)
        {
            this.watchFactory = watchFactory;
        }

        public void Watch(string path, string filter = null)
        {
            Guard.ArgumentNotNull(path, nameof(path));

            var key = new PathAndFilter { Path = path, Filter = filter };

            IFileSystemWatch watch;
            lock(watchesLock)
            {
                if (watches.ContainsKey(key))
                {
                    throw new Exception("path and filter combination already watched");
                }

                logger.Debug("Watching Path:{0} Filter:{1}", path, filter == null ? "[NONE]" : filter);

                watch = watchFactory.CreateWatch(path, filter);
                watches.Add(key, watch);
            }

            watch.AddListener(this);
        }

        public void ClearWatch(string path, string filter = null)
        {
            var key = new PathAndFilter { Path = path, Filter = filter };

            IFileSystemWatch value;
            lock(watchesLock)
            {
                if (!watches.TryGetValue(key, out value))
                {
                    throw new Exception("path and filter combination not watched");
                }

                watches.Remove(key);

                value.Enable = false;
                value.RemoveListener(this);
                value.Dispose();
            }
        }

        public bool IsWatched(string path, string filter = null)
        {
            var key = new PathAndFilter { Path = path, Filter = filter };
            lock(watchesLock)
            {
                return watches.ContainsKey(key);
            }
        }

        public void AddListener(IFileSystemWatchListener fileSystemWatchListener)
        {
            lock(listenerLock)
            {
                listeners.Add(fileSystemWatchListener);
                Changed += fileSystemWatchListener.OnChange;
                Created += fileSystemWatchListener.OnCreate;
                Deleted += fileSystemWatchListener.OnDelete;
                Renamed += fileSystemWatchListener.OnRename;
                Error += fileSystemWatchListener.OnError;
            }
        }

        public void RemoveListener(IFileSystemWatchListener fileSystemWatchListener)
        {
            lock(listenerLock)
            {
                var removed = listeners.Remove(fileSystemWatchListener);
                if (!removed)
                {
                    logger.Warning("Listener not found");
                    return;
                }

                Changed -= fileSystemWatchListener.OnChange;
                Created -= fileSystemWatchListener.OnCreate;
                Deleted -= fileSystemWatchListener.OnDelete;
                Renamed -= fileSystemWatchListener.OnRename;
                Error -= fileSystemWatchListener.OnError;
            }
        }

        public void OnChange(object sender, FileSystemEventArgs e)
        {
            Changed?.Invoke(this, e);
        }

        public void OnCreate(object sender, FileSystemEventArgs e)
        {
            Created?.Invoke(this, e);
        }

        public void OnDelete(object sender, FileSystemEventArgs e)
        {
            Deleted?.Invoke(this, e);
        }

        public void OnRename(object sender, RenamedEventArgs e)
        {
            Renamed?.Invoke(this, e);
        }

        public void OnError(object sender, ErrorEventArgs e)
        {
            Error?.Invoke(this, e);
        }

        public void Dispose()
        {
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

        private struct PathAndFilter
        {
            public string Path;
            public string Filter;
        }

        private event FileSystemEventHandler Changed;
        private event FileSystemEventHandler Created;
        private event FileSystemEventHandler Deleted;
        private event RenamedEventHandler Renamed;
        private event ErrorEventHandler Error;
    }
}
