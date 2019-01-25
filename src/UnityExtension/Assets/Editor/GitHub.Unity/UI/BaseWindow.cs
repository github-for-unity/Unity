using GitHub.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace GitHub.Unity
{
    public abstract class BaseWindow :  EditorWindow, IView
    {
        [NonSerialized] private bool initialized = false;
        [NonSerialized] private IUser cachedUser;
        [NonSerialized] private IRepository cachedRepository;
        [NonSerialized] private bool initializeWasCalled;
        [NonSerialized] protected bool inLayout;
        [NonSerialized] private bool firstOnGUI = true;
        [NonSerialized] private bool doneRefreshing;
        [NonSerialized] private object lck = new object();

        protected BaseWindow()
        {
            RefreshEvents = new HashSet<CacheType>();
        }

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

        /// <summary>
        /// This will call Repaint()/Redraw() and set IsRefreshing = true
        /// </summary>
        public virtual void Refresh()
        {
            InternalRefresh();
            if (Repository == null) return;
            lock(lck)
            {
                foreach (var type in RefreshEvents)
                    Repository.Refresh(type);
            }
        }

        private void InternalRefresh()
        {
            IsRefreshing = true;
            doneRefreshing = true;
            Redraw();
        }

        public virtual void Finish(bool result)
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

        public virtual void OnDataUpdate(bool first)
        {}

        public virtual void OnRepositoryChanged(IRepository oldRepository)
        {}

        /// <summary>
        /// OnUI is called everytime OnGUI is called, so override it to render as you would OnGUI
        /// </summary>
        public virtual void OnUI() {}

        // This is Unity's magic method
        private void OnGUI()
        {
            if (Event.current.type == EventType.Layout)
            {
                if (IsRefreshing)
                {
                    IsBusy = true;
                }

                if (cachedRepository != Environment.Repository || initializeWasCalled)
                {
                    initializeWasCalled = false;
                    OnRepositoryChanged(cachedRepository);
                    cachedRepository = Environment.Repository;
                }
                inLayout = true;
                OnDataUpdate(firstOnGUI);
                firstOnGUI = false;
            }

            OnUI();

            if (Event.current.type == EventType.Repaint)
            {
                inLayout = false;
                if (doneRefreshing)
                {
                    doneRefreshing = false;
                    IsBusy = false;
                    DoneRefreshing();
                }
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
            doneRefreshing = false;
            IsRefreshing = false;
        }

        public void Refresh(CacheType type)
        {
            IsRefreshing = true;
            if (Repository == null)
            {
                InternalRefresh();
                return;
            }

            doneRefreshing = false;
            lock(lck)
            {
                if (!RefreshEvents.Contains(type))
                    RefreshEvents.Add(type);
            }
        }

        public void ReceivedEvent(CacheType type)
        {
            int count = 0;
            lock(lck)
            {
                if (RefreshEvents.Contains(type))
                {
                    RefreshEvents.Remove(type);
                }
                count = RefreshEvents.Count;
            }

            if (IsRefreshing && count == 0)
            {
                InternalRefresh();
            }
        }

        public virtual void DoEmptyUI()
        {}
        public virtual void DoProgressUI()
        {}
        public virtual void UpdateProgress(IProgress progress)
        {}

        public Rect Position { get { return position; } }
        public IApplicationManager Manager { get; private set; }
        public virtual bool IsBusy { get; set; }
        public bool IsRefreshing { get; private set; }
        public bool HasFocus { get; private set; }
        public IRepository Repository { get { return inLayout ? cachedRepository : Environment.Repository; } }
        public bool HasRepository { get { return Repository != null; } }
        public IUser User { get { return cachedUser; } }
        public bool HasUser { get { return User != null; } }

        protected ITaskManager TaskManager { get { return Manager.TaskManager; } }
        protected IGitClient GitClient { get { return Manager.GitClient; } }
        protected IEnvironment Environment { get { return Manager.Environment; } }
        protected IPlatform Platform { get { return Manager.Platform; } }
        public HashSet<CacheType> RefreshEvents { get; set; }
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
