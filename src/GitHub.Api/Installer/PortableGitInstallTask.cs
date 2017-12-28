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

        private const string ExpectedVersion = "f02737a78695063deace08e96d5042710d3e32db";
        private const string PackageName = "PortableGit";
        private const string PackageNameWithVersion = PackageName + "_" + ExpectedVersion;

        public PortableGitInstallDetails(NPath targetInstallPath, bool onWindows)
        {
            var gitInstallPath = targetInstallPath.Combine(ApplicationInfo.ApplicationName, PackageNameWithVersion);
            GitInstallPath = gitInstallPath;

            if (onWindows)
            {
                GitExec += "git.exe";
                GitLfsExec += "git-lfs.exe";

                GitExecPath = gitInstallPath.Combine("cmd", GitExec);
                GitLfsExecPath = gitInstallPath.Combine("mingw32", "libexec", "git-core", GitLfsExec);
            }
            else
            {
                GitExec = "git";
                GitLfsExec = "git-lfs";

                GitExecPath = gitInstallPath.Combine("bin", GitExec);
                GitLfsExecPath = gitInstallPath.Combine("libexec", "git-core", GitLfsExec);
            }
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

            var installGit = InstallGit();
            if (installGit)
            {
                var installGitLfs = InstallGitLfs();
                if (installGitLfs)
                {
                    Logger.Trace("Completed PortableGitInstallTask");
                    return installDetails.GitExecPath;
                }
            }

            Logger.Warning("Unsuccessful PortableGitInstallTask");

            return null;
        }

        private bool IsPortableGitExtracted()
        {
            if (!installDetails.GitInstallPath.DirectoryExists())
            {
                Logger.Trace("{0} does not exist", installDetails.GitInstallPath);
                return false;
            }

            var installMD5 = environment.FileSystem.CalculateFolderMD5(installDetails.GitInstallPath);
            if (!installMD5.Equals(PortableGitInstallDetails.ExtractedMD5, StringComparison.InvariantCultureIgnoreCase))
            {
                Logger.Trace("MD5 {0} does not match expected {1}", installMD5, PortableGitInstallDetails.ExtractedMD5);
                return false;
            }

            Logger.Trace("Git Present");
            return true;
        }

        private bool InstallGit()
        {
            Logger.Trace("InstallGit");

            var tempPath = NPath.GetTempFilename();
            var gitArchivePath = AssemblyResources.ToFile(ResourceType.Platform, "git.zip", tempPath, environment);

            if (!environment.FileSystem.FileExists(gitArchivePath))
            {
                Logger.Warning("Archive \"{0}\" missing", gitArchivePath);

                return false;
            }

            Token.ThrowIfCancellationRequested();

            var tempDirectory = NPath.CreateTempDirectory("git_install_task");

            try
            {
                Logger.Trace("Extracting gitArchivePath:\"{0}\" tempDirectory:\"{1}\"",
                    gitArchivePath, tempDirectory);

                ZipHelper.ExtractZipFile(gitArchivePath, tempDirectory, Token);
            }
            catch (Exception ex)
            {
                Logger.Warning(ex, "Error Extracting gitArchivePath:\"{0}\" tempDirectory:\"{1}\"", 
                    gitArchivePath, tempDirectory);

                return false;
            }

            Token.ThrowIfCancellationRequested();

            try
            {
                installDetails.GitInstallPath.DeleteIfExists();
                installDetails.GitInstallPath.EnsureParentDirectoryExists();

                Logger.Trace("Moving tempDirectory:\"{0}\" to gitInstallPath:\"{1}\"",
                    tempDirectory, installDetails.GitInstallPath);

                tempDirectory.Move(installDetails.GitInstallPath);
            }
            catch (Exception ex)
            {
                Logger.Warning(ex, "Error Moving tempDirectory:\"{0}\" to gitInstallPath:\"{1}\"", 
                    tempDirectory, installDetails.GitInstallPath);

                return false;
            }

            Logger.Trace("Deleting tempDirectory:\"{0}\"", tempDirectory);
            tempDirectory.DeleteIfExists();

            return true;
        }

        private bool InstallGitLfs()
        {
            Logger.Trace("InstallGitLfs");

            var tempPath = NPath.GetTempFilename();
            var gitLfsArchivePath = AssemblyResources.ToFile(ResourceType.Platform, "git-lfs.zip", tempPath, environment);

            if (!environment.FileSystem.FileExists(gitLfsArchivePath))
            {
                Logger.Warning($"Archive \"{gitLfsArchivePath}\" missing");
                return false;
            }

            Token.ThrowIfCancellationRequested();

            var tempDirectory = NPath.CreateTempDirectory("git_install_task");

            try
            {
                Logger.Trace("Extracting gitLfsArchivePath:\"{0}\" tempDirectory:\"{1}\"", gitLfsArchivePath, tempDirectory);
                ZipHelper.ExtractZipFile(gitLfsArchivePath, tempDirectory, Token);
            }
            catch (Exception ex)
            {
                Logger.Warning($"Error Extracting gitLfsArchivePath:\"{gitLfsArchivePath}\" tempDirectory:\"{tempDirectory}\"", ex);
                return false;
            }

            Token.ThrowIfCancellationRequested();

            var tempDirectoryGitLfsExec = tempDirectory.Combine(installDetails.GitLfsExec);

            try
            {
                Logger.Trace("Moving tempDirectoryGitLfsExec:\"{0}\" to gitLfsExecFullPath:\"{1}\"", tempDirectoryGitLfsExec, installDetails.GitLfsExecPath);
                tempDirectoryGitLfsExec.Move(installDetails.GitLfsExecPath);
            }
            catch (Exception ex)
            {
                Logger.Warning($"Error Moving tempDirectoryGitLfsExec:\"{tempDirectoryGitLfsExec}\" to gitLfsExecFullPath:\"{installDetails.GitLfsExecPath}\"", ex);
                return false;
            }

            Logger.Trace("Deleting tempDirectory:\"{0}\"", tempDirectory);
            tempDirectory.DeleteIfExists();
            return true;
        }
    }
}