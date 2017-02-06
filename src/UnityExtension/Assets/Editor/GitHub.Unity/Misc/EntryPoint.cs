﻿using GitHub.Api;
using System;
using System.Linq;
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
        private static readonly ILogging logger;

        // this may run on the loader thread if it's an appdomain restart
        static EntryPoint()
        {
            Logging.LoggerFactory = s => new UnityLogAdapter(s);
            logger = Logging.GetLogger<EntryPoint>();
            logger.Debug("EntryPoint Initialize");
            ServicePointManager.ServerCertificateValidationCallback = ServerCertificateValidationCallback;
            EditorApplication.update += Initialize;
        }

        // we do this so we're guaranteed to run on the main thread, not the loader thread
        private static void Initialize()
        {
            EditorApplication.update -= Initialize;

            logger.Debug("Initialize");

            FileSystem = new FileSystem();

            Environment = new DefaultEnvironment();

            Platform = new Platform(Environment);

            GitEnvironment = Environment.IsWindows
                ? new WindowsGitEnvironment(FileSystem, Environment)
                : (Environment.IsLinux
                    ? (IGitEnvironment)new LinuxBasedGitEnvironment(FileSystem, Environment)
                    : new MacBasedGitEnvironment(FileSystem, Environment));

            ProcessManager = new ProcessManager(Environment, GitEnvironment, FileSystem);

            DeterminePaths(Environment, GitEnvironment, FileSystem);

            DetermineGitRepoRoot(Environment, GitEnvironment, FileSystem);

            Settings = new Settings(Environment);

            Settings.Initialize();

            Tasks.Initialize();

            DetermineGitInstallationPath(Environment, GitEnvironment, FileSystem, Settings);

            GitStatusEntryFactory = new GitStatusEntryFactory(Environment, FileSystem, GitEnvironment);

            Utility.Initialize();

            Tasks.Run();

            Utility.Run();

            ProjectWindowInterface.Initialize();

            Window.Initialize();
        }

        private static void DetermineGitRepoRoot(IEnvironment environment, IGitEnvironment gitEnvironment, IFileSystem fs)
        {
            var fullProjectRoot = fs.GetFullPath(Environment.UnityProjectPath);
            environment.GitRoot = gitEnvironment.FindRoot(fullProjectRoot);
        }

        private static void DeterminePaths(IEnvironment environment, IGitEnvironment gitEnvironment, IFileSystem fs)
        {
            // Unity paths

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

            environment.UnityAssetsPath = Application.dataPath;
            environment.UnityProjectPath = environment.UnityAssetsPath.Substring(0, environment.UnityAssetsPath.Length - "Assets".Length - 1);
        }

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
            // If there are errors in the certificate chain, look at each error to determine the cause.
            if (sslPolicyErrors != SslPolicyErrors.None)
            {
                foreach (var status in chain.ChainStatus.Where(st => st.Status != X509ChainStatusFlags.RevocationStatusUnknown))
                {
                    chain.ChainPolicy.RevocationFlag = X509RevocationFlag.EntireChain;
                    chain.ChainPolicy.RevocationMode = X509RevocationMode.Online;
                    chain.ChainPolicy.UrlRetrievalTimeout = new TimeSpan(0, 1, 0);
                    chain.ChainPolicy.VerificationFlags = X509VerificationFlags.AllFlags;
                    success &= chain.Build((X509Certificate2)certificate);
                }
            }
            return success;
        }

        public static IEnvironment Environment { get; private set; }
        public static IGitEnvironment GitEnvironment { get; private set; }

        public static IFileSystem FileSystem { get; private set; }
        public static IProcessManager ProcessManager { get; private set; }

        public static GitStatusEntryFactory GitStatusEntryFactory { get; private set; }
        public static ISettings Settings { get; private set; }
        public static IPlatform Platform { get; private set; }
    }
}
