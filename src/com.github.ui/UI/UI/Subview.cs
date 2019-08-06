using Unity.VersionControl.Git;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace GitHub.Unity
{
    abstract class Subview : IView
    {
        private const string NullParentError = "Subview parent is null";

        public Subview()
        {
        }

        public virtual void InitializeView(IView parent)
        {
            Debug.Assert(parent != null, NullParentError);
            Parent = parent;
        }

        public virtual void OnEnable()
        {}

        public virtual void OnDisable()
        {}
        public virtual void OnBeforeFirstDraw()
        {}

        public virtual void OnDataUpdate()
        {}

        public virtual void OnUI()
        {}

        public virtual void OnSelectionChange()
        {}

        public virtual void OnFocusChanged()
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

        public void DoEmptyGUI()
        {
            Parent.DoEmptyGUI();
        }

        public void DoProgressGUI()
        {
            Parent.DoProgressGUI();
        }

        public void UpdateProgress(IProgress progress)
        {
            Parent.UpdateProgress(progress);
        }

        public void Refresh(CacheType type)
        {
            Parent.Refresh(type);
        }

        public void ReceivedEvent(CacheType type)
        {
            Parent.ReceivedEvent(type);
        }

        public virtual void DoneRefreshing()
        {
            Parent.DoneRefreshing();
        }

        protected IView Parent { get; private set; }

        public IApplicationManager Manager { get { return Parent.Manager; } }
        public IRepository Repository { get { return Parent.Repository; } }
        public bool HasRepository { get { return Parent.HasRepository; } }
        public IUser User { get { return Parent.User; } }
        public bool HasUser { get { return Parent.HasUser; } }
        protected ITaskManager TaskManager { get { return Manager.TaskManager; } }
        protected IGitClient GitClient { get { return Manager.GitClient; } }
        protected IEnvironment Environment { get { return Manager.Environment; } }
        protected IPlatform Platform { get { return Manager.Platform; } }
        protected IUsageTracker UsageTracker { get { return Manager.UsageTracker; } }
        protected IOAuthCallbackManager OAuthCallbackManager { get { return Manager.OAuthCallbackManager; } }

        public bool HasFocus { get { return Parent != null && Parent.HasFocus; } }
        public virtual bool IsBusy
        {
            get { return (Manager != null && Manager.IsBusy) || (Repository != null && Repository.IsBusy); }
        }

        public Rect Position { get { return Parent.Position; } }
        public string Title { get; protected set; }
        public Vector2 Size { get; protected set; }
        public Dictionary<CacheType, int> RefreshEvents { get { return Parent.RefreshEvents; } }
        public bool IsRefreshing { get { return Parent.IsRefreshing; } }

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
