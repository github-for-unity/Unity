using System.Collections.Generic;

namespace GitHub.Unity
{
    interface IFileSystem
    {
        bool FileExists(string filename);
        string Combine(string path1, string path2);
        string GetFullPath(string path);
        IEnumerable<string> GetDirectories(string gitHubLocalAppDataPath);
        string GetTempPath();
        string GetDirectoryName(string path);
        bool DirectoryExists(string path);
        string GetParentDirectory(string path);
    }
}