﻿using GitHub.Logging;
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

            LogHelper.LogAdapter = new FileLogAdapter(tempEnv.LogPath);

            EditorApplication.update += Initialize;
        }

        // we do this so we're guaranteed to run on the main thread, not the loader thread
        private static void Initialize()
        {
            EditorApplication.update -= Initialize;

            // this will initialize ApplicationManager and Environment if they haven't yet
            var logPath = ApplicationManager.Environment.LogPath;

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
                    LogHelper.Error(ex, "Error rotating log files");
                }

                Debug.LogFormat("Initialized GitHub for Unity version {0}{1}Log file: {2}", ApplicationInfo.Version, ApplicationManager.Environment.NewLine, logPath);
            }

            LogHelper.LogAdapter = new MultipleLogAdapter(new FileLogAdapter(logPath)
#if DEBUG
                , new UnityLogAdapter()
#endif
                );
            LogHelper.Info("Initializing GitHubForUnity:'v{0}' Unity:'v{1}'", ApplicationInfo.Version, ApplicationManager.Environment.UnityVersion);

            ApplicationManager.Run();

            if (ApplicationCache.Instance.FirstRun)
                UpdateCheckWindow.CheckForUpdates(ApplicationManager);
        }

        internal static void Restart()
        {
            if (appManager != null)
            {
                appManager.Dispose();
                appManager = null;
            }

            Initialize();
        }

        private static ApplicationManager appManager;
        public static IApplicationManager ApplicationManager
        {
            get
            {
                if (appManager == null)
                {
                    appManager = new ApplicationManager(new MainThreadSynchronizationContext(), EnvironmentCache.Instance.Environment);
                }
                return appManager;
            }
        }
    }
}
