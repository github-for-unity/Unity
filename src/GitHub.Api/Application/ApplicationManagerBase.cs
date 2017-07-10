using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Octokit;
using System.Collections.Generic;

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
        }

        protected void Initialize()
        {
            // accessing Environment triggers environment initialization if it hasn't happened yet
            Platform = new Platform(Environment);

            UserSettings = new UserSettings(Environment);
            LocalSettings = new LocalSettings(Environment);
            SystemSettings = new SystemSettings(Environment);

            UserSettings.Initialize();
            LocalSettings.Initialize();
            SystemSettings.Initialize();

            Logging.TracingEnabled = UserSettings.Get(Constants.TraceLoggingKey, false);
            ProcessManager = new ProcessManager(Environment, Platform.GitEnvironment, CancellationToken);
            Platform.Initialize(ProcessManager, TaskManager);
        }

        public virtual async Task Run(bool firstRun)
        {
            Logger.Trace("Run - CurrentDirectory {0}", NPath.CurrentDirectory);

            if (Environment.GitExecutablePath != null)
            {
                GitClient = new GitClient(Environment, ProcessManager, Platform.CredentialManager, TaskManager);
            }
            else
            {
                var progress = new ProgressReport();

                var gitClient = new GitClient(Environment, ProcessManager, Platform.CredentialManager, TaskManager);
                var gitSetup = new GitInstaller(Environment, CancellationToken);
                var expectedPath = gitSetup.GitInstallationPath;
                var setupDone = await gitSetup.SetupIfNeeded(progress.Percentage, progress.Remaining);
                if (setupDone)
                    Environment.GitExecutablePath = gitSetup.GitExecutablePath;
                else
                    Environment.GitExecutablePath = await LookForGitInstallationPath(gitClient, SystemSettings).SafeAwait();

                GitClient = gitClient;

                Logger.Trace("Environment.GitExecutablePath \"{0}\" Exists:{1}", gitSetup.GitExecutablePath, gitSetup.GitExecutablePath.FileExists());

                if (Environment.IsWindows)
                {
                    var credentialHelper = await GitClient.GetConfig("credential.helper", GitConfigSource.Global).StartAwait();
                    if (string.IsNullOrEmpty(credentialHelper))
                    {
                        await GitClient.SetConfig("credential.helper", "wincred", GitConfigSource.Global).StartAwait();
                    }
                }
            }

            RestartRepository();
            InitializeUI();

            new ActionTask(CancellationToken, SetupMetrics).Start();
            new ActionTask(new Task(() => LoadKeychain().Start())).Start();
            new ActionTask(CancellationToken, RunRepositoryManager).Start();
        }

        public ITask InitializeRepository()
        {
            Logger.Trace("Running Repository Initialize");

            var targetPath = NPath.CurrentDirectory;

            var unityYamlMergeExec = Environment.UnityApplication.Parent.Combine("Tools", "UnityYAMLMerge");
            var yamlMergeCommand = $@"'{unityYamlMergeExec}' merge -p ""$BASE"" ""$REMOTE"" ""$LOCAL"" ""$MERGED""";

            var gitignore = targetPath.Combine(".gitignore");
            var gitAttrs = targetPath.Combine(".gitattributes");
            var assetsGitignore = targetPath.Combine("Assets", ".gitignore");

            var filesForInitialCommit = new List<string> { gitignore, gitAttrs, assetsGitignore };

            var task = 
                GitClient.Init()
                .Then(GitClient.SetConfig("merge.unityyamlmerge.cmd", yamlMergeCommand, GitConfigSource.Local))
                .Then(GitClient.SetConfig("merge.unityyamlmerge.trustExitCode", "false", GitConfigSource.Local))
                .Then(GitClient.LfsInstall())
                .ThenInUI(SetProjectToTextSerialization)
                .Then(new ActionTask(CancellationToken, _ => {
                    AssemblyResources.ToFile(ResourceType.Generic, ".gitignore", targetPath, Environment);
                    AssemblyResources.ToFile(ResourceType.Generic, ".gitattributes", targetPath, Environment);

                    assetsGitignore.CreateFile();
                }))
                .Then(GitClient.Add(filesForInitialCommit))
                .Then(GitClient.Commit("Initial commit", null))
                .Then(RestartRepository)
                .ThenInUI(InitializeUI)
                .Then(RunRepositoryManager);
            return task;
        }

        public void RestartRepository()
        {
            Environment.InitializeRepository();
            if (Environment.RepositoryPath != null)
            {
                var repositoryPathConfiguration = new RepositoryPathConfiguration(Environment.RepositoryPath);
                var gitConfig = new GitConfig(repositoryPathConfiguration.DotGitConfig);

                var repositoryWatcher = new RepositoryWatcher(Platform, repositoryPathConfiguration, TaskManager.Token);
                repositoryManager = new RepositoryManager(Platform, TaskManager, UsageTracker, gitConfig, repositoryWatcher,
                        GitClient, repositoryPathConfiguration, TaskManager.Token);
                Environment.Repository = repositoryManager.Repository;
                Logger.Trace($"Got a repository? {Environment.Repository}");
            }
        }

        private void RunRepositoryManager()
        {
            Logger.Trace("RunRepositoryManager");

            if (Environment.RepositoryPath != null)
            {
                new ActionTask(repositoryManager.Initialize())
                    .Then(repositoryManager.Start)
                    .Start();;
            }
        }

        private async Task LoadKeychain()
        {
            Logger.Trace("Loading Keychain");

            var firstConnection = Platform.Keychain.Hosts.FirstOrDefault();
            if (firstConnection == null)
            {
                Logger.Trace("No Host Found");
            }
            else
            {
                Logger.Trace("Loading Connection to Host:\"{0}\"", firstConnection);
                await Platform.Keychain.Load(firstConnection).SafeAwait();
            }
        }

        private static async Task<NPath> LookForGitInstallationPath(IGitClient gitClient, ISettings systemSettings)
        {
            NPath cachedGitInstallPath = null;
            var path = systemSettings.Get(Constants.GitInstallPathKey);
            if (!String.IsNullOrEmpty(path))
                cachedGitInstallPath = path.ToNPath();

            // Root paths
            if (cachedGitInstallPath != null && cachedGitInstallPath.DirectoryExists())
            {
                return cachedGitInstallPath;
            }
            return await gitClient.FindGitInstallation();
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

        protected abstract void SetupMetrics();
        protected abstract void InitializeUI();
        protected abstract void SetProjectToTextSerialization();

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

        public abstract IEnvironment Environment { get; }

        public IPlatform Platform { get; protected set; }
        public virtual IProcessEnvironment GitEnvironment { get; set; }
        public IProcessManager ProcessManager { get; protected set; }
        public CancellationToken CancellationToken { get { return TaskManager.Token; } }
        public ITaskManager TaskManager { get; protected set; }
        public IGitClient GitClient { get; protected set; }
        public ISettings LocalSettings { get; protected set; }
        public ISettings SystemSettings { get; protected set; }
        public ISettings UserSettings { get; protected set; }
        public IUsageTracker UsageTracker { get; protected set; }

        protected TaskScheduler UIScheduler { get; private set; }
        protected SynchronizationContext SynchronizationContext { get; private set; }
        protected IRepositoryManager RepositoryManager { get { return repositoryManager; } }
    }
}
