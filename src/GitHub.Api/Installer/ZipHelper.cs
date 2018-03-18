using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using ICSharpCode.SharpZipLib.Zip;
using GitHub.Logging;
using System.Collections.Generic;

namespace GitHub.Unity
{
    class ZipHelper : IZipHelper
    {
        private static IZipHelper instance;

        public static IZipHelper Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new ZipHelper();
                }

                return instance;
            }
        }

        public bool Extract(string archive, string outFolder, CancellationToken cancellationToken,
            Func<long, long, bool> onProgress = null)
        {
            return ExtractZipFile(archive, outFolder, cancellationToken, onProgress);
        }

        public static bool ExtractZipFile(string archive, string outFolder, CancellationToken cancellationToken,
            Func<long, long, bool> onProgress)
        {
            const int chunkSize = 4096; // 4K is optimum
            ZipFile zf = null;
            var processed = 0;
            var totalBytes = 0L;

            try
            {
                var fs = File.OpenRead(archive);
                zf = new ZipFile(fs);
                long totalSize = 0;
                var entries = new List<ZipEntry>((int)zf.Count);

                foreach (ZipEntry zipEntry in zf)
                {
                    if (zipEntry.IsDirectory)
                    {
                        continue; // Ignore directories
                    }
                    entries.Add(zipEntry);
                    totalSize += zipEntry.Size;
                }

                for (var i = 0; i < entries.Count; i++)
                {
                    var zipEntry = entries[i];
                    cancellationToken.ThrowIfCancellationRequested();

                    var entryFileName = zipEntry.Name;
                    // to remove the folder from the entry:- entryFileName = Path.GetFileName(entryFileName);
                    // Optionally match entrynames against a selection list here to skip as desired.
                    // The unpacked length is available in the zipEntry.Size property.

                    var zipStream = zf.GetInputStream(zipEntry);

                    var fullZipToPath = Path.Combine(outFolder, entryFileName);
                    var directoryName = Path.GetDirectoryName(fullZipToPath);
                    if (directoryName.Length > 0)
                    {
                        Directory.CreateDirectory(directoryName);
                    }
                    //#if !WINDOWS
                    //                    if (Environment.OSVersion.Platform == PlatformID.Unix || Environment.OSVersion.Platform == PlatformID.MacOSX)
                    //                    {
                    //                        if (zipEntry.ExternalFileAttributes > 0)
                    //                        {
                    //                            int fd = Mono.Unix.Native.Syscall.open(fullZipToPath,
                    //                                                                    Mono.Unix.Native.OpenFlags.O_CREAT | Mono.Unix.Native.OpenFlags.O_TRUNC,
                    //                                                                    (Mono.Unix.Native.FilePermissions)zipEntry.ExternalFileAttributes);
                    //                            Mono.Unix.Native.Syscall.close(fd);
                    //                        }
                    //                    }
                    //#endif

                    // Unzip file in buffered chunks. This is just as fast as unpacking to a buffer the full size
                    // of the file, but does not waste memory.
                    // The "using" will close the stream even if an exception occurs.
                    var targetFile = new FileInfo(fullZipToPath);
                    using (var streamWriter = targetFile.OpenWrite())
                    {
                        if (!Utils.Copy(zipStream, streamWriter, zipEntry.Size, chunkSize,
                            progress: (totalRead, timeToFinish) =>
                            {
                                totalBytes += totalRead;
                                return onProgress(totalBytes, totalSize);
                            }))
                            return false;
                    }

                    targetFile.LastWriteTime = zipEntry.DateTime;
                    processed++;
                }
            }
            catch (Exception ex)
            {
                LogHelper.GetLogger<ZipHelper>().Error(ex);
                return false;
            }
            finally
            {
                if (zf != null)
                {
                    //zf.IsStreamOwner = true; // Makes close also shut the underlying stream
                    zf.Close(); // Ensure we release resources
                }
            }
            return true;
        }
    }
}
