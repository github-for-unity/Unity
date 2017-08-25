using System;
using UnityEngine;

namespace GitHub.Unity
{
    abstract class Subview : IView
    {
        public event Action<bool> OnClose;

        private const string NullParentError = "Subview parent is null";

        public virtual void InitializeView(IView parent)
        {
            Debug.Assert(parent != null, NullParentError);
            Logger.Trace("InitializeView");
            Parent = parent;
        }

        public virtual void OnEnable()
        {
            Logger.Trace("OnEnable");
        }

        public virtual void OnDisable()
        {
            Logger.Trace("OnDisable");
        }

        public virtual void OnDataUpdate()
        {}

        public virtual void OnGUI()
        {}

        public virtual void OnSelectionChange()
        {}

        public virtual void Refresh()
        {
            Logger.Trace("Refresh");
        }

        public virtual void Redraw()
        {
            Parent.Redraw();
        }

        public virtual void Finish(bool result)
        {
            Parent.Finish(result);
        }

        public virtual void OnRepositoryChanged(IRepository oldRepository)
        {}

        protected IView Parent { get; private set; }
        public IApplicationManager Manager { get { return Parent.Manager; } }
        public IRepository Repository { get { return Parent.Repository; } }
        public bool HasRepository { get { return Parent.HasRepository; } }

        protected ITaskManager TaskManager { get { return Manager.TaskManager; } }
        protected IGitClient GitClient { get { return Manager.GitClient; } }
        protected IEnvironment Environment { get { return Manager.Environment; } }
        protected IPlatform Platform { get { return Manager.Platform; } }
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
