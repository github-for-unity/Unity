namespace GitHub.Unity
{
    interface IFileSystemWatchFactory
    {
        IFileSystemWatch GetOrCreate(NPath path, bool recursive = false, string filter = null);
    }
}