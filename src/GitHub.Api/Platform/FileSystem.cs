using System.Collections.Generic;
using System.IO;

namespace GitHub.Api
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

        public string Combine(string path1, string path2)
        {
            return Path.Combine(path1, path2);
        }

        public string GetFullPath(string path)
        {
            return Path.GetFullPath(path);
        }

        public string GetDirectoryName(string path)
        {
            return Path.GetDirectoryName(path);
        }

        public bool DirectoryExists(string path)
        {
            return Directory.Exists(path);
        }

        public string GetParentDirectory(string path)
        {
            return Directory.GetParent(path).FullName;
        }

        public string GetRandomFileName()
        {
            return Path.GetRandomFileName();
        }

        public void CreateDirectory(string path)
        {
            Directory.CreateDirectory(path);
        }
    }
}