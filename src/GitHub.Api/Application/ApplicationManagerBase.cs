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

            Logging.TracingEnabled = UserSettings.Get(Constants.TraceLoggingKey, false);
            ProcessManager = new ProcessManager(Environment, Platform.GitEnvironment, CancellationToken);
            Platform.Initialize(ProcessManager, TaskManager);
            ITaskManager taskManager = TaskManager;
            GitClient = new GitClient(Environment, ProcessManager, taskManager.Token);
            SetupMetrics();
        }

        public void Run(bool firstRun)
        {
            Logger.Trace("Run - CurrentDirectory {0}", NPath.CurrentDirectory);

            var afterGitSetup = new ActionTask(CancellationToken, RestartRepository)
                .ThenInUI(InitializeUI);

            Logger.Trace("afterGitSetup");

            var windowsCredentialSetup = new ActionTask(CancellationToken, () => {
                GitClient.GetConfig("credential.helper", GitConfigSource.Global).Then((b, credentialHelper) => {
                    if (!string.IsNullOrEmpty(credentialHelper))
                    {
                        Logger.Trace("Windows CredentialHelper: {0}", credentialHelper);
                        afterGitSetup.Start();
                    }
                    else
                    {
                        Logger.Warning("No Windows CredentialHeloper found: Setting to wincred");

                        GitClient.SetConfig("credential.helper", "wincred", GitConfigSource.Global)
                                 .Then(() => { afterGitSetup.Start(); }).Start();
                    }
                }).Start();
            });

            Logger.Trace("windowsCredentialSetup");

            var afterPathDetermined = new ActionTask<NPath>(CancellationToken, (b1, path) => {

                Logger.Trace("Setting Environment git path: {0}", path);
                Environment.GitExecutablePath = path;

            }).ThenInUI(() => {

                Environment.User.Initialize(GitClient);

                if (Environment.IsWindows)
                {
                    windowsCredentialSetup.Start();
                }
                else
                {
                    afterGitSetup.Start();
                }
            });

            Logger.Trace("afterPathDetermined");

            var applicationDataPath = Environment.GetSpecialFolder(System.Environment.SpecialFolder.LocalApplicationData).ToNPath();
            var installDetails = new GitInstallDetails(applicationDataPath, true);

            var gitInstaller = new GitInstaller(Environment, CancellationToken, installDetails);
            gitInstaller.SetupGitIfNeeded(new ActionTask<NPath>(CancellationToken, (b, s) => {

                Logger.Trace("Success: {0}", s);

                new FuncTask<NPath>(CancellationToken, () => s)
                    .Then(afterPathDetermined)
                    .Start();

            }), new ActionTask(CancellationToken, () => {
                Logger.Trace("Failure");
            }) );
        }

        public ITask InitializeRepository()
        {
            Logger.Trace("Running Repository Initialize");

            var targetPath = NPath.CurrentDirectory;

            var unityYamlMergeExec = Environment.IsWindows
                ? Environment.UnityApplication.Parent.Combine("Data", "Tools", "UnityYAMLMerge.exe")
                : Environment.UnityApplication.Combine("Contents", "Tools", "UnityYAMLMerge");

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
            if (Environment.RepositoryPath != null)
            {
                repositoryManager = Unity.RepositoryManager.CreateInstance(Platform, TaskManager, GitClient, Environment.RepositoryPath);
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
