using System.IO;

namespace GitHub.Api
{
    interface IFileSystemWatch
    {
        void AttachListener(IFileSystemWatchListener fileSystemWatchListener);
        void RemoveListener(IFileSystemWatchListener fileSystemWatchListener);
        event FileSystemEventHandler Changed;
        event FileSystemEventHandler Created;
        event FileSystemEventHandler Deleted;
        event RenamedEventHandler Renamed;
        event ErrorEventHandler Error;
        bool Enable { get; set; }
    }
}
