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
        protected IDownloadManager DownloadManager { get; private set; }
        protected IProcessEnvironment GitEnvironment => Platform.GitEnvironment;
        protected IGitClient GitClient { get; set; }
        public ICacheContainer CacheContainer { get;  set; }

        protected async Task InitializePlatform(NPath repoPath, NPath environmentPath, bool enableEnvironmentTrace)
        {
            InitializeTaskManager();

            CacheContainer = Substitute.For<ICacheContainer>();
            Environment = new IntegrationTestEnvironment(CacheContainer, repoPath, SolutionDirectory, environmentPath, enableEnvironmentTrace);

            Platform = new Platform(Environment);
            ProcessManager = new ProcessManager(Environment, GitEnvironment, TaskManager.Token);
            DownloadManager = new DownloadManager(Environment, ProcessManager, TaskManager.Token);

            var gitInstaller = new GitInstaller(Environment, DownloadManager, TaskManager.Token);
            await gitInstaller.SetupIfNeeded();
            Environment.GitExecutablePath = gitInstaller.GitExecutablePath;

            Platform.Initialize(ProcessManager, TaskManager);

            GitClient = new GitClient(Environment, ProcessManager, TaskManager);
        }
    }
}
