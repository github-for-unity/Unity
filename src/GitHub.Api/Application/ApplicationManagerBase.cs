﻿using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using GitHub.Logging;
using static GitHub.Unity.GitInstaller;

namespace GitHub.Unity
{
    class ApplicationManagerBase : IApplicationManager
    {
        protected static ILogging Logger { get; } = LogHelper.GetLogger<IApplicationManager>();

        private RepositoryManager repositoryManager;
        private ProgressReporter progressReporter = new ProgressReporter();
        private Progress progress = new Progress(TaskBase.Default);
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

        public ApplicationManagerBase(SynchronizationContext synchronizationContext, IEnvironment environment)
        {
            UIScheduler = ThreadingHelper.GetUIScheduler(synchronizationContext);

            SynchronizationContext = synchronizationContext;
            ThreadingHelper.SetUIThread();
            ThreadingHelper.MainThreadScheduler = UIScheduler;

            Environment = environment;
            TaskManager = new TaskManager(UIScheduler);
            Platform = new Platform(Environment);
            ProcessManager = new ProcessManager(Environment, Platform.GitEnvironment, TaskManager.Token);
            GitClient = new GitClient(Environment, ProcessManager, TaskManager.Token);
        }

        protected void Initialize()
        {
            LogHelper.TracingEnabled = UserSettings.Get(Constants.TraceLoggingKey, false);
            ApplicationConfiguration.WebTimeout = UserSettings.Get(Constants.WebTimeoutKey, ApplicationConfiguration.WebTimeout);
            ApplicationConfiguration.GitTimeout = UserSettings.Get(Constants.GitTimeoutKey, ApplicationConfiguration.GitTimeout);
            Platform.Initialize(ProcessManager, TaskManager);
            progress.OnProgress += progressReporter.UpdateProgress;
            UsageTracker = new UsageTracker(TaskManager, GitClient, ProcessManager, UserSettings, Environment, Platform.Keychain, InstanceId.ToString());

#if ENABLE_METRICS
            var metricsService = new MetricsService(ProcessManager,
                TaskManager,
                Platform.Keychain,
                Environment);
            UsageTracker.MetricsService = metricsService;
#endif
        }

        public void Run()
        {
            isBusy = true;
            progress.UpdateProgress(0, 100, "Initializing...");

            if (firstRun)
            {
                UsageTracker.IncrementNumberOfStartups();
            }

            var thread = new Thread(() =>
            {
                GitInstallationState state = new GitInstallationState();
                try
                {
                    if (Environment.IsMac)
                    {
                        var getEnvPath = new SimpleProcessTask(TaskManager.Token, "bash".ToNPath(), "-c \"/usr/libexec/path_helper\"")
                                   .Configure(ProcessManager, dontSetupGit: true)
                                   .Catch(e => true); // make sure this doesn't throw if the task fails
                        var path = getEnvPath.RunSynchronously();
                        if (getEnvPath.Successful)
                        {
                            Logger.Trace("Existing Environment Path Original:{0} Updated:{1}", Environment.Path, path);
                            Environment.Path = path?.Split(new[] { "\"" }, StringSplitOptions.None)[1];
                        }
                    }

                    progress.UpdateProgress(20, 100, "Setting up octorun...");

                    Environment.OctorunScriptPath = new OctorunInstaller(Environment, TaskManager)
                        .SetupOctorunIfNeeded();

                    progress.UpdateProgress(50, 100, "Setting up git...");

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


                    var installer = new GitInstaller(Environment, ProcessManager, TaskManager.Token);
                    installer.Progress.OnProgress += progressReporter.UpdateProgress;
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

                    if (!state.GitIsValid || !state.GitLfsIsValid)
                    {
                        state = installer.SetupGitIfNeeded();
                    }

                    SetupGit(state);

                    progress.UpdateProgress(80, 100, "Initializing repository...");

                    if (state.GitIsValid && state.GitLfsIsValid)
                    {
                        RestartRepository();
                    }

                    progress.UpdateProgress(100, 100, "Initialized");
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "A problem ocurred setting up Git");
                    progress.UpdateProgress(90, 100, "Initialization failed");
                }

                new ActionTask<bool>(TaskManager.Token, (s, gitIsValid) =>
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
                    ConfigureMergeSettings();

                    GitClient.LfsInstall()
                        .Catch(e =>
                        {
                            Logger.Error(e, "Error running lfs install");
                            return true;
                        })
                        .RunSynchronously();
                }

                if (Environment.IsWindows)
                {
                    var credentialHelper = GitClient.GetConfig("credential.helper", GitConfigSource.Global)
                        .Catch(e =>
                        {
                            Logger.Error(e, "Error getting the credential helper");
                            return true;
                        }).RunSynchronously();

                    if (string.IsNullOrEmpty(credentialHelper))
                    {
                        Logger.Warning("No Windows CredentialHelper found: Setting to wincred");
                        GitClient.SetConfig("credential.helper", "wincred", GitConfigSource.Global)
                            .Catch(e =>
                            {
                                Logger.Error(e, "Error setting the credential helper");
                                return true;
                            })
                            .RunSynchronously();
                    }
                }
            }
        }

