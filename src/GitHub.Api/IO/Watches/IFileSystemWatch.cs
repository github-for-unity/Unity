using System;
using System.IO;

namespace GitHub.Api
{
    interface IFileSystemWatch : IDisposable
    {
        event FileSystemEventHandler Changed;
        event FileSystemEventHandler Created;
        event FileSystemEventHandler Deleted;
        event RenamedEventHandler Renamed;
        event ErrorEventHandler Error;
        bool Enable { get; set; }
        void AddListener(IFileSystemWatchListener fileSystemWatchListener);
        void RemoveListener(IFileSystemWatchListener fileSystemWatchListener);
    }
}