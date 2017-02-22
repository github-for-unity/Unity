namespace GitHub.Api
{
    interface IFileSystemWatchWrapperFactory
    {
        IFileSystemWatcherWrapper CreateWatch(string path, bool recursive = false, string filter = null);
    }
}