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

        private IEnvironment environment;
        private FieldInfo quitActionField;

        // for unit testing (TODO)
        public ApplicationManager(IEnvironment environment, IFileSystem fileSystem, IPlatform platform,
            IProcessManager processManager)
            : base(null)
        {
            Environment = environment;
            FileSystem = fileSystem;
            NPath.FileSystem = FileSystem;
            Platform = platform;
            ProcessManager = processManager;
        }

        public ApplicationManager(IMainThreadSynchronizationContext synchronizationContext)
            : base(synchronizationContext as SynchronizationContext)
        {
            ListenToUnityExit();
            DetermineInstallationPath();

            var uiDispatcher = new AuthenticationUIDispatcher();
            Initialize();
        }

        public override ITask Run()
        {
            Utility.Initialize();

            return base.Run()
                .ThenInUI(_ =>
                {
                    Utility.Run();

                    ProjectWindowInterface.Initialize(Environment.Repository);

                    var view = Window.GetView();
                    if (view != null)
                        view.Initialize(this);

                    //logger.Debug("Application Restarted");
                }).Start();
        }


        protected override void InitializeEnvironment()
        {
            FileSystem = new FileSystem();
            NPath.FileSystem = FileSystem;

            Environment = new DefaultEnvironment();

            // figure out where we are
            Environment.ExtensionInstallPath = DetermineInstallationPath();

            // figure out where the project is
            var assetsPath = Application.dataPath.ToNPath();
            var projectPath = assetsPath.Parent;

            Environment.UnityApplication = EditorApplication.applicationPath.ToNPath();

            Environment.UnityAssetsPath = assetsPath;
            Environment.UnityProjectPath = projectPath;

            base.InitializeEnvironment();
        }

        public override ITask RestartRepository()
        {
            logger.Trace("Restarting");
            return base.RestartRepository()
                .ThenInUI(_ =>
                {
                    logger.Trace("Restarted");
                    ProjectWindowInterface.Initialize(Environment.Repository);
                    var view = Window.GetView();
                    if (view != null)
                        view.Initialize(this);
                });
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

        private NPath DetermineInstallationPath()
        {
            // Juggling to find out where we got installed
            var shim = ScriptableObject.CreateInstance<RunLocationShim>();
            var script = MonoScript.FromScriptableObject(shim);
            NPath ret = null;
            
            if (script != null)
            {
                var scriptPath = AssetDatabase.GetAssetPath(script).ToNPath();
                ret = scriptPath.Parent;
            }
            ScriptableObject.DestroyImmediate(shim);
            return ret;
        }

        private bool disposed = false;
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                base.Dispose(disposing);
                if (!disposed)
                {
                    disposed = true;
                }
            }
        }

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
