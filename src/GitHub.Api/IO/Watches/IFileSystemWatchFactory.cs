namespace GitHub.Api
{
    interface IFileSystemWatchFactory
    {
        FileSystemWatch CreteFileSystemWatch(string path, bool recursive = false, string filter = null);
    }
}