using System;
using System.Diagnostics;
using System.IO;
using System.Net;

namespace GitHub.Unity
{
    public static class Utils
    {
        public static bool Copy(Stream source, Stream destination,
            long totalSize = 0,
            int chunkSize = 8192,
            Func<long, long, bool> progress = null,
            int progressUpdateRate = 100)
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

                bytesRead = source.Read(buffer, 0, totalRead + chunkSize > totalSize ? (int)(totalSize - totalRead) : chunkSize);

                if (trackProgress)
                    watch.Stop();

                totalRead += bytesRead;

                if (bytesRead > 0)
                {
                    destination.Write(buffer, 0, bytesRead);
                    if (trackProgress)
                    {
                        readLastSecond += bytesRead;
                        if (watch.ElapsedMilliseconds >= progressUpdateRate || totalRead == totalSize || bytesRead == 0)
                        {
                            watch.Reset();
                            if (bytesRead == 0) // we've reached the end
                                totalSize = totalRead;

                            lastSpeed = readLastSecond;
                            readLastSecond = 0;
                            averageSpeed = averageSpeed < 0f
                                ? lastSpeed
                                : smoothing * lastSpeed + (1f - smoothing) * averageSpeed;
                            timeToFinish = Math.Max(1L,
                                (long)((totalSize - totalRead) / (averageSpeed / progressUpdateRate)));

                            Logging.Debug($"totalRead: {totalRead} of {totalSize}");
                            success = progress(totalRead, timeToFinish);
                            if (!success)
                                break;
                        }
                    }
                    else // we still need to call the callback if it's there, so we can abort if needed
                    {
                        success = progress?.Invoke(totalRead, timeToFinish) ?? true;
                        if (!success)
                            break;
                    }
                }
            } while (bytesRead > 0 && (totalSize == 0 || totalSize > totalRead));

            if (totalRead > 0)
                destination.Flush();

            return success;
        }

        public static bool Download(ILogging logger, UriString url,
            Stream destinationStream,
            Func<long, long, bool> onProgress)
        {
            long bytes = destinationStream.Length;

            var expectingResume = bytes > 0;

            var webRequest = (HttpWebRequest)WebRequest.Create(url);

            if (expectingResume)
            {
                // classlib for 3.5 doesn't take long overloads...
                webRequest.AddRange((int)bytes);
            }

            webRequest.Method = "GET";
            webRequest.Timeout = 5000;

            if (expectingResume)
                logger.Trace($"Resuming download of {url}");
            else
                logger.Trace($"Downloading {url}");

            using (var webResponse = (HttpWebResponse) webRequest.GetResponseWithoutException())
            {
                var httpStatusCode = webResponse.StatusCode;
                logger.Trace($"Downloading {url} StatusCode:{(int)webResponse.StatusCode}");

                if (expectingResume && httpStatusCode == HttpStatusCode.RequestedRangeNotSatisfiable)
                {
                    onProgress(bytes, bytes);
                    return true;
                }

                if (!(httpStatusCode == HttpStatusCode.OK || httpStatusCode == HttpStatusCode.PartialContent))
                {
                    return false;
                }

                if (expectingResume && httpStatusCode == HttpStatusCode.OK)
                {
                    expectingResume = false;
                    destinationStream.Seek(0, SeekOrigin.Begin);
                }

                var responseLength = webResponse.ContentLength;
                if (expectingResume)
                {
                    if (!onProgress(bytes, bytes + responseLength))
                        return false;
                }

                using (var responseStream = webResponse.GetResponseStream())
                {
                    return Copy(responseStream, destinationStream, responseLength,
                        progress: (totalRead, timeToFinish) => {
                            return onProgress(totalRead, responseLength);
                        });
                }
            }
        }
    }
}