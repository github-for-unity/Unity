using System.Linq;
using System.Threading;
using GitHub.Unity;
using System.Threading.Tasks;

namespace IntegrationTests
{
    class BaseGitEnvironmentTest : BaseGitRepoTest
    {
        protected async Task<IEnvironment> Initialize(NPath repoPath, NPath environmentPath = null,
            bool enableEnvironmentTrace = false)
        {
            TaskManager = new TaskManager();
            SyncContext = new ThreadSynchronizationContext(TaskManager.Token);
            TaskManager.UIScheduler = new SynchronizationContextTaskScheduler(SyncContext);

            Environment = new IntegrationTestEnvironment(repoPath, SolutionDirectory, environmentPath, enableEnvironmentTrace);

            var gitSetup = new GitInstaller(Environment, TaskManager.Token);
            await gitSetup.SetupIfNeeded();
            Environment.GitExecutablePath = gitSetup.GitExecutablePath;

            Platform = new Platform(Environment);
            GitEnvironment = Platform.GitEnvironment;
            ProcessManager = new ProcessManager(Environment, GitEnvironment, TaskManager.Token);

            Platform.Initialize(ProcessManager, TaskManager);

            GitClient = new GitClient(Environment, ProcessManager, Platform.CredentialManager, TaskManager);

            var usageTracker = new NullUsageTracker();

            RepositoryManager = GitHub.Unity.RepositoryManager.CreateInstance(Platform, TaskManager, usageTracker, GitClient, repoPath);
            RepositoryManager.Initialize();

            Environment.Repository = new Repository(GitClient, RepositoryManager, repoPath);

            RepositoryManager.Start();

            DotGitPath = repoPath.Combine(".git");

            if (DotGitPath.FileExists())
            {
                DotGitPath =
                    DotGitPath.ReadAllLines()
                              .Where(x => x.StartsWith("gitdir:"))
                              .Select(x => x.Substring(7).Trim().ToNPath())
                              .First();
            }

            BranchesPath = DotGitPath.Combine("refs", "heads");
            RemotesPath = DotGitPath.Combine("refs", "remotes");
            DotGitIndex = DotGitPath.Combine("index");
            DotGitHead = DotGitPath.Combine("HEAD");
            DotGitConfig = DotGitPath.Combine("config");
            return Environment;
        }

        public override void OnTearDown()
        {
            RepositoryManager?.Stop();
            RepositoryManager?.Dispose();
            RepositoryManager = null;
            base.OnTearDown();
        }

        public IRepositoryManager RepositoryManager { get; private set; }

        protected IPlatform Platform { get; private set; }
        protected IApplicationManager ApplicationManager { get; set; }
        protected IProcessManager ProcessManager { get; private set; }
        protected ITaskManager TaskManager { get; private set; }

        protected IProcessEnvironment GitEnvironment { get; private set; }
        protected IGitClient GitClient { get; set; }
        protected SynchronizationContext SyncContext { get; set; }

        protected NPath DotGitConfig { get; private set; }

        protected NPath DotGitHead { get; private set; }

        protected NPath DotGitIndex { get; private set; }

        protected NPath RemotesPath { get; private set; }

        protected NPath BranchesPath { get; private set; }

        protected NPath DotGitPath { get; private set; }
    }
}
