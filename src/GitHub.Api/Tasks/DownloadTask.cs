using System;
using System.Diagnostics;
using System.IO;
using System.Net;
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

    public class DownloadResult
    {

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
                return e.Response;
            }
        }
    }

    class DownloadTask: TaskBase<DownloadResult>
    {
        private long bytes;
        private WebRequest request;
        private bool restarted;

        public float Progress { get; set; }

        public DownloadTask(CancellationToken token, string url, string destination)
            : base(token)
        {
            Url = url;
            Destination = destination;
            Name = "DownloadTask";
        }

        protected override DownloadResult RunWithReturn(bool success)
        {
            DownloadResult result = base.RunWithReturn(success);

            RaiseOnStart();

            try
            {
                Logger.Trace("Downloading");

                InitializeDownload();
                RunDownload();

                Logger.Trace("Downloaded");
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

        protected virtual void UpdateProgress(float progress)
        {
            Progress = progress;
        }

        public bool RunDownload()
        {
            if (restarted && bytes > 0)
                Logger.Trace($"Resuming download of {Url} to {Destination}");
            else
                Logger.Trace($"Downloading {Url} to {Destination}");

            using (WebResponse response = request.GetResponseWithoutException())
            {
                if (response == null)
                    return false;

                if (restarted && bytes > 0 && response is HttpWebResponse)
                {
                    var httpStatusCode = ((HttpWebResponse)response).StatusCode;
                    if (httpStatusCode == HttpStatusCode.RequestedRangeNotSatisfiable)
                    {
                        UpdateProgress(1);
                        return true;
                    }

                    if (!(httpStatusCode == HttpStatusCode.OK || httpStatusCode == HttpStatusCode.PartialContent))
                    {
                        return false;
                    }
                }

                long respSize = response.ContentLength;
                if (restarted && bytes > 0)
                {
                    UpdateProgress(bytes / respSize);
                }

                using (Stream rStream = response.GetResponseStream())
                {
                    using (Stream localStream = new FileStream(Destination, FileMode.Append))
                    {
                        if (Token.IsCancellationRequested)
                            return false;

                        return Utils.Copy(rStream, localStream, 8192, respSize, null, 100);
                    }
                }
            }
        }

        protected string Url { get; }

        protected string Destination { get; }

        private void InitializeDownload()
        {
            var fi = new FileInfo(Destination);
            if (fi.Exists)
            {
                if (fi.Length > 0)
                {
                    bytes = fi.Length;
                    restarted = true;
                }
                else if (fi.Length == 0)
                {
                    fi.Delete();
                }
            }

            request = WebRequest.Create(Url);
            if (request is HttpWebRequest)
            {
                //((HttpWebRequest)request).UserAgent = "Unity PackageManager v" + PackageManager.Instance.Version;

                if (bytes > 0)
                    ((HttpWebRequest)request).AddRange((int)bytes); // TODO: fix classlibs to take long overloads
            }

            request.Method = "GET";
            request.Timeout = 3000;
        }
    }
}
