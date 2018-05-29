using System;
using System.Reflection;
using System.Threading;
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

        public ApplicationManager(IMainThreadSynchronizationContext synchronizationContext,
            IEnvironment environment)
            : base(synchronizationContext as SynchronizationContext, environment)
        {
            FirstRun = ApplicationCache.Instance.FirstRun;
            InstanceId = ApplicationCache.Instance.InstanceId;

            ListenToUnityExit();
            Initialize();
        }

        public override void OpenPopupWindow(PopupViewType viewType, object data, Action<bool, object> onClose)
        {
            PopupWindow.OpenWindow(viewType, data, onClose);
        }

        protected override void InitializeUI()
        {
            Logger.Trace("Restarted {0}", Environment.Repository != null ? Environment.Repository.LocalPath : "null");
            EnvironmentCache.Instance.Flush();

            isBusy = false;
            LfsLocksModificationProcessor.Initialize(Environment, Platform);
            ProjectWindowInterface.Initialize(this);
            var window = Window.GetWindow();
            if (window != null)
                window.InitializeWindow(this);
            SetProjectToTextSerialization();
        }

        protected void SetProjectToTextSerialization()
        {
            EditorSettings.serializationMode = SerializationMode.ForceText;
        }

        protected override void InitializationComplete()
        {
            ApplicationCache.Instance.Initialized = true;
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
                if (!disposed)
                {
                    disposed = true;
                }
            }
            base.Dispose(disposing);
        }

        public override IProcessEnvironment GitEnvironment { get { return Platform.GitEnvironment; } }
    }
}
