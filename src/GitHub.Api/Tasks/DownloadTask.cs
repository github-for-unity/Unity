using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;

namespace GitHub.Unity
{
    public static class Utils
    {
        public static bool Copy(Stream source, Stream destination, int chunkSize)
        {
            return Copy(source, destination, chunkSize, 0, null, 1000);
        }

        public static bool Copy(Stream source, Stream destination, int chunkSize, long totalSize,
            Func<long, long, bool> progress, int progressUpdateRate)
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
                }
            } while (bytesRead > 0);

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
            webRequest.Timeout = 3000;

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

                var responseLength = webResponse.ContentLength;
                if (expectingResume)
                {
                    if (!onProgress(bytes, bytes + responseLength))
                        return false;
                }

                using (var responseStream = webResponse.GetResponseStream())
                {
                    return Copy(responseStream, destinationStream, 8192, responseLength,
                        (totalRead, timeToFinish) => {
                            return onProgress(totalRead, responseLength);
                        }
                        , 100);
                }
            }
        }
    }

    public static class WebRequestExtensions
    {
        public static WebResponse GetResponseWithoutException(this WebRequest request)
        {
            try
            {
                return request.GetResponse();
            }
            catch (WebException e)
            {
                if (e.Response != null)
                {
                    return e.Response;
                }

                throw e;
            }
        }
    }

    class DownloadTask : TaskBase<string>
    {
        protected readonly IFileSystem fileSystem;
        private long bytes;
        private bool restarted;

        public DownloadTask(CancellationToken token,
            IFileSystem fileSystem, UriString url,
            NPath targetDirectory = null,
            string filename = null,
            string validationHash = null, int retryCount = 0)
            : base(token)
        {
            this.fileSystem = fileSystem;
            ValidationHash = validationHash;
            RetryCount = retryCount;
            Url = url;
            Filename = filename ?? url.Filename;
            TargetDirectory = targetDirectory ?? NPath.CreateTempDirectory("ghu");
            Name = nameof(DownloadTask);
        }

        protected override string RunWithReturn(bool success)
        {
            var result = base.RunWithReturn(success);

            RaiseOnStart();

            try
            {
                result = RunDownload(success);
            }
            catch (Exception ex)
            {
                Errors = ex.Message;
                if (!RaiseFaultHandlers(ex))
                    throw;
            }
            finally
            {
                RaiseOnEnd(result);
            }

            return result;
        }

        /// <summary>
        /// The actual functionality to download with optional hash verification
        /// subclasses that wish to return the contents of the downloaded file
        /// or do something else with it can override this instead of RunWithReturn.
        /// If you do, you must call RaiseOnStart()/RaiseOnEnd()
        /// </summary>
        /// <param name="success"></param>
        /// <returns></returns>
        protected virtual string RunDownload(bool success)
        {
            Exception exception = null;
            var attempts = 0;
            bool result = false;
            do
            {
                if (Token.IsCancellationRequested)
                    break;

                exception = null;

                try
                {
                    Logger.Trace($"Download of {Url} to {Destination} Attempt {attempts + 1} of {RetryCount + 1}");

                    using (var destinationStream = fileSystem.OpenWrite(Destination, FileMode.Append))
                    {
                        result = Utils.Download(Logger, Url, destinationStream,
                            (value, total) =>
                            {
                                UpdateProgress(value, total);
                                return !Token.IsCancellationRequested;
                            });
                    }

                    if (result && ValidationHash != null)
                    {
                        var md5 = fileSystem.CalculateFileMD5(TargetDirectory);
                        result = md5.Equals(ValidationHash, StringComparison.CurrentCultureIgnoreCase);

                        if (!result)
                        {
                            Logger.Warning($"Downloaded MD5 {md5} does not match {ValidationHash}. Deleting {TargetDirectory}.");
                            fileSystem.FileDelete(TargetDirectory);
                        }
                        else
                        {
                            Logger.Trace($"Download confirmed {md5}");
                            break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    exception = new DownloadException("Error downloading file", ex);
                }
            } while (attempts++ < RetryCount);

            if (!result)
            {
                if (exception == null)
                    exception = new DownloadException("Error downloading file");
                throw exception;
            }

            return Destination;
        }


        public UriString Url { get; }

        public NPath TargetDirectory { get; }

        public string Filename { get; }

        public NPath Destination { get { return TargetDirectory?.Combine(Filename); } }

        public string ValidationHash { get; set; }

        protected int RetryCount { get; }
    }

    class DownloadException : Exception
    {
        public DownloadException(string message) : base(message)
        { }

        public DownloadException(string message, Exception innerException) : base(message, innerException)
        { }
    }

    class DownloadTextTask : DownloadTask
    {
        public DownloadTextTask(CancellationToken token,
            IFileSystem fileSystem, UriString url,
            NPath targetDirectory = null,
            string filename = null,
            int retryCount = 0)
            : base(token, fileSystem, url, targetDirectory, filename, retryCount: retryCount)
        {
            Name = nameof(DownloadTextTask);
        }

        protected override string RunDownload(bool success)
        {
            string result = null;

            RaiseOnStart();

            try
            {
                result = base.RunDownload(success);
                result = fileSystem.ReadAllText(result, Encoding.UTF8);
            }
            catch (Exception ex)
            {
                Errors = ex.Message;
                if (!RaiseFaultHandlers(ex))
                    throw;
            }
            finally
            {
                RaiseOnEnd(result);
            }

            return result;
        }
    }
}
