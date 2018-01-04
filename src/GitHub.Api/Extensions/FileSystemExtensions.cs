using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace GitHub.Unity
{
    static class FileSystemExtensions
    {
        public static string CalculateFileMD5(this IFileSystem fileSystem, string file)
        {
            byte[] computeHash;
            using (var md5 = MD5.Create())
            {
                using (var stream = fileSystem.OpenRead(file))
                {
                    computeHash = md5.ComputeHash(stream);
                }
            }

            return BitConverter.ToString(computeHash).Replace("-", string.Empty).ToLower();
        }

        public static string CalculateFolderMD5(this IFileSystem fileSystem, string path, bool includeContents = true)
        {
            //https://stackoverflow.com/questions/3625658/creating-hash-for-folder

            var filePaths = fileSystem.GetFiles(path, "*", SearchOption.AllDirectories)
                .OrderBy(p => p)
                .ToArray();

            using (var md5 = MD5.Create())
            {
                foreach (var filePath in filePaths)
                {
                    // hash path
                    var relativeFilePath = filePath.Substring(path.Length + 1);
                    var pathBytes = Encoding.UTF8.GetBytes(relativeFilePath);
                    md5.TransformBlock(pathBytes, 0, pathBytes.Length, pathBytes, 0);

                    if (includeContents)
                    {
                        // hash contents
                        var contentBytes = File.ReadAllBytes(filePath);
                        md5.TransformBlock(contentBytes, 0, contentBytes.Length, contentBytes, 0);
                    }
                }

                //Handles empty filePaths case
                md5.TransformFinalBlock(new byte[0], 0, 0);

                return BitConverter.ToString(md5.Hash).Replace("-", "").ToLower();
            }
        }
    }
}