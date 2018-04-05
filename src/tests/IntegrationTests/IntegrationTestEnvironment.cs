using System;
using System.Globalization;
using GitHub.Unity;
using GitHub.Logging;

namespace IntegrationTests
{


    class IntegrationTestEnvironment : IEnvironment
    {
        private static readonly ILogging logger = LogHelper.GetLogger<IntegrationTestEnvironment>();
        private readonly bool enableTrace;

        private DefaultEnvironment defaultEnvironment;

        public IntegrationTestEnvironment(ICacheContainer cacheContainer,
            NPath repoPath,
            NPath solutionDirectory,
            NPath? environmentPath = null,
            bool enableTrace = false,
            bool initializeRepository = true)
        {
            defaultEnvironment = new DefaultEnvironment(cacheContainer);
            defaultEnvironment.FileSystem.SetCurrentDirectory(repoPath);
            environmentPath = environmentPath ??
                defaultEnvironment.UserCachePath.EnsureDirectoryExists("IntegrationTests");

            UserCachePath = environmentPath.Value.Combine("User");
            SystemCachePath = environmentPath.Value.Combine("System");

            var installPath = solutionDirectory.Parent.Parent.Combine("src", "GitHub.Api");

            Initialize(UnityVersion, installPath, solutionDirectory, NPath.Default, repoPath.Combine("Assets"));

            InitializeRepository(initializeRepository ? (NPath?)repoPath : null);

            this.enableTrace = enableTrace;

            if (enableTrace)
            {
                logger.Trace("EnvironmentPath: \"{0}\" SolutionDirectory: \"{1}\" ExtensionInstallPath: \"{2}\"",
                    environmentPath, solutionDirectory, ExtensionInstallPath);
            }
        }

        public void Initialize(string unityVersion, NPath extensionInstallPath, NPath unityPath, NPath unityContentsPath, NPath assetsPath)
        {
            defaultEnvironment.Initialize(unityVersion, extensionInstallPath, unityPath, unityContentsPath, assetsPath);
        }

        public void InitializeRepository(NPath? expectedPath = null)
        {
            defaultEnvironment.InitializeRepository(expectedPath);
        }

        public string ExpandEnvironmentVariables(string name)
        {
            return name;
        }

        public string GetEnvironmentVariable(string v)
        {
            var environmentVariable = defaultEnvironment.GetEnvironmentVariable(v);
            if (enableTrace)
            {
                logger.Trace("GetEnvironmentVariable: {0}={1}", v, environmentVariable);
            }
            return environmentVariable;
        }

        public string GetSpecialFolder(Environment.SpecialFolder folder)
        {
            var ensureDirectoryExists = UserCachePath.Parent.EnsureDirectoryExists(folder.ToString());
            var specialFolderPath = ensureDirectoryExists.ToString();

            if (enableTrace)
            {
                logger.Trace("GetSpecialFolder: {0}", specialFolderPath);
            }

            return specialFolderPath;
        }

        public string UserProfilePath => UserCachePath.Parent.CreateDirectory("user profile path");

        public string Path { get; set; } = Environment.GetEnvironmentVariable("PATH").ToNPath();

        public string NewLine => Environment.NewLine;
        public string UnityVersion => "5.6";

        public bool IsCustomGitExecutable { get; set; }

        public NPath GitExecutablePath
        {
            get { return defaultEnvironment.GitExecutablePath; }
            set
            {
                if (enableTrace)
                {
                    logger.Trace("Setting GitExecutablePath to " + value);
                }
                defaultEnvironment.GitExecutablePath = value;
            }
        }

        public NPath NodeJsExecutablePath => defaultEnvironment.NodeJsExecutablePath;

        public NPath OctorunScriptPath { get; set; }

        public bool IsWindows => defaultEnvironment.IsWindows;
        public bool IsLinux => defaultEnvironment.IsLinux;
        public bool IsMac => defaultEnvironment.IsMac;

        public NPath UnityApplication => defaultEnvironment.UnityApplication;

        public NPath UnityApplicationContents => defaultEnvironment.UnityApplicationContents;

        public NPath UnityAssetsPath => defaultEnvironment.UnityAssetsPath;

        public NPath UnityProjectPath => defaultEnvironment.UnityProjectPath;

        public NPath ExtensionInstallPath => defaultEnvironment.ExtensionInstallPath;

        public NPath UserCachePath { get { return defaultEnvironment.UserCachePath; } set { defaultEnvironment.UserCachePath = value; } }
        public NPath SystemCachePath { get { return defaultEnvironment.SystemCachePath; } set { defaultEnvironment.SystemCachePath = value; } }
        public NPath LogPath => defaultEnvironment.LogPath;

        public NPath RepositoryPath => defaultEnvironment.RepositoryPath;

        public NPath GitInstallPath => defaultEnvironment.GitInstallPath;
        public NPath GitLfsInstallPath => defaultEnvironment.GitLfsInstallPath;
        public NPath GitLfsExecutablePath { get { return defaultEnvironment.GitLfsExecutablePath; } set { defaultEnvironment.GitLfsExecutablePath = value; } }

        public IRepository Repository { get { return defaultEnvironment.Repository; } set { defaultEnvironment.Repository = value; } }
        public IUser User { get { return defaultEnvironment.User; } set { defaultEnvironment.User = value; } }
        public IFileSystem FileSystem { get { return defaultEnvironment.FileSystem; } set { defaultEnvironment.FileSystem = value; } }
        public string ExecutableExtension => defaultEnvironment.ExecutableExtension;

        public ICacheContainer CacheContainer => defaultEnvironment.CacheContainer;
    }
}
