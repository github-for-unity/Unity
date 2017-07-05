﻿using System;
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

        public ApplicationManager(IMainThreadSynchronizationContext synchronizationContext)
            : base(synchronizationContext as SynchronizationContext)
        {
            ListenToUnityExit();
            Initialize();
        }

        protected override void SetupMetrics()
        {

            new ActionTask(CancellationToken,
                    () => SetupMetrics(Environment.UnityVersion, ApplicationCache.Instance.FirstRun))
                { Affinity = TaskAffinity.UI }
                .Start();
        }

        protected override void InitializeUI()
        {
            Logger.Trace("Restarted {0}", Environment.Repository);
            ProjectWindowInterface.Initialize(Environment.Repository);
            var view = Window.GetWindow();
            if (view != null)
                view.Initialize(this);
        }

        protected override void SetProjectToTextSerialization()
        {
            Logger.Trace("SetProjectToTextSerialization");
            EditorSettings.serializationMode = SerializationMode.ForceText;
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
        public override IEnvironment Environment { get { return EnvironmentCache.Instance.Environment; } }
    }
}
