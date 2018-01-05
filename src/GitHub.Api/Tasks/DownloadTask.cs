using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;

namespace GitHub.Unity
{
    public class Utils
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

    class DownloadTask : TaskBase
    {
        private readonly IFileSystem fileSystem;
        private long bytes;
        private bool restarted;

        public float Progress { get; set; }

        public DownloadTask(CancellationToken token, IFileSystem fileSystem, string url, string destination, string validationHash = null, int retryCount = 0)
            : base(token)
        {
            this.fileSystem = fileSystem;
            ValidationHash = validationHash;
            RetryCount = retryCount;
            Url = url;
            Destination = destination;
            Name = "DownloadTask";
        }

        protected override void Run(bool success)
        {
            base.Run(success);

            RaiseOnStart();

            var attempts = 0;
            try
            {
                bool result;
                do
                {
                    Logger.Trace($"Download of {Url} Attempt {attempts + 1} of {RetryCount + 1}");
                    result = Download();
                    if (result && ValidationHash != null)
                    {
                        var md5 = fileSystem.CalculateFileMD5(Destination);
                        result = md5.Equals(ValidationHash, StringComparison.CurrentCultureIgnoreCase);

                        if (!result)
                        {
                            Logger.Warning($"Downloaded MD5 {md5} does not match {ValidationHash}. Deleting {Destination}.");
                            fileSystem.FileDelete(Destination);
                        }
                        else
                        {
                            Logger.Trace($"Download confirmed {md5}");
                            break;
                        }
                    }
                } while (attempts++ < RetryCount);

                if (!result)
                {
                    throw new DownloadException("Error downloading file");
                }
            }
            catch (Exception ex)
            {
                Errors = ex.Message;
                if (!RaiseFaultHandlers(new DownloadException("Error downloading file", ex)))
                    throw;
            }
            finally
            {
                RaiseOnEnd();
            }
        }

        protected virtual void UpdateProgress(float progress)
        {
            Progress = progress;
        }

        public bool Download()
        {
            var fileInfo = new FileInfo(Destination);
            if (fileSystem.FileExists(Destination))
            {
                var fileLength = fileSystem.FileLength(Destination);
                if (fileLength > 0)
                {
                    bytes = fileInfo.Length;
                    restarted = true;
                }
                else if (fileLength == 0)
                {
                    fileSystem.FileDelete(Destination);
                }
            }

            var expectingResume = restarted && bytes > 0;

            var webRequest = (HttpWebRequest)WebRequest.Create(Url);

            if (expectingResume)
            {
                // TODO: fix classlibs to take long overloads
                webRequest.AddRange((int)bytes);
            }

            webRequest.Method = "GET";
            webRequest.Timeout = 3000;

            if (expectingResume)
                Logger.Trace($"Resuming download of {Url} to {Destination}");
            else
                Logger.Trace($"Downloading {Url} to {Destination}");

            using (var webResponse = (HttpWebResponse) webRequest.GetResponseWithoutException())
            {
                var httpStatusCode = webResponse.StatusCode;
                Logger.Trace($"Downloading {Url} StatusCode:{(int)webResponse.StatusCode}");

                if (expectingResume && httpStatusCode == HttpStatusCode.RequestedRangeNotSatisfiable)
                {
                    UpdateProgress(1);
                    return true;
                }

                if (!(httpStatusCode == HttpStatusCode.OK || httpStatusCode == HttpStatusCode.PartialContent))
                {
                    return false;
                }

                var responseLength = webResponse.ContentLength;
                if (expectingResume)
                {
                    UpdateProgress(bytes / (float)responseLength);
                }

                using (var responseStream = webResponse.GetResponseStream())
                {
                    using (var destinationStream = fileSystem.OpenWrite(Destination, FileMode.Append))
                    {
                        if (Token.IsCancellationRequested)
                            return false;

                        return Utils.Copy(responseStream, destinationStream, 8192, responseLength, null, 100);
                    }
                }
            }
        }

        protected string Url { get; }

        protected string Destination { get; }

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

    class DownloadTextTask : TaskBase<string>
    {
        public float Progress { get; set; }

        public DownloadTextTask(CancellationToken token, string url)
            : base(token)
        {
            Url = url;
            Name = "DownloadTask";
        }

        protected override string RunWithReturn(bool success)
        {
            var result = base.RunWithReturn(success);

            RaiseOnStart();

            try
            {
                Logger.Trace($"Downloading {Url}");
                var webRequest = WebRequest.Create(Url);
                webRequest.Method = "GET";
                webRequest.Timeout = 3000;

                using (var webResponse = (HttpWebResponse)webRequest.GetResponseWithoutException())
                {
                    var webResponseCharacterSet = webResponse.CharacterSet ?? Encoding.UTF8.BodyName;
                    var encoding = Encoding.GetEncoding(webResponseCharacterSet);

                    using (var responseStream = webResponse.GetResponseStream())
                    using (var reader = new StreamReader(responseStream, encoding))
                    {
                        result = reader.ReadToEnd();
                    }
                }
            }
            catch (Exception ex)
            {
                Errors = ex.Message;
                if (!RaiseFaultHandlers(new DownloadException("Error downloading text", ex)))
                    throw;
            }
            finally
            {
                RaiseOnEnd(result);
            }

            return result;
        }

        protected virtual void UpdateProgress(float progress)
        {
            Progress = progress;
        }

        protected string Url { get; }
    }
}
