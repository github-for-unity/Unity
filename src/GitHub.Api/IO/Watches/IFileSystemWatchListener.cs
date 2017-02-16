using System.IO;

namespace GitHub.Api.IO
{
    interface IFileSystemWatchListener
    {
        void OnChange(object sender, FileSystemEventArgs e);
        void OnCreate(object sender, FileSystemEventArgs e);
        void OnDelete(object sender, FileSystemEventArgs e);
        void OnRename(object sender, RenamedEventArgs e);
        void OnError(object sender, ErrorEventArgs e);
    }
}