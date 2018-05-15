using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using GitHub.Logging;
using static GitHub.Unity.GitInstaller;

namespace GitHub.Unity
{
    abstract class ApplicationManagerBase : IApplicationManager
    {
        protected static ILogging Logger { get; } = LogHelper.GetLogger<IApplicationManager>();

        private RepositoryManager repositoryManager;
        private Progress progressReporter;
        protected bool isBusy;
        private bool firstRun;
        protected bool FirstRun { get { return firstRun; } set { firstRun = value; } }        
        private Guid instanceId;
        protected Guid InstanceId { get { return instanceId; } set { instanceId = value; } }

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

            LogHelper.TracingEnabled = UserSettings.Get(Constants.TraceLoggingKey, false);
            ProcessManager = new ProcessManager(Environment, Platform.GitEnvironment, CancellationToken);
            Platform.Initialize(ProcessManager, TaskManager);
            GitClient = new GitClient(Environment, ProcessManager, TaskManager.Token);
        }

        public void Run()
        {
            isBusy = true;

            var thread = new Thread(() =>
            {
                GitInstallationState state = new GitInstallationState();
                try
                {
                    SetupMetrics(Environment.UnityVersion, instanceId);

                    if (Environment.IsMac)
                    {
                        var getEnvPath = new SimpleProcessTask(CancellationToken, "bash".ToNPath(), "-c \"/usr/libexec/path_helper\"")
                                   .Configure(ProcessManager, dontSetupGit: true)
                                   .Catch(e => true); // make sure this doesn't throw if the task fails
                        var path = getEnvPath.RunWithReturn(true);
                        if (getEnvPath.Successful)
                        {
                            Logger.Trace("Existing Environment Path Original:{0} Updated:{1}", Environment.Path, path);
                            Environment.Path = path?.Split(new[] { "\"" }, StringSplitOptions.None)[1];
                        }
                    }

                    var installer = new GitInstaller(Environment, ProcessManager, CancellationToken)
                                                    { Progress = progressReporter };
                    state = Environment.GitInstallationState;
                    if (!state.GitIsValid && !state.GitLfsIsValid && FirstRun)
                    {
                        // importing old settings
                        NPath gitExecutablePath = Environment.SystemSettings.Get(Constants.GitInstallPathKey, NPath.Default);
                        if (gitExecutablePath.IsInitialized)
                        {
                            Environment.SystemSettings.Unset(Constants.GitInstallPathKey);
                            state.GitExecutablePath = gitExecutablePath;
                            state.GitInstallationPath = gitExecutablePath.Parent.Parent;
                            Environment.GitInstallationState = state;
                        }
                    }

                    if (state.GitIsValid && state.GitLfsIsValid)
                    {
                        if (firstRun)
                        {
                            installer.ValidateGitVersion(state);
                            if (state.GitIsValid)
                            {
                                installer.ValidateGitLfsVersion(state);
                            }
                        }
                    }

                    Environment.OctorunScriptPath = new OctorunInstaller(Environment, TaskManager)
                        .SetupOctorunIfNeeded();

                    if (!state.GitIsValid || !state.GitLfsIsValid)
                    {
                        state = installer.SetupGitIfNeeded();
                    }

                    SetupGit(state);

                    if (state.GitIsValid && state.GitLfsIsValid)
                    {
                        RestartRepository();
                    }

                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "A problem ocurred setting up Git");
                }

                new ActionTask<bool>(CancellationToken, (s, gitIsValid) =>
                    {
                        InitializationComplete();
                        if (gitIsValid)
                        {
                            InitializeUI();
                        }
                    },
                    () => state.GitIsValid && state.GitLfsIsValid)
                    { Affinity = TaskAffinity.UI }
                .Start();
            });
            thread.Start();
        }

        public void SetupGit(GitInstaller.GitInstallationState state)
        {
            if (!state.GitIsValid || !state.GitLfsIsValid)
            {
                if (!state.GitExecutablePath.IsInitialized)
                {
                    Logger.Warning(Localization.GitNotFound);
                }
                else if (!state.GitLfsExecutablePath.IsInitialized)
                {
                    Logger.Warning(Localization.GitLFSNotFound);
                }
                else if (state.GitVersion < Constants.MinimumGitVersion)
                {
                    Logger.Warning(String.Format(Localization.GitVersionTooLow, state.GitExecutablePath, state.GitVersion, Constants.MinimumGitVersion));
                }
                else if (state.GitLfsVersion < Constants.MinimumGitLfsVersion)
                {
                    Logger.Warning(String.Format(Localization.GitLfsVersionTooLow, state.GitLfsExecutablePath, state.GitLfsVersion, Constants.MinimumGitLfsVersion));
                }
                return;
            }

            Environment.GitInstallationState = state;
            Environment.User.Initialize(GitClient);

            if (firstRun)
            {
                if (Environment.RepositoryPath.IsInitialized)
                {
                    var unityYamlMergeExec = Environment.UnityApplicationContents.Combine("Tools", "UnityYAMLMerge" + Environment.ExecutableExtension);
                    var yamlMergeCommand = Environment.IsWindows
                        ? $@"'{unityYamlMergeExec}' merge -p ""$BASE"" ""$REMOTE"" ""$LOCAL"" ""$MERGED"""
                        : $@"'{unityYamlMergeExec}' merge -p '$BASE' '$REMOTE' '$LOCAL' '$MERGED'";

                    GitClient.SetConfig("merge.unityyamlmerge.cmd", yamlMergeCommand, GitConfigSource.Local)
                        .Catch(e =>
                        {
                            Logger.Error(e, "Error setting merge.unityyamlmerge.cmd");
                            return true;
                        })
                        .RunWithReturn(true);

                    GitClient.SetConfig("merge.unityyamlmerge.trustExitCode", "false", GitConfigSource.Local)
                        .Catch(e =>
                        {
                            Logger.Error(e, "Error setting merge.unityyamlmerge.trustExitCode");
                            return true;
                        })
                        .RunWithReturn(true);

                    GitClient.LfsInstall()
                        .Catch(e =>
                        {
                            Logger.Error(e, "Error running lfs install");
                            return true;
                        })
                        .RunWithReturn(true);
                }

                if (Environment.IsWindows)
                {
                    var credentialHelper = GitClient.GetConfig("credential.helper", GitConfigSource.Global)
                        .Catch(e =>
                        {
                            Logger.Error(e, "Error getting the credential helper");
                            return true;
                        }).RunWithReturn(true);

                    if (string.IsNullOrEmpty(credentialHelper))
                    {
                        Logger.Warning("No Windows CredentialHelper found: Setting to wincred");
                        GitClient.SetConfig("credential.helper", "wincred", GitConfigSource.Global)
                            .Catch(e =>
                            {
                                Logger.Error(e, "Error setting the credential helper");
                                return true;
                            })
                            .RunWithReturn(true);
                    }
                }
            }
        }

        public void InitializeRepository()
        {
            isBusy = true;
            var thread = new Thread(() =>
            {
                var success = true;
                try
                {
                    var targetPath = NPath.CurrentDirectory;

                    var gitignore = targetPath.Combine(".gitignore");
                    var gitAttrs = targetPath.Combine(".gitattributes");
                    var assetsGitignore = targetPath.Combine("Assets", ".gitignore");

                    var filesForInitialCommit = new List<string> { gitignore, gitAttrs, assetsGitignore };

                    GitClient.Init().RunWithReturn(true);
                    GitClient.LfsInstall().RunWithReturn(true);
                    AssemblyResources.ToFile(ResourceType.Generic, ".gitignore", targetPath, Environment);
                    AssemblyResources.ToFile(ResourceType.Generic, ".gitattributes", targetPath, Environment);
                    assetsGitignore.CreateFile();
                    GitClient.Add(filesForInitialCommit).RunWithReturn(true);
                    GitClient.Commit("Initial commit", null).RunWithReturn(true);
                    Environment.InitializeRepository();
                    UsageTracker.IncrementProjectsInitialized();
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "A problem ocurred initializing the repository");
                    success = false;
                }

                if (success)
                {
                    RestartRepository();
                    TaskManager.RunInUI(InitializeUI);
                }
                isBusy = false;
            });
            thread.Start();
        }

        public void RestartRepository()
        {
            if (!Environment.RepositoryPath.IsInitialized)
                return;

            repositoryManager?.Dispose();

            repositoryManager = Unity.RepositoryManager.CreateInstance(Platform, TaskManager, GitClient, Environment.FileSystem, Environment.RepositoryPath);
            repositoryManager.Initialize();
            Environment.Repository.Initialize(repositoryManager, TaskManager);
            repositoryManager.Start();
            Environment.Repository.Start();
            Logger.Trace($"Got a repository? {(Environment.Repository != null ? Environment.Repository.LocalPath : "null")}");
        }

        protected void SetupMetrics(string unityVersion, Guid instanceId)
        {
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

            UsageTracker = new UsageTracker(metricsService, UserSettings, Environment, userId, unityVersion, instanceId.ToString());

            if (firstRun)
            {
                UsageTracker.IncrementNumberOfStartups();
            }
#endif
        }
        protected abstract void InitializeUI();
        protected abstract void InitializationComplete();

        private bool disposed = false;
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (disposed) return;
                disposed = true;
                if (TaskManager != null)
                {
                    TaskManager.Dispose();
                    TaskManager = null;
                }
                if (repositoryManager != null)
                {
                    repositoryManager.Dispose();
                    repositoryManager = null;
                }
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
        public ISettings LocalSettings { get { return Environment.LocalSettings; } }
        public ISettings SystemSettings { get { return Environment.SystemSettings; } }
        public ISettings UserSettings { get { return Environment.UserSettings; } }
        public IUsageTracker UsageTracker { get; protected set; }
        public bool IsBusy { get { return isBusy; } }
        protected TaskScheduler UIScheduler { get; private set; }
        protected SynchronizationContext SynchronizationContext { get; private set; }
        protected IRepositoryManager RepositoryManager { get { return repositoryManager; } }
    }
}
