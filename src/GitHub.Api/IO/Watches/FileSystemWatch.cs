using System.IO;

namespace GitHub.Api.IO
{
    class FileSystemWatch : IFileSystemWatch
    {
        public event FileSystemEventHandler Changed;
        public event FileSystemEventHandler Created;
        public event FileSystemEventHandler Deleted;
        public event RenamedEventHandler Renamed;
        public event ErrorEventHandler Error;

        private FileSystemWatcher fileSystemWatcher;

        public FileSystemWatch(string path, string filter = null)
        {
            fileSystemWatcher = filter == null ? new FileSystemWatcher(path) : new FileSystemWatcher(path, filter);
            fileSystemWatcher.Changed += (sender, args) => Changed?.Invoke(sender, args);
            fileSystemWatcher.Created += (sender, args) => Created?.Invoke(sender, args);
            fileSystemWatcher.Deleted += (sender, args) => Deleted?.Invoke(sender, args);
            fileSystemWatcher.Error += (sender, args) => Error?.Invoke(sender, args);
            fileSystemWatcher.Renamed += (sender, args) => Renamed?.Invoke(sender, args);
        }

        public bool Enable
        {
            get { return fileSystemWatcher.EnableRaisingEvents; }
            set { fileSystemWatcher.EnableRaisingEvents = value; }
        }

        public void Dispose()
        {
            fileSystemWatcher?.Dispose();
            fileSystemWatcher = null;
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
    }
}