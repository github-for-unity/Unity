using System.Collections.Generic;
using System.IO;

namespace GitHub.Api
{
    interface IFileSystem
    {
        bool FileExists(string path);
        string Combine(string path1, string path2);
        string Combine(string path1, string path2, string path3);
        string GetFullPath(string path);
        IEnumerable<string> GetDirectories(string path);
        IEnumerable<string> GetDirectories(string path, string pattern, SearchOption searchOption);
        string GetTempPath();
        string GetDirectoryName(string path);
        bool DirectoryExists(string path);
        string GetParentDirectory(string path);
        string GetRandomFileName();
        string ChangeExtension(string path, string extension);
        string GetFileNameWithoutExtension(string fileName);
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
        string ReadAllText(string path);
        void WriteAllLines(string path, string[] contents);
        string[] ReadAllLines(string path);
        IEnumerable<string> GetDirectories(string path, string pattern);
        char DirectorySeparatorChar { get; }
    }
}