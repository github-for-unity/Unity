using System;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace GitHub.Unity
{
    internal sealed class ApplicationCache : ScriptableObject, ISerializationCallbackReceiver
    {
        private static ApplicationCache instance;
        private static string cachePath;

        [SerializeField] private bool firstRun = true;
        public bool FirstRun { get { return firstRun; } private set { firstRun = value; Flush(); } }
        [SerializeField] private string createdDate;
        public string CreatedDate { get { return createdDate; } }

        public static ApplicationCache Instance {
            get {
                return instance ?? CreateApplicationCache(EntryPoint.Environment);
            }
        }

        private static ApplicationCache CreateApplicationCache(IEnvironment environment)
        {
            cachePath = environment.UnityProjectPath + "/Temp/github_cache.yaml";

            if (System.IO.File.Exists(cachePath))
            {
                Debug.Log("Loading from cache");

                var objects = UnityEditorInternal.InternalEditorUtility.LoadSerializedFileAndForget(cachePath);
                if (objects.Any())
                {
                    instance = objects[0] as ApplicationCache;
                    if (instance != null)
                    {
                        Debug.LogFormat("Loading from cache successful {0}", instance);
                        if (instance.FirstRun)
                            instance.FirstRun = false;
                        return instance;
                    }
                }
            }

            Debug.Log("Creating instance");
            instance = CreateInstance<ApplicationCache>();
            return instance.Initialize();
        }

        private ApplicationCache Initialize()
        {
            createdDate = DateTime.Now.ToLongTimeString();
            Flush();
            return this;
        }

        private void Flush()
        {
            UnityEditorInternal.InternalEditorUtility.SaveToSerializedFileAndForget(new Object[] { this }, cachePath, true);
        }

        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
            Debug.LogFormat("ApplicationCache OnBeforeSerialize {0} {1}", firstRun, GetInstanceID());
        }

        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            Debug.LogFormat("ApplicationCache OnAfterDeserialize {0} {1}", firstRun, GetInstanceID());
        }
    }

    [InitializeOnLoad]
    class EntryPoint : ScriptableObject
    {
        private static ILogging logger;

        private static ApplicationManager appManager;

        // this may run on the loader thread if it's an appdomain restart
        static EntryPoint()
        {
            Logging.LoggerFactory = s => new UnityLogAdapter(s);
            if (System.Environment.GetEnvironmentVariable("GITHUB_UNITY_DISABLE") == "1")
            {
                Debug.Log("GitHub for Unity " + ApplicationInfo.Version + " is disabled");
                return;
            }

            ServicePointManager.ServerCertificateValidationCallback = ServerCertificateValidationCallback;
            EditorApplication.update += Initialize;
        }

        // we do this so we're guaranteed to run on the main thread, not the loader thread
        private static void Initialize()
        {
            EditorApplication.update -= Initialize;

            var logPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData)
                                .ToNPath().Combine(ApplicationInfo.ApplicationName, "github-unity.log");

            if (ApplicationCache.Instance.FirstRun)
            {
                Logging.Info("Initializing GitHub for Unity version " + ApplicationInfo.Version);
                //Logging.Info("ApplicationCache: " + ApplicationCache.Instance.CreatedDate);
                Logging.Info("Initializing GitHub for Unity log file: " + logPath);
            }

            //try
            //{
            //    if (logPath.FileExists())
            //    {
            //        logPath.Move(logPath.Parent.Combine(string.Format("github-unity-{0}.log"), System.DateTime.Now.ToString("s")));
            //    }
            //}
            //catch
            //{}

            Logging.LoggerFactory = s => new FileLogAdapter(logPath, s);
            logger = Logging.GetLogger<EntryPoint>();

            Logging.Info("Initializing GitHub for Unity version " + ApplicationInfo.Version);

            ApplicationManager.Run();

            //Logging.Trace("ApplicationCache: " + ApplicationCache.Instance.CreatedDate);
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

        public static IApplicationManager AppManager { get { return ApplicationManager; } }

        public static IEnvironment Environment { get { return ApplicationManager.Environment; } }

        public static IProcessEnvironment GitEnvironment { get { return ApplicationManager.GitEnvironment; } }

        public static IFileSystem FileSystem { get { return ApplicationManager.FileSystem; } }

        public static IPlatform Platform { get { return ApplicationManager.Platform; } }
        public static ICredentialManager CredentialManager { get { return Platform.CredentialManager; } }
        public static IKeychain Keychain { get { return Platform.Keychain; } }

        public static IProcessManager ProcessManager { get { return ApplicationManager.ProcessManager; } }
        public static GitObjectFactory GitObjectFactory { get { return new GitObjectFactory(Environment); } }

        public static ISettings LocalSettings { get { return ApplicationManager.LocalSettings; } }
        public static ISettings UserSettings { get { return ApplicationManager.UserSettings; } }
        public static ISettings SystemSettings { get { return ApplicationManager.SystemSettings; } }
        public static ITaskResultDispatcher TaskResultDispatcher { get { return ApplicationManager.MainThreadResultDispatcher; } }

        public static bool Initialized { get; private set; }
    }
}
