using GitHub.Api;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using UnityEditor;
using UnityEngine;

namespace GitHub.Unity
{
    [InitializeOnLoad]
    class EntryPoint : ScriptableObject
    {
        private static readonly ILogging logger;
        private static bool cctorCalled = false;

        // this may run on the loader thread if it's an appdomain restart
        static EntryPoint()
        {
            if (cctorCalled)
            {
                return;
            }
            cctorCalled = true;
            Logging.LoggerFactory = s => new UnityLogAdapter(s);
            logger = Logging.GetLogger<EntryPoint>();
            logger.Debug("EntryPoint Initialize");

            ServicePointManager.ServerCertificateValidationCallback = ServerCertificateValidationCallback;
            EditorApplication.update += Initialize;
        }

        // we do this so we're guaranteed to run on the main thread, not the loader thread
        private static void Initialize()
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

            ThreadUtils.SetMainThread();
            var syncCtx = new MainThreadSynchronizationContext();
            SynchronizationContext.SetSynchronizationContext(syncCtx);

            EditorApplication.update -= Initialize;

            logger.Debug("Initialize");

            ProcessManager = new ProcessManager(Environment, GitEnvironment, FileSystem);

            DeterminePaths(Environment, GitEnvironment, FileSystem);

            DetermineGitRepoRoot(Environment, GitEnvironment, FileSystem);

            Settings = new Settings(Environment);

            Settings.Initialize();

            Tasks.Initialize(syncCtx);

            DetermineGitInstallationPath(Environment, GitEnvironment, FileSystem, Settings);

            GitObjectFactory = new GitObjectFactory(Environment, FileSystem, GitEnvironment);

            Utility.Initialize();

            Tasks.Run();

            Utility.Run();

            ProjectWindowInterface.Initialize();

            Window.Initialize();
        }


        // TODO: Move these out to a proper location
        private static void DetermineGitRepoRoot(IEnvironment environment, IGitEnvironment gitEnvironment, IFileSystem fs)
        {
            var fullProjectRoot = FileSystem.GetFullPath(Environment.UnityProjectPath);
            environment.GitRoot = gitEnvironment.FindRoot(fullProjectRoot);
        }

        // TODO: Move these out to a proper location
        private static void DeterminePaths(IEnvironment environment, IGitEnvironment gitEnvironment, IFileSystem fs)
        {
            // Unity paths
            environment.UnityAssetsPath = Application.dataPath;
            environment.UnityProjectPath = environment.UnityAssetsPath.Substring(0, environment.UnityAssetsPath.Length - "Assets".Length - 1);

            // Juggling to find out where we got installed
            var instance = FindObjectOfType(typeof(EntryPoint)) as EntryPoint;
            if (instance == null)
            {
                instance = CreateInstance<EntryPoint>();
            }

            var script = MonoScript.FromScriptableObject(instance);
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

            DestroyImmediate(instance);

        }

        // TODO: Move these out to a proper location
        private static void DetermineGitInstallationPath(IEnvironment environment, IGitEnvironment gitEnvironment, IFileSystem fs,
            ISettings settings)
        {
            var cachedGitInstallPath = settings.Get("GitInstallPath");

            // Root paths
            if (string.IsNullOrEmpty(cachedGitInstallPath) || !fs.FileExists(cachedGitInstallPath))
            {
                FindGitTask.Schedule(path => {
                    logger.Debug("found " + path);
                    if (!string.IsNullOrEmpty(path))
                    {
                        environment.GitInstallPath = path;
                    }
                }, () => logger.Debug("NOT FOUND"));
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
        private static IGitEnvironment gitEnvironment;
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

        public static IProcessManager ProcessManager { get; private set; }

        public static GitObjectFactory GitObjectFactory { get; private set; }
        public static ISettings Settings { get; private set; }
    }
}
