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

        public NPath UnityApplication { get; set; }
        public NPath UnityAssetsPath { get; set; }
        public NPath UnityProjectPath { get; set; }
        public NPath ExtensionInstallPath { get; set; }
        public NPath UserCachePath { get; set; }
        public NPath SystemCachePath { get; set; }
        public NPath Path { get { return Environment.GetEnvironmentVariable("PATH").ToNPath(); } }
        public string NewLine { get { return Environment.NewLine; } }

        private NPath gitExecutablePath;
        public NPath GitExecutablePath
        {
            get { return gitExecutablePath; }
            set
            {
                logger.Trace("Setting GitExecutablePath to " + value);
                gitExecutablePath = value;
                gitInstallPath = null;
            }
        }

        private NPath gitInstallPath;
        public NPath GitInstallPath
        {
            get
            {
                if (gitInstallPath == null)
                {

                    if (!String.IsNullOrEmpty(GitExecutablePath))
                    {
                        if (IsWindows)
                        {
                            gitInstallPath = GitExecutablePath.Parent.Parent;
                        }
                        else
                        {
                            gitInstallPath = GitExecutablePath.Parent;
                        }
                        logger.Trace("Setting GitInstallPath to " + gitInstallPath);
                    }
                    else
                        gitInstallPath = GitExecutablePath;
                }
                return gitInstallPath;
            }
        }

        public NPath RepositoryPath { get { return Repository.LocalPath; } }
        public IRepository Repository { get; set; }

        public bool IsWindows { get { return OnWindows; } }
        public bool IsLinux { get { return OnLinux; } }
        public bool IsMac { get { return OnMac; } }

        /// <summary>
        /// This is for tests to reset the static OS flags
        /// </summary>
        public static void Reset()
        {
            onWindows = null;
            onLinux = null;
            onMac = null;
        }

        private static bool? onWindows;
        public static bool OnWindows
        {
            get
            {
                if (onWindows.HasValue)
                    return onWindows.Value;
                return Environment.OSVersion.Platform != PlatformID.Unix && Environment.OSVersion.Platform != PlatformID.MacOSX;
            }
            set { onWindows = value; }
        }

        private static bool? onLinux;
        public static bool OnLinux
        {
            get
            {
                if (onLinux.HasValue)
                    return onLinux.Value;
                return Environment.OSVersion.Platform == PlatformID.Unix && Directory.Exists("/proc");
            }
            set { onLinux = value; }
        }

        private static bool? onMac;
        public static bool OnMac
        {
            get
            {
                if (onMac.HasValue)
                    return onMac.Value;
                // most likely it'll return the proper id but just to be on the safe side, have a fallback
                return Environment.OSVersion.Platform == PlatformID.MacOSX ||
                      (Environment.OSVersion.Platform == PlatformID.Unix && !Directory.Exists("/proc"));
            }
            set { onMac = value; }
        }
    }
}