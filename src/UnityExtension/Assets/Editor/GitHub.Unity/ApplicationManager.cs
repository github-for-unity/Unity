using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;

namespace GitHub.Unity
{
    class ApplicationManager : IApplicationManager
    {
        private readonly MainThreadSynchronizationContext synchronizationContext;
        private static readonly ILogging logger = Logging.GetLogger<ApplicationManager>();

        private const string QuitActionFieldName = "editorApplicationQuit";
        private const BindingFlags quitActionBindingFlags = BindingFlags.NonPublic | BindingFlags.Static;

        private CancellationTokenSource cancellationTokenSource;
        private FieldInfo quitActionField;
        private TaskScheduler scheduler;

        private Tasks taskRunner;

        // for unit testing (TODO)
        public ApplicationManager(IEnvironment environment, IFileSystem fileSystem,
            IPlatform platform, IProcessManager processManager, ITaskResultDispatcher taskResultDispatcher)
        {
            Environment = environment;
            FileSystem = fileSystem;
            NPathFileSystemProvider.Current = FileSystem;
            Platform = platform;
            ProcessManager = processManager;
            TaskResultDispatcher = taskResultDispatcher;
        }

        public ApplicationManager(MainThreadSynchronizationContext synchronizationContext)
        {
            this.synchronizationContext = synchronizationContext;
            InitializeThreading(synchronizationContext);
            ListenToUnityExit();
            DetermineInstallationPath();

            TaskResultDispatcher = new TaskResultDispatcher();

            // accessing Environment triggers environment initialization if it hasn't happened yet
            LocalSettings = new LocalSettings(Environment);
            UserSettings = new UserSettings(Environment, ApplicationInfo.ApplicationName);
            SystemSettings = new SystemSettings(Environment, ApplicationInfo.ApplicationName);

            Platform = new Platform(Environment, FileSystem);
            GitObjectFactory = new GitObjectFactory(Environment, Platform.GitEnvironment);
            ProcessManager = new ProcessManager(Environment, Platform.GitEnvironment, CancellationToken);
            Platform.Initialize(ProcessManager);
            CredentialManager = Platform.CredentialManager;
            ApiClientFactory.Instance = new ApiClientFactory(new AppConfiguration(), Platform.CredentialManager);
        }

        public void Run()
        {
            Utility.Initialize();

            taskRunner = new Tasks(synchronizationContext, cancellationTokenSource.Token);

            Task.Factory.StartNew(() =>
                {
                    var gitInstaller = new PortableGitManager(Environment, cancellationToken: CancellationToken);


                    try
                    {
                        Environment.GitExecutablePath = DetermineGitInstallationPath();                   
                        Environment.Repository = GitClient.GetRepository();
                    }
                    catch (Exception ex)
                    {
                        logger.Error(ex);
                        throw;
                    }
                })
                .ContinueWith(_ =>
                {
                    taskRunner.Run();

                    Utility.Run();

                    ProjectWindowInterface.Initialize();

                    Window.Initialize();
                }, scheduler);
        }


        private void InitializeEnvironment()
        {
            NPathFileSystemProvider.Current = new FileSystem();

            Environment = new DefaultEnvironment();

            // figure out where we are
            Environment.ExtensionInstallPath = DetermineInstallationPath();

            // figure out where the project is
            var assetsPath = Application.dataPath.ToNPath();
            var projectPath = assetsPath.Parent;

            Environment.UnityAssetsPath = assetsPath.ToString(SlashMode.Forward);
            Environment.UnityProjectPath = projectPath.ToString(SlashMode.Forward);

            // figure out where the repository root is
            GitClient = new GitClient(projectPath);
            Environment.Repository = GitClient.GetRepository();

            // Make sure CurrentDirectory always returns the repository root, so all
            // file system path calculations use it as a base
            FileSystem = new FileSystem(Environment.Repository.LocalPath);
            NPathFileSystemProvider.Current = FileSystem;
        }

        private void ListenToUnityExit()
        {
            EditorApplicationQuit = (UnityAction)Delegate.Combine(EditorApplicationQuit, new UnityAction(OnShutdown));
            EditorApplication.playmodeStateChanged += () => {
                if (EditorApplication.isPlayingOrWillChangePlaymode && !EditorApplication.isPlaying)
                {
                    OnShutdown();
                }
            };
        }

        private void InitializeThreading(MainThreadSynchronizationContext syncCtx)
        {
            ThreadUtils.SetMainThread();
            SynchronizationContext.SetSynchronizationContext(syncCtx);
            scheduler = TaskScheduler.FromCurrentSynchronizationContext();
            cancellationTokenSource = new CancellationTokenSource();
        }

        private void OnShutdown()
        {
            taskRunner.Shutdown();
            cancellationTokenSource.Cancel();
        }

        private UnityAction EditorApplicationQuit
        {
            get
            {
                SecureQuitActionField();
                return (UnityAction)quitActionField.GetValue(null);
            }
            set
            {
                SecureQuitActionField();
                quitActionField.SetValue(null, value);
            }
        }

        private void SecureQuitActionField()
        {
            if (quitActionField == null)
            {
                quitActionField = typeof(EditorApplication).GetField(QuitActionFieldName, quitActionBindingFlags);

                if (quitActionField == null)
                {
                    throw new InvalidOperationException("Unable to reflect EditorApplication." + QuitActionFieldName);
                }
            }
        }

        private string DetermineInstallationPath()
        {
            // Juggling to find out where we got installed
            var shim = ScriptableObject.CreateInstance<RunLocationShim>();
            var script = MonoScript.FromScriptableObject(shim);
            string ret = String.Empty;
            
            if (script != null)
            {
                var scriptPath = AssetDatabase.GetAssetPath(script).ToNPath();
                ret = scriptPath.Parent.ToString(SlashMode.Forward);
            }
            ScriptableObject.DestroyImmediate(shim);
            return ret;
        }

        private string DetermineGitInstallationPath()
        {
            var cachedGitInstallPath = SystemSettings.Get("GitInstallPath");

            // Root paths
            if (string.IsNullOrEmpty(cachedGitInstallPath) || !cachedGitInstallPath.ToNPath().Exists())
            {
                return GitEnvironment.FindGitInstallationPath(ProcessManager).Result;
            }
            else
            {
                return cachedGitInstallPath;
            }
        }

        public CancellationToken CancellationToken { get { return cancellationTokenSource.Token; } }

        private IEnvironment environment;
        public IEnvironment Environment
        {
            get
            {
                // if this is called while still null, it's because Unity wants
                // to render something and we need to load icons, and that runs
                // before EntryPoint. Do an early initialization
                if (environment == null)
                    InitializeEnvironment();
                return environment;
            }
            set { environment = value; }
        }
        public IFileSystem FileSystem { get; private set; }
        public IPlatform Platform { get; private set; }
        public IGitEnvironment GitEnvironment { get { return Platform.GitEnvironment; } }
        public IProcessManager ProcessManager { get; private set; }
        public ICredentialManager CredentialManager { get; private set; }
        public IGitClient GitClient { get; private set; }
        public ITaskResultDispatcher TaskResultDispatcher { get; private set; }
        public ISettings SystemSettings { get; private set; }
        public ISettings LocalSettings { get; private set; }
        public ISettings UserSettings { get; private set; }
        public GitObjectFactory GitObjectFactory { get; private set; }
    }
}