using System;
using System.Threading;
using System.Threading.Tasks;

namespace GitHub.Unity
{
    class UnzipTask: TaskBase
    {
        private readonly string archiveFilePath;
        private readonly string extractedPath;
        private readonly IProgress<float> zipFileProgress;
        private readonly IProgress<long> estimatedDurationProgress;

        public UnzipTask(CancellationToken token, string archiveFilePath, string extractedPath, IProgress<float> zipFileProgress = null, IProgress<long> estimatedDurationProgress = null)
            : base(token)
        {
            this.archiveFilePath = archiveFilePath;
            this.extractedPath = extractedPath;
            this.zipFileProgress = zipFileProgress;
            this.estimatedDurationProgress = estimatedDurationProgress;
        }

        protected override void Run(bool success)
        {
            base.Run(success);

            UnzipArchive();
        }

        private void UnzipArchive()
        {
            Logger.Trace("Zip File: {0}", archiveFilePath);
            Logger.Trace("Target Path: {0}", extractedPath);

            ZipHelper.ExtractZipFile(archiveFilePath, extractedPath, Token, zipFileProgress, estimatedDurationProgress);

            Logger.Trace("Completed");
        }
    }
}
