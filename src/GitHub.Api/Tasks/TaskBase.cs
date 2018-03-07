using GitHub.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace GitHub.Unity
{
    public enum TaskRunOptions
    {
        OnSuccess,
        OnFailure,
        OnAlways
    }

    public interface ITask : IAsyncResult
    {
        T Then<T>(T continuation, TaskRunOptions runOptions = TaskRunOptions.OnSuccess, bool taskIsTopOfChain = false) where T : ITask;
        ITask Catch(Action<Exception> handler);
        ITask Catch(Func<Exception, bool> handler);
        /// <summary>
        /// Run a callback at the end of the task execution, on the same thread as the task that just finished, regardless of execution state
        /// </summary>
        ITask Finally(Action<bool> handler);
        /// <summary>
        /// Run a callback at the end of the task execution, on a separate thread, regardless of execution state
        /// </summary>
        ITask Finally(Action<bool, Exception> actionToContinueWith, TaskAffinity affinity);
        /// <summary>
        /// Run another task at the end of the task execution, on a separate thread, regardless of execution state
        /// </summary>
        T Finally<T>(T taskToContinueWith) where T : ITask;
        ITask Start();
        ITask Start(TaskScheduler scheduler);
        ITask Progress(Action<IProgress> progressHandler);

        bool Successful { get; }
        string Errors { get; }
        Task Task { get; }
        string Name { get; }
        TaskAffinity Affinity { get; set; }
        CancellationToken Token { get; }
        TaskBase DependsOn { get; }
        event Action<ITask> OnStart;
        event Action<ITask, bool, Exception> OnEnd;
        ITask GetTopOfChain();

        /// <summary>
        /// </summary>
        /// <returns>true if any task on the chain is marked as exclusive</returns>
        bool IsChainExclusive();

        void UpdateProgress(long value, long total, string message = null);
    }

    public interface ITask<TResult> : ITask
    {
        new ITask<TResult> Catch(Action<Exception> handler);
        new ITask<TResult> Catch(Func<Exception, bool> handler);
        /// <summary>
        /// Run a callback at the end of the task execution, on the same thread as the task that just finished, regardless of execution state
        /// </summary>
        ITask<TResult> Finally(Action<bool, TResult> handler);
        /// <summary>
        /// Run a callback at the end of the task execution, on a separate thread, regardless of execution state
        /// </summary>
        ITask<TResult> Finally(Func<bool, Exception, TResult, TResult> continuation, TaskAffinity affinity = TaskAffinity.Concurrent);
        /// <summary>
        /// Run a callback at the end of the task execution, on a separate thread, regardless of execution state
        /// </summary>
        ITask Finally(Action<bool, Exception, TResult> continuation, TaskAffinity affinity = TaskAffinity.Concurrent);
        new ITask<TResult> Start();
        new ITask<TResult> Start(TaskScheduler scheduler);
        new ITask<TResult> Progress(Action<IProgress> progressHandler);
        TResult Result { get; }
        new Task<TResult> Task { get; }
        new event Action<ITask<TResult>> OnStart;
        new event Action<ITask<TResult>, TResult, bool, Exception> OnEnd;
    }

    interface ITask<TData, T> : ITask<T>
    {
        event Action<TData> OnData;
    }

    public abstract class TaskBase : ITask
    {
        protected const TaskContinuationOptions runAlwaysOptions = TaskContinuationOptions.None;
        protected const TaskContinuationOptions runOnSuccessOptions = TaskContinuationOptions.OnlyOnRanToCompletion;
        protected const TaskContinuationOptions runOnFaultOptions = TaskContinuationOptions.OnlyOnFaulted;

        public event Action<ITask> OnStart;
        public event Action<ITask, bool, Exception> OnEnd;

        protected bool previousSuccess = true;
        protected Exception previousException;
        protected bool taskFailed = false;
        protected bool exceptionWasHandled = false;
        protected Exception exception;

        protected TaskBase continuationOnSuccess;
        protected TaskBase continuationOnFailure;
        protected TaskBase continuationOnAlways;

        protected event Func<Exception, bool> catchHandler;
        private event Action<bool> finallyHandler;

        protected Progress progress;

        protected TaskBase(CancellationToken token)
            : this()
        {
            Guard.ArgumentNotNull(token, "token");

            Token = token;
            Task = new Task(() => Run(DependsOn?.Successful ?? previousSuccess), Token, TaskCreationOptions.None);
        }

        protected TaskBase(Task task)
            : this()
        {
            Task = new Task(t =>
            {
                var scheduler = TaskManager.GetScheduler(Affinity);
                RaiseOnStart();
                var tk = ((Task)t);
                try
                {
                    if (tk.Status == TaskStatus.Created && !tk.IsCompleted &&
                      ((tk.CreationOptions & (TaskCreationOptions)512) == TaskCreationOptions.None))
                    {
                        tk.RunSynchronously(scheduler);
                    }
                }
                catch (Exception ex)
                {
                    Errors = ex.Message;
                    if (!RaiseFaultHandlers(ex))
                        throw;
                }
                finally
                {
                    RaiseOnEnd();
                }
            }, task, Token, TaskCreationOptions.None);
        }

        protected TaskBase()
        {
            this.progress = new Progress { Task = this };
        }

        public virtual T Then<T>(T nextTask, TaskRunOptions runOptions = TaskRunOptions.OnSuccess, bool taskIsTopOfChain = false)
            where T : ITask
        {
            Guard.ArgumentNotNull(nextTask, nameof(nextTask));
            var nextTaskBase = ((TaskBase)(object)nextTask);

            // find the task at the top of the chain
            if (!taskIsTopOfChain)
                nextTaskBase = nextTaskBase.GetTopMostTask() ?? nextTaskBase;
            // make the next task dependent on this one so it can get values from us
            nextTaskBase.SetDependsOn(this);

            if (runOptions == TaskRunOptions.OnSuccess)
            {
                this.continuationOnSuccess = nextTaskBase;

                // if there are fault handlers in the chain we're appending, propagate them
                // up this chain as well
                if (nextTaskBase.continuationOnFailure != null)
                    SetFaultHandler(nextTaskBase.continuationOnFailure);
                else if (nextTaskBase.continuationOnAlways != null)
                    SetFaultHandler(nextTaskBase.continuationOnAlways);
                if (nextTaskBase.catchHandler != null)
                    Catch(nextTaskBase.catchHandler);
                if (nextTaskBase.finallyHandler != null)
                    Finally(nextTaskBase.finallyHandler);
            }
            else if (runOptions == TaskRunOptions.OnFailure)
            {
                this.continuationOnFailure = nextTaskBase;
                DependsOn?.Then(nextTaskBase, TaskRunOptions.OnFailure, true);
            }
            else
            {
                this.continuationOnAlways = nextTaskBase;
                DependsOn?.SetFaultHandler(nextTaskBase);
            }
            return nextTask;
        }

        /// <summary>
        /// Catch runs right when the exception happens (on the same thread)
        /// Chain will be cancelled
        /// </summary>
        public ITask Catch(Action<Exception> handler)
        {
            Guard.ArgumentNotNull(handler, "handler");
            catchHandler += e => { handler(e); return false; };
            DependsOn?.Catch(handler);
            return this;
        }

        /// <summary>
        /// Catch runs right when the exception happens (on the same threaD)
        /// Return true if you want the task to completely successfully
        /// </summary>
        public ITask Catch(Func<Exception, bool> handler)
        {
            Guard.ArgumentNotNull(handler, "handler");
            catchHandler += handler;
            DependsOn?.Catch(handler);
            return this;
        }

        /// <summary>
        /// Run a callback at the end of the task execution, on the same thread as the task that just finished, regardless of execution state
        /// This will always run on the same thread as the previous task
        /// </summary>
        public ITask Finally(Action<bool> handler)
        {
            Guard.ArgumentNotNull(handler, "handler");
            finallyHandler += handler;
            DependsOn?.Finally(handler);
            return this;
        }

        /// <summary>
        /// Run a callback at the end of the task execution, on a separate thread, regardless of execution state
        /// </summary>
        public ITask Finally(Action<bool, Exception> actionToContinueWith, TaskAffinity affinity = TaskAffinity.Concurrent)
        {
            Guard.ArgumentNotNull(actionToContinueWith, nameof(actionToContinueWith));
            return Finally(new ActionTask(Token, actionToContinueWith) { Affinity = affinity, Name = "Finally" });
        }

        /// <summary>
        /// Run another task at the end of the task execution, on a separate thread, regardless of execution state
        /// </summary>
        public T Finally<T>(T taskToContinueWith)
            where T : ITask
        {
            Guard.ArgumentNotNull(taskToContinueWith, nameof(taskToContinueWith));
            continuationOnAlways = (TaskBase)(object)taskToContinueWith;
            continuationOnAlways.SetDependsOn(this);
            DependsOn?.SetFaultHandler(continuationOnAlways);
            return taskToContinueWith;
        }

        /// <summary>
        /// This does not set a dependency between the two tasks. Instead,
        /// the Start method grabs the state of the previous task to pass on
        /// to the next task via previousSuccess and previousException
        /// </summary>
        /// <param name="handler"></param>
        internal void SetFaultHandler(TaskBase handler)
        {
            Task.ContinueWith(t => handler.Start(t), Token,
                TaskContinuationOptions.OnlyOnFaulted,
                TaskManager.GetScheduler(handler.Affinity));
            DependsOn?.SetFaultHandler(handler);
        }

        /// <summary>
        /// Progress provides progress reporting from the task (on the same thread)
        /// </summary>
        public ITask Progress(Action<IProgress> handler)
        {
            Guard.ArgumentNotNull(handler, nameof(handler));
            progress.OnProgress += handler;
            return this;
        }

        public virtual ITask Start()
        {
            var depends = GetTopMostTaskInCreatedState() ?? this;
            depends.Run();
            return this;
        }

        protected void Run()
        {
            if (Task.Status == TaskStatus.Created)
            {
                TaskManager.Instance.Schedule(this);
            }
        }

        /// <summary>
        /// Call this to run a task after another task is done, without
        /// having them depend on each other
        /// </summary>
        /// <param name="task"></param>
        protected void Start(Task task)
        {
            previousSuccess = task.Status == TaskStatus.RanToCompletion && task.Status != TaskStatus.Faulted;
            previousException = task.Exception;
            Task.Start(TaskManager.GetScheduler(Affinity));
            SetContinuation();
        }

        public virtual ITask Start(TaskScheduler scheduler)
        {
            if (Task.Status == TaskStatus.Created)
            {
                //Logger.Trace($"Starting {Affinity} {ToString()}");
                Task.Start(scheduler);
                SetContinuation();
            }
            return this;
        }

        public ITask GetTopOfChain()
        {
            return GetTopMostTaskInCreatedState() ?? this;
        }

        /// <summary>
        /// </summary>
        /// <returns>true if any task on the chain is marked as exclusive</returns>
        public bool IsChainExclusive()
        {
            if (Affinity == TaskAffinity.Exclusive)
                return true;
            return DependsOn?.IsChainExclusive() ?? false;
        }

        protected void SetContinuation()
        {
            if (continuationOnAlways != null)
            {
                //Logger.Trace($"Setting ContinueWith {Affinity} {continuation}");
                SetContinuation(continuationOnAlways, runAlwaysOptions);
            }
        }

        protected void SetContinuation(TaskBase continuation, TaskContinuationOptions runOptions)
        {
            Task.ContinueWith(_ => ((TaskBase)(object)continuation).Run(), Token,
                    runOptions,
                    TaskManager.GetScheduler(continuation.Affinity));
        }

        protected ITask SetDependsOn(ITask dependsOn)
        {
            DependsOn = (TaskBase)dependsOn;
            return this;
        }

        protected TaskBase GetTopMostTaskInCreatedState()
        {
            var depends = DependsOn;
            if (depends == null)
                return null;
            return depends.GetTopMostTask(null, true);
        }

        protected TaskBase GetTopMostTask()
        {
            var depends = DependsOn;
            if (depends == null)
                return null;
            return depends.GetTopMostTask(null, false);
        }

        protected TaskBase GetTopMostTask(TaskBase ret, bool onlyCreatedState)
        {
            ret = (!onlyCreatedState || Task.Status == TaskStatus.Created ? this : ret);
            var depends = DependsOn;
            if (depends == null)
                return ret;
            return depends.GetTopMostTask(ret, onlyCreatedState);
        }

        protected virtual void Run(bool success)
        {
        }

        protected virtual void RaiseOnStart()
        {
            //Logger.Trace($"Executing {ToString()}");
            OnStart?.Invoke(this);
        }

        protected virtual bool RaiseFaultHandlers(Exception ex)
        {
            taskFailed = true;
            exception = ex;
            if (catchHandler == null)
                return false;
            foreach (var handler in catchHandler.GetInvocationList())
            {
                if ((bool)handler.DynamicInvoke(new object[] { ex }))
                {
                    exceptionWasHandled = true;
                    break;
                }
            }
            // if a catch handler returned true, don't throw
            return exceptionWasHandled;
        }

        protected virtual void RaiseOnEnd()
        {
            OnEnd?.Invoke(this, !taskFailed, exception);
            SetupContinuations();
            //Logger.Trace($"Finished {ToString()}");
        }

        protected void SetupContinuations()
        {
            if (!taskFailed || exceptionWasHandled)
            {
                var taskToContinueWith = continuationOnSuccess ?? continuationOnAlways;
                if (taskToContinueWith != null)
                    SetContinuation(taskToContinueWith, runOnSuccessOptions);
                else
                { // there are no more tasks to schedule, call a finally handler if it exists
                  // we need to do this only when there are no more continuations
                  // so that the in-thread finally handler is guaranteed to run after any Finally tasks
                    CallFinallyHandler();
                }
            }
            else
            {
                var taskToContinueWith = continuationOnFailure ?? continuationOnAlways;
                if (taskToContinueWith != null)
                    SetContinuation(taskToContinueWith, runOnFaultOptions);
                else
                { // there are no more tasks to schedule, call a finally handler if it exists
                  // we need to do this only when there are no more continuations
                  // so that the in-thread finally handler is guaranteed to run after any Finally tasks
                    CallFinallyHandler();
                }
            }
        }

        protected virtual void CallFinallyHandler()
        {
            finallyHandler?.Invoke(!taskFailed);
        }

        protected Exception GetThrownException()
        {
            if (DependsOn == null)
                return null;

            if (DependsOn.Task.Status == TaskStatus.Faulted)
            {
                var ex = DependsOn.Task.Exception;
                return ex?.InnerException ?? ex;
            }
            return DependsOn.GetThrownException();
        }

        public void UpdateProgress(long value, long total, string message = null)
        {
            progress.UpdateProgress(value, total, message);
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
        public string Name { get; set; }
        public virtual TaskAffinity Affinity { get; set; }
        private ILogging logger;
        protected ILogging Logger { get { return logger = logger ?? LogHelper.GetLogger(GetType()); } }
        public TaskBase DependsOn { get; private set; }
        public CancellationToken Token { get; }
    }

    abstract class TaskBase<TResult> : TaskBase, ITask<TResult>
    {
        protected TaskCompletionSource<TResult> tcs = new TaskCompletionSource<TResult>();
        private event Action<bool, TResult> finallyHandler;

        public new event Action<ITask<TResult>> OnStart;
        public new event Action<ITask<TResult>, TResult, bool, Exception> OnEnd;
        private TResult result;

        protected TaskBase(CancellationToken token)
            : base(token)
        {
            Task = new Task<TResult>(() =>
            {
                var ret = RunWithReturn(DependsOn?.Successful ?? previousSuccess);
                tcs.SetResult(ret);
                return ret;
            }, Token, TaskCreationOptions.None);
        }

        protected TaskBase(Task<TResult> task)
            : base()
        {
            Task = new Task<TResult>(t =>
            {
                TResult ret = default(TResult);
                RaiseOnStart();
                var tk = ((Task<TResult>)t);
                try
                {
                    if (tk.Status == TaskStatus.Created && !tk.IsCompleted &&
                      ((tk.CreationOptions & (TaskCreationOptions)512) == TaskCreationOptions.None))
                    {
                        tk.RunSynchronously();
                    }
                    ret = tk.Result;
                }
                catch (Exception ex)
                {
                    Errors = ex.Message;
                    if (!RaiseFaultHandlers(ex))
                        throw;
                }
                finally
                {
                    RaiseOnEnd();
                }
                return ret;
            }, task, Token, TaskCreationOptions.None);
        }

        public override T Then<T>(T continuation, TaskRunOptions runOptions = TaskRunOptions.OnSuccess, bool taskIsTopOfChain = false)
        {
            return base.Then<T>(continuation, runOptions, taskIsTopOfChain);
        }

        /// <summary>
        /// Catch runs right when the exception happens (on the same threaD)
        /// Marks the catch as handled so other Catch statements down the chain
        /// won't be called for this exception (but the chain will be cancelled)
        /// </summary>
        public new ITask<TResult> Catch(Action<Exception> handler)
        {
            Guard.ArgumentNotNull(handler, "handler");
            catchHandler += e => { handler(e); return false; };
            DependsOn?.Catch(handler);
            return this;
        }

        /// <summary>
        /// Catch runs right when the exception happens (on the same thread)
        /// Return false if you want other Catch statements on the chain to also
        /// get called for this exception
        /// </summary>
        public new ITask<TResult> Catch(Func<Exception, bool> handler)
        {
            Guard.ArgumentNotNull(handler, "handler");
            catchHandler += handler;
            DependsOn?.Catch(handler);
            return this;
        }

        /// <summary>
        /// Run a callback at the end of the task execution, on the same thread as the task that just finished, regardless of execution state
        /// This will always run on the same thread as the last task that runs
        /// </summary>
        public ITask<TResult> Finally(Action<bool, TResult> handler)
        {
            Guard.ArgumentNotNull(handler, "handler");
            finallyHandler += handler;
            DependsOn?.Finally(success => handler(success, default(TResult)));
            return this;
        }

        /// <summary>
        /// Run a callback at the end of the task execution, on a separate thread, regardless of execution state
        /// </summary>
        public ITask<TResult> Finally(Func<bool, Exception, TResult, TResult> continuation, TaskAffinity affinity = TaskAffinity.Concurrent)
        {
            Guard.ArgumentNotNull(continuation, "continuation");
            var ret = Then(new FuncTask<TResult, TResult>(Token, continuation) { Affinity = affinity, Name = "Finally" }, TaskRunOptions.OnAlways);
            DependsOn?.SetFaultHandler(ret);
            return ret;
        }

        /// <summary>
        /// Run a callback at the end of the task execution, on a separate thread, regardless of execution state
        /// </summary>
        public ITask Finally(Action<bool, Exception, TResult> continuation, TaskAffinity affinity = TaskAffinity.Concurrent)
        {
            Guard.ArgumentNotNull(continuation, "continuation");
            var ret = Then(new ActionTask<TResult>(Token, continuation) { Affinity = affinity, Name = "Finally" }, TaskRunOptions.OnAlways);
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

        /// <summary>
        /// Progress provides progress reporting from the task (on the same thread)
        /// </summary>
        public new ITask<TResult> Progress(Action<IProgress> handler)
        {
            base.Progress(handler);
            return this;
        }

        protected virtual TResult RunWithReturn(bool success)
        {
            base.Run(success);
            return result;
        }

        protected override void RaiseOnStart()
        {
            //Logger.Trace($"Executing {ToString()}");
            OnStart?.Invoke(this);
            base.RaiseOnStart();
        }

        protected virtual void RaiseOnEnd(TResult data)
        {
            this.result = data;
            OnEnd?.Invoke(this, result, !taskFailed, exception);
            SetupContinuations();
            //Logger.Trace($"Finished {ToString()} {result}");
        }

        protected override void CallFinallyHandler()
        {
            finallyHandler?.Invoke(!taskFailed, result);
            base.CallFinallyHandler();
        }

        public new Task<TResult> Task
        {
            get { return base.Task as Task<TResult>; }
            set { base.Task = value; }
        }
        public TResult Result { get { return Task.Result; } }
    }

    abstract class TaskBase<T, TResult> : TaskBase<TResult>
    {
        public TaskBase(CancellationToken token)
            : base(token)
        {
            Task = new Task<TResult>(() =>
            {
                var ret = RunWithData(DependsOn?.Successful ?? previousSuccess, (DependsOn?.Successful ?? false) ? ((ITask<T>)DependsOn).Result : default(T));
                tcs.SetResult(ret);
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
        public DataTaskBase(CancellationToken token)
            : base(token)
        {}

        public DataTaskBase(Task<TResult> task)
            : base(task)
        {}

        public event Action<TData> OnData;
        protected void RaiseOnData(TData data)
        {
            OnData?.Invoke(data);
        }
    }

    abstract class DataTaskBase<T, TData, TResult> : TaskBase<T, TResult>, ITask<TData, TResult>
    {
        public DataTaskBase(CancellationToken token)
            : base(token)
        {}

        public DataTaskBase(Task<TResult> task)
            : base(task)
        {}

        public event Action<TData> OnData;
        protected void RaiseOnData(TData data)
        {
            OnData?.Invoke(data);
        }
    }

    public enum TaskAffinity
    {
        Concurrent,
        Exclusive,
        UI
    }
}