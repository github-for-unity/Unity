using System.Collections.Generic;

namespace GitHub.Api
{
    interface IFileSystem
    {
        bool FileExists(string path);
        string Combine(string path1, string path2);
        string Combine(string path1, string path2, string path3);
        string GetFullPath(string path);
        IEnumerable<string> GetDirectories(string path);
        string GetTempPath();
        string GetDirectoryName(string path);
        bool DirectoryExists(string path);
        string GetParentDirectory(string path);
        string GetRandomFileName();
        void CreateDirectory(string path);
        string ReadAllText(string path);
        void DeleteAllFiles(string path);
    }
}