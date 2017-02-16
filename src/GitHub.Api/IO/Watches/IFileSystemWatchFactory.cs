namespace GitHub.Api.IO
{
    interface IFileSystemWatchFactory
    {
        IFileSystemWatch CreateWatch(string path);
        IFileSystemWatch CreateWatch(string path, string filter);
    }
}