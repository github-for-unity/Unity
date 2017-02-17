using System;
using System.Collections.Generic;
using System.IO;
using GitHub.Unity;

namespace GitHub.Api
{
    abstract class FileSystemWatchStrategyBase : IFileSystemWatchStrategy, IFileSystemWatchListener, IDisposable
    {
        private readonly object listenerLock = new object();
        private readonly List<IFileSystemWatchListener> listeners = new List<IFileSystemWatchListener>();
        protected ILogging Logger { get; }

        protected FileSystemWatchStrategyBase()
        {
            Logger = Logging.GetLogger(GetType());
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
                    Logger.Warning("Listener not found");
                    return;
                }

                Changed -= fileSystemWatchListener.OnChange;
                Created -= fileSystemWatchListener.OnCreate;
                Deleted -= fileSystemWatchListener.OnDelete;
                Renamed -= fileSystemWatchListener.OnRename;
                Error -= fileSystemWatchListener.OnError;
            }
        }

        public virtual void Dispose()
        {}

        public virtual void OnChange(object sender, FileSystemEventArgs e)
        {
            Changed?.Invoke(this, e);
        }

        public virtual void OnCreate(object sender, FileSystemEventArgs e)
        {
            Created?.Invoke(this, e);
        }

        public virtual void OnDelete(object sender, FileSystemEventArgs e)
        {
            Deleted?.Invoke(this, e);
        }

        public virtual void OnRename(object sender, RenamedEventArgs e)
        {
            Renamed?.Invoke(this, e);
        }

        public virtual void OnError(object sender, ErrorEventArgs e)
        {
            Error?.Invoke(this, e);
        }

        public abstract void Watch(string path, string filter = null);
        public abstract bool ClearWatch(string path);

        protected virtual event FileSystemEventHandler Changed;
        protected virtual event FileSystemEventHandler Created;
        protected virtual event FileSystemEventHandler Deleted;
        protected virtual event RenamedEventHandler Renamed;
        protected virtual event ErrorEventHandler Error;
    }
}