using Unity.VersionControl.Git;
using System;
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
        [NonSerialized] private bool firstDrawCalled = false;

        public BaseWindow()
        {
            RefreshEvents = new Dictionary<CacheType, int>();
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

        public virtual void Refresh()
        {
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
            firstDrawCalled = false;
            if (!initialized)
                InitializeWindow(EntryPoint.ApplicationManager, false);
        }

        public virtual void OnDisable()
        {}

        public virtual void OnBeforeFirstDraw()
        {}

        private void MaybeRaiseFirstDraw()
        {
            if (!firstDrawCalled)
            {
                firstDrawCalled = true;
                OnBeforeFirstDraw();
            }
        }

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
                MaybeRaiseFirstDraw();

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

        public void Refresh(CacheType type)
        {
            if (Repository == null)
                return;

            IsRefreshing = true;
            if (!RefreshEvents.ContainsKey(type))
                RefreshEvents.Add(type, 0);
            RefreshEvents[type]++;
            Repository.Refresh(type);
        }

        public void ReceivedEvent(CacheType type)
        {
            if (!RefreshEvents.ContainsKey(type))
                RefreshEvents.Add(type, 0);
            var val = RefreshEvents[type] - 1;
            RefreshEvents[type] = val > -1 ? val : 0;
            if (IsRefreshing && !RefreshEvents.Values.Any(x => x > 0))
            {
                DoneRefreshing();
            }
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

        protected ITaskManager TaskManager { get { return Manager.TaskManager; } }
        protected IGitClient GitClient { get { return Manager.GitClient; } }
        protected IEnvironment Environment { get { return Manager.Environment; } }
        protected IPlatform Platform { get { return Manager.Platform; } }
        public Dictionary<CacheType, int> RefreshEvents { get; set; }
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
