using System.IO;

namespace GitHub.Unity
{
    class FileSystem : IFileSystem
    {
        public bool FileExists(string filename)
        {
            return File.Exists(filename);
        }
    }
}