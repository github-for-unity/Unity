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
    public class EntryPoint : ScriptableObject
    {
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
                var oldLogPath = logPath.Parent.Combine(logPath.FileNameWithoutExtension + "-old" + logPath.ExtensionWithDot);
                try
                {
                    var shouldRotate = true;
#if DEVELOPER_BUILD
                    shouldRotate = new FileInfo(logPath).Length > 10 * 1024 * 1024;
#endif
                    if (shouldRotate)
                    {
                        oldLogPath.DeleteIfExists();
                        if (logPath.FileExists())
                        {
                            logPath.Move(oldLogPath);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logging.Error(ex, "Error rotating log files");
                }

                Debug.LogFormat("Initialized GitHub for Unity version {0}{1}Log file: {2}", ApplicationInfo.Version, Environment.NewLine, logPath);
            }

            Logging.LogAdapter = new FileLogAdapter(logPath);
            Logging.Info("Initializing GitHub for Unity version " + ApplicationInfo.Version);

            ApplicationManager.Run(ApplicationCache.Instance.FirstRun);
        }

        private static bool ServerCertificateValidationCallback(object sender, X509Certificate certificate,
            X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            return true;
        }

        private static ApplicationManager appManager;
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
