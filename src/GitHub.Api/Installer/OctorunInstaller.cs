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

        public ITask<NPath> SetupOctorunIfNeeded()
        {
            //Logger.Trace("SetupOctorunIfNeeded");

            var task = new FuncTask<NPath>(taskManager.Token, () =>
            {
                var isOctorunExtracted = IsOctorunExtracted();
                Logger.Trace("isOctorunExtracted: {0}", isOctorunExtracted);
                if (isOctorunExtracted)
                    return installDetails.ExecutablePath;
                GrabZipFromResources();
                return NPath.Default;
            });

            task.OnEnd += (t, path, _, __) =>
            {
                if (!path.IsInitialized)
                {
                    var tempZipExtractPath = NPath.CreateTempDirectory("octorun_extract_archive_path");
                    var unzipTask = new UnzipTask(taskManager.Token, installDetails.ZipFile,
                            tempZipExtractPath, sharpZipLibHelper,
                            fileSystem)
                        .Then((success, extractPath) => MoveOctorun(extractPath.Combine("octorun")));
                    t.Then(unzipTask);
                }
            };

            return task;
        }

        private NPath GrabZipFromResources()
        {
            if (!installDetails.ZipFile.FileExists())
            {
                AssemblyResources.ToFile(ResourceType.Generic, "octorun.zip", installDetails.BaseZipPath, environment);
            }
            return installDetails.ZipFile;
        }

        private NPath MoveOctorun(NPath fromPath)
        {
            var toPath = installDetails.InstallationPath;
            Logger.Trace($"Moving tempDirectory:'{fromPath}' to extractTarget:'{toPath}'");

            toPath.DeleteIfExists();
            toPath.EnsureParentDirectoryExists();
            fromPath.Move(toPath);
            return installDetails.ExecutablePath;
        }

        private bool IsOctorunExtracted()
        {
            if (!installDetails.InstallationPath.DirectoryExists())
            {
                //Logger.Warning($"{octorunPath} does not exist");
                return false;
            }

            if (!installDetails.VersionFile.FileExists())
            {
                //Logger.Warning($"{versionFilePath} does not exist");
                return false;
            }

            var octorunVersion = installDetails.VersionFile.ReadAllText();
            if (!OctorunInstallDetails.PackageVersion.Equals(octorunVersion))
            {
                Logger.Warning("Current version {0} does not match expected {1}", octorunVersion, OctorunInstallDetails.PackageVersion);
                return false;
            }
            return true;
        }

        public class OctorunInstallDetails
        {
            public const string DefaultZipMd5Url = "https://ghfvs-installer.github.com/unity/octorun/octorun.zip.md5";
            public const string DefaultZipUrl = "https://ghfvs-installer.github.com/unity/octorun/octorun.zip";

            public const string PackageVersion = "8bc23505";
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