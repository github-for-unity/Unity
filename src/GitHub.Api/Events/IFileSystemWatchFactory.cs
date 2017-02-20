namespace GitHub.Unity
{
    interface IFileSystemWatchFactory
    {
        IFileSystemWatch GetOrCreate(NPath path, bool recursive = false, bool filesOnly = false);
    }
}