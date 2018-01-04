using GitHub.Unity;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace GitHub.Unity
{
    class FileSystem : IFileSystem
    {
        private string currentDirectory;

        public FileSystem()
        {
        }

        /// <summary>
        /// Initialize the filesystem object with the path passed in set as the current directory
        /// </summary>
        /// <param name="directory">Current directory</param>
        public FileSystem(string directory)
        {
            this.currentDirectory = directory;
        }

        public void SetCurrentDirectory(string directory)
        {
            Debug.Assert(Path.IsPathRooted(directory));
            this.currentDirectory = directory;
        }

        public bool FileExists(string filename)
        {
            return File.Exists(filename);
        }

        public long FileLength(string path)
        {
            var fileInfo = new FileInfo(path);
            return fileInfo.Length;
        }

        public IEnumerable<string> GetDirectories(string path)
        {
            return Directory.GetDirectories(path);
        }

        public string GetTempPath()
        {
            return Path.GetTempPath();
        }

        public string Combine(string path1, string path2)
        {
            return Path.Combine(path1, path2);
        }

        public string Combine(string path1, string path2, string path3)
        {
            return Path.Combine(Path.Combine(path1, path2), path3);
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

        public bool ExistingPathIsDirectory(string path)
        {
            var attr = File.GetAttributes(path);
            return (attr & FileAttributes.Directory) == FileAttributes.Directory;
        }

        public string GetParentDirectory(string path)
        {
            return Directory.GetParent(path).FullName;
        }

        public IEnumerable<string> GetDirectories(string path, string pattern)
        {
            return Directory.GetDirectories(path, pattern);
        }

        public IEnumerable<string> GetDirectories(string path, string pattern, SearchOption searchOption)
        {
            return Directory.GetDirectories(path, pattern, searchOption);
        }

        public string ChangeExtension(string path, string extension)
        {
            return Path.ChangeExtension(path, extension);
        }

        public string GetFileNameWithoutExtension(string fileName)
        {
            return Path.GetFileNameWithoutExtension(fileName);
        }

        public IEnumerable<string> GetFiles(string path)
        {
            return Directory.GetFiles(path);
        }

        public IEnumerable<string> GetFiles(string path, string pattern)
        {
            return Directory.GetFiles(path, pattern);
        }

        public IEnumerable<string> GetFiles(string path, string pattern, SearchOption searchOption)
        {
            return Directory.GetFiles(path, pattern, searchOption);
        }

        public void WriteAllBytes(string path, byte[] bytes)
        {
            File.WriteAllBytes(path, bytes);
        }

        public void CreateDirectory(string toString)
        {
            Directory.CreateDirectory(toString);
        }

        public void FileCopy(string sourceFileName, string destFileName, bool overwrite)
        {
            File.Copy(sourceFileName, destFileName, overwrite);
        }

        public void FileDelete(string path)
        {
            File.Delete(path);
        }

        public void DirectoryDelete(string path, bool recursive)
        {
            Directory.Delete(path, recursive);
        }

        public void FileMove(string sourceFileName, string destFileName)
        {
            File.Move(sourceFileName, destFileName);
        }

        public void DirectoryMove(string toString, string s)
        {
            Directory.Move(toString, s);
        }

        public string GetCurrentDirectory()
        {
            if (currentDirectory != null)
                return currentDirectory;
            return Directory.GetCurrentDirectory();
        }

        public void WriteAllText(string path, string contents)
        {
            File.WriteAllText(path, contents);
        }

        public void WriteAllText(string path, string contents, Encoding encoding)
        {
            File.WriteAllText(path, contents, encoding);
        }

        public byte[] ReadAllBytes(string path)
        {
            return File.ReadAllBytes(path);
        }

        public string ReadAllText(string path)
        {
            return File.ReadAllText(path);
        }

        public string ReadAllText(string path, Encoding encoding)
        {
            return File.ReadAllText(path, encoding);
        }

        public void WriteAllLines(string path, string[] contents)
        {
            File.WriteAllLines(path, contents);
        }

        public string[] ReadAllLines(string path)
        {
            return File.ReadAllLines(path);
        }

        public char DirectorySeparatorChar
        {
            get { return Path.DirectorySeparatorChar; }
        }

        public string GetRandomFileName()
        {
            return Path.GetRandomFileName();
        }

        public Stream OpenRead(string path)
        {
            return File.OpenRead(path);
        }

        public Stream OpenWrite(string path, FileMode mode)
        {
            return new FileStream(path, mode);
        }
    }
}
