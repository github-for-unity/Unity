using System.IO;
using System.Linq;
using System.Threading;
using GitHub.Unity;
using Ionic.Zip;
using NUnit.Framework;

namespace IntegrationTests
{
    class BaseGitIntegrationTest : BaseIntegrationTest
    {
        protected override void OnSetup()
        {
            base.OnSetup();

            Platform = new Platform(Environment, FileSystem, new TestUIDispatcher());
            GitEnvironment = Platform.GitEnvironment;
            ProcessManager = new ProcessManager(Environment, GitEnvironment);

            Environment.UnityProjectPath = TestBasePath;
            Environment.GitExecutablePath = GitEnvironment.FindGitInstallationPath(ProcessManager).Result;

            using (var zipFile = new ZipFile(TestZipFilePath))
            {
                zipFile.ExtractAll(TestBasePath.ToString(), ExtractExistingFileAction.OverwriteSilently);
            }

            var repositoryManager = new RepositoryManager(TestBasePath, Platform, CancellationToken.None);
            Environment.Repository = repositoryManager.Repository;
        }

        public Platform Platform { get; set; }

        private static string SolutionDirectory => TestContext.CurrentContext.TestDirectory;

        private static string TestZipFilePath => Path.Combine(SolutionDirectory, "IOTestsRepo.zip");

        protected ProcessManager ProcessManager { get; set; }

        protected IProcessEnvironment GitEnvironment { get; set; }
    }
}
