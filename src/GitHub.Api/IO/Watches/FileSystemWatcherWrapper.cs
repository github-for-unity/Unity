using System.IO;

namespace GitHub.Api
{
    class FileSystemWatcherWrapper : IFileSystemWatcherWrapper
    {
        private readonly string path;
        private readonly string filter;

        public event FileSystemEventHandler Changed;
        public event FileSystemEventHandler Created;
        public event FileSystemEventHandler Deleted;
        public event RenamedEventHandler Renamed;
        public event ErrorEventHandler Error;

        private FileSystemWatcher fileSystemWatcher;

        public FileSystemWatcherWrapper(string path, string filter = null, bool recursive = false)
        {
            this.path = path;
            this.filter = filter;
            fileSystemWatcher = filter == null ? new FileSystemWatcher(path) : new FileSystemWatcher(path, filter);
            fileSystemWatcher.IncludeSubdirectories = recursive;

            fileSystemWatcher.Changed += (sender, args) => Changed?.Invoke(sender, args);
            fileSystemWatcher.Created += (sender, args) => Created?.Invoke(sender, args);
            fileSystemWatcher.Deleted += (sender, args) => Deleted?.Invoke(sender, args);
            fileSystemWatcher.Error += (sender, args) => Error?.Invoke(sender, args);
            fileSystemWatcher.Renamed += (sender, args) => Renamed?.Invoke(sender, args);
        }

        public string Path
        {
            get { return fileSystemWatcher.Path; }
        }

        public string Filter
        {
            get { return fileSystemWatcher.Filter; }
        }

        public bool Recursive
        {
            get { return fileSystemWatcher.IncludeSubdirectories; }
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

        public override string ToString()
        {
            var filterString = filter == null ? "[NONE]" : string.Format("\"{0}\"" + "", filter);
            return string.Format("FileSystemWatch Path:\"{0}\" File:{1}", path, filterString);
        }
    }
}
