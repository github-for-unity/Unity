using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;

namespace GitHub.Unity
{
    class ApplicationManager : ApplicationManagerBase
    {

        private const string QuitActionFieldName = "editorApplicationQuit";
        private const BindingFlags quitActionBindingFlags = BindingFlags.NonPublic | BindingFlags.Static;

        private FieldInfo quitActionField;

        private Tasks taskRunner;

        // for unit testing (TODO)
        public ApplicationManager(IEnvironment environment, IFileSystem fileSystem,
            IPlatform platform, IProcessManager processManager, ITaskResultDispatcher taskResultDispatcher)
            : base(null)
        {
            Environment = environment;
            FileSystem = fileSystem;
            NPathFileSystemProvider.Current = FileSystem;
            Platform = platform;
            ProcessManager = processManager;
            TaskResultDispatcher = taskResultDispatcher;
        }

        public ApplicationManager(MainThreadSynchronizationContext synchronizationContext)
            : base(synchronizationContext)
        {
            ListenToUnityExit();
            DetermineInstallationPath();

            TaskResultDispatcher = new TaskResultDispatcher();

            // accessing Environment triggers environment initialization if it hasn't happened yet
            LocalSettings = new LocalSettings(Environment);
            UserSettings = new UserSettings(Environment, ApplicationInfo.ApplicationName);
            SystemSettings = new SystemSettings(Environment, ApplicationInfo.ApplicationName);

            Platform = new Platform(Environment, FileSystem);
            GitObjectFactory = new GitObjectFactory(Environment, Platform.GitEnvironment);
            ProcessManager = new ProcessManager(Environment, Platform.GitEnvironment, CancellationToken);
            Platform.Initialize(ProcessManager);
            CredentialManager = Platform.CredentialManager;
            ApiClientFactory.Instance = new ApiClientFactory(new AppConfiguration(), Platform.CredentialManager);
        }

        public override Task Run()
        {
            Utility.Initialize();

            taskRunner = new Tasks((MainThreadSynchronizationContext)SynchronizationContext,
                CancellationTokenSource.Token);

            return base.Run()
                .ContinueWith(_ =>
                {
                    taskRunner.Run();

                    Utility.Run();

                    ProjectWindowInterface.Initialize();

                    Window.Initialize();
                }, Scheduler);
        }


        private void InitializeEnvironment()
        {
            NPathFileSystemProvider.Current = new FileSystem();

            Environment = new DefaultEnvironment();

            // figure out where we are
            Environment.ExtensionInstallPath = DetermineInstallationPath();

            // figure out where the project is
            var assetsPath = Application.dataPath.ToNPath();
            var projectPath = assetsPath.Parent;

            Environment.UnityAssetsPath = assetsPath.ToString(SlashMode.Forward);
            Environment.UnityProjectPath = projectPath.ToString(SlashMode.Forward);

            // figure out where the repository root is
            GitClient = new GitClient(projectPath);
            Environment.Repository = GitClient.GetRepository();

            // Make sure CurrentDirectory always returns the repository root, so all
            // file system path calculations use it as a base
            FileSystem = new FileSystem(Environment.Repository.LocalPath);
            NPathFileSystemProvider.Current = FileSystem;
        }

        private void ListenToUnityExit()
        {
            EditorApplicationQuit = (UnityAction)Delegate.Combine(EditorApplicationQuit, new UnityAction(OnShutdown));
            EditorApplication.playmodeStateChanged += () => {
                if (EditorApplication.isPlayingOrWillChangePlaymode && !EditorApplication.isPlaying)
                {
                    OnShutdown();
                }
            };
        }

        private void OnShutdown()
        {
            taskRunner.Shutdown();
            CancellationTokenSource.Cancel();
        }

        private UnityAction EditorApplicationQuit
        {
            get
            {
                SecureQuitActionField();
                return (UnityAction)quitActionField.GetValue(null);
            }
            set
            {
                SecureQuitActionField();
                quitActionField.SetValue(null, value);
            }
        }

        private void SecureQuitActionField()
        {
            if (quitActionField == null)
            {
                quitActionField = typeof(EditorApplication).GetField(QuitActionFieldName, quitActionBindingFlags);

                if (quitActionField == null)
                {
                    throw new InvalidOperationException("Unable to reflect EditorApplication." + QuitActionFieldName);
                }
            }
        }

        private string DetermineInstallationPath()
        {
            // Juggling to find out where we got installed
            var shim = ScriptableObject.CreateInstance<RunLocationShim>();
            var script = MonoScript.FromScriptableObject(shim);
            string ret = String.Empty;
            
            if (script != null)
            {
                var scriptPath = AssetDatabase.GetAssetPath(script).ToNPath();
                ret = scriptPath.Parent.ToString(SlashMode.Forward);
            }
            ScriptableObject.DestroyImmediate(shim);
            return ret;
        }

        private IEnvironment environment;
        public override IEnvironment Environment
        {
            get
            {
                // if this is called while still null, it's because Unity wants
                // to render something and we need to load icons, and that runs
                // before EntryPoint. Do an early initialization
                if (environment == null)
                    InitializeEnvironment();
                return environment;
            }
            set { environment = value; }
        }
        public override IProcessEnvironment GitEnvironment { get { return Platform.GitEnvironment; } }
    }
}