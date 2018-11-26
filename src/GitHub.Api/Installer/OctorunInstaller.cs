using System;
using System.Threading;
using GitHub.Logging;

namespace GitHub.Unity
{
    class OctorunInstaller
    {
        private static readonly ILogging Logger = LogHelper.GetLogger<OctorunInstaller>();

        private readonly IEnvironment environment;
        private readonly IFileSystem fileSystem;
        private readonly ITaskManager taskManager;
        private readonly IZipHelper sharpZipLibHelper;
        private readonly OctorunInstallDetails installDetails;

        public OctorunInstaller(IEnvironment environment, ITaskManager taskManager,
            OctorunInstallDetails installDetails = null)
        {
            this.environment = environment;
            this.sharpZipLibHelper = ZipHelper.Instance;
            this.installDetails = installDetails ?? new OctorunInstallDetails(environment.UserCachePath);
            this.fileSystem = environment.FileSystem;
            this.taskManager = taskManager;
        }

        public NPath SetupOctorunIfNeeded()
        {
            NPath path = NPath.Default;
            var isOctorunExtracted = IsOctorunExtracted();
            if (isOctorunExtracted)
                return installDetails.ExecutablePath;

            GrabZipFromResources();

            var extractPath = NPath.CreateTempDirectory("octorun_extract_archive_path");
            var unzipTask = new UnzipTask(taskManager.Token, installDetails.ZipFile,
                    extractPath, sharpZipLibHelper,
                    fileSystem)
                    .Catch(e => { Logger.Error(e, "Error extracting octorun"); return true; });
            unzipTask.RunSynchronously();

            if (unzipTask.Successful)
                path = MoveOctorun(extractPath.Combine("octorun"));
            return path;
        }

        private NPath GrabZipFromResources()
        {
            return AssemblyResources.ToFile(ResourceType.Generic, "octorun.zip", installDetails.BaseZipPath, environment);
        }

        private NPath MoveOctorun(NPath fromPath)
        {
            var toPath = installDetails.InstallationPath;

            Logger.Trace("MoveOctorun fromPath: {0} toPath:{1}", fromPath.ToString(), toPath.ToString());

            CopyHelper.Copy(fromPath, toPath);

            return installDetails.ExecutablePath;
        }

        private bool IsOctorunExtracted()
        {
            if (!installDetails.InstallationPath.DirectoryExists())
            {
                return false;
            }

            if (!installDetails.VersionFile.FileExists())
            {
                return false;
            }

            var octorunVersion = installDetails.VersionFile.ReadAllText().Trim();
            if (!OctorunInstallDetails.PackageVersion.Equals(octorunVersion))
            {
                Logger.Warning("Current version {0} does not match expected {1}", octorunVersion, OctorunInstallDetails.PackageVersion);
                return false;
            }
            return true;
        }

        public class OctorunInstallDetails
        {
            public const string DefaultZipMd5Url = "http://github-vs.s3.amazonaws.com/unity/octorun/octorun.zip.md5";
            public const string DefaultZipUrl = "http://github-vs.s3.amazonaws.com/unity/octorun/octorun.zip";

            public const string PackageVersion = "902910f48";
            private const string PackageName = "octorun";
            private const string zipFile = "octorun.zip";

            public OctorunInstallDetails(NPath baseDataPath)
            {
                BaseZipPath = baseDataPath.Combine("downloads");
                BaseZipPath.EnsureDirectoryExists();
                ZipFile = BaseZipPath.Combine(zipFile);

                var installPath = baseDataPath.Combine(PackageName);
                InstallationPath = installPath;

                Executable = "app.js";
                ExecutablePath = installPath.Combine("src", "bin", Executable);
            }

            public NPath BaseZipPath { get; }
            public NPath ZipFile { get; }
            public NPath InstallationPath { get; }
            public string Executable { get; }
            public NPath ExecutablePath { get; }
            public UriString ZipMd5Url { get; set; } = DefaultZipMd5Url;
            public UriString ZipUrl { get; set; } = DefaultZipUrl;
            public NPath VersionFile => InstallationPath.Combine("version");
        }
    }
}
