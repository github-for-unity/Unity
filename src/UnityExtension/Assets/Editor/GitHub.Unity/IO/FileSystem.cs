using System.Collections.Generic;
using System.IO;

namespace GitHub.Unity
{
    class FileSystem : IFileSystem
    {
        public bool FileExists(string filename)
        {
            return File.Exists(filename);
        }

        public IEnumerable<string> GetDirectories(string gitHubLocalAppDataPath)
        {
            return Directory.GetDirectories(gitHubLocalAppDataPath);
        }

        public string GetTempPath()
        {
            return Path.GetTempPath();
        }
    }
}