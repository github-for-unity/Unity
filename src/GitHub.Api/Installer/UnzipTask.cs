using System;
using System.Threading;

namespace GitHub.Unity
{
    class UnzipTask : TaskBase<NPath>
    {
        private readonly string archiveFilePath;
        private readonly NPath extractedPath;
        private readonly IZipHelper zipHelper;
        private readonly IFileSystem fileSystem;
        private readonly string expectedMD5;

        public UnzipTask(CancellationToken token, NPath archiveFilePath, NPath extractedPath,
            IZipHelper zipHelper, IFileSystem fileSystem)
            : base(token)
        {
            this.archiveFilePath = archiveFilePath;
            this.extractedPath = extractedPath;
            this.zipHelper = zipHelper;
            this.fileSystem = fileSystem;
            Name = $"Unzip {archiveFilePath.FileName}";
        }

        protected NPath BaseRun(bool success)
        {
            return base.RunWithReturn(success);
        }

        protected override NPath RunWithReturn(bool success)
        {
            var ret = BaseRun(success);

            RaiseOnStart();

            try
            {
                ret = RunUnzip(success);
            }
            catch (Exception ex)
            {
                Errors = ex.Message;
                if (!RaiseFaultHandlers(ex))
                    throw;
            }
            finally
            {
                RaiseOnEnd(ret);
            }
            return ret;
        }

        protected virtual NPath RunUnzip(bool success)
        {
            Logger.Trace("Unzip File: {0} to Path: {1}", archiveFilePath, extractedPath);

            Exception exception = null;
            var attempts = 0;
            do
            {
                if (Token.IsCancellationRequested)
                    break;

                exception = null;
                try
                {
                    success = zipHelper.Extract(archiveFilePath, extractedPath, Token,
                        (value, total) =>
                        {
                            UpdateProgress(value, total);
                            return !Token.IsCancellationRequested;
                        });

                    if (!success)
                    {
                        extractedPath.DeleteIfExists();

                        var message = $"Failed to extract {archiveFilePath} to {extractedPath}";
                        exception = new UnzipException(message);
                    }
                }
                catch (Exception ex)
                {
                    exception = ex;
                    success = false;
                }
            } while (attempts++ < RetryCount);

            if (!success)
            {
                Token.ThrowIfCancellationRequested();
                throw new UnzipException("Error unzipping file", exception);
            }
            return extractedPath;
        }
        protected int RetryCount { get; }
    }

    public class UnzipException : Exception {
        public UnzipException(string message) : base(message)
        { }

        public UnzipException(string message, Exception innerException) : base(message, innerException)
        { }
    }
}
