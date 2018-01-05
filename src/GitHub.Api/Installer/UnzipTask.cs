using System;
using System.Threading;
using System.Threading.Tasks;

namespace GitHub.Unity
{
    class UnzipTask: TaskBase
    {
        private readonly string archiveFilePath;
        private readonly string extractedPath;
        private readonly IZipHelper zipHelper;
        private readonly IProgress<float> zipFileProgress;
        private readonly IProgress<long> estimatedDurationProgress;

        public UnzipTask(CancellationToken token, string archiveFilePath, string extractedPath,
            IProgress<float> zipFileProgress = null, IProgress<long> estimatedDurationProgress = null) :
            this(token, archiveFilePath, extractedPath, ZipHelper.Instance, zipFileProgress, estimatedDurationProgress)
        {
            
        }

        public UnzipTask(CancellationToken token, string archiveFilePath, string extractedPath, IZipHelper zipHelper, IProgress<float> zipFileProgress = null, IProgress<long> estimatedDurationProgress = null)
            : base(token)
        {
            this.archiveFilePath = archiveFilePath;
            this.extractedPath = extractedPath;
            this.zipHelper = zipHelper;
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
            Logger.Trace("Unzip File: {0} to Path: {1}", archiveFilePath, extractedPath);

            zipHelper.Extract(archiveFilePath, extractedPath, Token, zipFileProgress, estimatedDurationProgress);

            Logger.Trace("Completed Unzip");
        }
    }
}
