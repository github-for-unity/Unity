using GitHub.Logging;
using System;
using System.IO;
using System.Linq;

namespace GitHub.Unity
{
    public class DefaultEnvironment : IEnvironment
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

        public DefaultEnvironment(ICacheContainer cacheContainer)
            : this()
        {
            this.CacheContainer = cacheContainer;
        }

        public void Initialize(string unityVersion, NPath extensionInstallPath, NPath unityPath, NPath assetsPath)
        {
            ExtensionInstallPath = extensionInstallPath;
            UnityApplication = unityPath;
            UnityAssetsPath = assetsPath;
            UnityProjectPath = assetsPath.Parent;
            UnityVersion = unityVersion;
            User = new User(CacheContainer);
        }

        public void InitializeRepository(NPath? repositoryPath = null)
        {
            Guard.NotNull(this, FileSystem, nameof(FileSystem));

            //Logger.Trace("InitializeRepository expectedRepositoryPath:{0}", repositoryPath);

            NPath expectedRepositoryPath;
            if (!RepositoryPath.IsInitialized)
            {
                Guard.NotNull(this, UnityProjectPath, nameof(UnityProjectPath));

                //Logger.Trace("RepositoryPath is null");

                expectedRepositoryPath = repositoryPath != null ? repositoryPath.Value : UnityProjectPath;

                if (!expectedRepositoryPath.DirectoryExists(".git"))
                {
                    Logger.Trace(".git folder exists");

                    NPath reporoot = UnityProjectPath.RecursiveParents.FirstOrDefault(d => d.DirectoryExists(".git"));
                    if (reporoot.IsInitialized)
                        expectedRepositoryPath = reporoot;
                }
            }
            else
            {
                //Logger.Trace("Set to RepositoryPath");
                expectedRepositoryPath = RepositoryPath;
            }

            FileSystem.SetCurrentDirectory(expectedRepositoryPath);
            if (expectedRepositoryPath.DirectoryExists(".git"))
            {
                //Logger.Trace("Determined expectedRepositoryPath:{0}", expectedRepositoryPath);
                RepositoryPath = expectedRepositoryPath;
                Repository = new Repository(RepositoryPath, CacheContainer);
            }
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
        public string UnityVersion { get; set; }
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
                if (String.IsNullOrEmpty(gitExecutablePath))
                    GitInstallPath = NPath.Default;
                else
                    GitInstallPath = GitExecutablePath.Resolve().Parent.Parent;
            }
        }

        public NPath GitInstallPath { get; private set; }

        public NPath RepositoryPath { get; private set; }
        public ICacheContainer CacheContainer { get; private set; }
        public IRepository Repository { get; set; }
        public IUser User { get; set; }

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
        public string ExecutableExtension { get { return IsWindows ? ".exe" : null; } }
        protected static ILogging Logger { get; } = LogHelper.GetLogger<DefaultEnvironment>();
    }
}