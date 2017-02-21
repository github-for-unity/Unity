using System;
using System.IO;
using GitHub.Unity;

namespace GitHub.Api
{
    abstract class FileSystemWatch : IFileSystemWatch, IFileSystemWatchListener, IDisposable
    {
        protected FileSystemWatch()
        {
            Logger = Logging.GetLogger(GetType());
        }

        public void AttachListener(IFileSystemWatchListener fileSystemWatchListener)
        {
            if (fileSystemWatchListener == null)
            {
                return;
            }

            Changed += fileSystemWatchListener.OnChange;
            Created += fileSystemWatchListener.OnCreate;
            Deleted += fileSystemWatchListener.OnDelete;
            Renamed += fileSystemWatchListener.OnRename;
            Error += fileSystemWatchListener.OnError;
        }

        public void RemoveListener(IFileSystemWatchListener fileSystemWatchListener)
        {
            if (fileSystemWatchListener == null)
            {
                return;
            }

            Changed -= fileSystemWatchListener.OnChange;
            Created -= fileSystemWatchListener.OnCreate;
            Deleted -= fileSystemWatchListener.OnDelete;
            Renamed -= fileSystemWatchListener.OnRename;
            Error -= fileSystemWatchListener.OnError;
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

        public event FileSystemEventHandler Changed;
        public event FileSystemEventHandler Created;
        public event FileSystemEventHandler Deleted;
        public event RenamedEventHandler Renamed;
        public event ErrorEventHandler Error;

        public abstract bool Enable { get; set; }
        protected ILogging Logger { get; }
    }
}
