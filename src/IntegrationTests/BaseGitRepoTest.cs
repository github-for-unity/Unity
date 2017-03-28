using System.IO;
using System.Threading;
using GitHub.Unity;
using Ionic.Zip;
using NUnit.Framework;

namespace IntegrationTests
{
    class BaseGitRepoTest : BaseIntegrationTest
    {
        protected override void OnSetup()
        {
            base.OnSetup();

            TestRepoPath = TestBasePath.Combine("IOTestsRepo");

            Environment = new IntegrationTestEnvironment(SolutionDirectory) {
                RepositoryPath = TestRepoPath
            };

            var gitSetup = new GitSetup(Environment, CancellationToken.None);
            gitSetup.SetupIfNeeded().Wait();

            Environment.GitExecutablePath = gitSetup.GitExecutablePath;

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
            var repositoryManager = repositoryManagerFactory.CreateRepositoryManager(Platform, TestRepoPath,
                CancellationToken.None);

            Environment.Repository = repositoryManager.Repository;
        }

        public IEnvironment Environment { get; protected set; }

        protected NPath TestRepoPath { get; private set; }

        protected Platform Platform { get; private set; }

        private static string TestZipFilePath => Path.Combine(SolutionDirectory, "IOTestsRepo.zip");

        protected ProcessManager ProcessManager { get; private set; }

        protected IProcessEnvironment GitEnvironment { get; private set; }
    }
}
