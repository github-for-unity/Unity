using System;
using System.IO;

namespace GitHub.Unity
{
    class DefaultEnvironment : IEnvironment
    {
        private const string logFile = "github-unity.log";

        public NPath LogPath { get; }
        public DefaultEnvironment()
        {
            NPath localAppData;
            NPath commonAppData;
            if (IsWindows)
            {
                localAppData = GetSpecialFolder(Environment.SpecialFolder.LocalApplicationData).ToNPath();
                commonAppData = GetSpecialFolder(Environment.SpecialFolder.CommonApplicationData).ToNPath();
            }
            else if (IsMac)
            {
                localAppData = NPath.HomeDirectory.Combine("Library", "Application Support");
                // there is no such thing on the mac that is guaranteed to be user accessible (/usr/local might not be)
                commonAppData = GetSpecialFolder(Environment.SpecialFolder.ApplicationData).ToNPath();
            }
            else
            {
                localAppData = GetSpecialFolder(Environment.SpecialFolder.LocalApplicationData).ToNPath();
                commonAppData = "/usr/local/share/".ToNPath();
            }

            UserCachePath = localAppData.Combine(ApplicationInfo.ApplicationName);
            SystemCachePath = commonAppData.Combine(ApplicationInfo.ApplicationName);
            LogPath = UserCachePath.Combine(logFile);
        }

        public void Initialize(NPath extensionInstallPath, NPath unityPath, NPath assetsPath)
        {
            ExtensionInstallPath = extensionInstallPath;
            UnityApplication = unityPath;
            UnityAssetsPath = assetsPath;
            UnityProjectPath = assetsPath.Parent;
            Initialize();
        }

        public void Initialize()
        {
            Guard.NotNull(this, UnityProjectPath, nameof(UnityProjectPath));
            Guard.NotNull(this, FileSystem, nameof(FileSystem));
            RepositoryPath = new RepositoryLocator(UnityProjectPath).FindRepositoryRoot();
            if (RepositoryPath == null)
                FileSystem.SetCurrentDirectory(UnityProjectPath);
            else
                FileSystem.SetCurrentDirectory(RepositoryPath);
        }

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

        public IFileSystem FileSystem { get { return NPath.FileSystem; } set { NPath.FileSystem = value; } }
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
                    }
                    else
                        gitInstallPath = GitExecutablePath;
                }
                return gitInstallPath;
            }
        }

        public NPath RepositoryPath { get; private set; }
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