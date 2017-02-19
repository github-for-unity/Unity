using System;
using System.IO;
using GitHub.Api;

namespace GitHub.Unity.Tests
{
    class TestFileSystemWatcherWrapper : IFileSystemWatcherWrapper
    {
        public TestFileSystemWatcherWrapper(string path, bool recursive = false, string filter = null)
        {
            Path = path;
            Recursive = recursive;
            Filter = filter;
        }

        public void Dispose()
        {}

        public void RaiseChanged(string name)
        {
            Changed?.Invoke(this, new FileSystemEventArgs(WatcherChangeTypes.Changed, Path, name));
        }

        public void RaiseCreated(string name)
        {
            Created?.Invoke(this, new FileSystemEventArgs(WatcherChangeTypes.Created, Path, name));
        }

        public void RaiseDeleted(string name)
        {
            Deleted?.Invoke(this, new FileSystemEventArgs(WatcherChangeTypes.Deleted, Path, name));
        }

        public void RaiseRenamed(string name, string oldName)
        {
            Renamed?.Invoke(this, new RenamedEventArgs(WatcherChangeTypes.Renamed, Path, name, oldName));
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

        public override string ToString()
        {
            return Filter == null
                ? string.Format("TestFileSystemWatch Path:\"{0}\" Recursive:{1}", Path, Recursive)
                : string.Format("TestFileSystemWatch Path:\"{0}\" Recursive:{1} Filter:\"{2}\"", Path, Recursive, Filter);
        }

        public event FileSystemEventHandler Changed;
        public event FileSystemEventHandler Created;
        public event FileSystemEventHandler Deleted;
        public event RenamedEventHandler Renamed;
        public event ErrorEventHandler Error;

        public string Path { get; }
        public string Filter { get; }
        public bool Enable { get; set; }
        public bool Recursive { get; }
    }
}
