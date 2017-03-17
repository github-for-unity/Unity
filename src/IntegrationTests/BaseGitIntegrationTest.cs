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

            TestRepoPath = TestBasePath.Combine("IOTestsRepo");
            FileSystem.SetCurrentDirectory(TestRepoPath);

            Platform = new Platform(Environment, FileSystem, new TestUIDispatcher());
            GitEnvironment = Platform.GitEnvironment;
            ProcessManager = new ProcessManager(Environment, GitEnvironment);
            Platform.Initialize(ProcessManager);

            Environment.UnityProjectPath = TestRepoPath;
            Environment.GitExecutablePath = GitEnvironment.FindGitInstallationPath(ProcessManager).Result;

            using (var zipFile = new ZipFile(TestZipFilePath))
            {
                zipFile.ExtractAll(TestBasePath.ToString(), ExtractExistingFileAction.OverwriteSilently);
            }

            var repositoryManagerFactory = new RepositoryManagerFactory();
            var repositoryManager = repositoryManagerFactory.CreateRepositoryManager(Platform, TestRepoPath, CancellationToken.None);

            Environment.Repository = repositoryManager.Repository;
        }

        protected NPath TestRepoPath { get; private set; }

        protected Platform Platform { get; private set; }

        private static string SolutionDirectory => TestContext.CurrentContext.TestDirectory;

        private static string TestZipFilePath => Path.Combine(SolutionDirectory, "IOTestsRepo.zip");

        protected ProcessManager ProcessManager { get; private set; }

        protected IProcessEnvironment GitEnvironment { get; private set; }
    }
}
