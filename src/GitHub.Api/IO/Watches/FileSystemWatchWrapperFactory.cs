namespace GitHub.Api
{
    class FileSystemWatchWrapperFactory : IFileSystemWatchWrapperFactory
    {
        public IFileSystemWatcherWrapper CreateWatch(string path, bool recursive = false, string filter = null)
        {
            return new FileSystemWatcherWrapper(path, filter, recursive);
        }
    }
}