using System;
using System.Threading;
using System.Threading.Tasks;

namespace GitHub.Unity
{
    interface ITask : IAsyncResult
    {
        ITask ContinueWith(ITask dependsOn);
        ITask ContinueWith(Action<bool> continuation, bool always = false);
        ITask ContinueWithUI(Action<bool> continuation, bool always = false);
        ITask ContinueWith<TResult>(Func<bool, TResult> continuation, bool always = false);
        ITask ContinueWithUI<TResult>(Func<bool, TResult> continuation, bool always = false);
        ITask Start();
        ITask Start(TaskScheduler scheduler);
        void Wait();
        bool Wait(int milliseconds);
        bool Successful { get; }
        string Errors { get; }
        Task Task { get; }
        string Name { get; }
        TaskAffinity Affinity { get; }
        event Action<ITask> OnStart;
        event Action<ITask> OnEnd;
    }

    interface ITask<T> : ITask
    {
        ITask<T> ContinueWith(Action<bool, T> continuation, bool always = false);
        ITask<T> ContinueWithUI(Action<bool, T> continuation, bool always = false);
        ITask<T> ContinueWith<TResult>(Func<bool, T, TResult> continuation, bool always = false);
        ITask<T> ContinueWithUI<TResult>(Func<bool, T, TResult> continuation, bool always = false);
        new ITask<T> Start(TaskScheduler scheduler);
        T Result { get; }
        new Task<T> Task { get; }
        new event Action<ITask<T>> OnStart;
        new event Action<ITask<T>> OnEnd;
    }

    interface ITask<T, TData> : ITask<T>
    {
        event Action<TData> OnData;
    }

    abstract class TaskBase : ITask
    {
        protected const TaskContinuationOptions runAlwaysOptions = TaskContinuationOptions.None;
        protected const TaskContinuationOptions runOnSuccessOptions = TaskContinuationOptions.NotOnCanceled | TaskContinuationOptions.NotOnFaulted;
        protected const TaskContinuationOptions runOnFaultOptions = TaskContinuationOptions.OnlyOnFaulted;

        public event Action<ITask> OnStart;
        public event Action<ITask> OnEnd;

        public TaskBase(CancellationToken token)
        {
            Guard.ArgumentNotNull(token, "token");

            Task = new Task(Run, Token, TaskCreationOptions.None);
            Token = token;
            ConfigureTask();
        }

        public TaskBase(CancellationToken token, ITask dependsOn)
            : this(token)
        {
            Guard.ArgumentNotNull(dependsOn, "dependsOn");

            DependsOn = dependsOn;
            ConfigureTask();
        }

        public ITask ContinueWith(ITask previousTask)
        {
            Guard.ArgumentNotNull(previousTask, "previousTask");

            DependsOn = previousTask;
            ConfigureTask();
            return this;
        }

        public ITask ContinueWith(Action<bool> continuation, bool always = false)
        {
            Task.ContinueWith(_ => continuation(Successful), Token,
                always ? runAlwaysOptions : runOnSuccessOptions,
                TaskManager.Instance.ConcurrentScheduler);
            return this;
        }

        public ITask ContinueWithUI(Action<bool> continuation, bool always = false)
        {
            Task.ContinueWith(_ => continuation(Successful), Token,
                always ? runAlwaysOptions : runOnSuccessOptions,
                TaskManager.Instance.UIScheduler);
            return this;
        }

        public ITask ContinueWith<T>(Func<bool, T> continuation, bool always = false)
        {
            Task.ContinueWith(_ => continuation(Successful), Token,
                always ? runAlwaysOptions : runOnSuccessOptions,
                TaskManager.Instance.ConcurrentScheduler);
            return this;
        }

        public ITask ContinueWithUI<T>(Func<bool, T> continuation, bool always = false)
        {
            Task.ContinueWith(_ => continuation(Successful), Token,
                always ? runAlwaysOptions : runOnSuccessOptions,
                TaskManager.Instance.UIScheduler);
            return this;
        }

        public virtual ITask Start()
        {
            Task.Start(TaskManager.GetScheduler(Affinity));
            return this;
        }

        public virtual ITask Start(TaskScheduler scheduler)
        {
            Task.Start(scheduler);
            return this;
        }

        public virtual void Wait()
        {
            Task.Wait(Token);
        }

        public virtual bool Wait(int milliseconds)
        {
            return Task.Wait(milliseconds, Token);
        }

        protected virtual void ConfigureTask()
        {
            if (DependsOn != null)
                DependsOn.Task.ContinueWith(_ => Start(), Token, runOnSuccessOptions, TaskManager.GetScheduler(TaskAffinity.Concurrent));
        }

        protected virtual void Run()
        { }

        protected virtual void RaiseOnStart()
        {
            OnStart?.Invoke(this);
        }

        protected virtual void RaiseOnEnd()
        {
            OnEnd?.Invoke(this);
        }

        public virtual bool Successful { get { return Task.Status == TaskStatus.RanToCompletion && Task.Status != TaskStatus.Faulted; } }
        public string Errors { get; protected set; }
        public Task Task { get; protected set; }

        public bool IsCompleted { get { return (Task as IAsyncResult).IsCompleted; } }

