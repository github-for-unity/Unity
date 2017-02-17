namespace GitHub.Api
{
    interface IFileSystemWatchStrategy
    {
        void Watch(string path, string filter = null);
        bool ClearWatch(string path);
        void AddListener(IFileSystemWatchListener fileSystemWatchListener);
        void RemoveListener(IFileSystemWatchListener fileSystemWatchListener);
    }
}