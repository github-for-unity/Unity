using System;

namespace GitHub.Unity
{
    interface IFileSystemWatch : IDisposable
    {
        bool Enable { get; set; }

        event Action<NPath> Changed;
        event Action<NPath> Created;
        event Action<NPath> Deleted;
        event Action<NPath, NPath> Renamed;
    }
}