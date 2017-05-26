using System;
using System.Threading;
using System.Threading.Tasks;

namespace GitHub.Unity
{
    interface ITask : IAsyncResult
    {
        T ContinueWith<T>(T continuation, bool always = true) where T : ITask;
        // Continues and also sends a flag indicating whether the current task was successful or not
        ITask ContinueWith(Action<bool> continuation, bool always = true);
        // Continues and also sends a flag indicating whether the current task was successful or not
        ITask ContinueWithUI(Action<bool> continuation, bool always = true);
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
        ITask SetDependsOn(ITask dependsOn);
    }

    interface ITask<TResult> : ITask
    {
        ActionTask<TResult> ContinueWith(Action<bool, TResult> continuation, bool always = true);
        FuncTask<TResult, T> ContinueWith<T>(Func<bool, TResult, T> continuation, bool always = true);
        ActionTask<TResult> ContinueWithUI(Action<bool, TResult> continuation, bool always = true);
        FuncTask<TResult, T> ContinueWithUI<T>(Func<bool, TResult, T> continuation, bool always = true);
        new ITask<TResult> Start(TaskScheduler scheduler);
        TResult Result { get; }
        new Task<TResult> Task { get; }
        new event Action<ITask<TResult>> OnStart;
        new event Action<ITask<TResult>> OnEnd;
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
            : this(token, null)
        {
        }

        public TaskBase(CancellationToken token, ITask dependsOn)
        {
            Guard.ArgumentNotNull(token, "token");

            Token = token;
            DependsOn = dependsOn;
            Task = new Task(() => Run(DependsOn?.Successful ?? true), Token, TaskCreationOptions.None);
        }

        public TaskBase(Task task)
        {
            Task = task;
        }

        public ITask SetDependsOn(ITask dependsOn)
        {
            DependsOn = dependsOn;
            return this;
        }

        public T ContinueWith<T>(T continuation, bool always = true)
            where T : ITask
        {
            Guard.ArgumentNotNull(continuation, "continuation");

            continuation.SetDependsOn(this);
            Task.ContinueWith(_ => continuation.Start(), Token,
                always ? runAlwaysOptions : runOnSuccessOptions,
                TaskManager.GetScheduler(continuation.Affinity));
            return continuation;
        }

        public ITask ContinueWith(Action<bool> continuation, bool always = true)
        {
            Guard.ArgumentNotNull(continuation, "continuation");

            var ret = new ActionTask(Token, continuation, this);
            return ContinueWith(ret, always);
        }

        public ITask ContinueWithUI(Action<bool> continuation, bool always = true)
        {
            Guard.ArgumentNotNull(continuation, "continuation");
            var ret = new ActionTask(Token, continuation, this) { Affinity = TaskAffinity.UI };
            return ContinueWith(ret, always);
        }

        public virtual ITask Start()
        {
            TaskManager.Instance.Schedule(this);
            //Task.Start(TaskManager.GetScheduler(Affinity));
            return this;
        }

        public virtual ITask Start(TaskScheduler scheduler)
        {
            if (DependsOn != null && DependsOn.Task.Status == TaskStatus.Created)
                DependsOn.Start();
            else
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

        protected virtual void Run(bool success)
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

    abstract class TaskBase<TResult> : TaskBase, ITask<TResult>
    {
        public TaskBase(CancellationToken token, ITask dependsOn = null)
            : base(token, dependsOn)
        {
            Task = new Task<TResult>(() => RunWithReturn(DependsOn?.Successful ?? true), Token, TaskCreationOptions.None);
        }

        public ActionTask<TResult> ContinueWith(Action<bool, TResult> continuation, bool always = true)
        {
            Guard.ArgumentNotNull(continuation, "continuation");
            var ret = new ActionTask<TResult>(Token, continuation, this);
            return ContinueWith(ret, always);
        }

        public FuncTask<TResult, T> ContinueWith<T>(Func<bool, TResult, T> continuation, bool always = true)
        {
            Guard.ArgumentNotNull(continuation, "continuation");
            var ret = new FuncTask<TResult, T>(Token, continuation, this);
            return ContinueWith(ret, always);
        }

        public ActionTask<TResult> ContinueWithUI(Action<bool, TResult> continuation, bool always = true)
        {
            Guard.ArgumentNotNull(continuation, "continuation");
            var ret = new ActionTask<TResult>(Token, continuation, this) { Affinity = TaskAffinity.UI };
            return ContinueWith(ret, always);
        }

        public FuncTask<TResult, T> ContinueWithUI<T>(Func<bool, TResult, T> continuation, bool always = true)
        {
            Guard.ArgumentNotNull(continuation, "continuation");
            var ret = new FuncTask<TResult, T>(Token, continuation, this) { Affinity = TaskAffinity.UI };
            return ContinueWith(ret, always);
        }

        public new ITask<TResult> Start()
        {
            Task.Start(TaskManager.GetScheduler(Affinity));
            return this;
        }

        public new ITask<TResult> Start(TaskScheduler scheduler)
        {
            Task.Start(scheduler);
            return this;
        }

        protected virtual TResult RunWithReturn(bool success)
        {
            return default(TResult);
        }

        public new event Action<ITask<TResult>> OnStart;
        public new event Action<ITask<TResult>> OnEnd;
        public new Task<TResult> Task
        {
            get { return base.Task as Task<TResult>; }
            set { base.Task = value; }
        }
        public TResult Result { get { return Task.Result; } }
    }

    abstract class TaskBase<T, TResult> : TaskBase<TResult>
    {
        public TaskBase(CancellationToken token, ITask<T> dependsOn)
            : base(token, dependsOn)
        {
            Task = new Task<TResult>(() => RunWithData(dependsOn.Successful, dependsOn.Result), Token, TaskCreationOptions.None);
        }

        protected virtual TResult RunWithData(bool success, T previousResult)
        {
            return default(TResult);
        }
    }

    abstract class ListTaskBase<TResult, TData> : TaskBase<TResult>, ITask<TResult, TData>
    {
        public ListTaskBase(CancellationToken token)
            : base(token) { }

        public ListTaskBase(CancellationToken token, ITask dependsOn)
            : base(token, dependsOn) { }

        public event Action<TData> OnData;
        protected void RaiseOnData(TData data)
        {
            OnData?.Invoke(data);
        }
    }

    abstract class ListTaskBase<T, TResult, TData> : TaskBase<T, TResult>, ITask<TResult, TData>
    {
        public ListTaskBase(CancellationToken token, ITask<T> dependsOn)
            : base(token, dependsOn) { }

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