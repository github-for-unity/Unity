using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using GitHub.Unity;
using NSubstitute;
using Octokit;

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
                var autoResetEvent = new AutoResetEvent(false);

                var applicationDataPath = Environment.GetSpecialFolder(System.Environment.SpecialFolder.LocalApplicationData).ToNPath();
                var installDetails = new GitInstallDetails(applicationDataPath, true);
                var gitInstaller = new GitInstaller(Environment, CancellationToken.None, installDetails);

                NPath result = null;
                Exception ex = null;

                gitInstaller.SetupGitIfNeeded(new ActionTask<NPath>(CancellationToken.None, (b, path) => {
                        result = path;
                        autoResetEvent.Set();
                    }),
                    new ActionTask(CancellationToken.None, (b, exception) => {
                        ex = exception;
                        autoResetEvent.Set();
                    }));

                autoResetEvent.WaitOne();

                if (result == null)
                {
                    if (ex != null)
                    {
                        throw ex;
                    }

                    throw new Exception("Did not install git");
                }
                
                Environment.GitExecutablePath = result;
                GitClient = new GitClient(Environment, ProcessManager, TaskManager.Token);
            }
        }
    }
}
