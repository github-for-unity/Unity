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

    public class DownloadDetails
    {
        public string Url { get; }
        public string Destination { get; }
        public bool Restart { get; }
        public string Md5Sum { get; }

        public DownloadDetails(string url, string destination, bool restart = false, string md5sum = null)
        {
            Url = url;
            Restart = restart;
            Destination = destination;
            Md5Sum = md5sum;
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
        private readonly DownloadDetails downloadDetails;
        private long bytes;
        private WebRequest request;

        public float Progress { get; set; }

        public DownloadTask(CancellationToken token, DownloadDetails downloadDetails)
            : base(token)
        {
            this.downloadDetails = downloadDetails;
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
            if (Restarted && bytes > 0)
                Logger.Trace($"Resuming download of {Url} to {Destination}");
            else
                Logger.Trace($"Downloading {Url} to {Destination}");

            using (WebResponse response = request.GetResponseWithoutException())
            {
                if (response == null)
                    return false;

                else if (Restarted && bytes > 0 && response is HttpWebResponse)
                {
                    if ((int)(((HttpWebResponse)response).StatusCode) == 416)
                    {
                        UpdateProgress(1);
                        return true;
                    }
                    else if ((int)(((HttpWebResponse)response).StatusCode) != 200)
                    {
                        return false;
                    }
                }

                long respSize = response.ContentLength;
                if (Restarted && bytes > 0)
                {
                    UpdateProgress(bytes / respSize);
                    if (bytes == respSize)
                        return true;
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

        public bool Restarted => downloadDetails.Restart;

        private string Url => downloadDetails.Url;

        private string Destination => downloadDetails.Destination;

        private void InitializeDownload()
        {
            if (Restarted)
            {
                var fi = new FileInfo(Destination);
                if (fi.Exists && fi.Length > 0)
                    bytes = fi.Length;
                else if (fi.Length == 0)
                    fi.Delete();
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
