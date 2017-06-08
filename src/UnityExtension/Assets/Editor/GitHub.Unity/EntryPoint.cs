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
        private static ApplicationManager appManager;

        // this may run on the loader thread if it's an appdomain restart
        static EntryPoint()
        {
            var tempEnv = new DefaultEnvironment();
            if (tempEnv.GetEnvironmentVariable("GITHUB_UNITY_DISABLE") == "1")
            {
                Debug.Log("GitHub for Unity " + ApplicationInfo.Version + " is disabled");
                return;
            }

            Logging.LogAdapter = new FileLogAdapter(tempEnv.LogPath);

            ServicePointManager.ServerCertificateValidationCallback = ServerCertificateValidationCallback;
            EditorApplication.update += Initialize;
        }

        // we do this so we're guaranteed to run on the main thread, not the loader thread
        private static void Initialize()
        {
            EditorApplication.update -= Initialize;

            // this will initialize ApplicationManager and Environment if they haven't yet
            var logPath = Environment.LogPath;

            if (ApplicationCache.Instance.FirstRun)
            {
                Debug.Log("Initializing GitHub for Unity version " + ApplicationInfo.Version);

                var oldLogPath = logPath.Parent.Combine(logPath.FileNameWithoutExtension + "-old" + logPath.ExtensionWithDot);
                try
                {
                    oldLogPath.DeleteIfExists();
                    if (logPath.FileExists())
                    {
                        logPath.Move(oldLogPath);
                    }
                }
                catch (Exception ex)
                {
                    Logging.Error(ex, "Error rotating log files");
                }

                Debug.Log("Initializing GitHub for Unity log file: " + logPath);
            }
            Logging.LogAdapter = new FileLogAdapter(logPath);
            Logging.Info("Initializing GitHub for Unity version " + ApplicationInfo.Version);

            ((ApplicationManager)ApplicationManager).Run();
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

        public static IApplicationManager ApplicationManager
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

        public static IEnvironment Environment { get { return ApplicationManager.Environment; } }
        public static IUsageTracker UsageTracker { get { return ApplicationManager.UsageTracker; } }

    }
}
