using System;
using System.Threading;
using System.Threading.Tasks;

namespace GitHub.Unity
{
    class UnzipTask: TaskBase
    {
        private readonly string archiveFilePath;
        private readonly NPath extractedPath;
        private readonly IZipHelper zipHelper;
        private readonly IFileSystem fileSystem;
        private readonly string expectedMD5;
        private readonly IProgress<float> zipFileProgress;
        private readonly IProgress<long> estimatedDurationProgress;

        public UnzipTask(CancellationToken token, string archiveFilePath, NPath extractedPath, IFileSystem fileSystem, string expectedMD5 = null, IProgress<float> zipFileProgress = null, IProgress<long> estimatedDurationProgress = null) :
            this(token, archiveFilePath, extractedPath, ZipHelper.Instance, fileSystem, expectedMD5, zipFileProgress, estimatedDurationProgress)
        {
            
        }

        public UnzipTask(CancellationToken token, string archiveFilePath, NPath extractedPath, IZipHelper zipHelper, IFileSystem fileSystem, string expectedMD5 = null, IProgress<float> zipFileProgress = null, IProgress<long> estimatedDurationProgress = null)
            : base(token)
        {
            this.archiveFilePath = archiveFilePath;
            this.extractedPath = extractedPath;
            this.zipHelper = zipHelper;
            this.fileSystem = fileSystem;
            this.expectedMD5 = expectedMD5;
            this.zipFileProgress = zipFileProgress;
            this.estimatedDurationProgress = estimatedDurationProgress;
        }

        protected void BaseRun(bool success)
        {
            base.Run(success);
        }

        protected override void Run(bool success)
        {
            BaseRun(success);

            RaiseOnStart();

            try
            {
                RunUnzip(success);
            }
            catch (Exception ex)
            {
                Errors = ex.Message;
                if (!RaiseFaultHandlers(ex))
                    throw;
            }
            finally
            {
                RaiseOnEnd();
            }
        }

        protected virtual void RunUnzip(bool success)
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
                    zipHelper.Extract(archiveFilePath, extractedPath, Token, zipFileProgress, estimatedDurationProgress);

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
