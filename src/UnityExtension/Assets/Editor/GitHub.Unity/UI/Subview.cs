using System;
using UnityEditor;
using UnityEngine;

namespace GitHub.Unity
{
    abstract class BaseWindow :  EditorWindow, IView
    {
        private bool finishCalled = false;

        public event Action<bool> OnClose;

        public virtual void Initialize(IApplicationManager applicationManager)
        {
            Logger.Trace("Initialize(IApplicationManager:{0})", applicationManager != null ? applicationManager.ToString() : "NULL");
            Manager = applicationManager;
        }

        public virtual void Redraw()
        {
            Repaint();
        }

        public virtual void Refresh()
        {
        }
        public virtual void Finish(bool result)
        {
            finishCalled = true;
            RaiseOnClose(result);
        }

        protected void RaiseOnClose(bool result)
        {
            OnClose.SafeInvoke(result);
        }

        public virtual void Awake()
        {
        }

        public virtual void OnEnable()
        {
        }

        public virtual void OnDisable()
        {
        }

        public virtual void Update() {}
        public virtual void OnGUI() {}
        public virtual void OnDestroy()
        {
            if (!finishCalled)
            {
                RaiseOnClose(false);
            }

        }
        public virtual void OnSelectionChange()
        {}

        public virtual Rect Position { get { return position; } }
        public IApplicationManager Manager { get; private set; }
        public IRepository Repository { get { return Manager != null ? Manager.Environment.Repository : null; } }
        public ITaskManager TaskManager { get { return Manager != null ? Manager.TaskManager : null; } }
        protected IGitClient GitClient { get { return Manager != null ? Manager.GitClient : null; } }
        protected IEnvironment Environment { get { return Manager != null ? Manager.Environment : null; } }
        protected IPlatform Platform { get { return Manager != null ? Manager.Platform : null; } }


        private ILogging logger;
        protected ILogging Logger
        {
            get
            {
                if (logger == null)
                    logger = Logging.GetLogger(GetType());
                return logger;
            }
        }
    }

    abstract class Subview : IView
    {
        public event Action<bool> OnClose;

        private const string NullParentError = "Subview parent is null";
        protected IView Parent { get; private set; }
        public IApplicationManager Manager { get; private set; }
        public IRepository Repository { get { return Manager != null ? Manager.Environment.Repository : null; } }
        public ITaskManager TaskManager { get { return Manager != null ? Manager.TaskManager : null; } }
        protected IGitClient GitClient { get { return Manager != null ? Manager.GitClient : null; } }
        protected IEnvironment Environment { get { return Manager != null ? Manager.Environment : null; } }
        protected IPlatform Platform { get { return Manager != null ? Manager.Platform : null; } }

        public virtual void Initialize(IApplicationManager applicationManager)
        {
            Logger.Trace("Initialize(IApplicationManager: {0})", applicationManager != null);
            Manager = applicationManager;
        }

        public virtual void InitializeView(IView parent)
        {
            Debug.Assert(parent != null, NullParentError);
            Logger.Trace("Initialize(IView)");
            Parent = parent;
            ((IView)this).Initialize(parent.Manager);
        }

        public virtual void OnShow()
        {
        }

        public virtual void OnHide()
        {
        }

        public virtual void OnUpdate()
        {}

        public virtual void OnGUI()
        {}

        public virtual void OnDestroy()
        {}

        public virtual void OnSelectionChange()
        {}

        public virtual void Refresh()
        {}

        public virtual void Redraw()
        {
            Parent.Redraw();
        }

        public virtual void Finish(bool result)
        {
            Parent.Finish(result);
        }

        public virtual Rect Position { get { return Parent.Position; } }

        private ILogging logger;
        protected ILogging Logger
        {
            get
            {
                if (logger == null)
                    logger = Logging.GetLogger(GetType());
                return logger;
            }
        }
    }
}
