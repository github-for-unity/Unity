namespace GitHub.Api.IO
{
    class FileSystemWatchFactory : IFileSystemWatchFactory
    {
        public FileSystemWatchFactory()
        {}

        public IFileSystemWatch CreateWatch(string path)
        {
            return new FileSystemWatch(path);
        }

        public IFileSystemWatch CreateWatch(string path, string filter)
        {
            return new FileSystemWatch(path, filter);
        }
    }
}