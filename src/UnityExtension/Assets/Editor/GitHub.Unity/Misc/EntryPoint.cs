using GitHub.Api;
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

        [NonSerialized] private ApplicationManager appManager;

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

            CreateInstance<EntryPoint>().Run();
        }

        private void Run()
        {
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

            appManager = new ApplicationManager(new MainThreadSynchronizationContext());
            processManager = new ProcessManager(Environment, GitEnvironment, FileSystem, appManager.CancellationToken);

            DetermineUnityPaths(Environment, GitEnvironment, FileSystem);

            var scheduler = TaskScheduler.FromCurrentSynchronizationContext();
            Task.Factory.StartNew(() =>
            {
                DetermineGitRepoRoot(Environment, GitEnvironment, FileSystem);

                ApiClientFactory.Instance = new ApiClientFactory(new AppConfiguration(), Platform.GetCredentialManager(ProcessManager));

                DetermineGitInstallationPath(Environment, GitEnvironment, FileSystem, ProcessManager, LocalSettings);

                var remotesTask = new GitRemoteListTask(Environment, ProcessManager, null,
                    list =>
                    {
                        var remote = list.FirstOrDefault(x => x.Name == "origin");
                        if (remote.Name != null)
                        {
                            Environment.DefaultRemote = remote.Name;
                            Environment.RepositoryHost = remote.Host;
                        }
                    });

                var task = remotesTask
                    .RunAsync(CancellationToken.None)
                    .ContinueWith(_ =>
                    {
                        appManager.Run();

                        Utility.Run();

                        ProjectWindowInterface.Initialize();

                        Window.Initialize();

                        Initialized = true;
                    }, scheduler);
                task.Wait();
            });
        }


        // TODO: Move these out to a proper location
        private static void DetermineGitRepoRoot(IEnvironment environment, IGitEnvironment gitEnvironment, IFileSystem fs)
        {
            var fullProjectRoot = FileSystem.GetFullPath(Environment.UnityProjectPath);
            environment.RepositoryRoot = gitEnvironment.FindRoot(fullProjectRoot);
        }

        // TODO: Move these out to a proper location
        private void DetermineUnityPaths(IEnvironment environment, IGitEnvironment gitEnvironment, IFileSystem fs)
        {
            // Unity paths
            environment.UnityAssetsPath = Application.dataPath;
            environment.UnityProjectPath = environment.UnityAssetsPath.Substring(0, environment.UnityAssetsPath.Length - "Assets".Length - 1);

            // Juggling to find out where we got installed
            var script = MonoScript.FromScriptableObject(this);
            if (script == null)
            {
                environment.ExtensionInstallPath = string.Empty;
            }
            else
            {
                environment.ExtensionInstallPath = AssetDatabase.GetAssetPath(script);
                environment.ExtensionInstallPath = environment.ExtensionInstallPath.Substring(0,
                    environment.ExtensionInstallPath.LastIndexOf('/'));
                environment.ExtensionInstallPath = environment.ExtensionInstallPath.Substring(0,
                    environment.ExtensionInstallPath.LastIndexOf('/'));
            }
        }

        // TODO: Move these out to a proper location
        private static void DetermineGitInstallationPath(IEnvironment environment, IGitEnvironment gitEnvironment, IFileSystem fs,
            IProcessManager processManager, ISettings settings)
        {
            var cachedGitInstallPath = settings.Get("GitInstallPath");

            // Root paths
            if (string.IsNullOrEmpty(cachedGitInstallPath) || !fs.FileExists(cachedGitInstallPath))
            {
                environment.GitExecutablePath = gitEnvironment.FindGitInstallationPath(processManager).Result;
            }
            else
            {
                environment.GitExecutablePath = cachedGitInstallPath;
            }
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

        private static IEnvironment environment;
        public static IEnvironment Environment
        {
            get
            {
                if (environment == null)
                {
                    environment = new DefaultEnvironment();
                }
                return environment;
            }
        }

        public static IGitEnvironment GitEnvironment
        {
            get
            {
                return Platform.GitEnvironment;
            }
        }

        private static IFileSystem filesystem;
        public static IFileSystem FileSystem
        {
            get
            {
                if (filesystem == null)
                {
                    filesystem = new FileSystem();
                }
                return filesystem;
            }
        }

        private static IPlatform platform;
        public static IPlatform Platform
        {
            get
            {
                if (platform == null)
                {
                    platform = new Platform(Environment, FileSystem);
                }
                return platform;
            }
        }

        private static IProcessManager processManager;
        public static IProcessManager ProcessManager { get { return processManager; } }


        private static GitObjectFactory gitObjectFactory;
        public static GitObjectFactory GitObjectFactory
        {
            get
            {
                if (gitObjectFactory == null)
                {
                    gitObjectFactory = new GitObjectFactory(Environment, GitEnvironment, FileSystem);
                }
                return gitObjectFactory;
            }
        }

        private static ISettings localSettings;
        public static ISettings LocalSettings
        {
            get
            {
                if (localSettings == null)
                {
                    localSettings = new LocalSettings(Environment);
                    localSettings.Initialize();
                }
                return localSettings;
            }
        }

        private static ISettings userSettings;
        public static ISettings UserSettings
        {
            get
            {
                if (userSettings == null)
                {
                    userSettings = new UserSettings(Environment, new AppConfiguration());
                    userSettings.Initialize();
                }
                return userSettings;
            }
        }

        private static ITaskResultDispatcher taskResultDispatcher;
        public static ITaskResultDispatcher TaskResultDispatcher
        {
            get
            {
                if (taskResultDispatcher == null)
                {
                    taskResultDispatcher = new TaskResultDispatcher();
                }
                return taskResultDispatcher;
            }
        }

        public static bool Initialized { get; private set; }
    }


    class ApplicationManager
    {
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
            taskRunner = new Tasks(syncCtx, cancellationTokenSource.Token);
        }

        public void Initialize()
        {
            Utility.Initialize();
        }

        public void Run()
        {
            taskRunner.Run();
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

        public CancellationToken CancellationToken { get { return cancellationTokenSource.Token; } }
    }
}
