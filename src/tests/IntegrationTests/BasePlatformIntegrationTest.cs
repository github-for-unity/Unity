using System.IO;
using System.Threading.Tasks;
using GitHub.Unity;
using NSubstitute;

namespace IntegrationTests
{
    class BasePlatformIntegrationTest : BaseTaskManagerTest
    {
        protected IPlatform Platform { get; private set; }
        protected IProcessManager ProcessManager { get; private set; }
        protected IProcessEnvironment GitEnvironment { get; private set; }
        protected IGitClient GitClient { get; set; }
        public ICacheContainer CacheContainer { get;  set; }

        protected async Task InitializePlatform(NPath repoPath, NPath environmentPath, bool enableEnvironmentTrace)
        {
            InitializeTaskManager();

            CacheContainer = Substitute.For<ICacheContainer>();
            Environment = new IntegrationTestEnvironment(CacheContainer, repoPath, SolutionDirectory, environmentPath,
                enableEnvironmentTrace);

            var gitSetup = new GitInstaller(Environment, TaskManager.Token);
            await gitSetup.SetupIfNeeded();
            Environment.GitExecutablePath = gitSetup.GitExecutablePath;

            Platform = new Platform(Environment);

            GitEnvironment = Platform.GitEnvironment;
            ProcessManager = new ProcessManager(Environment, GitEnvironment, TaskManager.Token);

            Platform.Initialize(ProcessManager, TaskManager);

            GitClient = new GitClient(Environment, ProcessManager, TaskManager);
        }
    }
}
