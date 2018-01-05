using System.Collections.Generic;
using System.IO;
using System.Text;

namespace GitHub.Unity
{
    public interface IFileSystem
    {
        bool FileExists(string path);
        long FileLength(string path);
        string Combine(string path1, string path2);
        string Combine(string path1, string path2, string path3);
        string GetFullPath(string path);
        IEnumerable<string> GetDirectories(string path);
        IEnumerable<string> GetDirectories(string path, string pattern);
        IEnumerable<string> GetDirectories(string path, string pattern, SearchOption searchOption);
        string GetTempPath();
        string GetDirectoryName(string path);
        bool DirectoryExists(string path);
        string GetParentDirectory(string path);
        string GetRandomFileName();
        string ChangeExtension(string path, string extension);
        string GetFileNameWithoutExtension(string fileName);
        IEnumerable<string> GetFiles(string path);
        IEnumerable<string> GetFiles(string path, string pattern);
        IEnumerable<string> GetFiles(string path, string pattern, SearchOption searchOption);
        void WriteAllBytes(string path, byte[] bytes);
        void CreateDirectory(string path);
        void FileCopy(string sourceFileName, string destFileName, bool overwrite);
        void FileDelete(string path);
        void DirectoryDelete(string path, bool recursive);
        void FileMove(string sourceFileName, string s);
        void DirectoryMove(string toString, string s);
        string GetCurrentDirectory();
        void WriteAllText(string path, string contents);
        void WriteAllText(string path, string contents, Encoding encoding);
        void WriteAllLines(string path, string[] contents);
        byte[] ReadAllBytes(string path);
        string ReadAllText(string path);
        string ReadAllText(string path, Encoding encoding);
        Stream OpenRead(string path);
        Stream OpenWrite(string path, FileMode mode);
        string[] ReadAllLines(string path);
        char DirectorySeparatorChar { get; }
        bool ExistingPathIsDirectory(string path);
        void SetCurrentDirectory(string currentDirectory);
    }
}