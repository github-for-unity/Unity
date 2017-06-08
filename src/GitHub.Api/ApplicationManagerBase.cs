using System;
using System.Diagnostics;
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
            CancellationTokenSource = new CancellationTokenSource();
            // accessing Environment triggers environment initialization if it hasn't happened yet
            Platform = new Platform(Environment, Environment.FileSystem);
        }

        protected void Initialize(IUIDispatcher uiDispatcher)
        {
            UserSettings = new UserSettings(Environment);
            UserSettings.Initialize();
            Logging.TracingEnabled = UserSettings.Get("EnableTraceLogging", false);

            LocalSettings = new LocalSettings(Environment);
            LocalSettings.Initialize();

            SystemSettings = new SystemSettings(Environment);
            SystemSettings.Initialize();

            ProcessManager = new ProcessManager(Environment, Platform.GitEnvironment, CancellationToken);
            Platform.Initialize(ProcessManager, uiDispatcher);
        }

        public virtual Task Run()
        {
            Task task = null;
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

        public virtual async Task RestartRepository()
        {
            await ThreadingHelper.SwitchToThreadAsync();

            Environment.Initialize();

            if (Environment.RepositoryPath != null)
            {
                try
                {
                    var repositoryManagerFactory = new RepositoryManagerFactory();
                    repositoryManager = repositoryManagerFactory.CreateRepositoryManager(Platform, TaskRunner, UsageTracker, Environment.RepositoryPath, CancellationToken);
                }
                catch (Exception ex)
                {
                    logger.Error(ex);
                }
                Environment.Repository = repositoryManager.Repository;
                repositoryManager.Initialize();
                repositoryManager.Start();
            }
        }

        private async Task RunInternal()
        {
            await ThreadingHelper.SwitchToThreadAsync();

            var gitSetup = new GitSetup(Environment, Environment.FileSystem, CancellationToken);
            var expectedPath = gitSetup.GitInstallationPath;

            var setupDone = await gitSetup.SetupIfNeeded(
                //new Progress<float>(x => logger.Trace("Percentage: {0}", x)),
                //new Progress<long>(x => logger.Trace("Remaining: {0}", x))
            );

            if (setupDone)
                Environment.GitExecutablePath = gitSetup.GitExecutablePath;
            else
                Environment.GitExecutablePath = await LookForGitInstallationPath();

            logger.Trace("Environment.GitExecutablePath \"{0}\" Exists:{1}", gitSetup.GitExecutablePath, gitSetup.GitExecutablePath.FileExists());

            await RestartRepository();

            if (Environment.IsWindows)
            {
                string credentialHelper = null;
                var gitConfigGetTask = new GitConfigGetTask(Environment, ProcessManager,
                    new TaskResultDispatcher<string>(s => {
                        credentialHelper = s;
                    }), "credential.helper", GitConfigSource.Global);


                await gitConfigGetTask.RunAsync(CancellationToken.None);

                if (string.IsNullOrEmpty(credentialHelper))
                {
                    var gitConfigSetTask = new GitConfigSetTask(Environment, ProcessManager,
                        new TaskResultDispatcher<string>(s => { }), "credential.helper", "wincred",
                        GitConfigSource.Global);

                    await gitConfigSetTask.RunAsync(CancellationToken.None);
                }
            }
        }

        private async Task<string> LookForGitInstallationPath()
        {
            NPath cachedGitInstallPath = null;
            var path = SystemSettings.Get("GitInstallPath");
            if (!String.IsNullOrEmpty(path))
                cachedGitInstallPath = path.ToNPath();

            // Root paths
            if (cachedGitInstallPath == null ||
               !cachedGitInstallPath.DirectoryExists())
            {
                return await GitEnvironment.FindGitInstallationPath(ProcessManager);
            }
            else
            {
                return cachedGitInstallPath.ToString();
            }
        }

        private bool disposed = false;
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (disposed) return;
                disposed = true;
                if (CancellationTokenSource != null) CancellationTokenSource.Cancel();
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
        public ITaskResultDispatcher MainThreadResultDispatcher { get; protected set; }
        public CancellationToken CancellationToken { get; protected set; }
        public ITaskRunner TaskRunner { get; protected set; }

        protected CancellationTokenSource CancellationTokenSource { get; private set; }
        protected TaskScheduler UIScheduler { get; private set; }
        protected SynchronizationContext SynchronizationContext { get; private set; }
        protected IRepositoryManager RepositoryManager { get { return repositoryManager; } }

        public ISettings LocalSettings { get; protected set; }
        public ISettings SystemSettings { get; protected set; }
        public ISettings UserSettings { get; protected set; }
        public IUsageTracker UsageTracker { get; protected set; }
    }
}
