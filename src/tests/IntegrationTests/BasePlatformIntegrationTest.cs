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

        protected void InitializePlatform(NPath repoPath, NPath environmentPath, bool enableEnvironmentTrace, bool setupGit = true)
        {
            InitializeTaskManager();
            InitializeEnvironment(repoPath, environmentPath, enableEnvironmentTrace);

            Platform = new Platform(Environment);
            ProcessManager = new ProcessManager(Environment, GitEnvironment, TaskManager.Token);

            Platform.Initialize(ProcessManager, TaskManager);

            if (setupGit)
            {
                var autoResetEvent = new AutoResetEvent(false);

                var applicationDataPath = Environment.GetSpecialFolder(System.Environment.SpecialFolder.LocalApplicationData).ToNPath();
                var installDetails = new GitInstaller.GitInstallDetails(applicationDataPath, true);

                var zipArchivesPath = TestBasePath.Combine("ZipArchives").CreateDirectory();
                AssemblyResources.ToFile(ResourceType.Platform, "git.zip", zipArchivesPath, Environment);
                AssemblyResources.ToFile(ResourceType.Platform, "git-lfs.zip", zipArchivesPath, Environment);

                var gitInstaller = new GitInstaller(Environment, TaskManager.Token, installDetails);

                NPath result = null;
                Exception ex = null;

                var setupTask = gitInstaller.SetupGitIfNeeded();
                setupTask.OnEnd += (thisTask, path, success, exception) =>
                {
                    result = path;
                    ex = exception;
                    autoResetEvent.Set();
                };

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
