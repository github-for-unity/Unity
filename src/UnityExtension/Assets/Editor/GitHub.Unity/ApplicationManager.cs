using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Rackspace.Threading;
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

        public ApplicationManager(IMainThreadSynchronizationContext synchronizationContext)
            : base(synchronizationContext as SynchronizationContext)
        {
            ListenToUnityExit();
            DetermineInstallationPath();

            Initialize();
        }

        public override ITask Run()
        {
            Utility.Initialize();

            return base.Run()
                .ThenInUI(_ =>
                {
                    Logger.Debug("Run");
                    Utility.Run();

                    ProjectWindowInterface.Initialize(Environment.Repository);

                    var view = Window.GetView();
                    if (view != null)
                        view.Initialize(this);
                    return new { Version = Application.unityVersion, FirstRun = ApplicationCache.Instance.FirstRun };
                })
                .Then((s, x) => SetupMetrics(x.Version, x.FirstRun))
                .Start();
        }


        protected override string GetAssetsPath()
        {
            return Application.dataPath;
        }

        protected override string GetUnityPath()
        {
            return EditorApplication.applicationPath;
        }

        protected override string DetermineInstallationPath()
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

        public override ITask RestartRepository()
        {
            Logger.Trace("Restarting");
            return base.RestartRepository()
                .ThenInUI(_ =>
                {
                    Logger.Trace("Restarted {0}", Environment.Repository);
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

        public override IProcessEnvironment GitEnvironment { get { return Platform.GitEnvironment; } }
    }
}
