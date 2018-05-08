using System;
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
        private Progress progressReporter;
        protected bool isBusy;
        public event Action<IProgress> OnProgress
        {
            add { progressReporter.OnProgress += value; }
            remove { progressReporter.OnProgress -= value; }
        }

        public ApplicationManagerBase(SynchronizationContext synchronizationContext)
        {
            progressReporter = new Progress();
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
            isBusy = true;

            var endTask = new ActionTask<GitInstaller.GitInstallationState>(CancellationToken,
                    (_, state) => InitializeEnvironment(state))
                { Affinity = TaskAffinity.UI };

            ITask<string> setExistingEnvironmentPath;
            if (Environment.IsMac)
            {
                setExistingEnvironmentPath = new SimpleProcessTask(CancellationToken, "bash".ToNPath(), "-c \"/usr/libexec/path_helper\"")
                           .Configure(ProcessManager, dontSetupGit: true)
                           .Catch(e => true) // make sure this doesn't throw if the task fails
                           .Then((success, path) => success ? path?.Split(new[] { "\"" }, StringSplitOptions.None)[1] : null);
            }
            else
            {
                setExistingEnvironmentPath = new FuncTask<string>(CancellationToken, () => null);
            }

            setExistingEnvironmentPath.OnEnd += (t, path, success, ex) =>
            {
                if (path != null)
                {
                    Logger.Trace("Existing Environment Path Original:{0} Updated:{1}", Environment.Path, path);
                    Environment.Path = path;
                }
            };
            
            var setupOctorun = new OctorunInstaller(Environment, TaskManager).SetupOctorunIfNeeded();
            var setOctorunEnvironment = new ActionTask<NPath>(CancellationToken,
                (s, octorunPath) => Environment.OctorunScriptPath = octorunPath);

            var getGitFromSettings = new FuncTask<NPath>(CancellationToken, () =>
            {
                var gitExecutablePath = SystemSettings.Get(Constants.GitInstallPathKey)?.ToNPath();
                if (gitExecutablePath.HasValue && gitExecutablePath.Value.FileExists()) // we have a git path
                {
                    Logger.Trace("Using git install path from settings: {0}", gitExecutablePath);
                    return gitExecutablePath.Value;
                }
                return NPath.Default;
            });

            getGitFromSettings.OnEnd += (t, path, _, __) =>
            {
                if (path.IsInitialized)
                {
                    var state = new GitInstaller.GitInstallationState {
                        GitExecutablePath = path,
                        GitIsValid = true
                    };
                    endTask.PreviousResult = state;
                    endTask.Start();
                    return;
                }
                Logger.Trace("Using portable git");

                var setupGit = new GitInstaller(Environment, ProcessManager, TaskManager).SetupGitIfNeeded();
                t.Then(setupGit);
                setupGit.Finally((s, state) =>
                {
                    endTask.PreviousResult = state;
                    endTask.Start();
                });
                setupGit.Progress(progressReporter.UpdateProgress);
                // append installer task to top chain
            };

            var setupChain = setExistingEnvironmentPath.Then(setupOctorun);
            setupChain.OnEnd += (t, path, _, __) =>
            {
                t.GetEndOfChain().Then(setOctorunEnvironment).Then(getGitFromSettings);
            };

            setupChain.Start();
        }

        public ITask InitializeRepository()
        {
            //Logger.Trace("Running Repository Initialize");

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
                .ThenInUI(() =>
                {
                    TaskManager.Run(UsageTracker.IncrementNumberOfProjectsInitialized);
                    InitializeUI();
                });
            return task;
        }

        public void RestartRepository()
        {
            if (Environment.RepositoryPath.IsInitialized)
            {
                repositoryManager = Unity.RepositoryManager.CreateInstance(Platform, TaskManager, GitClient, Environment.FileSystem, Environment.RepositoryPath);
                repositoryManager.Initialize();
                Environment.Repository.Initialize(repositoryManager, TaskManager);
                repositoryManager.Start();
                Environment.Repository.Start();
                Logger.Trace($"Got a repository? {(Environment.Repository != null ? Environment.Repository.LocalPath : "null")}");
            }
        }

        protected void SetupMetrics(string unityVersion, bool firstRun, Guid instanceId)
        {
            //Logger.Trace("Setup metrics");

            var usagePath = Environment.UserCachePath.Combine(Constants.UsageFile);

            string userId = null;
            if (UserSettings.Exists(Constants.GuidKey))
            {
                userId = UserSettings.Get(Constants.GuidKey);
            }

            if (String.IsNullOrEmpty(userId))
            {
                userId = Guid.NewGuid().ToString();
                UserSettings.Set(Constants.GuidKey, userId);
            }

#if ENABLE_METRICS
            var metricsService = new MetricsService(ProcessManager,
                TaskManager,
                Environment.FileSystem,
                Environment.NodeJsExecutablePath,
                Environment.OctorunScriptPath);

            UsageTracker = new UsageTracker(metricsService, UserSettings, usagePath, userId, unityVersion, instanceId.ToString());

            if (firstRun)
            {
                TaskManager.Run(UsageTracker.IncrementNumberOfStartups);
            }
#endif
        }

        protected abstract void SetupMetrics();
        protected abstract void InitializeUI();
        protected abstract void SetProjectToTextSerialization();

        /// <summary>
        /// Initialize environment after finding where git is. This needs to run on the main thread
        /// </summary>
        /// <param name="gitExecutablePath"></param>
        /// <param name="octorunScriptPath"></param>
        private void InitializeEnvironment(GitInstaller.GitInstallationState installationState)
        {
            isBusy = false;
            SetupMetrics();

            if (!installationState.GitIsValid)
            {
                return;
            }

            var gitInstallDetails = new GitInstaller.GitInstallDetails(Environment.UserCachePath, Environment.IsWindows);
            var isCustomGitExec = installationState.GitExecutablePath != gitInstallDetails.GitExecutablePath;

            Environment.GitExecutablePath = installationState.GitExecutablePath;
            Environment.GitLfsExecutablePath = installationState.GitLfsExecutablePath;

            Environment.IsCustomGitExecutable = isCustomGitExec;
            Environment.User.Initialize(GitClient);

            var afterGitSetup = new ActionTask(CancellationToken, RestartRepository)
                .ThenInUI(InitializeUI);

            ITask task = afterGitSetup;
            if (Environment.IsWindows)
            {
                var credHelperTask = GitClient.GetConfig("credential.helper", GitConfigSource.Global);
                credHelperTask.OnEnd += (thisTask, credentialHelper, success, exception) =>
                    {   
                        if (!success || string.IsNullOrEmpty(credentialHelper))
                        {
                            Logger.Warning("No Windows CredentialHelper found: Setting to wincred");
                            thisTask
                                .Then(GitClient.SetConfig("credential.helper", "wincred", GitConfigSource.Global))
                                .Then(afterGitSetup);
                        }
                        else
                            thisTask.Then(afterGitSetup);
                    };
                task = credHelperTask;
            }
            task.Start();
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
        public bool IsBusy { get { return isBusy; } }
        protected TaskScheduler UIScheduler { get; private set; }
        protected SynchronizationContext SynchronizationContext { get; private set; }
        protected IRepositoryManager RepositoryManager { get { return repositoryManager; } }
    }
}
