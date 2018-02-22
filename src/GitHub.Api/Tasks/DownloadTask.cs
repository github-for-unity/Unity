using System;
using System.IO;
using System.Net;
using System.Text;
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

    class DownloadTask : TaskBase<string>
    {
        protected readonly IFileSystem fileSystem;

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

        protected string BaseRunWithReturn(bool success)
        {
            return base.RunWithReturn(success);
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

                    var fileExistsAndValid = false;
                    if (Destination.FileExists())
                    {
                        if (ValidationHash == null)
                        {
                            Destination.Delete();
                        }
                        else
                        {
                            var md5 = fileSystem.CalculateFileMD5(Destination);
                            result = md5.Equals(ValidationHash, StringComparison.CurrentCultureIgnoreCase);

                            if (result)
                            {
                                Logger.Trace($"Download previously exists & confirmed {md5}");
                                fileExistsAndValid = true;
                            }
                        }
                    }

                    if (!fileExistsAndValid)
                    {
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
                            var md5 = fileSystem.CalculateFileMD5(Destination);
                            result = md5.Equals(ValidationHash, StringComparison.CurrentCultureIgnoreCase);

                            if (!result)
                            {
                                Logger.Warning($"Downloaded MD5 {md5} does not match {ValidationHash}. Deleting {Destination}.");
                                fileSystem.FileDelete(TargetDirectory);
                            }
                            else
                            {
                                Logger.Trace($"Download confirmed {md5}");
                                break;
                            }
                        }
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

        protected override string RunWithReturn(bool success)
        {
            var result = BaseRunWithReturn(success);

            RaiseOnStart();

            try
            {
                result = RunDownload(success);
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
