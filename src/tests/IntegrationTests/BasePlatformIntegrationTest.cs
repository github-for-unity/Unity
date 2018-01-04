using System.IO;
using System.Threading;
using System.Threading.Tasks;
using GitHub.Unity;
using NSubstitute;

namespace IntegrationTests
{
    class BasePlatformIntegrationTest : BaseTaskManagerTest
    {
        protected IPlatform Platform { get; private set; }
        protected IProcessManager ProcessManager { get; private set; }
        protected IProcessEnvironment GitEnvironment => Platform.GitEnvironment;
        protected IGitClient GitClient { get; set; }
        public ICacheContainer CacheContainer { get;  set; }

        protected void InitializePlatform(NPath repoPath, NPath environmentPath, bool enableEnvironmentTrace, bool setupGit = true)
        {
            InitializeTaskManager();

            CacheContainer = Substitute.For<ICacheContainer>();
            Environment = new IntegrationTestEnvironment(CacheContainer, repoPath, SolutionDirectory, environmentPath,
                enableEnvironmentTrace);

            Platform = new Platform(Environment);
            ProcessManager = new ProcessManager(Environment, GitEnvironment, TaskManager.Token);

            Platform.Initialize(ProcessManager, TaskManager);

            if (setupGit)
            {
                var applicationDataPath = Environment.GetSpecialFolder(System.Environment.SpecialFolder.LocalApplicationData).ToNPath();
                var installDetails = new GitInstallDetails(applicationDataPath, true);
                var gitInstallTask = new PortableGitInstallTask(CancellationToken.None, Environment, installDetails);

                var installPath = gitInstallTask.Start().Result;
                Environment.GitExecutablePath = installPath;

                GitClient = new GitClient(Environment, ProcessManager, TaskManager.Token);
            }
        }
    }
}
