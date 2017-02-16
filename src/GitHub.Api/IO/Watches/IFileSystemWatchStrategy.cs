namespace GitHub.Api
{
    interface IFileSystemWatchStrategy
    {
        void Watch(string path, string filter = null);
        void ClearWatch(string path, string filter = null);

        void AddListener(IFileSystemWatchListener fileSystemWatchListener);
        void RemoveListener(IFileSystemWatchListener fileSystemWatchListener);
    }
}