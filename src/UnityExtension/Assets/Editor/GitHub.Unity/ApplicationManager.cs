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
            MainThreadResultDispatcher = taskResultDispatcher;
        }

        public ApplicationManager(MainThreadSynchronizationContext synchronizationContext)
            : base(synchronizationContext)
        {
            ListenToUnityExit();
            DetermineInstallationPath();

            MainThreadResultDispatcher = new MainThreadTaskResultDispatcher();
            var uiDispatcher = new AuthenticationUIDispatcher();
            Initialize(uiDispatcher);
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

                    ProjectWindowInterface.Initialize(Environment.Repository);

                    Window.Initialize(Environment.Repository);

                    //logger.Debug("Application Restarted");
                }, UIScheduler);
        }


        protected override void InitializeEnvironment()
        {
            FileSystem = new FileSystem();
            NPathFileSystemProvider.Current = FileSystem;

            Environment = new DefaultEnvironment();

            // figure out where we are
            Environment.ExtensionInstallPath = DetermineInstallationPath();

            // figure out where the project is
            var assetsPath = Application.dataPath.ToNPath();
            var projectPath = assetsPath.Parent;

            Environment.UnityAssetsPath = assetsPath.ToString(SlashMode.Forward);
            Environment.UnityProjectPath = projectPath.ToString(SlashMode.Forward);

            base.InitializeEnvironment();
        }

        public override Task RestartRepository()
        {
            logger.Trace("Restarting");
            return base.RestartRepository()
                .ContinueWith(_ =>
                {
                    logger.Trace("Restarted");
                    Window.Initialize(Environment.Repository);
                }, UIScheduler);
        }

        public override void SetProjectToTextSerialization()
        {
            logger.Trace("SetProjectToTextSerialization");
            Tasks.Add(new SimpleTask(() => { EditorSettings.serializationMode = SerializationMode.ForceText; }, ThreadingHelper.MainThreadScheduler));
        }

        private void ListenToUnityExit()
        {
            EditorApplicationQuit = (UnityAction)Delegate.Combine(EditorApplicationQuit, new UnityAction(Dispose));
            EditorApplication.playmodeStateChanged += () => {
                if (EditorApplication.isPlayingOrWillChangePlaymode && !EditorApplication.isPlaying)
                {
                    Dispose();
                }
            };
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

        private bool disposed = false;
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing)
            {
                if (!disposed)
                {
                    disposed = true;
                    taskRunner.Shutdown();
                }
            }
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