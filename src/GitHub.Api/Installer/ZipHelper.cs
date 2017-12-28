using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using ICSharpCode.SharpZipLib.Zip;

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

        public static bool Copy(Stream source, Stream destination, int chunkSize, long totalSize,
            Func<long, long, bool> progress, int progressUpdateRate)
        {
            var buffer = new byte[chunkSize];
            var bytesRead = 0;
            long totalRead = 0;
            var averageSpeed = -1f;
            var lastSpeed = 0f;
            var smoothing = 0.005f;
            long readLastSecond = 0;
            long timeToFinish = 0;
            Stopwatch watch = null;
            var success = true;

            var trackProgress = totalSize > 0 && progress != null;
            if (trackProgress)
            {
                watch = new Stopwatch();
            }

            do
            {
                if (trackProgress)
                {
                    watch.Start();
                }

                bytesRead = source.Read(buffer, 0, chunkSize);

                if (trackProgress)
                {
                    watch.Stop();
                }

                totalRead += bytesRead;

                if (bytesRead > 0)
                {
                    destination.Write(buffer, 0, bytesRead);
                    if (trackProgress)
                    {
                        readLastSecond += bytesRead;
                        if (watch.ElapsedMilliseconds >= progressUpdateRate || totalRead == totalSize)
                        {
                            watch.Reset();
                            lastSpeed = readLastSecond;
                            readLastSecond = 0;
                            averageSpeed = averageSpeed < 0f
                                ? lastSpeed
                                : smoothing * lastSpeed + (1f - smoothing) * averageSpeed;
                            timeToFinish = Math.Max(1L,
                                (long)((totalSize - totalRead) / (averageSpeed / progressUpdateRate)));

                            if (!progress(totalRead, timeToFinish))
                            {
                                break;
                            }
                        }
                    }
                }
            } while (bytesRead > 0);

            if (totalRead > 0)
            {
                destination.Flush();
            }

            return success;
        }

        public void Extract(string archive, string outFolder, CancellationToken cancellationToken,
            IProgress<float> zipFileProgress = null, IProgress<long> estimatedDurationProgress = null)
        {
            ExtractZipFile(archive, outFolder, cancellationToken, zipFileProgress, estimatedDurationProgress);
        }

        public static void ExtractZipFile(string archive, string outFolder, CancellationToken cancellationToken,
            IProgress<float> zipFileProgress = null, IProgress<long> estimatedDurationProgress = null)
        {
            ZipFile zf = null;
            var estimatedDuration = 1L;
            var startTime = DateTime.Now;
            var processed = 0;
            var totalBytes = 0L;

            try
            {
                var fs = File.OpenRead(archive);
                zf = new ZipFile(fs);

                foreach (ZipEntry zipEntry in zf)
                {
                    if (zipEntry.IsDirectory)
                    {
                        continue; // Ignore directories
                    }

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
                        const int chunkSize = 4096; // 4K is optimum
                        Copy(zipStream, streamWriter, chunkSize, targetFile.Length, (totalRead, timeToFinish) =>
                        {
                            estimatedDuration = timeToFinish;

                            estimatedDurationProgress.Report(estimatedDuration);
                            zipFileProgress?.Report((float)(totalBytes + totalRead) / targetFile.Length);
                            return true;
                        }, 100);
                        cancellationToken.ThrowIfCancellationRequested();
                    }

                    targetFile.LastWriteTime = zipEntry.DateTime;
                    processed++;
                    totalBytes += zipEntry.Size;

                    var elapsedMillisecondsPerFile = (DateTime.Now - startTime).TotalMilliseconds / processed;
                    estimatedDuration = Math.Max(1L, (long)((fs.Length - totalBytes) * elapsedMillisecondsPerFile));

                    estimatedDurationProgress?.Report(estimatedDuration);
                    zipFileProgress?.Report((float)processed / zf.Count);
                    cancellationToken.ThrowIfCancellationRequested();
                }
            }
            finally
            {
                if (zf != null)
                {
                    //zf.IsStreamOwner = true; // Makes close also shut the underlying stream
                    zf.Close(); // Ensure we release resources
                }
            }
        }
    }
}
