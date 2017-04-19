using System;
using System.Security.Cryptography;
using System.Text;

namespace GitHub.Unity
{
    static class FileSystemExtensions
    {
        public static string CalculateMD5(this IFileSystem fileSystem, string file)
        {
            byte[] computeHash;
            using (var md5 = MD5.Create())
            {
                using (var stream = fileSystem.OpenRead(file))
                {
                    computeHash = md5.ComputeHash(stream);
                }
            }

            return BitConverter.ToString(computeHash).Replace("-", string.Empty);
        }
    }
}