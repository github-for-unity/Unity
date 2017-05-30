using System;
using System.Threading;
using System.Threading.Tasks;

namespace GitHub.Unity
{
    interface ITask : IAsyncResult
    {
        T Then<T>(T continuation, bool always = false) where T : ITask;
        ITask Finally(Action<bool, Exception> continuation, TaskAffinity affinity = TaskAffinity.Concurrent);
        ITask SetDependsOn(ITask dependsOn);
        ITask Start();
        ITask Start(TaskScheduler scheduler);

        void Wait();
        bool Wait(int milliseconds);
        bool Successful { get; }
        string Errors { get; }
        Task Task { get; }
        string Name { get; }
        TaskAffinity Affinity { get; set; }
        CancellationToken Token { get; }
        TaskBase DependsOn { get; }
        event Action<ITask> OnStart;
        event Action<ITask> OnEnd;
    }

    interface ITask<TResult> : ITask
    {
        ITask<TResult> Finally(Func<bool, Exception, TResult, TResult> continuation, TaskAffinity affinity = TaskAffinity.Concurrent);
        ITask Finally(Action<bool, Exception, TResult> continuation, TaskAffinity affinity = TaskAffinity.Concurrent);
        new ITask<TResult> Start();
        new ITask<TResult> Start(TaskScheduler scheduler);
        TResult Result { get; }
        new Task<TResult> Task { get; }
        new event Action<ITask<TResult>> OnStart;
        new event Action<ITask<TResult>> OnEnd;
        ITask<T> Defer<T>(Func<TResult, Task<T>> continueWith, TaskAffinity affinity = TaskAffinity.Concurrent, bool always = false);
    }

    interface ITask<TData, T> : ITask<T>
    {
        event Action<TData> OnData;
    }

    abstract class TaskBase : ITask
    {
        protected const TaskContinuationOptions runAlwaysOptions = TaskContinuationOptions.None;
        protected const TaskContinuationOptions runOnSuccessOptions = TaskContinuationOptions.OnlyOnRanToCompletion;
        protected const TaskContinuationOptions runOnFaultOptions = TaskContinuationOptions.OnlyOnFaulted;

        public event Action<ITask> OnStart;
        public event Action<ITask> OnEnd;

        protected bool previousSuccess = true;
        protected Exception previousException;
        protected object previousResult;

        protected TaskBase continuation;
        protected bool continuationAlways;

        public TaskBase(CancellationToken token, ITask dependsOn = null, bool always = false)
        {
            Guard.ArgumentNotNull(token, "token");

            Token = token;
            Task = new Task(() => Run(DependsOn?.Successful ?? previousSuccess), Token, TaskCreationOptions.None);
            dependsOn?.Then(this, always);
        }

        public TaskBase(Task task)
        {
            Task = task;
        }

        protected TaskBase()
        {}

        public ITask SetDependsOn(ITask dependsOn)
        {
            DependsOn = (TaskBase)dependsOn;
            return this;
        }

        public virtual T Then<T>(T cont, bool always = false)
            where T : ITask
        {
            Guard.ArgumentNotNull(cont, nameof(cont));
            cont.SetDependsOn(this);
            this.continuation = (TaskBase)(object)cont;
            this.continuationAlways = always;
            return cont;
        }

        public ITask Finally(Action<bool, Exception> continuation, TaskAffinity affinity = TaskAffinity.Concurrent)
        {
            Guard.ArgumentNotNull(continuation, "continuation");
            var ret = new ActionTask(Token, continuation, this, true) { Affinity = affinity, Name = "Finally" };
            DependsOn?.SetFaultHandler(ret);
            ret.ContinuationIsFinally = true;
            return ret;
        }

        internal virtual ITask Finally<T>(T continuation)
            where T : ITask
        {
            Guard.ArgumentNotNull(continuation, "continuation");
            continuation.SetDependsOn(this);
            this.continuation = (TaskBase)(object)continuation;
            this.continuationAlways = true;
            DependsOn?.SetFaultHandler((TaskBase)(object)continuation);
            return continuation;
        }

        internal void SetFaultHandler(TaskBase handler)
        {
            Task.ContinueWith(t => handler.Start(t), Token,
                TaskContinuationOptions.OnlyOnFaulted,
                TaskManager.GetScheduler(handler.Affinity));
            DependsOn?.SetFaultHandler(handler);
        }

