using System.Threading;
using GitHub.Unity;

namespace IntegrationTests
{
    class BaseGitEnvironmentTest : BaseGitRepoTest
    {
        protected IEnvironment Environment { get; private set; }

        protected Platform Platform { get; private set; }

        protected ProcessManager ProcessManager { get; private set; }

        protected IProcessEnvironment GitEnvironment { get; private set; }

        protected override void OnSetup()
        {
            base.OnSetup();

            Environment = new IntegrationTestEnvironment(SolutionDirectory)
            {
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

            var repositoryManagerFactory = new RepositoryManagerFactory();
            var repositoryManager = repositoryManagerFactory.CreateRepositoryManager(Platform, TestRepoPath,
                CancellationToken.None);

            Environment.Repository = repositoryManager.Repository;
        }
    }
}