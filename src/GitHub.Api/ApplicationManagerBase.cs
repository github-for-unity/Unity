using System;
using System.Threading;
using System.Threading.Tasks;

namespace GitHub.Unity
{
    abstract class ApplicationManagerBase : IApplicationManager
    {
        protected static readonly ILogging logger = Logging.GetLogger<IApplicationManager>();

        private IEnvironment environment;
        private AppConfiguration appConfiguration;
        private RepositoryManager repositoryManager;

        public ApplicationManagerBase(SynchronizationContext synchronizationContext)
        {
            SynchronizationContext = synchronizationContext;
            SynchronizationContext.SetSynchronizationContext(SynchronizationContext);
            ThreadingHelper.SetMainThread();
            UIScheduler = TaskScheduler.FromCurrentSynchronizationContext();
            ThreadingHelper.MainThreadScheduler = UIScheduler;
            TaskManager = new TaskManager(UIScheduler);
            // accessing Environment triggers environment initialization if it hasn't happened yet
            Platform = new Platform(Environment, Environment.FileSystem);
        }

        protected void Initialize()
        {
            UserSettings = new UserSettings(Environment);
            LocalSettings = new LocalSettings(Environment);
            SystemSettings = new SystemSettings(Environment);

            UserSettings.Initialize();
            LocalSettings.Initialize();
            SystemSettings.Initialize();

            Logging.TracingEnabled = UserSettings.Get("EnableTraceLogging", false);
            ProcessManager = new ProcessManager(Environment, Platform.GitEnvironment, CancellationToken);
            Platform.Initialize(ProcessManager, TaskManager);
            GitClient = new GitClient(Environment, ProcessManager, Platform.CredentialManager, TaskManager);
        }

        public virtual ITask Run()
        {
            ITask task = null;
            try
            {
                task = RunInternal();
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                throw;
            }
            return task;
        }

        protected abstract string DetermineInstallationPath();
        protected abstract string GetAssetsPath();
        protected abstract string GetUnityPath();

        public virtual ITask RestartRepository()
        {
            return new ActionTask(TaskManager.Token, _ =>
            {
                Environment.Initialize();

                if (Environment.RepositoryPath != null)
                {
                    try
                    {
                        var repositoryManagerFactory = new RepositoryManagerFactory();
                        repositoryManager = repositoryManagerFactory.CreateRepositoryManager(Platform, TaskManager, GitClient,  Environment.RepositoryPath);
                    }
                    catch (Exception ex)
                    {
                        logger.Error(ex);
                    }
                    Environment.Repository = repositoryManager.Repository;
                    repositoryManager.Initialize();
                    repositoryManager.Start();
                }
            });
        }

        private async Task SetupAndRestart(ProgressReport progress)
        {
            var gitSetup = new GitSetup(Environment, Environment.FileSystem, CancellationToken);
            var expectedPath = gitSetup.GitInstallationPath;
            var setupDone = await gitSetup.SetupIfNeeded(progress.Percentage, progress.Remaining);
            if (setupDone)
                Environment.GitExecutablePath = gitSetup.GitExecutablePath;
            else
                Environment.GitExecutablePath = await LookForGitInstallationPath();

            logger.Trace("Environment.GitExecutablePath \"{0}\" Exists:{1}", gitSetup.GitExecutablePath, gitSetup.GitExecutablePath.FileExists());

            await RestartRepository().Task;

            if (Environment.IsWindows)
            {
                string credentialHelper = null;
                var gitConfigGetTask = new GitConfigGetTask("credential.helper", GitConfigSource.Global, CancellationToken);

                await gitConfigGetTask.Task;

                if (string.IsNullOrEmpty(credentialHelper))
                {
                    var gitConfigSetTask = new GitConfigSetTask("credential.helper", "wincred", GitConfigSource.Global, CancellationToken);

                    await gitConfigSetTask.Task;
                }
            }
        }

        private ITask RunInternal()
        {
            var progress = new ProgressReport();
            return new ActionTask(SetupAndRestart(progress));
        }

        private async Task<NPath> LookForGitInstallationPath()
        {
            NPath cachedGitInstallPath = null;
            var path = SystemSettings.Get("GitInstallPath");
            if (!String.IsNullOrEmpty(path))
                cachedGitInstallPath = path.ToNPath();

            // Root paths
            if (cachedGitInstallPath == null ||
               !cachedGitInstallPath.DirectoryExists())
            {
                return await GitEnvironment.FindGitInstallationPath(ProcessManager).StartAwait();
            }
            else
            {
                return cachedGitInstallPath;
            }
        }

        private bool disposed = false;
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (disposed) return;
                disposed = true;
                if (TaskManager != null) TaskManager.Stop();
                if (repositoryManager != null) repositoryManager.Dispose();
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }

        public AppConfiguration AppConfiguration
        {
            get
            {
                return appConfiguration ?? (appConfiguration = new AppConfiguration());
            }
        }

        public IEnvironment Environment
        {
            get
            {
                // if this is called while still null, it's because Unity wants
                // to render something and we need to load icons, and that runs
                // before EntryPoint. Do an early initialization
                if (environment == null)
                {
                    environment = new DefaultEnvironment();
                    var assetsPath = GetAssetsPath();
                    var unityPath = GetUnityPath();
                    var extensionInstallPath = DetermineInstallationPath();

                    // figure out where we are
                    environment.Initialize(extensionInstallPath.ToNPath(), unityPath.ToNPath(), assetsPath.ToNPath());
                }
                return environment;
            }
            protected set { environment = value; }
        }

        public IPlatform Platform { get; protected set; }
        public virtual IProcessEnvironment GitEnvironment { get; set; }
        public IProcessManager ProcessManager { get; protected set; }
        public CancellationToken CancellationToken { get { return TaskManager.Token; } }
        public ITaskManager TaskManager { get; protected set; }
        public IGitClient GitClient { get; protected set; }


        protected TaskScheduler UIScheduler { get; private set; }
        protected SynchronizationContext SynchronizationContext { get; private set; }
        protected IRepositoryManager RepositoryManager { get { return repositoryManager; } }

        public ISettings LocalSettings { get; protected set; }
        public ISettings SystemSettings { get; protected set; }
        public ISettings UserSettings { get; protected set; }
    }
}
