using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using GitHub.Logging;

namespace GitHub.Unity
{
    abstract class ApplicationManagerBase : IApplicationManager
    {
        protected static ILogging Logger { get; } = LogHelper.GetLogger<IApplicationManager>();

        private RepositoryManager repositoryManager;

        public ApplicationManagerBase(SynchronizationContext synchronizationContext)
        {
            SynchronizationContext = synchronizationContext;
            SynchronizationContext.SetSynchronizationContext(SynchronizationContext);
            ThreadingHelper.SetUIThread();
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

            LogHelper.TracingEnabled = UserSettings.Get(Constants.TraceLoggingKey, false);
            ProcessManager = new ProcessManager(Environment, Platform.GitEnvironment, CancellationToken);
            Platform.Initialize(ProcessManager, TaskManager);
            GitClient = new GitClient(Environment, ProcessManager, TaskManager.Token);
        }

        public void Run(bool firstRun)
        {
            Logger.Trace("Run - CurrentDirectory {0}", NPath.CurrentDirectory);

            var octorunPath = Environment.UserCachePath.Combine("octorun");
            var octorunScriptPath = octorunPath.Combine("src", "bin", "app.js");
            Logger.Trace("Using octorunScriptPath: {0}", octorunScriptPath);

            var gitExecutablePath = SystemSettings.Get(Constants.GitInstallPathKey)?.ToNPath();            
            if (gitExecutablePath.HasValue && gitExecutablePath.Value.FileExists()) // we have a git path
            {
                Logger.Trace("Using git install path from settings: {0}", gitExecutablePath);
                InitializeEnvironment(gitExecutablePath.Value, octorunScriptPath);
            }
            else // we need to go find git
            {
                Logger.Trace("No git path found in settings");

                var initEnvironmentTask = new ActionTask<NPath>(CancellationToken, (_, path) => InitializeEnvironment(path, octorunScriptPath)) { Affinity = TaskAffinity.UI };
                var findExecTask = new FindExecTask("git", CancellationToken)
                    .FinallyInUI((b, ex, path) => {
                        if (b && path.IsInitialized)
                        {
                            Logger.Trace("FindExecTask Success: {0}", path);
                            InitializeEnvironment(path, octorunScriptPath);
                        }
                        else
                        {
                            Logger.Warning("FindExecTask Failure");
                            Logger.Error("Git not found");
                        }
                    });

                var installDetails = new GitInstallDetails(Environment.UserCachePath, true);
                var gitInstaller = new GitInstaller(Environment, CancellationToken, installDetails);

                // if successful, continue with environment initialization, otherwise try to find an existing git installation
                gitInstaller.SetupGitIfNeeded(initEnvironmentTask, findExecTask);
            }
        }

        public ITask InitializeRepository()
        {
            Logger.Trace("Running Repository Initialize");

            var targetPath = NPath.CurrentDirectory;

            var unityYamlMergeExec = Environment.UnityApplicationContents.Combine("Tools", "UnityYAMLMerge" + Environment.ExecutableExtension);

            var yamlMergeCommand = Environment.IsWindows
                ? $@"'{unityYamlMergeExec}' merge -p ""$BASE"" ""$REMOTE"" ""$LOCAL"" ""$MERGED"""
                : $@"'{unityYamlMergeExec}' merge -p '$BASE' '$REMOTE' '$LOCAL' '$MERGED'";

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
                .Then(_ =>
                {
                    Environment.InitializeRepository();
                    RestartRepository();
                })
                .ThenInUI(InitializeUI);
            return task;
        }

        public void RestartRepository()
        {
            if (Environment.RepositoryPath.IsInitialized)
            {
                repositoryManager = Unity.RepositoryManager.CreateInstance(Platform, TaskManager, GitClient, ProcessManager, Environment.FileSystem, Environment.RepositoryPath);
                repositoryManager.Initialize();
                Environment.Repository.Initialize(repositoryManager);
                repositoryManager.Start();
                Logger.Trace($"Got a repository? {Environment.Repository}");
            }
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

            var metricsService = new MetricsService(ProcessManager,
                TaskManager,
                Environment.FileSystem,
                Environment.NodeJsExecutablePath,
                Environment.OctorunScriptPath);

            UsageTracker = new UsageTracker(metricsService, UserSettings, usagePath, id, unityVersion);

            if (firstRun)
            {
                UsageTracker.IncrementLaunchCount();
            }
        }

        protected abstract void SetupMetrics();
        protected abstract void InitializeUI();
        protected abstract void SetProjectToTextSerialization();

        /// <summary>
        /// Initialize environment after finding where git is. This needs to run on the main thread
        /// </summary>
        /// <param name="gitExecutablePath"></param>
        /// <param name="octorunScriptPath"></param>
        private void InitializeEnvironment(NPath gitExecutablePath, NPath octorunScriptPath)
        {
            var afterGitSetup = new ActionTask(CancellationToken, RestartRepository)
                .ThenInUI(InitializeUI);

            Environment.GitExecutablePath = gitExecutablePath;
            Environment.OctorunScriptPath = octorunScriptPath;
            Environment.User.Initialize(GitClient);
            SetupMetrics();

            if (Environment.IsWindows)
            {
                GitClient
                    .GetConfig("credential.helper", GitConfigSource.Global)
                    .Then((b, credentialHelper) => {
                        if (!string.IsNullOrEmpty(credentialHelper))
                        {
                            Logger.Trace("Windows CredentialHelper: {0}", credentialHelper);
                            afterGitSetup.Start();
                        }
                        else
                        {
                            Logger.Warning("No Windows CredentialHelper found: Setting to wincred");

                            GitClient.SetConfig("credential.helper", "wincred", GitConfigSource.Global)
                                .Then(afterGitSetup)
                                .Start();
                        }
                    })
                    .Start();
            }
            else
            {
                afterGitSetup.Start();
            }
        }

        private bool disposed = false;
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (disposed) return;
                disposed = true;
                if (TaskManager != null) TaskManager.Dispose();
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
