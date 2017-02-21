using System;
using System.IO;

namespace GitHub.Api
{
    interface IFileSystemWatcherWrapper : IDisposable
    {
        event FileSystemEventHandler Changed;
        event FileSystemEventHandler Created;
        event FileSystemEventHandler Deleted;
        event RenamedEventHandler Renamed;
        event ErrorEventHandler Error;
        bool Enable { get; set; }
        string Path { get; }
        string Filter { get; }
        bool Recursive { get; }
        void AddListener(IFileSystemWatchListener fileSystemWatchListener);
        void RemoveListener(IFileSystemWatchListener fileSystemWatchListener);
    }
}