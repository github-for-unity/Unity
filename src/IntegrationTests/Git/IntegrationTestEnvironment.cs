using System;
using GitHub.Unity;

namespace IntegrationTests
{
    class IntegrationTestEnvironment : IEnvironment
    {
        private static readonly ILogging logger = Logging.GetLogger<IntegrationTestEnvironment>();
        private DefaultEnvironment defaultEnvironment;

        private string gitExecutablePath;
        private NPath integrationTestEnvironmentPath;
        private string unityProjectPath;

        public IntegrationTestEnvironment(NPath environmentPath = null)
        {
            defaultEnvironment = new DefaultEnvironment();

            environmentPath = environmentPath ??
                defaultEnvironment.GetSpecialFolder(Environment.SpecialFolder.LocalApplicationData)
                           .ToNPath()
                           .EnsureDirectoryExists(ApplicationInfo.ApplicationName + "-IntegrationTests");

            logger.Trace("EnvironmentPath: \"{0}\"", environmentPath);

            integrationTestEnvironmentPath = environmentPath;
        }

        public string ExpandEnvironmentVariables(string name)
        {
            throw new NotImplementedException();
        }

        public string GetEnvironmentVariable(string v)
        {
            var environmentVariable = defaultEnvironment.GetEnvironmentVariable(v);
            logger.Trace("GetEnvironmentVariable: {0}={1}", v, environmentVariable);
            return environmentVariable;
        }

        public string GetSpecialFolder(Environment.SpecialFolder folder)
        {
            return integrationTestEnvironmentPath.EnsureDirectoryExists(folder.ToString());
        }

        public string UserProfilePath => integrationTestEnvironmentPath.CreateDirectory("user-profile-path");

        public string Path => Environment.GetEnvironmentVariable("PATH");
        public string NewLine => Environment.NewLine;

        public string GitExecutablePath
        {
            get { return gitExecutablePath; }
            set
            {
                logger.Trace("Setting GitExecutablePath to " + value);
                gitExecutablePath = value;
            }
        }

        public bool IsWindows => defaultEnvironment.IsWindows;
        public bool IsLinux => defaultEnvironment.IsLinux;
        public bool IsMac => defaultEnvironment.IsMac;

        public string UnityAssetsPath { get; set; }

        public string UnityProjectPath
        {
            get { return unityProjectPath; }
            set
            {
                logger.Trace("Setting UnityProjectPath to " + value);
                unityProjectPath = value;
            }
        }

        public string ExtensionInstallPath
        {
            get { return integrationTestEnvironmentPath.EnsureDirectoryExists("ExtensionInstallPath"); }
            set { throw new NotImplementedException(); }
        }

        public string RepositoryPath { get; set; }

        public string GitInstallPath
        {
            get { return integrationTestEnvironmentPath.EnsureDirectoryExists("GitInstallPath"); }
            set { throw new NotImplementedException(); }
        }

        public IRepository Repository { get; set; }
    }
}
