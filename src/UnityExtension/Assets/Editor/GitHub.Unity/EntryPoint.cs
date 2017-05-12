using System;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using UnityEditor;
using UnityEngine;

namespace GitHub.Unity
{
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

            var logPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData)
                                .ToNPath().Combine(ApplicationInfo.ApplicationName, "github-unity.log");

            Logging.LoggerFactory = s => new FileLogAdapter(logPath, s);
            logger = Logging.GetLogger<EntryPoint>();

            ServicePointManager.ServerCertificateValidationCallback = ServerCertificateValidationCallback;
            EditorApplication.update += Initialize;
        }

        // we do this so we're guaranteed to run on the main thread, not the loader thread
        private static void Initialize()
        {
            EditorApplication.update -= Initialize;

            var applicationName = System.Environment
                                        .GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData)
                                        .ToNPath().Combine(ApplicationInfo.ApplicationName);


            var logPath = applicationName.Combine("github-unity.log").ToString();


            if (ApplicationCache.Instance.FirstRun)
            {
                Logging.Info("Initializing GitHub for Unity version " + ApplicationInfo.Version);

                var oldLogPath = applicationName.Combine("github-unity-old.log").ToString();
                try
                {
                    if (File.Exists(oldLogPath))
                    {
                        File.Delete(oldLogPath);
                    }

                    if (File.Exists(logPath))
                    {
                        File.Move(logPath, oldLogPath);
                    }
                }
                catch (Exception ex)
                {
                    Logging.Error(ex, "Error rotating log files");
                }
            }

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
