using System;
using System.Threading;

namespace GitHub.Unity
{
    class PortableGitInstallDetails
    {
        public NPath GitInstallPath { get; }
        public string GitExec { get; }
        public NPath GitExecPath { get; }
        public string GitLfsExec { get; }
        public NPath GitLfsExecPath { get; }

        public const string ExtractedMD5 = "65fd0575d3b47d8207b9e19d02faca4f";
        public const string FileListMD5 = "a152a216b2e76f6c127053251187a278";

        private const string PackageVersion = "f02737a78695063deace08e96d5042710d3e32db";
        private const string PackageName = "PortableGit";
        private const string PackageNameWithVersion = PackageName + "_" + PackageVersion;

        private readonly bool onWindows;

        public PortableGitInstallDetails(NPath targetInstallPath, bool onWindows)
        {
            this.onWindows = onWindows;
            var gitInstallPath = targetInstallPath.Combine(ApplicationInfo.ApplicationName, PackageNameWithVersion);
            GitInstallPath = gitInstallPath;

            if (onWindows)
            {
                GitExec += "git.exe";
                GitLfsExec += "git-lfs.exe";

                GitExecPath = gitInstallPath.Combine("cmd", GitExec);
            }
            else
            {
                GitExec = "git";
                GitLfsExec = "git-lfs";

                GitExecPath = gitInstallPath.Combine("bin", GitExec);
            }

            GitLfsExecPath = GetGitLfsExecPath(gitInstallPath);
        }

        public NPath GetGitLfsExecPath(NPath gitInstallRoot)
        {
            return onWindows
                ? gitInstallRoot.Combine("mingw32", "libexec", "git-core", GitLfsExec)
                : gitInstallRoot.Combine("libexec", "git-core", GitLfsExec);
        }
    }

    class PortableGitInstallTask : TaskBase<NPath>
    {
        private readonly PortableGitInstallDetails installDetails;
        private readonly IEnvironment environment;

        public PortableGitInstallTask(CancellationToken token, IEnvironment environment, PortableGitInstallDetails installDetails) : base(token)
        {
            this.environment = environment;
            this.installDetails = installDetails;
        }

        protected override NPath RunWithReturn(bool success)
        {
            base.RunWithReturn(success);

            Logger.Trace("Starting PortableGitInstallTask");

            if (IsPortableGitExtracted())
            {
                Logger.Trace("Completed PortableGitInstallTask");
                return installDetails.GitExecPath;
            }

            Token.ThrowIfCancellationRequested();

            installDetails.GitInstallPath.DeleteIfExists();
            installDetails.GitInstallPath.EnsureParentDirectoryExists();

            Token.ThrowIfCancellationRequested();

            var extractTarget = NPath.CreateTempDirectory("git_install_task");
            var installGit = InstallGit(extractTarget);
            if (!installGit)
            {
                Logger.Warning("Failed PortableGitInstallTask");
                return null;
            }

            Token.ThrowIfCancellationRequested();

            var installGitLfs = InstallGitLfs(extractTarget);
            if (!installGitLfs)
            {
                Logger.Warning("Failed PortableGitInstallTask");
                return null;
            }

            Token.ThrowIfCancellationRequested();

            var extractedMD5 = environment.FileSystem.CalculateFolderMD5(extractTarget);
            if (!extractedMD5.Equals(PortableGitInstallDetails.ExtractedMD5, StringComparison.InvariantCultureIgnoreCase))
            {
                Logger.Warning("MD5 {0} does not match expected {1}", extractedMD5, PortableGitInstallDetails.ExtractedMD5);
                Logger.Warning("Failed PortableGitInstallTask");
                return null;
            }

            var moveSuccessful = MoveExtractTarget(extractTarget);
            if (!moveSuccessful)
            {
                Logger.Warning("Failed PortableGitInstallTask");
                return null;
            }

            Logger.Trace("Completed PortableGitInstallTask");
            return installDetails.GitExecPath;
        }

