using System;
using GitHub.Unity;

namespace IntegrationTests
{
    class IntegrationTestEnvironment : IEnvironment
    {
        private static readonly ILogging logger = Logging.GetLogger<IntegrationTestEnvironment>();
        private readonly bool enableTrace;

        private readonly NPath integrationTestEnvironmentPath;

        private DefaultEnvironment defaultEnvironment;

        public IntegrationTestEnvironment(NPath repoPath, NPath solutionDirectory, NPath environmentPath = null,
            bool enableTrace = false)
        {
            defaultEnvironment = new DefaultEnvironment();
            environmentPath = environmentPath ??
                defaultEnvironment.GetSpecialFolder(Environment.SpecialFolder.LocalApplicationData)
                                  .ToNPath()
                                  .EnsureDirectoryExists(ApplicationInfo.ApplicationName + "-IntegrationTests");

            integrationTestEnvironmentPath = environmentPath;
            UserCachePath = integrationTestEnvironmentPath.Combine("User");
            SystemCachePath = integrationTestEnvironmentPath.Combine("System");

            var installPath = solutionDirectory.Parent.Parent.Parent.Combine("GitHub.Api");

            Initialize(installPath, solutionDirectory, repoPath.Combine("Assets"));
            Initialize();

            this.enableTrace = enableTrace;

            if (enableTrace)
            {
                logger.Trace("EnvironmentPath: \"{0}\" SolutionDirectory: \"{1}\" ExtensionInstallPath: \"{2}\"",
                    environmentPath, solutionDirectory, ExtensionInstallPath);
            }
        }

        public void Initialize(NPath extensionInstallPath, NPath unityPath, NPath assetsPath)
        {
            defaultEnvironment.Initialize(extensionInstallPath, unityPath, assetsPath);
        }

        public void Initialize()
        {
            defaultEnvironment.Initialize();
        }

        public string ExpandEnvironmentVariables(string name)
        {
            throw new NotImplementedException();
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
            var ensureDirectoryExists = integrationTestEnvironmentPath.EnsureDirectoryExists(folder.ToString());
            var specialFolderPath = ensureDirectoryExists.ToString();

            if (enableTrace)
            {
                logger.Trace("GetSpecialFolder: {0}", specialFolderPath);
            }

            return specialFolderPath;
        }

        public string UserProfilePath => integrationTestEnvironmentPath.CreateDirectory("user-profile-path");

        public NPath Path => Environment.GetEnvironmentVariable("PATH").ToNPath();
        public string NewLine => Environment.NewLine;

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

        public bool IsWindows => defaultEnvironment.IsWindows;
        public bool IsLinux => defaultEnvironment.IsLinux;
        public bool IsMac => defaultEnvironment.IsMac;

        public NPath UnityApplication => defaultEnvironment.UnityApplication;

        public NPath UnityAssetsPath => defaultEnvironment.UnityAssetsPath;

        public NPath UnityProjectPath => defaultEnvironment.UnityProjectPath;

        public NPath ExtensionInstallPath => defaultEnvironment.ExtensionInstallPath;

        public NPath UserCachePath { get; set; }
        public NPath SystemCachePath { get; set; }
        public NPath LogPath { get; set; }

        public NPath RepositoryPath => defaultEnvironment.RepositoryPath;

        public NPath GitInstallPath => defaultEnvironment.GitInstallPath;

        public IRepository Repository { get; set; }
        public IFileSystem FileSystem { get { return defaultEnvironment.FileSystem; } set { defaultEnvironment.FileSystem = value; } }
    }
}
