using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using ICSharpCode.SharpZipLib.Zip;

namespace GitHub.Api.Installer
{
    class Util
    {
        void ExtractZipFile(string archive, string outFolder)
        {
            ZipFile zf = null;
            EstimatedDuration = 1L;
            DateTime startTime = DateTime.Now;
            int processed = 0;
            long totalBytes = 0L;

            try
            {
                FileStream fs = File.OpenRead(archive);
                zf = new ZipFile(fs);

                foreach (ZipEntry zipEntry in zf)
                {
                    if (zipEntry.IsDirectory)
                        continue;           // Ignore directories

                    String entryFileName = zipEntry.Name;
                    // to remove the folder from the entry:- entryFileName = Path.GetFileName(entryFileName);
                    // Optionally match entrynames against a selection list here to skip as desired.
                    // The unpacked length is available in the zipEntry.Size property.

                    Stream zipStream = zf.GetInputStream(zipEntry);

                    String fullZipToPath = Path.Combine(outFolder, entryFileName);
                    string directoryName = Path.GetDirectoryName(fullZipToPath);
                    if (directoryName.Length > 0)
                        Directory.CreateDirectory(directoryName);
#if !WINDOWS
                    if (Environment.OSVersion.Platform == PlatformID.Unix || Environment.OSVersion.Platform == PlatformID.MacOSX)
                    {
                        if (zipEntry.ExternalFileAttributes > 0)
                        {
                            int fd = Mono.Unix.Native.Syscall.open(fullZipToPath,
                                                                    Mono.Unix.Native.OpenFlags.O_CREAT | Mono.Unix.Native.OpenFlags.O_TRUNC,
                                                                    (Mono.Unix.Native.FilePermissions)zipEntry.ExternalFileAttributes);
                            Mono.Unix.Native.Syscall.close(fd);
                        }
                    }
#endif
                    // Unzip file in buffered chunks. This is just as fast as unpacking to a buffer the full size
                    // of the file, but does not waste memory.
                    // The "using" will close the stream even if an exception occurs.
                    FileInfo targetFile = new FileInfo(fullZipToPath);
                    using (FileStream streamWriter = targetFile.OpenWrite())
                    {
                        Utils.Copy(zipStream, streamWriter, 4096, targetFile.Length,
                            (totalRead, timeToFinish) =>
                            {
                                EstimatedDuration = timeToFinish;
                                UpdateProgress((float)(totalBytes + totalRead) / targetFile.Length);
                                return !CancelRequested;
                            },
                        100);

                        //Utils.Copy (zipStream, streamWriter, 4096);// 4K is optimum
                    }
                    targetFile.LastWriteTime = zipEntry.DateTime;
                    processed++;
                    totalBytes += zipEntry.Size;
                    double elapsedMillisecondsPerFile = (DateTime.Now - startTime).TotalMilliseconds / processed;
                    EstimatedDuration = Math.Max(1L, (long)((fs.Length - totalBytes) * elapsedMillisecondsPerFile));
                    UpdateProgress((float)processed / zf.Size);
                }
            }
            catch
            {
                throw;
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

        public static bool Copy(Stream source, Stream destination,
            int chunkSize, long totalSize, Func<long, long, bool> progress, int progressUpdateRate)
        {
            byte[] buffer = new byte[chunkSize];
            int bytesRead = 0;
            long totalRead = 0;
            float averageSpeed = -1f;
            float lastSpeed = 0f;
            float smoothing = 0.005f;
            long readLastSecond = 0;
            long timeToFinish = 0;
            Stopwatch watch = null;
            bool success = true;

            bool trackProgress = totalSize > 0 && progress != null;
            if (trackProgress)
                watch = new Stopwatch();

            do
            {
                if (trackProgress)
                    watch.Start();

                bytesRead = source.Read(buffer, 0, chunkSize);

                if (trackProgress)
                    watch.Stop();

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
                            averageSpeed = averageSpeed < 0f ? lastSpeed : smoothing * lastSpeed + (1f - smoothing) * averageSpeed;
                            timeToFinish = Math.Max(1L, (long)((totalSize - totalRead) / (averageSpeed / progressUpdateRate)));

                            if (!progress(totalRead, timeToFinish))
                                break;
                        }
                    }
                }
            } while (bytesRead > 0);

            if (totalRead > 0)
                destination.Flush();

            return success;
        }
    }
}
