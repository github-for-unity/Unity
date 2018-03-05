using System;
using System.IO;
using System.Net;
using System.Threading;

namespace GitHub.Unity
{
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

    class DownloadTask : TaskBase<NPath>
    {
        protected readonly IFileSystem fileSystem;

        public DownloadTask(CancellationToken token,
            IFileSystem fileSystem, UriString url,
            NPath targetDirectory = null,
            string filename = null,
            int retryCount = 0)
            : base(token)
        {
            this.fileSystem = fileSystem;
            RetryCount = retryCount;
            Url = url;
            Filename = filename ?? url.Filename;
            TargetDirectory = targetDirectory ?? NPath.CreateTempDirectory("ghu");
            this.Name = $"Download {Url}";
        }

        protected string BaseRunWithReturn(bool success)
        {
            return base.RunWithReturn(success);
        }

        protected override NPath RunWithReturn(bool success)
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
        protected virtual NPath RunDownload(bool success)
        {
            Exception exception = null;
            var attempts = 0;
            bool result = false;
            var partialFile = TargetDirectory.Combine(Filename + ".partial");
            do
            {
                exception = null;

                if (Token.IsCancellationRequested)
                    break;

                try
                {
                    Logger.Trace($"Download of {Url} to {Destination} Attempt {attempts + 1} of {RetryCount + 1}");

                    using (var destinationStream = fileSystem.OpenWrite(partialFile, FileMode.Append))
                    {
                        result = Downloader.Download(Logger, Url, destinationStream,
                            (value, total) =>
                            {
                                UpdateProgress(value, total);
                                return !Token.IsCancellationRequested;
                            });
                    }

                    if (result)
                    {
                        partialFile.Move(Destination);
                    }
                }
                catch (Exception ex)
                {
                    exception = ex;
                }
            } while (attempts++ < RetryCount);

            if (!result)
            {
                Token.ThrowIfCancellationRequested();
                throw new DownloadException("Error downloading file", exception);
            }

            return Destination;
        }

        public override string ToString()
        {
            return $"{base.ToString()} {Url}";
        }

        public UriString Url { get; }

        public NPath TargetDirectory { get; }

        public string Filename { get; }

        public NPath Destination { get { return TargetDirectory?.Combine(Filename); } }

        protected int RetryCount { get; }
    }

    class DownloadException : Exception
    {
        public DownloadException(string message) : base(message)
        { }

        public DownloadException(string message, Exception innerException) : base(message, innerException)
        { }
    }
}
