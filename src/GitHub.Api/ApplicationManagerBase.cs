using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Octokit;

namespace GitHub.Unity
{
    abstract class ApplicationManagerBase : IApplicationManager
    {
        protected static ILogging Logger { get; } = Logging.GetLogger<IApplicationManager>();

        private IEnvironment environment;
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
            Platform = new Platform(Environment);
        }

        protected void Initialize()
        {
            UserSettings = new UserSettings(Environment);
            LocalSettings = new LocalSettings(Environment);
            SystemSettings = new SystemSettings(Environment);

            UserSettings.Initialize();
            LocalSettings.Initialize();
            SystemSettings.Initialize();

            Logging.TracingEnabled = UserSettings.Get(Constants.TraceLoggingKey, false);
            ProcessManager = new ProcessManager(Environment, Platform.GitEnvironment, CancellationToken);
            Platform.Initialize(ProcessManager, TaskManager);
            GitClient = new GitClient(Environment, ProcessManager, Platform.CredentialManager, TaskManager);
        }

        public virtual ITask Run()
        {
            Logger.Trace("Run");

            var progress = new ProgressReport();
            return new ActionTask(SetupAndRestart(progress))
                .Then(new ActionTask(LoadKeychain()));
        }

        private async Task LoadKeychain()
        {
            Logger.Trace("Loading Keychain");

            var firstConnection = Platform.Keychain.Connections.FirstOrDefault();
            if (firstConnection == null)
            {
                Logger.Trace("No Host Found");
            }
            else
            {
                Logger.Trace("Loading Connection to Host:\"{0}\"", firstConnection);
                var keychainAdapter = await Platform.Keychain.Load(firstConnection).SafeAwait();
                if (keychainAdapter.OctokitCredentials == Credentials.Anonymous)
                { }
            }
        }

        protected abstract string DetermineInstallationPath();
        protected abstract string GetAssetsPath();
        protected abstract string GetUnityPath();

        public virtual ITask RestartRepository()
        {
            return new FuncTask<bool>(TaskManager.Token, _ =>
            {
                Environment.Initialize();

                if (Environment.RepositoryPath == null)
                    return false;
                return true;

            })
            .Defer(async s =>
            {
                if (!s)
                    return false;

                var repositoryPathConfiguration = new RepositoryPathConfiguration(Environment.RepositoryPath);
                var gitConfig = new GitConfig(repositoryPathConfiguration.DotGitConfig);

                var repositoryWatcher = new RepositoryWatcher(Platform, repositoryPathConfiguration, TaskManager.Token);
                repositoryManager = new RepositoryManager(Platform, TaskManager, UsageTracker, gitConfig, repositoryWatcher,
                        GitClient, repositoryPathConfiguration, TaskManager.Token);

                await repositoryManager.Initialize().SafeAwait();
                Environment.Repository = repositoryManager.Repository;
                Logger.Trace($"Got a repository? {Environment.Repository}");
                repositoryManager.Start();
                return true;
            })
            .Finally((_, __) => { });
        }

        private async Task SetupAndRestart(ProgressReport progress)
        {
            Logger.Trace("SetupAndRestart");

            var gitSetup = new GitSetup(Environment, CancellationToken);
            var expectedPath = gitSetup.GitInstallationPath;
            var setupDone = await gitSetup.SetupIfNeeded(progress.Percentage, progress.Remaining);
            if (setupDone)
                Environment.GitExecutablePath = gitSetup.GitExecutablePath;
            else
                Environment.GitExecutablePath = await LookForGitInstallationPath();

            Logger.Trace("Environment.GitExecutablePath \"{0}\" Exists:{1}", gitSetup.GitExecutablePath, gitSetup.GitExecutablePath.FileExists());

            await RestartRepository().StartAwait();

            if (Environment.IsWindows)
            {
                var credentialHelper = await GitClient.GetConfig("credential.helper", GitConfigSource.Global).StartAwait();

                if (string.IsNullOrEmpty(credentialHelper))
                {
                    await GitClient.SetConfig("credential.helper", "wincred", GitConfigSource.Global).StartAwait();
                }
            }
        }

        private async Task<NPath> LookForGitInstallationPath()
        {
            NPath cachedGitInstallPath = null;
            var path = SystemSettings.Get(Constants.GitInstallPathKey);
            if (!String.IsNullOrEmpty(path))
                cachedGitInstallPath = path.ToNPath();

            // Root paths
            if (cachedGitInstallPath != null && cachedGitInstallPath.DirectoryExists())
            {
                return cachedGitInstallPath;
            }
            return await GitClient.FindGitInstallation().SafeAwait();
        }

        protected void SetupMetrics(string unityVersion, bool firstRun)
        {
            Logger.Trace("Setup metrics");

            var usagePath = Environment.UserCachePath.Combine(Constants.UsageFile);

            string id = null;
            if (UserSettings.Exists(Constants.GuidKey))
            {
                id = UserSettings.Get(Constants.GuidKey);
            }

            if (String.IsNullOrEmpty(id))
            {
                id = Guid.NewGuid().ToString();
                UserSettings.Set(Constants.GuidKey, id);
            }

            UsageTracker = new UsageTracker(UserSettings, usagePath, id, unityVersion);

            if (firstRun)
            {
                UsageTracker.IncrementLaunchCount();
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
        public IUsageTracker UsageTracker { get; protected set; }
    }
}
