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
            IZipHelper zipHelper, IFileSystem fileSystem, string expectedMD5)
            : base(token)
        {
            this.archiveFilePath = archiveFilePath;
            this.extractedPath = extractedPath;
            this.zipHelper = zipHelper;
            this.fileSystem = fileSystem;
            this.expectedMD5 = expectedMD5;
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

                    if (expectedMD5 != null)
                    {
                        var calculatedMD5 = fileSystem.CalculateFolderMD5(extractedPath);
                        success = calculatedMD5.Equals(expectedMD5, StringComparison.InvariantCultureIgnoreCase);
                        if (!success)
                        {
                            extractedPath.DeleteIfExists();

                            var message = $"Extracted MD5: {calculatedMD5} Does not match expected: {expectedMD5}";
                            Logger.Error(message);

                            exception = new UnzipException(message);
                        }
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
