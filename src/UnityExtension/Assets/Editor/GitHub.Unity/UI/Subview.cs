using System;
using UnityEditor;
using UnityEngine;

namespace GitHub.Unity
{
    abstract class BaseWindow :  EditorWindow, IView
    {
        [NonSerialized] private bool finishCalled = false;
        [NonSerialized] private bool initialized = false;

        [NonSerialized] private IApplicationManager cachedManager;
        [NonSerialized] private IRepository cachedRepository;
        [NonSerialized] private bool initializeWasCalled;
        [NonSerialized] private bool inLayout;

        public event Action<bool> OnClose;

        public virtual void Initialize(IApplicationManager applicationManager)
        {
            Logger.Trace("Initialize ApplicationManager:{0} Initialized:{1}", applicationManager, initialized);

            if (inLayout)
            {
                initializeWasCalled = true;
                cachedManager = applicationManager;
                return;
            }

            Manager = applicationManager;
            cachedRepository = Environment.Repository;
            initialized = true;
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
            Logger.Trace("Awake Initialized:{0}", initialized);
            if (!initialized)
                Initialize(EntryPoint.ApplicationManager);
        }

        public virtual void OnEnable()
        {
            Logger.Trace("OnEnable Initialized:{0}", initialized);
            if (!initialized)
                Initialize(EntryPoint.ApplicationManager);
        }

        public virtual void OnDisable() {}

        public virtual void Update() {}

        public virtual void OnDataUpdate()
        {}

        // OnGUI calls this everytime, so override it to render as you would OnGUI
        public virtual void OnUI() {}

        // This is Unity's magic method
        private void OnGUI()
        {
            if (Event.current.type == EventType.layout)
            {
                RepositoryHasChanged = cachedRepository != Environment.Repository;
                cachedRepository = Environment.Repository;
                inLayout = true;
                OnDataUpdate();
            }

            OnUI();

            if (Event.current.type == EventType.repaint)
            {
                inLayout = false;
                if (initializeWasCalled)
                {
                    initializeWasCalled = false;
                    Initialize(cachedManager);
                }
            }
        }

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
        public IRepository Repository { get { return inLayout ? cachedRepository : Environment.Repository; } }
        public bool RepositoryHasChanged { get; private set; }
        public ITaskManager TaskManager { get { return Manager.TaskManager; } }
        protected IGitClient GitClient { get { return Manager.GitClient; } }
        protected IEnvironment Environment { get { return Manager.Environment; } }
        protected IPlatform Platform { get { return Manager.Platform; } }

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

        public virtual void InitializeView(IView parent)
        {
            Debug.Assert(parent != null, NullParentError);
            Parent = parent;
        }

        public virtual void OnEnable()
        {}

        public virtual void OnDisable()
        {}

        public virtual void OnDataUpdate()
        {}

        public virtual void OnGUI()
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

        protected IView Parent { get; private set; }
        public IApplicationManager Manager { get { return Parent.Manager; } }
        public IRepository Repository { get { return Manager.Environment.Repository; } }
        public bool RepositoryHasChanged { get { return Parent.RepositoryHasChanged; } }
        public ITaskManager TaskManager { get { return Manager.TaskManager; } }
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
