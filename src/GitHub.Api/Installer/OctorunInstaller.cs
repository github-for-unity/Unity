using System;
using System.Threading;
using GitHub.Logging;

namespace GitHub.Unity
{
    class OctorunInstaller
    {
        private const string ExpectedOctorunVersion = "8008bf3da68428f50368cf2fe3fe290df4acad54";
        private const string OctorunExtractedMD5 = "b7341015bc701a9f5bf83f51b1b596b7";

        private static readonly ILogging Logger = LogHelper.GetLogger<OctorunInstaller>();

        private readonly IFileSystem fileSystem;
        private readonly ITaskManager taskManager;
        private readonly IZipHelper sharpZipLibHelper;
        private readonly NPath octorunArchivePath;
        private NPath octorunPath;

        public OctorunInstaller(IFileSystem fileSystem, ITaskManager taskManager,
            NPath octorunPath, IZipHelper sharpZipLibHelper, NPath octorunArchivePath)
        {
            this.fileSystem = fileSystem;
            this.taskManager = taskManager;
            this.octorunPath = octorunPath;
            this.sharpZipLibHelper = sharpZipLibHelper;
            this.octorunArchivePath = octorunArchivePath;
        }

        public void SetupOctorunIfNeeded(ActionTask<NPath> onSuccess, ITask onFailure)
        {
            Logger.Trace("SetupOctorunIfNeeded");

            var isOctorunExtracted = IsOctorunExtracted();
            Logger.Trace("isOctorunExtracted: {0}", isOctorunExtracted);

            if (!isOctorunExtracted)
            {
                ExtractOctorun(onSuccess, onFailure);
            }
            else
            {
                onSuccess.PreviousResult = octorunPath;
                onSuccess.Start();
            }
        }

        private void ExtractOctorun(ActionTask<NPath> onSuccess, ITask onFailure)
        {
            Logger.Trace("ExtractOctorun");

            var tempZipExtractPath = NPath.CreateTempDirectory("octorun_extract_archive_path");
            var resultTask = new UnzipTask(taskManager.Token, octorunArchivePath, tempZipExtractPath, sharpZipLibHelper,
                fileSystem, OctorunExtractedMD5)
                .Then(s => MoveOctorun(tempZipExtractPath));

            resultTask.Then(onFailure, TaskRunOptions.OnFailure);
            resultTask.Then(onSuccess, TaskRunOptions.OnSuccess);

            resultTask.Start();
        }

        private NPath MoveOctorun(NPath octorunExtractPath)
        {
            Logger.Trace($"Moving tempDirectory:'{octorunExtractPath}' to extractTarget:'{octorunPath}'");

            octorunPath.DeleteIfExists();
            octorunPath.EnsureParentDirectoryExists();
            octorunExtractPath.Move(octorunPath);

            Logger.Trace($"Deleting targetGitLfsExecPath:'{octorunExtractPath}'");
            octorunExtractPath.DeleteIfExists();

            return octorunPath;
        }

        private bool IsOctorunExtracted()
        {
            if (!octorunPath.DirectoryExists())
            {
                Logger.Warning($"{octorunPath} does not exist");
                return false;
            }

            var versionFilePath = octorunPath.Combine("version");

            if (!versionFilePath.FileExists())
            {
                Logger.Warning($"{versionFilePath} does not exist");
                return false;
            }

            var octorunVersion = versionFilePath.ReadAllText();
            if (!ExpectedOctorunVersion.Equals(octorunVersion))
            {
                Logger.Warning("Current version {0} does not match expected {1}", octorunVersion, ExpectedOctorunVersion);
                return false;
            }

            return true;
        }
    }
}