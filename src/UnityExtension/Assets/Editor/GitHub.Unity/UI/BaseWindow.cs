using GitHub.Logging;
using System;
using UnityEditor;
using UnityEngine;

namespace GitHub.Unity
{
    abstract class BaseWindow :  EditorWindow, IView
    {
        [NonSerialized] private bool initialized = false;
        [NonSerialized] private IUser cachedUser;
        [NonSerialized] private IRepository cachedRepository;
        [NonSerialized] private bool initializeWasCalled;
        [NonSerialized] protected bool inLayout;

        public virtual void Initialize(IApplicationManager applicationManager)
        {
        }

        public void InitializeWindow(IApplicationManager applicationManager, bool requiresRedraw = true)
        {
            initialized = true;
            initializeWasCalled = true;
            Manager = applicationManager;
            cachedUser = Environment.User;
            cachedRepository = Environment.Repository;
            Initialize(applicationManager);
            if (requiresRedraw)
                Redraw();
        }

        public virtual void Redraw()
        {
            Repaint();
        }

        public virtual void Refresh()
        {
        }

        public virtual void Finish(bool result, object output)
        {}

        public virtual void Awake()
        {
            if (!initialized)
                InitializeWindow(EntryPoint.ApplicationManager, false);
        }

        public virtual void OnEnable()
        {
            if (!initialized)
                InitializeWindow(EntryPoint.ApplicationManager, false);
        }

        public virtual void OnDisable()
        {}

        public virtual void Update()
        {}

        public virtual void OnDataUpdate()
        {}

        public virtual void OnRepositoryChanged(IRepository oldRepository)
        {}

        // OnGUI calls this everytime, so override it to render as you would OnGUI
        public virtual void OnUI() {}

        // This is Unity's magic method
        private void OnGUI()
        {
            if (Event.current.type == EventType.Layout)
            {
                if (cachedRepository != Environment.Repository || initializeWasCalled)
                {
                    initializeWasCalled = false;
                    OnRepositoryChanged(cachedRepository);
                    cachedRepository = Environment.Repository;
                }
                inLayout = true;
                OnDataUpdate();
            }

            OnUI();

            if (Event.current.type == EventType.Repaint)
            {
                inLayout = false;
            }
        }

        private void OnFocus()
        {
            HasFocus = true;
            OnFocusChanged();
        }

        private void OnLostFocus()
        {
            HasFocus = false;
            OnFocusChanged();
        }

        public virtual void OnFocusChanged()
        {}

        public virtual void OnDestroy()
        {}

        public virtual void OnSelectionChange()
        {}

        public virtual void DoneRefreshing()
        {
            IsRefreshing = false;
        }

        public virtual void DoEmptyGUI()
        {}
        public virtual void DoProgressGUI()
        {}
        public virtual void UpdateProgress(IProgress progress)
        {}

        public Rect Position { get { return position; } }
        public IApplicationManager Manager { get; private set; }
        public abstract bool IsBusy { get; }
        public bool IsRefreshing { get; private set; }
        public bool HasFocus { get; private set; }
        public IRepository Repository { get { return inLayout ? cachedRepository : Environment.Repository; } }
        public bool HasRepository { get { return Repository != null; } }
        public IUser User { get { return cachedUser; } }
        public bool HasUser { get { return User != null; } }
        public object Data { get; protected set; }

        protected ITaskManager TaskManager { get { return Manager.TaskManager; } }
        protected IGitClient GitClient { get { return Manager.GitClient; } }
        protected IEnvironment Environment { get { return Manager.Environment; } }
        protected IPlatform Platform { get { return Manager.Platform; } }
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