using GitHub.Logging;
using System;
using UnityEngine;

namespace GitHub.Unity
{
    abstract class Subview : IView
    {
        private const string NullParentError = "Subview parent is null";

        public virtual void InitializeView(IView parent)
        {
            Debug.Assert(parent != null, NullParentError);
            //Logger.Trace("InitializeView");
            Parent = parent;
        }

        public virtual void OnEnable()
        {
            //Logger.Trace("OnEnable");
        }

        public virtual void OnDisable()
        {
            //Logger.Trace("OnDisable");
        }

        public virtual void OnDataUpdate()
        {}

        public virtual void OnGUI()
        { }

        public virtual void OnSelectionChange()
        { }

        public virtual void OnFocusChanged()
        { }

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

        protected IView Parent { get; private set; }
        public IApplicationManager Manager { get { return Parent.Manager; } }
        public IRepository Repository { get { return Parent.Repository; } }
        public bool HasRepository { get { return Parent.HasRepository; } }
        public IUser User { get { return Parent.User; } }
        public bool HasUser { get { return Parent.HasUser; } }
        public bool HasFocus { get { return Parent != null && Parent.HasFocus; } }
        public abstract bool IsBusy { get; }
        protected ITaskManager TaskManager { get { return Manager.TaskManager; } }
        protected IGitClient GitClient { get { return Manager.GitClient; } }
        protected IEnvironment Environment { get { return Manager.Environment; } }
        protected IPlatform Platform { get { return Manager.Platform; } }
        public Rect Position { get { return Parent.Position; } }
        public string Title { get; protected set; }
        public Vector2 Size { get; protected set; }

        private ILogging logger;

        protected ILogging Logger
        {
            get
            {
                if (logger == null)
                    logger = LogHelper.GetLogger(GetType());
                return logger;
            }
        }
    }
}
