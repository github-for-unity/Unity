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
    internal sealed class ApplicationCache : ScriptableObject
        //, ISerializationCallbackReceiver
    {
        private static ApplicationCache instance;

        public static ApplicationCache Instance {
            get {
                return instance ?? (instance = CreateInstance());
            }
        }

        private static ApplicationCache CreateInstance()
        {
            var foundInstance = FindObjectOfType<ApplicationCache>();
            if (foundInstance != null)
            {
                Debug.Log("Instance Found");
                return foundInstance;
            }

            if (System.IO.File.Exists(GetCachePath()))
            {
                Debug.Log("Loading from cache");

                var objects = UnityEditorInternal.InternalEditorUtility.LoadSerializedFileAndForget(GetCachePath());
                if (objects.Any())
                {
                    var applicationCache = objects[0] as ApplicationCache;
                    if (applicationCache != null)
                    {
                        Debug.Log("Loading from cache successful");
                        return applicationCache;
                    }
                }
            }

            Debug.Log("Creating instance");
            var createdInstance = CreateInstance<ApplicationCache>();
            createdInstance.Initialize();

            return createdInstance;
        }

        [SerializeField] public bool Initialized;

        [SerializeField] public string CreatedDate;
        public static Texture2D LockedModifiedStatusIcon;
        public static Texture2D LockedStatusIcon;
        public static Texture2D ModifiedStatusIcon;
        public static Texture2D AddedStatusIcon;
        public static Texture2D DeletedStatusIcon;
        public static Texture2D RenamedStatusIcon;
        public static Texture2D UntrackedStatusIcon;

        public void Initialize()
        {
            if (!Initialized)
            {
                Debug.Log("Initializing");
                Initialized = true;
                CreatedDate = DateTime.Now.ToLongTimeString();
            }
        }

        private static string GetCachePath()
        {
            return Application.dataPath + "/../Temp/github_cache.yaml";
        }

        private void OnDisable()
        {
            Debug.Log("ApplicationCache OnDisable");
            if (instance != null)
            {
                UnityEditorInternal.InternalEditorUtility.SaveToSerializedFileAndForget(new Object[] { instance }, GetCachePath(), true);
            }
        }

//        public void OnBeforeSerialize()
//        {
//            Debug.Log("ApplicationCache OnBeforeSerialize");
//        }
//
//        public void OnAfterDeserialize()
//        {
//        }
    }

    [InitializeOnLoad]
    class EntryPoint : ScriptableObject
    {
        private static ILogging logger;
        private static bool cctorCalled = false;

        private static ApplicationManager appManager;

        // this may run on the loader thread if it's an appdomain restart
        static EntryPoint()
        {
            if (System.Environment.GetEnvironmentVariable("GITHUB_UNITY_DISABLE") == "1")
            {
                Debug.Log("GitHub for Unity " + ApplicationInfo.Version + " is disabled");
                return;
            }

            if (cctorCalled)
            {
                return;
            }
            cctorCalled = true;
            Logging.LoggerFactory = s => new UnityLogAdapter(s);
            Logging.Info("Initializing GitHub for Unity version " + ApplicationInfo.Version);

            Logging.Trace("ApplicationCache: " + ApplicationCache.Instance.CreatedDate);

            ServicePointManager.ServerCertificateValidationCallback = ServerCertificateValidationCallback;
            EditorApplication.update += Initialize;
        }

        // we do this so we're guaranteed to run on the main thread, not the loader thread
        private static void Initialize()
        {
            EditorApplication.update -= Initialize;

            var logPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData)
                .ToNPath().Combine(ApplicationInfo.ApplicationName, "github-unity.log");
            Logging.Info("Initializing GitHub for Unity log file: " + logPath);
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

        public static IProcessManager ProcessManager { get { return ApplicationManager.ProcessManager; } }
        public static GitObjectFactory GitObjectFactory { get { return new GitObjectFactory(Environment); } }

        public static ISettings LocalSettings { get { return ApplicationManager.LocalSettings; } }
        public static ISettings UserSettings { get { return ApplicationManager.UserSettings; } }
        public static ISettings SystemSettings { get { return ApplicationManager.SystemSettings; } }
        public static ITaskResultDispatcher TaskResultDispatcher { get { return ApplicationManager.MainThreadResultDispatcher; } }

        public static bool Initialized { get; private set; }
    }
}
