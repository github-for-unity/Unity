using UnityEditor;
using UnityEngine;
using ILogger = GitHub.Unity.Logging.ILogger;

namespace GitHub.Unity
{
    [InitializeOnLoad]
    class EntryPoint : ScriptableObject
    {
        private static readonly ILogger logger = Logging.Logger.GetLogger<EntryPoint>();

        // this may run on the loader thread if it's an appdomain restart
        static EntryPoint()
        {
            EditorApplication.update += Initialize;
        }

        // we do this so we're guaranteed to run on the main thread, not the loader thread
        private static void Initialize()
        {
            EditorApplication.update -= Initialize;

            logger.Debug("Initialize");

            FileSystem = new FileSystem();

            Environment = new DefaultEnvironment();

            GitEnvironment = Environment.IsWindows
                ? new WindowsGitEnvironment(FileSystem, Environment)
                : (Environment.IsLinux
                    ? (IGitEnvironment)new LinuxBasedGitEnvironment(FileSystem, Environment)
                    : new MacBasedGitEnvironment(FileSystem, Environment));

            ProcessManager = new ProcessManager(Environment, GitEnvironment, FileSystem);

            Settings = new Settings();

            DetermineInstallationPath(Environment);

            DetermineGitRepoRoot();

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

        private static void DetermineGitRepoRoot()
        {
            var fullProjectRoot = FileSystem.GetFullPath(Environment.UnityProjectPath);
            Environment.GitRoot = GitEnvironment.FindRoot(fullProjectRoot);
        }

        private static void DetermineInstallationPath(IEnvironment environment)
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

        public static IEnvironment Environment { get; private set; }
        public static IGitEnvironment GitEnvironment { get; private set; }

        public static IFileSystem FileSystem { get; private set; }
        public static IProcessManager ProcessManager { get; private set; }

        public static GitStatusEntryFactory GitStatusEntryFactory { get; private set; }
        public static ISettings Settings { get; private set; }
    }
}
