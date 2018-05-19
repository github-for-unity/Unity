using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace GitHub.Unity
{
    public interface IFileSystem
    {
        string ChangeExtension(string path, string extension);
        string Combine(string path1, string path2);
        string Combine(string path1, string path2, string path3);
        void DirectoryCreate(string path);
        void DirectoryDelete(string path, bool recursive);
        bool DirectoryExists(string path);
        void DirectoryMove(string toString, string s);
        bool ExistingPathIsDirectory(string path);
        void FileCopy(string sourceFileName, string destFileName, bool overwrite);
        void FileDelete(string path);
        bool FileExists(string path);
        void FileMove(string sourceFileName, string s);
        string GetCurrentDirectory();
        IEnumerable<string> GetDirectories(string path);
        IEnumerable<string> GetDirectories(string path, string pattern);
        IEnumerable<string> GetDirectories(string path, string pattern, SearchOption searchOption);
        string GetDirectoryName(string path);
        string GetFileNameWithoutExtension(string fileName);
        IEnumerable<string> GetFiles(string path);
        IEnumerable<string> GetFiles(string path, string pattern);
        IEnumerable<string> GetFiles(string path, string pattern, SearchOption searchOption);
        string GetFullPath(string path);
        string GetParentDirectory(string path);
        string GetRandomFileName();
        string GetTempPath();
        Stream OpenRead(string path);
        Stream OpenWrite(string path, FileMode mode);
        byte[] ReadAllBytes(string path);
        string[] ReadAllLines(string path);
        string ReadAllText(string path);
        string ReadAllText(string path, Encoding encoding);
        void SetCurrentDirectory(string currentDirectory);
        void WriteAllBytes(string path, byte[] bytes);
        void WriteAllLines(string path, string[] contents);
        void WriteAllText(string path, string contents);
        void WriteAllText(string path, string contents, Encoding encoding);
        void WriteLines(string path, string[] contents);

        char DirectorySeparatorChar { get; }
    }


    public class FileSystem : IFileSystem
    {
        private string currentDirectory;

        public FileSystem()
        { }

        /// <summary>
        /// Initialize the filesystem object with the path passed in set as the current directory
        /// </summary>
        /// <param name="directory">Current directory</param>
        public FileSystem(string directory)
        {
            currentDirectory = directory;
        }

        public void SetCurrentDirectory(string directory)
        {
            if (!Path.IsPathRooted(directory))
                throw new ArgumentException("SetCurrentDirectory requires a rooted path", "directory");
            currentDirectory = directory;
        }

        public bool FileExists(string filename)
        {
            return File.Exists(filename);
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
            return GetFiles(path, "*");
        }

        public IEnumerable<string> GetFiles(string path, string pattern)
        {
            return Directory.GetFiles(path, pattern);
        }

        public IEnumerable<string> GetFiles(string path, string pattern, SearchOption searchOption)
        {
            foreach (var file in GetFiles(path, pattern))
                yield return file;

            if (searchOption != SearchOption.AllDirectories)
                yield break;

#if ENABLE_MONO
            if (NPath.IsLinux)
            {
                try
                {
                    path = Mono.Unix.UnixPath.GetCompleteRealPath(path);
                }
                catch
                {}
            }
#endif
            foreach (var dir in GetDirectories(path))
            {
                var realdir = dir;
#if ENABLE_MONO
                if (NPath.IsLinux)
                {
                    try
                    {
                        realdir = Mono.Unix.UnixPath.GetCompleteRealPath(dir);
                    }
                    catch
                    {}
                }
#endif
                if (path != realdir)
                {
                    foreach (var file in GetFiles(dir, pattern, searchOption))
                        yield return file;
                }
            }
        }

        public byte[] ReadAllBytes(string path)
        {
            return File.ReadAllBytes(path);
        }

        public void WriteAllBytes(string path, byte[] bytes)
        {
            File.WriteAllBytes(path, bytes);
        }

        public void DirectoryCreate(string toString)
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

        public void WriteLines(string path, string[] contents)
        {
            using (var fs = File.AppendText(path))
            {
                foreach (var line in contents)
                    fs.WriteLine(line);
            }
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

        public char DirectorySeparatorChar
        {
            get { return Path.DirectorySeparatorChar; }
        }
    }
}
