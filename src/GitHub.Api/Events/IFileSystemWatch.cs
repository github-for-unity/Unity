using System;
using System.IO;

namespace GitHub.Unity
{
    interface IFileSystemWatch : IDisposable
    {
        bool Enable { get; set; }

        event Action<string> Changed;
        event Action<string> Created;
        event Action<string> Deleted;
        event Action<string, string> Renamed;
    }
}