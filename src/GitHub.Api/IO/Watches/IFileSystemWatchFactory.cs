namespace GitHub.Api
{
    interface IFileSystemWatchFactory
    {
        IFileSystemWatch CreateWatch(string path);
        IFileSystemWatch CreateWatch(string path, string filter);
    }
}