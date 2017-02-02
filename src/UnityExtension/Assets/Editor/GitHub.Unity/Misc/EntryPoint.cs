using UnityEditor;
using UnityEngine;
using ILogger = GitHub.Unity.Logging.ILogger;

namespace GitHub.Unity
{
    [InitializeOnLoad]
    class EntryPoint
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
            logger.Debug("Initialize");

            EditorApplication.update -= Initialize;

            Tasks.Initialize();

            Utility.Initialize();

            Installer.Initialize();

            Tasks.Run();

            ProjectWindowInterface.Initialize();

            FileSystem = new FileSystem();

            Environment = new DefaultEnvironment();

            GitEnvironment = Environment.IsWindows
                ? new WindowsGitEnvironment(FileSystem, Environment)
                : (Environment.IsLinux
                    ? (IGitEnvironment)new LinuxBasedGitEnvironment(FileSystem, Environment)
                    : new MacBasedGitEnvironment(FileSystem, Environment));

            ProcessManager = new ProcessManager(Environment, GitEnvironment, FileSystem);

            GitStatusEntryFactory = new GitStatusEntryFactory(Environment, FileSystem, GitEnvironment);

            Window.Initialize();
        }

        public static IEnvironment Environment { get; private set; }
        public static IGitEnvironment GitEnvironment { get; private set; }

        public static IFileSystem FileSystem { get; private set; }
        public static IProcessManager ProcessManager { get; private set; }

        public static GitStatusEntryFactory GitStatusEntryFactory { get; private set; }
    }
}