using System.Collections.Generic;

namespace GitHub.Unity
{
    interface IFileSystem
    {
        bool FileExists(string filename);
        IEnumerable<string> GetDirectories(string gitHubLocalAppDataPath);
        string GetTempPath();
    }
}