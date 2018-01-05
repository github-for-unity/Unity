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

        protected override void Run(bool success)
        {
            base.Run(success);

            Logger.Trace("Unzip File: {0} to Path: {1}", archiveFilePath, extractedPath);

            try
            {
                zipHelper.Extract(archiveFilePath, extractedPath, Token, zipFileProgress, estimatedDurationProgress);
            }
            catch (Exception ex)
            {
                var message = "Error Unzipping file";

                Logger.Error(ex, message);
                throw new UnzipTaskException(message);
            }

            if (expectedMD5 != null)
            {
                var calculatedMD5 = fileSystem.CalculateFolderMD5(extractedPath);
                if (!calculatedMD5.Equals(expectedMD5, StringComparison.InvariantCultureIgnoreCase))
                {
                    extractedPath.DeleteIfExists();

                    var message = $"Extracted MD5: {calculatedMD5} Does not match expected: {expectedMD5}";
                    Logger.Error(message);

                    throw new UnzipTaskException(message);
                }
            }

            Logger.Trace("Completed Unzip");
        }
    }

    public class UnzipTaskException : Exception {
        public UnzipTaskException(string message) : base(message)
        { }

        public UnzipTaskException(string message, Exception innerException) : base(message, innerException)
        { }
    }
}
