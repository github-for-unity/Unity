using GitHub.Unity;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;

namespace GitHub.Unity
{
    [InitializeOnLoad]
    class EntryPoint : ScriptableObject
    {
        private static ILogging logger;
        private static bool cctorCalled = false;

        private static ApplicationManager appManager;

        // this may run on the loader thread if it's an appdomain restart
        static EntryPoint()
        {
            if (cctorCalled)
            {
                return;
            }
            cctorCalled = true;
            Logging.LoggerFactory = s => new UnityLogAdapter(s);
            Logging.Debug("EntryPoint Initialize");

            ServicePointManager.ServerCertificateValidationCallback = ServerCertificateValidationCallback;
            EditorApplication.update += Initialize;
        }

        // we do this so we're guaranteed to run on the main thread, not the loader thread
        private static void Initialize()
        {
            EditorApplication.update -= Initialize;

            var persistentPath = Application.persistentDataPath;
            var filepath = Path.Combine(persistentPath, "github-unity-log.txt");
            try
            {

                if (File.Exists(filepath))
                {
                    File.Move(filepath, filepath + "-old");
                }
            }
            catch
            {
            }
            Logging.LoggerFactory = s => new FileLogAdapter(filepath, s);
            logger = Logging.GetLogger<EntryPoint>();

            ApplicationManager.Run();
        }

        private static bool ServerCertificateValidationCallback(object sender, X509Certificate certificate,
            X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            var success = true;
            // TODO: Invoke MozRoots.Process() to populate the certificate store and make this code work properly.
            // If there are errors in the certificate chain, look at each error to determine the cause.
            //if (sslPolicyErrors != SslPolicyErrors.None)
            //{
            //    foreach (var status in chain.ChainStatus.Where(st => st.Status != X509ChainStatusFlags.RevocationStatusUnknown))
            //    {
            //        chain.ChainPolicy.RevocationFlag = X509RevocationFlag.EntireChain;
            //        chain.ChainPolicy.RevocationMode = X509RevocationMode.Online;
            //        chain.ChainPolicy.UrlRetrievalTimeout = new TimeSpan(0, 1, 0);
            //        chain.ChainPolicy.VerificationFlags = X509VerificationFlags.AllFlags;
            //        success &= chain.Build((X509Certificate2)certificate);
            //    }
            //}
            return success;
        }

        private static ApplicationManager ApplicationManager
        {
            get
            {
                if (appManager == null)
                {
                    appManager = new ApplicationManager(new MainThreadSynchronizationContext());
                }
                return appManager;
            }
        }

        public static IGitClient GitClient { get { return ApplicationManager.GitClient; } }

        public static IEnvironment Environment { get { return ApplicationManager.Environment; } }

        public static IGitEnvironment GitEnvironment { get { return ApplicationManager.GitEnvironment; } }

        public static IFileSystem FileSystem { get { return ApplicationManager.FileSystem; } }

        public static IPlatform Platform { get { return ApplicationManager.Platform; } }
        public static ICredentialManager CredentialManager { get { return ApplicationManager.CredentialManager; } }

        public static IProcessManager ProcessManager { get { return ApplicationManager.ProcessManager; } }
        public static GitObjectFactory GitObjectFactory { get { return ApplicationManager.GitObjectFactory; } }

        public static ISettings LocalSettings { get { return ApplicationManager.LocalSettings; } }
        public static ISettings UserSettings { get { return ApplicationManager.UserSettings; } }
        public static ISettings SystemSettings { get { return ApplicationManager.SystemSettings; } }
        public static ITaskResultDispatcher TaskResultDispatcher { get { return ApplicationManager.TaskResultDispatcher; } }

        public static bool Initialized { get; private set; }
    }

    class ApplicationManager : IApplicationManager
    {
        private static readonly ILogging logger = Logging.GetLogger<ApplicationManager>();

        private const string QuitActionFieldName = "editorApplicationQuit";
        private CancellationTokenSource cancellationTokenSource;
        private FieldInfo quitActionField;
        private const BindingFlags quitActionBindingFlags = BindingFlags.NonPublic | BindingFlags.Static;

        private Tasks taskRunner;

        public ApplicationManager(MainThreadSynchronizationContext syncCtx)
        {
            ThreadUtils.SetMainThread();
            SynchronizationContext.SetSynchronizationContext(syncCtx);

            cancellationTokenSource = new CancellationTokenSource();
            EditorApplicationQuit = (UnityAction)Delegate.Combine(EditorApplicationQuit, new UnityAction(OnShutdown));
            EditorApplication.playmodeStateChanged += () =>
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode && !EditorApplication.isPlaying)
                {
                    OnShutdown();
                }
            };

            Platform = new Platform(Environment, FileSystem);
            GitObjectFactory = new GitObjectFactory(Environment, GitEnvironment);
            ProcessManager = new ProcessManager(Environment, GitEnvironment, CancellationToken);
            Platform.Initialize(ProcessManager);
            CredentialManager = Platform.CredentialManager;
            TaskResultDispatcher = new TaskResultDispatcher();
            ApiClientFactory.Instance = new ApiClientFactory(new AppConfiguration(), CredentialManager);
            LocalSettings = new LocalSettings(Environment);
            UserSettings = new UserSettings(Environment, ApplicationInfo.ApplicationName);
            SystemSettings = new SystemSettings(Environment, ApplicationInfo.ApplicationName);

            taskRunner = new Tasks(syncCtx, cancellationTokenSource.Token);
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

        // for unit testing
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

        public void Run()
        {
            Utility.Initialize();

            DetermineInstallationPath();

            var scheduler = TaskScheduler.FromCurrentSynchronizationContext();

            Task.Factory.StartNew(() =>
            {
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