        public virtual ITask Start()
        {
            var depends = GetFirstDepends();
            if (depends != null)
            {
                depends.Run();
                return this;
            }
            else
            {
                return TaskManager.Instance.Schedule(this);
            }
        }

        protected void Run()
        {
            if (Task.Status == TaskStatus.Created)
            {
                TaskManager.Instance.Schedule(this);
            }
            else
            {
                RunContinuation();
            }
        }

        protected void Start(Task task)
        {
            previousSuccess = task.Status == TaskStatus.RanToCompletion && task.Status != TaskStatus.Faulted;
            previousException = task.Exception;
            Task.Start(TaskManager.GetScheduler(Affinity));
        }

        public virtual ITask Start(TaskScheduler scheduler)
        {
            if (Task.Status == TaskStatus.Created)
            {
                Logger.Trace($"Starting {Affinity} {ToString()}");
                Task.Start(scheduler);
            }
            RunContinuation();
            return this;
        }

        protected virtual void RunContinuation()
        {
            if (continuation != null)
            {
                Logger.Trace($"Setting ContinueWith {Affinity} {continuation}");
                Task.ContinueWith(_ => ((TaskBase)(object)continuation).Run(), Token, continuationAlways ? runAlwaysOptions : runOnSuccessOptions,
                    TaskManager.GetScheduler(continuation.Affinity));
            }
        }

        protected TaskBase GetFirstDepends()
        {
            var depends = DependsOn;
            if (depends == null)
                return null;
            return depends.GetFirstDepends(null);
        }

