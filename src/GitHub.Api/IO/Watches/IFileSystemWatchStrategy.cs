using System.IO;

namespace GitHub.Api.IO
{
    interface IFileSystemWatchStrategy
    {
        void Watch(string path, string filter = null);
        void ClearWatch(string path, string filter = null);
        bool IsWatched(string path, string filter = null);

        void AddListener(IFileSystemWatchListener fileSystemWatchListener);
        void RemoveListener(IFileSystemWatchListener fileSystemWatchListener);
    }
}