        public WaitHandle AsyncWaitHandle { get { return (Task as IAsyncResult).AsyncWaitHandle; } }

        public object AsyncState { get { return (Task as IAsyncResult).AsyncState; } }

        public bool CompletedSynchronously { get { return (Task as IAsyncResult).CompletedSynchronously; } }
        public virtual string Name { get; set; }
        public virtual TaskAffinity Affinity { get; set; }

        private ILogging logger;
        protected ILogging Logger { get { return logger = logger ?? Logging.GetLogger(GetType()); } }
        protected ITask DependsOn { get; private set; }
        protected CancellationToken Token { get; }
    }

    abstract class TaskBase<T> : TaskBase, ITask<T>
    {
        public TaskBase(CancellationToken token)
            : base(token)
        {
            Task = new Task<T>(RunWithReturn, Token, TaskCreationOptions.None);
        }

        public TaskBase(CancellationToken token, ITask dependsOn)
            : base(token, dependsOn)
        {
            Task = new Task<T>(RunWithReturn, Token, TaskCreationOptions.None);
        }

        public ITask<T> ContinueWith(Action<bool, T> continuation, bool always = false)
        {
            Task.ContinueWith(_ => continuation(Successful, Successful ? Result : default(T)), Token,
                always ? runAlwaysOptions : runOnSuccessOptions,
                TaskManager.Instance.ConcurrentScheduler);
            return this;
        }

        public ITask<T> ContinueWithUI(Action<bool, T> continuation, bool always = false)
        {
            Task.ContinueWith(_ => continuation(Successful, Successful ? Result : default(T)), Token,
                always ? runAlwaysOptions : runOnSuccessOptions,
                TaskManager.Instance.UIScheduler);
            return this;
        }

        public ITask<T> ContinueWith<TResult>(Func<bool, T, TResult> continuation, bool always = false)
        {
            Task.ContinueWith(_ => continuation(Successful, Successful ? Result : default(T)), Token,
                always ? runAlwaysOptions : runOnSuccessOptions,
                TaskManager.Instance.ConcurrentScheduler);
            return this;
        }

        public ITask<T> ContinueWithUI<TResult>(Func<bool, T, TResult> continuation, bool always = false)
        {
            Task.ContinueWith(_ => continuation(Successful, Successful ? Result : default(T)), Token,
                always ? runAlwaysOptions : runOnSuccessOptions,
                TaskManager.Instance.UIScheduler);
            return this;
        }

        public new ITask<T> Start()
        {
            Task.Start(TaskManager.GetScheduler(Affinity));
            return this;
        }

        public new ITask<T> Start(TaskScheduler scheduler)
        {
            Task.Start(scheduler);
            return this;
        }

        protected virtual T RunWithReturn()
        {
            return default(T);
        }

        public new event Action<ITask<T>> OnStart;
        public new event Action<ITask<T>> OnEnd;
        public new Task<T> Task
        {
            get { return base.Task as Task<T>; }
            set { base.Task = value; }
        }
        public T Result { get { return Task.Result; } }
    }

    abstract class TaskBase<TDependentResult, T> : TaskBase<T>
    {
        public TaskBase(CancellationToken token, ITask<TDependentResult> dependsOn)
            : base(token, dependsOn)
        {
            Task = new Task<T>(o => RunWithData(((Lazy<TDependentResult>)o).Value), new Lazy<TDependentResult>(() => dependsOn.Result), Token, TaskCreationOptions.None);
        }

        protected virtual T RunWithData(TDependentResult previousResult)
        {
            return default(T);
        }
    }

    abstract class ListTaskBase<T, TData> : TaskBase<T>, ITask<T, TData>
    {
        public ListTaskBase(CancellationToken token) : base(token)
        { }

        public ListTaskBase(CancellationToken token, ITask dependsOn) : base(token, dependsOn)
        { }

        public event Action<TData> OnData;
        protected void RaiseOnData(TData data)
        {
            OnData?.Invoke(data);
        }
    }

    abstract class ListTaskBase<TDependentResult, T, TData> : TaskBase<TDependentResult, T>, ITask<T, TData>
    {
        public ListTaskBase(CancellationToken token, ITask<TDependentResult> dependsOn)
            : base(token, dependsOn)
        { }

        public event Action<TData> OnData;
        protected void RaiseOnData(TData data)
        {
            OnData?.Invoke(data);
        }
    }

    static class TaskBaseExtensions
    {
        public static T Schedule<T>(this T task, ITaskManager taskManager)
            where T : ITask
        {
            return taskManager.Schedule(task);
        }

        public static T ScheduleUI<T>(this T task, ITaskManager taskManager)
            where T : ITask
        {
            return taskManager.ScheduleUI(task);
        }

        public static T ScheduleExclusive<T>(this T task, ITaskManager taskManager)
            where T : ITask
        {
            return taskManager.ScheduleExclusive(task);
        }

        public static T ScheduleConcurrent<T>(this T task, ITaskManager taskManager)
            where T : ITask
        {
            return taskManager.ScheduleConcurrent(task);
        }
    }

    enum TaskAffinity
    {
        Concurrent,
        Exclusive,
        UI
    }
}