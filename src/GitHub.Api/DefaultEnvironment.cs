using System;
using System.IO;

namespace GitHub.Unity
{
    class DefaultEnvironment : IEnvironment
    {
        private static readonly ILogging logger = Logging.GetLogger<DefaultEnvironment>();

        public string GetSpecialFolder(Environment.SpecialFolder folder)
        {
            return Environment.GetFolderPath(folder);
        }

        public string ExpandEnvironmentVariables(string name)
        {
            return Environment.ExpandEnvironmentVariables(name);
        }

        public string GetEnvironmentVariable(string variable)
        {
            return Environment.GetEnvironmentVariable(variable);
        }

        public string UnityAssetsPath { get; set; }
        public string UnityProjectPath { get; set; }
        public string ExtensionInstallPath { get; set; }

        public string UserProfilePath { get { return Environment.GetEnvironmentVariable("USERPROFILE"); } }
        public string Path { get { return Environment.GetEnvironmentVariable("PATH"); } }
        public string NewLine { get { return Environment.NewLine; } }

        private string gitExecutablePath;
        public string GitExecutablePath
        {
            get { return gitExecutablePath; }
            set
            {
                logger.Trace("Setting GitExecutablePath to " + value);
                gitExecutablePath = value;
            }
        }

        private string gitInstallPath;
        public string GitInstallPath
        {
            get
            {
                if (gitInstallPath == null)
                {

                    if (!String.IsNullOrEmpty(GitExecutablePath))
                    {
                        gitInstallPath = GitExecutablePath.ToNPath().Parent.Parent;
                        logger.Trace("Setting GitInstallPath to " + gitInstallPath);
                    }
                    else
                        gitInstallPath = GitExecutablePath;
                }
                return gitInstallPath;
            }
        }

        public string RepositoryPath { get { return Repository.LocalPath; } }
        public IRepository Repository { get; set; }

        public bool IsWindows { get { return OnWindows; } }
        public bool IsLinux { get { return OnLinux; } }
        public bool IsMac { get { return OnMac; } }

        public static bool OnWindows
        {
            get { return Environment.OSVersion.Platform != PlatformID.Unix && Environment.OSVersion.Platform != PlatformID.MacOSX; }
        }

        public static bool OnLinux
        {
            get { return Environment.OSVersion.Platform == PlatformID.Unix && Directory.Exists("/proc"); }
        }

        public static bool OnMac
        {
            get
            {
                // most likely it'll return the proper id but just to be on the safe side, have a fallback
                return Environment.OSVersion.Platform == PlatformID.MacOSX ||
                      (Environment.OSVersion.Platform == PlatformID.Unix && !Directory.Exists("/proc"));
            }
        }
    }
}