        protected TaskBase GetFirstDepends(TaskBase ret)
        {
            ret = (Task.Status == TaskStatus.Created ? this : ret);
            var depends = DependsOn;
            if (depends == null)
                return ret;
            return depends.GetFirstDepends(ret);
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
        {
            Logger.Trace($"Executing {ToString()} success?:{success}");
        }

        protected virtual void RaiseOnStart()
        {
            OnStart?.Invoke(this);
        }

        protected virtual void RaiseOnEnd()
        {
            OnEnd?.Invoke(this);
        }

        protected AggregateException GetThrownException()
        {
            if (DependsOn == null)
                return null;

            if (DependsOn.Task.Status != TaskStatus.Created && !DependsOn.Successful)
            {
                return DependsOn.Task.Exception;
            }
            return DependsOn.GetThrownException();
        }

        protected class DeferredContinuation
        {
            public bool Always;
            public Func<object, ITask> GetContinueWith;
        }

        private DeferredContinuation deferred;
        internal object GetDeferred()
        {
            return deferred;
        }

        internal void SetDeferred(object def)
        {
            deferred = (DeferredContinuation)def;
        }

        internal void ClearDeferred()
        {
            deferred = null;
        }

        public override string ToString()
        {
            return $"{Task?.Id ?? -1} {Name} {GetType()}";
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
        public TaskBase DependsOn { get; private set; }
        public CancellationToken Token { get; }
        internal TaskBase Continuation => continuation;
        internal bool ContinuationAlways => continuationAlways;
        internal bool ContinuationIsFinally { get; set; }
    }

    abstract class TaskBase<TResult> : TaskBase, ITask<TResult>
    {
        protected TaskCompletionSource<TResult> tcs = new TaskCompletionSource<TResult>();

        public TaskBase(CancellationToken token, ITask dependsOn = null, bool always = false)
            : base(token, dependsOn, always)
        {
            Task = new Task<TResult>(() =>
            {
                var ret = RunWithReturn(DependsOn?.Successful ?? previousSuccess);
                tcs.SetResult(ret);
                AdjustNextTask(ret);
                return ret;
            }, Token, TaskCreationOptions.None);
        }

        protected void AdjustNextTask(TResult ret)
        {
            var def = GetDeferred();
            if (def != null)
            {
                var next = (def as DeferredContinuation)?.GetContinueWith(ret);
                var cont = continuation.Continuation;
                var nextDefer = continuation.GetDeferred();
                if (continuation is IStubTask)
                {
                    ((TaskBase)next).SetDeferred(nextDefer);
                    ((TaskBase)continuation).ClearDeferred();
                }

                if (cont != null)
                {
                    if (cont.ContinuationIsFinally)
                    {
                        ((TaskBase)next).Finally(cont);
                    }
                    else
                    {
                        next.Then(cont, cont.ContinuationAlways);
                    }
                }
                continuation.Then(next, continuationAlways);
                ClearDeferred();
            }
        }

        public TaskBase(Task<TResult> task)
        {
            Task = task;
        }

        public override T Then<T>(T continuation, bool always = false)
        {
            return base.Then<T>(continuation, always);
        }

        public ITask<T> ThenIf<T>(Func<TResult, ITask<T>> continueWith, bool always = false)
        {
            Guard.ArgumentNotNull(continueWith, "continueWith");
            var ret = new StubTask<T>(Token, (s, d) => default(T), this, always);
            SetDeferred(new DeferredContinuation { Always = always, GetContinueWith = d => continueWith((TResult)d) });
            return ret;
        }

        public ITask<T> Defer<T>(Func<TResult, Task<T>> continueWith, TaskAffinity affinity = TaskAffinity.Concurrent, bool always = false)
        {
            Guard.ArgumentNotNull(continueWith, "continueWith");
            var ret = new StubTask<T>(Token, (s, d) => default(T), this, always) { Affinity = affinity };
            SetDeferred(new DeferredContinuation { Always = always, GetContinueWith = d => new FuncTask<T>(continueWith((TResult)d)) { Affinity = affinity, Name = "Deferred" } });
            return ret;
        }

        interface IStubTask {}
        class StubTask<T> : FuncTask<TResult, T>, IStubTask
        {
            public StubTask(CancellationToken token, Func<bool, TResult, T> func, ITask<TResult> dependsOn, bool always)
                : base(token, func, dependsOn, always)
            {
                Name = "Stub";
            }
        }

        public ITask<TResult> Finally(Func<bool, Exception, TResult, TResult> continuation, TaskAffinity affinity = TaskAffinity.Concurrent)
        {
            Guard.ArgumentNotNull(continuation, "continuation");
            var ret = new FuncTask<TResult, TResult>(Token, continuation, this, true) { Affinity = affinity, Name = "Finally" };
            ret.ContinuationIsFinally = true;
            DependsOn?.SetFaultHandler(ret);
            return ret;
        }

        public ITask Finally(Action<bool, Exception, TResult> continuation, TaskAffinity affinity = TaskAffinity.Concurrent)
        {
            Guard.ArgumentNotNull(continuation, "continuation");
            var ret = new ActionTask<TResult>(Token, continuation, this, true) { Affinity = affinity, Name = "Finally" };
            ret.ContinuationIsFinally = true;
            DependsOn?.SetFaultHandler(ret);
            return ret;
        }

        public new ITask<TResult> Start()
        {
            base.Start();
            return this;
        }

        public new ITask<TResult> Start(TaskScheduler scheduler)
        {
            base.Start(scheduler);
            return this;
        }

        protected virtual TResult RunWithReturn(bool success)
        {
            base.Run(success);
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
        public TaskBase(CancellationToken token, ITask<T> dependsOn = null, bool always = false)
            : base(token, dependsOn, always)
        {
            Task = new Task<TResult>(() =>
            {
                var ret = RunWithData(DependsOn?.Successful ?? previousSuccess, DependsOn.Successful ? ((ITask<T>)DependsOn).Result : default(T));
                tcs.SetResult(ret);
                AdjustNextTask(ret);
                return ret;
            },
                Token, TaskCreationOptions.None);
        }

        public TaskBase(Task<TResult> task)
            : base(task)
        { }

        protected virtual TResult RunWithData(bool success, T previousResult)
        {
            base.Run(success);
            return default(TResult);
        }
    }

    abstract class DataTaskBase<TData, TResult> : TaskBase<TResult>, ITask<TData, TResult>
    {
        public DataTaskBase(CancellationToken token, ITask dependsOn = null, bool always = false)
            : base(token, dependsOn, always) { }

        public DataTaskBase(Task<TResult> task)
            : base(task)
        { }

        public event Action<TData> OnData;
        protected void RaiseOnData(TData data)
        {
            OnData?.Invoke(data);
        }
    }

    abstract class DataTaskBase<T, TData, TResult> : TaskBase<T, TResult>, ITask<TData, TResult>
    {
        public DataTaskBase(CancellationToken token, ITask<T> dependsOn = null, bool always = false)
            : base(token, dependsOn, always) { }

        public DataTaskBase(Task<TResult> task)
            : base(task)
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