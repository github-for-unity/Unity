using System.Linq;
using System.Threading;
using GitHub.Unity;
using NSubstitute;
using TestUtils;

namespace IntegrationTests
{
    class BaseGitEnvironmentTest : BaseGitRepoTest
    {
        protected void InitializeEnvironment(NPath repoPath, bool enableEnvironmentTrace = false)
        {
            TaskManager = new TaskManager();
            var sc = new ThreadSynchronizationContext(TaskManager.Token);
            TaskManager.UIScheduler = new SynchronizationContextTaskScheduler(sc);

            Environment = new IntegrationTestEnvironment(SolutionDirectory, enableTrace: enableEnvironmentTrace) {
                RepositoryPath = repoPath,
                UnityProjectPath = repoPath
            };

            var gitSetup = new GitSetup(Environment, FileSystem, CancellationToken.None);
            gitSetup.SetupIfNeeded().Wait();

            Environment.GitExecutablePath = gitSetup.GitExecutablePath;

            FileSystem.SetCurrentDirectory(repoPath);

            Platform = new Platform(Environment, FileSystem);
            GitEnvironment = Platform.GitEnvironment;
            ProcessManager = new ProcessManager(Environment, GitEnvironment);

            Platform.Initialize(ProcessManager, TaskManager);

            Environment.UnityProjectPath = repoPath;
            Environment.GitExecutablePath = GitEnvironment.FindGitInstallationPath(ProcessManager).Result;

            GitClient = new GitClient(Environment, ProcessManager, Platform.CredentialManager, TaskManager);

            var repositoryManagerFactory = new RepositoryManagerFactory();
            RepositoryManager = repositoryManagerFactory.CreateRepositoryManager(Platform, TaskManager, GitClient, repoPath);
            RepositoryManager.Initialize();
            RepositoryManager.Start();

            Environment.Repository = RepositoryManager.Repository;

            DotGitPath = repoPath.Combine(".git");

            if (DotGitPath.FileExists())
            {
                DotGitPath =
                    DotGitPath.ReadAllLines()
                              .Where(x => x.StartsWith("gitdir:"))
                              .Select(x => x.Substring(7).Trim())
                              .First();
            }

            BranchesPath = DotGitPath.Combine("refs", "heads");
            RemotesPath = DotGitPath.Combine("refs", "remotes");
            DotGitIndex = DotGitPath.Combine("index");
            DotGitHead = DotGitPath.Combine("HEAD");
            DotGitConfig = DotGitPath.Combine("config");
        }

        protected override void OnTearDown()
        {
            base.OnTearDown();
            RepositoryManager?.Stop();
        }

        public IRepositoryManager RepositoryManager { get; private set; }

        public IEnvironment Environment { get; private set; }

        protected IPlatform Platform { get; private set; }

        protected IProcessManager ProcessManager { get; private set; }
        protected ITaskManager TaskManager { get; private set; }

        protected IProcessEnvironment GitEnvironment { get; private set; }
        protected IGitClient GitClient { get; set; }

        protected NPath DotGitConfig { get; private set; }

        protected NPath DotGitHead { get; private set; }

        protected NPath DotGitIndex { get; private set; }

        protected NPath RemotesPath { get; private set; }

        protected NPath BranchesPath { get; private set; }

        protected NPath DotGitPath { get; private set; }
    }
}