        public void InitializeRepository()
        {
            isBusy = true;
            progress.UpdateProgress(0, 100, "Initializing...");
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

                    GitClient.Init().RunSynchronously();
                    progress.UpdateProgress(10, 100, "Initializing...");

                    ConfigureMergeSettings();
                    progress.UpdateProgress(20, 100, "Initializing...");

                    GitClient.LfsInstall().RunSynchronously();
                    progress.UpdateProgress(30, 100, "Initializing...");

                    GenerateGitignore(targetPath);
                    AssemblyResources.ToFile(ResourceType.Generic, ".gitattributes", targetPath, Environment);
                    assetsGitignore.CreateFile();
                    GitClient.Add(filesForInitialCommit).RunSynchronously();
                    progress.UpdateProgress(60, 100, "Initializing...");
                    GitClient.Commit("Initial commit", null).RunSynchronously();
                    progress.UpdateProgress(70, 100, "Initializing...");
                    Environment.InitializeRepository();
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "A problem ocurred initializing the repository");
                    progress.UpdateProgress(90, 100, "Failed to initialize repository");
                    success = false;
                }

                if (success)
                {
                    progress.UpdateProgress(90, 100, "Initializing...");
                    RestartRepository();
                    TaskManager.RunInUI(InitializeUI);
                    UsageTracker.IncrementProjectsInitialized();
                    progress.UpdateProgress(100, 100, "Initialized");
                }
                isBusy = false;
            });
            thread.Start();
        }

        public void GenerateGitignore(NPath path)
        {
            AssemblyResources.ToFile(ResourceType.Generic, ".gitignore", path, Environment);
        }

        private void ConfigureMergeSettings()
        {
            var unityYamlMergeExec =
                Environment.UnityApplicationContents.Combine("Tools", "UnityYAMLMerge" + Environment.ExecutableExtension);
            var yamlMergeCommand = Environment.IsWindows
                ? $@"'{unityYamlMergeExec}' merge -p ""$BASE"" ""$REMOTE"" ""$LOCAL"" ""$MERGED"""
                : $@"'{unityYamlMergeExec}' merge -p '$BASE' '$REMOTE' '$LOCAL' '$MERGED'";

            GitClient.SetConfig("merge.unityyamlmerge.cmd", yamlMergeCommand, GitConfigSource.Local).Catch(e => {
                Logger.Error(e, "Error setting merge.unityyamlmerge.cmd");
                return true;
            }).RunSynchronously();

            GitClient.SetConfig("merge.unityyamlmerge.trustExitCode", "false", GitConfigSource.Local).Catch(e => {
                Logger.Error(e, "Error setting merge.unityyamlmerge.trustExitCode");
                return true;
            }).RunSynchronously();
        }

        public void RestartRepository()
        {
            if (!Environment.RepositoryPath.IsInitialized)
                return;

            repositoryManager?.Dispose();

            repositoryManager = Unity.RepositoryManager.CreateInstance(Platform, TaskManager, GitClient, Environment.RepositoryPath);
            repositoryManager.Initialize();
            Environment.Repository.Initialize(repositoryManager, TaskManager);
            repositoryManager.Start();
            Environment.Repository.Start();
            Logger.Trace($"Got a repository? {(Environment.Repository != null ? Environment.Repository.LocalPath : "null")}");
        }

        protected virtual void InitializeUI() {}
        protected virtual void InitializationComplete() {}

        private bool disposed = false;
        private IOAuthCallbackManager oAuthCallbackManager;

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (disposed) return;
                disposed = true;
                if (ProcessManager != null)
                {
                    ProcessManager.Stop();
                }
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

        public IEnvironment Environment { get; private set; }
        public IPlatform Platform { get; protected set; }
        public virtual IProcessEnvironment GitEnvironment { get; set; }
        public IProcessManager ProcessManager { get; protected set; }
        public ITaskManager TaskManager { get; protected set; }
        public IGitClient GitClient { get; protected set; }
        public ISettings LocalSettings { get { return Environment.LocalSettings; } }
        public ISettings SystemSettings { get { return Environment.SystemSettings; } }
        public ISettings UserSettings { get { return Environment.UserSettings; } }
        public IUsageTracker UsageTracker { get; protected set; }

        public IOAuthCallbackManager OAuthCallbackManager
        {
            get
            {
                if (oAuthCallbackManager == null)
                {
                    oAuthCallbackManager = new OAuthCallbackManager();
                }

                return oAuthCallbackManager;
            }
        }

        public bool IsBusy { get { return isBusy; } }
        protected TaskScheduler UIScheduler { get; private set; }
        protected SynchronizationContext SynchronizationContext { get; private set; }
        protected IRepositoryManager RepositoryManager { get { return repositoryManager; } }
    }
}