        private bool MoveExtractTarget(NPath extractTarget)
        {
            try
            {
                Logger.Trace("Moving tempDirectory:\"{0}\" to extractTarget:\"{1}\"", extractTarget,
                    installDetails.GitInstallPath);

                extractTarget.Move(installDetails.GitInstallPath);

                Logger.Trace("Deleting extractTarget:\"{0}\"", extractTarget);
                extractTarget.DeleteIfExists();

                Logger.Trace("Completed PortableGitInstallTask");
            }
            catch (Exception ex)
            {
                Logger.Warning(ex, "Error Moving tempDirectory:\"{0}\" to extractTarget:\"{1}\"", extractTarget,
                    installDetails.GitInstallPath);

                return false;
            }

            return true;
        }

        private bool IsPortableGitExtracted()
        {
            if (!installDetails.GitInstallPath.DirectoryExists())
            {
                Logger.Trace("{0} does not exist", installDetails.GitInstallPath);
                return false;
            }

            var fileListMD5 = environment.FileSystem.CalculateFolderMD5(installDetails.GitInstallPath, false);
            if (!fileListMD5.Equals(PortableGitInstallDetails.FileListMD5, StringComparison.InvariantCultureIgnoreCase))
            {
                Logger.Trace("MD5 {0} does not match expected {1}", fileListMD5, PortableGitInstallDetails.FileListMD5);
                return false;
            }

            Logger.Trace("Git Present");
            return true;
        }

        private bool InstallGit(NPath targetPath)
        {
            Logger.Trace("InstallGit");

            var tempZipPath = NPath.CreateTempDirectory("git_zip_path");
            var gitArchivePath = AssemblyResources.ToFile(ResourceType.Platform, "git.zip", tempZipPath, environment);

            if (!environment.FileSystem.FileExists(gitArchivePath))
            {
                Logger.Warning("Archive \"{0}\" missing", gitArchivePath);

                return false;
            }

            Token.ThrowIfCancellationRequested();

            try
            {
                Logger.Trace("Extracting gitArchivePath:\"{0}\" targetPath:\"{1}\"",
                    gitArchivePath, targetPath);

                ZipHelper.ExtractZipFile(gitArchivePath, targetPath, Token);
            }
            catch (Exception ex)
            {
                Logger.Warning(ex, "Error Extracting gitArchivePath:\"{0}\" tempDirectory:\"{1}\"", 
                    gitArchivePath, targetPath);

                return false;
            }

            tempZipPath.DeleteIfExists();

            return true;
        }

        private bool InstallGitLfs(NPath targetPath)
        {
            Logger.Trace("InstallGitLfs");

            var tempZipPath = NPath.CreateTempDirectory("git_lfs_zip_path");
            var gitLfsArchivePath = AssemblyResources.ToFile(ResourceType.Platform, "git-lfs.zip", tempZipPath, environment);

            if (!environment.FileSystem.FileExists(gitLfsArchivePath))
            {
                Logger.Warning($"Archive \"{gitLfsArchivePath}\" missing");
                return false;
            }

            Token.ThrowIfCancellationRequested();

            var tempZipExtractPath = NPath.CreateTempDirectory("git_lfs_extract_path");

            try
            {
                Logger.Trace("Extracting gitLfsArchivePath:\"{0}\" tempDirectory:\"{1}\"", gitLfsArchivePath, tempZipExtractPath);
                ZipHelper.ExtractZipFile(gitLfsArchivePath, tempZipExtractPath, Token);
            }
            catch (Exception ex)
            {
                Logger.Warning(ex, $"Error Extracting gitLfsArchivePath:\"{gitLfsArchivePath}\" tempDirectory:\"{tempZipExtractPath}\"");
                return false;
            }

            Token.ThrowIfCancellationRequested();

            var tempDirectoryGitLfsExec = tempZipExtractPath.Combine(installDetails.GitLfsExec);

            var targetLfsExecPath = installDetails.GetGitLfsExecPath(targetPath);
            try
            {
                Logger.Trace("Moving tempDirectoryGitLfsExec:\"{0}\" to targetLfsExecPath:\"{1}\"", tempDirectoryGitLfsExec, targetLfsExecPath);
                tempDirectoryGitLfsExec.Move(targetLfsExecPath);
            }
            catch (Exception ex)
            {
                Logger.Warning(ex, $"Error Moving tempDirectoryGitLfsExec:\"{tempDirectoryGitLfsExec}\" to targetLfsExecPath:\"{targetLfsExecPath}\"");
                return false;
            }

            tempZipPath.DeleteIfExists();
            tempZipExtractPath.DeleteIfExists();

            return true;
        }
    }
}