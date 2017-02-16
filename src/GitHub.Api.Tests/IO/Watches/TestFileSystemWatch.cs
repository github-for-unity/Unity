using System;
using System.IO;
using GitHub.Api;

namespace GitHub.Unity.Tests
{
    class TestFileSystemWatch : IFileSystemWatch
    {
        public void Dispose()
        {}

        public void RaiseChanged(string directory, string name)
        {
            Changed?.Invoke(this, new FileSystemEventArgs(WatcherChangeTypes.Changed, directory, name));
        }

        public void RaiseCreated(string directory, string name)
        {
            Created?.Invoke(this, new FileSystemEventArgs(WatcherChangeTypes.Created, directory, name));
        }

        public void RaiseDeleted(string directory, string name)
        {
            Deleted?.Invoke(this, new FileSystemEventArgs(WatcherChangeTypes.Deleted, directory, name));
        }

        public void RaiseRenamed(string directory, string name, string oldName)
        {
            Renamed?.Invoke(this, new RenamedEventArgs(WatcherChangeTypes.Renamed, directory, name, oldName));
        }

        public void RaiseError(Exception exception)
        {
            Error?.Invoke(this, new ErrorEventArgs(exception));
        }

        public void AddListener(IFileSystemWatchListener fileSystemWatchListener)
        {
            Changed += fileSystemWatchListener.OnChange;
            Created += fileSystemWatchListener.OnCreate;
            Deleted += fileSystemWatchListener.OnDelete;
            Renamed += fileSystemWatchListener.OnRename;
            Error += fileSystemWatchListener.OnError;
        }

        public void RemoveListener(IFileSystemWatchListener fileSystemWatchListener)
        {
            Changed -= fileSystemWatchListener.OnChange;
            Created -= fileSystemWatchListener.OnCreate;
            Deleted -= fileSystemWatchListener.OnDelete;
            Renamed -= fileSystemWatchListener.OnRename;
            Error -= fileSystemWatchListener.OnError;
        }

        public event FileSystemEventHandler Changed;
        public event FileSystemEventHandler Created;
        public event FileSystemEventHandler Deleted;
        public event RenamedEventHandler Renamed;
        public event ErrorEventHandler Error;

        public bool Enable { get; set; }
    }
}
