using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using GitHub.Logging;
using System.Linq;

namespace GitHub.Unity
{
    class DownloadData
    {
        public UriString Url { get; }
        public NPath File { get; }
        public DownloadData(UriString url, NPath file)
        {
            this.Url = url;
            this.File = file;
        }
    }

    class Downloader : FuncListTask<DownloadData>
    {
        public event Action<DownloadData> DownloadStart;
        public event Action<DownloadData> DownloadComplete;
        public event Action<DownloadData, Exception> DownloadFailed;

        private readonly List<DownloaderTask> downloaders = new List<DownloaderTask>();

        public Downloader() : base(TaskManager.Instance.Token, RunDownloaders)
        {
            Name = "Downloader";
        }

        public void QueueDownload(UriString url, NPath targetDirectory)
        {
            var downloaderTask = new DownloaderTask();
            downloaderTask.QueueDownload(url, targetDirectory);
            downloaders.Add(downloaderTask);
        }

        private static List<DownloadData> RunDownloaders(bool success, FuncListTask<DownloadData> source)
        {
            Downloader self = (Downloader)source;
            List<DownloadData> result = null;
            var listOfTasks = new List<Task<DownloadData>>();
            foreach (var downloader in self.downloaders)
            {
                downloader.DownloadStart += self.DownloadStart;
                downloader.DownloadComplete += self.DownloadComplete;
                downloader.DownloadFailed += self.DownloadFailed;
                listOfTasks.Add(downloader.Run());
            }
            var res = TaskEx.WhenAll(listOfTasks).Result;
            if (res != null)
                result = new List<DownloadData>(res);
            return result;
        }

        class DownloaderTask
        {
            public event Action<DownloadData> DownloadStart;
            public event Action<DownloadData> DownloadComplete;
            public event Action<DownloadData, Exception> DownloadFailed;

            private readonly List<ITask<NPath>> queuedTasks = new List<ITask<NPath>>();
            private readonly TaskCompletionSource<DownloadData> aggregateDownloads = new TaskCompletionSource<DownloadData>();
            private readonly IFileSystem fs;
            private readonly CancellationToken cancellationToken;

            private volatile bool isSuccessful = true;
            private volatile Exception exception;
            private DownloadData result;

            public DownloaderTask()
            {
                fs = NPath.FileSystem;
                cancellationToken = TaskManager.Instance.Token;
                DownloadComplete += d => aggregateDownloads.TrySetResult(d);
                DownloadFailed += (_, e) => aggregateDownloads.TrySetException(e);
            }

            public Task<DownloadData> Run()
            {
                foreach (var task in queuedTasks)
                    task.Start();
                if (queuedTasks.Count == 0)
                    DownloadComplete(result);
                return aggregateDownloads.Task;
            }

            public Task<DownloadData> QueueDownload(UriString url, NPath targetDirectory)
            {
                var destinationFile = targetDirectory.Combine(url.Filename);
                result = new DownloadData(url, destinationFile);

                Action<ITask<NPath>, NPath, bool, Exception> verifyDownload = (t, res, success, ex) =>
                {
                    isSuccessful &= success;
                    if (!success)
                        exception = ex;
                    if (!isSuccessful)
                    {
                        DownloadFailed(result, exception);
                    }
                    else
                    {
                        DownloadComplete(result);
                    }
                };

                var fileDownload = DownloadFile(url, targetDirectory, result, verifyDownload);
                fileDownload.OnStart += _ => DownloadStart?.Invoke(result);
                queuedTasks.Add(fileDownload);
                return aggregateDownloads.Task;
            }

            private ITask<NPath> DownloadFile(UriString url, NPath targetDirectory, DownloadData result, Action<ITask<NPath>, NPath, bool, Exception> verifyDownload)
            {
                var download = new DownloadTask(cancellationToken, fs, url, targetDirectory)
                    .Catch(e => { DownloadFailed(result, e); return true; });
                download.OnEnd += verifyDownload;
                return download;
            }
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
            webRequest.Timeout = ApplicationConfiguration.WebTimeout;

            if (expectingResume)
                logger.Trace($"Resuming download of {url}");
            else
                logger.Trace($"Downloading {url}");

            using (var webResponse = (HttpWebResponse)webRequest.GetResponseWithoutException())
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
                    return Utils.Copy(responseStream, destinationStream, responseLength,
                        progress: (totalRead, timeToFinish) =>
                        {
                            return onProgress(totalRead, responseLength);
                        });
                }
            }
        }
    }
}
