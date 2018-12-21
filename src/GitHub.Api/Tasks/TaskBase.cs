using GitHub.Logging;
using System;
using System.Collections.Generic;
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
        void RunSynchronously();

        ITask Progress(Action<IProgress> progressHandler);
        void UpdateProgress(long value, long total, string message = null);

        ITask GetTopOfChain(bool onlyCreated = true);
        ITask GetEndOfChain();

        /// <summary>
        /// </summary>
        /// <returns>true if any task on the chain is marked as exclusive</returns>
        bool IsChainExclusive();


        bool Successful { get; }
        string Errors { get; }
        Task Task { get; }
        string Name { get; }
        TaskAffinity Affinity { get; set; }
        CancellationToken Token { get; }
        TaskBase DependsOn { get; }
        event Action<ITask> OnStart;
        event Action<ITask, bool, Exception> OnEnd;
        string Message { get; }
        Exception Exception { get; }
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
        new TResult RunSynchronously();
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

    public class TaskBase : ITask
    {
        public static ITask Default = new TaskBase { Name = "Global" };

        protected const TaskContinuationOptions runAlwaysOptions = TaskContinuationOptions.None;
        protected const TaskContinuationOptions runOnSuccessOptions = TaskContinuationOptions.OnlyOnRanToCompletion;
        protected const TaskContinuationOptions runOnFaultOptions = TaskContinuationOptions.OnlyOnFaulted;

        public event Action<ITask> OnStart;
        public event Action<ITask, bool, Exception> OnEnd;

        protected bool? previousSuccess;
        protected Exception previousException;
        protected bool taskFailed = false;
        protected bool exceptionWasHandled = false;
        protected bool hasRun = false;
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
            Task = new Task(RunSynchronously, Token, TaskCreationOptions.None);
        }

        protected TaskBase()
        {
            this.progress = new Progress(this);
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
            var nextTaskFinallyHandler = nextTaskBase.finallyHandler;

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
                if (nextTaskFinallyHandler != null)
                    Finally(nextTaskFinallyHandler);
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

            // if the current task has a fault handler, attach it to the chain we're appending
            if (finallyHandler != null)
            {
                TaskBase endOfChainTask = (TaskBase)nextTaskBase.GetEndOfChain();
                while (endOfChainTask != this && endOfChainTask != null)
                {
                    endOfChainTask.finallyHandler += finallyHandler;
                    endOfChainTask = endOfChainTask.DependsOn;
                }
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
            CatchInternal(handler);
            DependsOn?.Catch(handler);
            return this;
        }

        internal ITask CatchInternal(Func<Exception, bool> handler)
        {
            Guard.ArgumentNotNull(handler, "handler");
            catchHandler += handler;
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
            return Then(new ActionTask(Token, (s, ex) =>
                {
                    actionToContinueWith(s, ex);
                    if (!s)
                        throw ex;
                })
                { Affinity = affinity, Name = "Finally" }, TaskRunOptions.OnAlways)
                .CatchInternal(_ => true);
        }

        /// <summary>
        /// Run another task at the end of the task execution, on a separate thread, regardless of execution state
        /// </summary>
        public T Finally<T>(T taskToContinueWith)
            where T : ITask
        {
            return Then(taskToContinueWith, TaskRunOptions.OnAlways);
        }

        /// <summary>
        /// This does not set a dependency between the two tasks. Instead,
        /// the Start method grabs the state of the previous task to pass on
        /// to the next task via previousSuccess and previousException
        /// </summary>
        /// <param name="handler"></param>
        internal void SetFaultHandler(TaskBase handler)
        {
            Task.ContinueWith(t =>
                {
                    Token.ThrowIfCancellationRequested();
                    handler.Start(t);
                },
                Token,
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

        public ITask Start()
        {
            var depends = GetTopMostStartableTask();
            depends?.Schedule();
            return this;
        }

        public virtual void RunSynchronously()
        {
            RaiseOnStart();
            Token.ThrowIfCancellationRequested();
            var previousIsSuccessful = previousSuccess.HasValue ? previousSuccess.Value : (DependsOn?.Successful ?? true);
            try
            {
                Run(previousIsSuccessful);
            }
            finally
            {
                RaiseOnEnd();
            }
        }

        protected virtual void Schedule()
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
                Task.Start(scheduler);
            }
            return this;
        }

        public ITask GetTopOfChain(bool onlyCreated = true)
        {
            return GetTopMostTask(null, onlyCreated, false);
        }

        public ITask GetEndOfChain()
        {
            if (continuationOnSuccess != null)
                return continuationOnSuccess.GetEndOfChain();
            else if (continuationOnAlways != null)
                return continuationOnAlways.GetEndOfChain();
            return this;
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
                SetContinuation(continuationOnAlways, runAlwaysOptions);
            }
        }

        protected void SetContinuation(TaskBase continuation, TaskContinuationOptions runOptions)
        {
            Token.ThrowIfCancellationRequested();
            Task.ContinueWith(_ =>
                {
                    Token.ThrowIfCancellationRequested();
                    ((TaskBase)(object)continuation).Schedule();
                },
                Token,
                runOptions,
                TaskManager.GetScheduler(continuation.Affinity));
        }

        protected ITask SetDependsOn(ITask dependsOn)
        {
            DependsOn = (TaskBase)dependsOn;
            return this;
        }

        /// <summary>
        /// Returns the first startable task on the chain. If the chain has been started
        /// already, returns null
        /// </summary>
        protected TaskBase GetTopMostStartableTask()
        {
            return GetTopMostTask(null, true, true);
        }

        protected TaskBase GetTopMostCreatedTask()
        {
            return GetTopMostTask(null, true, false);
        }

        protected TaskBase GetTopMostTask()
        {
            return GetTopMostTask(null, false, false);
        }

        protected TaskBase GetTopMostTask(TaskBase ret, bool onlyCreated, bool onlyUnstartedChain)
        {
            ret = (!onlyCreated || Task.Status == TaskStatus.Created ? this : ret);
            var depends = DependsOn;
            if (depends == null)
            {
                // if we're at the top of the chain and the chain has already been started
                // and we only care about unstarted chains, return null
                if (onlyUnstartedChain && Task.Status != TaskStatus.Created)
                    return null;
                return ret;
            }
            return depends.GetTopMostTask(ret, onlyCreated, onlyUnstartedChain);
        }

        protected virtual void Run(bool success)
        {
            taskFailed = false;
            hasRun = false;
            exception = null;
            Token.ThrowIfCancellationRequested();
        }

        protected virtual void RaiseOnStart()
        {
            UpdateProgress(0, 100);
            RaiseOnStartInternal();
        }

        protected void RaiseOnStartInternal()
        {
            OnStart?.Invoke(this);
        }

        protected virtual bool RaiseFaultHandlers(Exception ex)
        {
            exception = ex;
            if (exception is AggregateException)
                exception = exception.GetBaseException() ?? exception;
            Errors = exception.Message;
            taskFailed = true;
            if (catchHandler == null)
                return false;
            var args = new object[] { exception };
            foreach (var handler in catchHandler.GetInvocationList())
            {
                if ((bool)handler.DynamicInvoke(args))
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
            hasRun = true;
            RaiseOnEndInternal();
            SetupContinuations();
            UpdateProgress(100, 100);
        }

        protected void RaiseOnEndInternal()
        {
            OnEnd?.Invoke(this, !taskFailed, exception);
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
            var depends = DependsOn;
            while (depends != null)
            {
                if (depends.taskFailed)
                    return depends.exception;
                depends = depends.DependsOn;
            }
            return null;
        }

        public void UpdateProgress(long value, long total, string message = null)
        {
            progress.UpdateProgress(value, total, message ?? this.Message);
        }

        public override string ToString()
        {
            return $"{Task?.Id ?? -1} {Name} {GetType()}";
        }

        public virtual bool Successful { get { return hasRun && !taskFailed; } }
        public bool IsCompleted { get { return hasRun; } }
        public Exception Exception => exception ?? GetThrownException();

        public string Errors { get; protected set; }
        public Task Task { get; protected set; }
        public WaitHandle AsyncWaitHandle { get { return (Task as IAsyncResult).AsyncWaitHandle; } }
        public object AsyncState { get { return (Task as IAsyncResult).AsyncState; } }
        public bool CompletedSynchronously { get { return (Task as IAsyncResult).CompletedSynchronously; } }
        public string Name { get; set; }
        public virtual TaskAffinity Affinity { get; set; }
        private ILogging logger;
        protected ILogging Logger { get { return logger = logger ?? LogHelper.GetLogger(GetType()); } }
        public TaskBase DependsOn { get; private set; }
        public CancellationToken Token { get; }
        public virtual string Message { get; set; }
    }

    public abstract class TaskBase<TResult> : TaskBase, ITask<TResult>
    {
        private event Action<bool, TResult> finallyHandler;

        public new event Action<ITask<TResult>> OnStart;
        public new event Action<ITask<TResult>, TResult, bool, Exception> OnEnd;
        private TResult result;

        protected TaskBase()
            : base()
        {
        }

        protected TaskBase(CancellationToken token)
            : base(token)
        {
            Task = new Task<TResult>(RunSynchronously, Token, TaskCreationOptions.None);
        }

        public override T Then<T>(T continuation, TaskRunOptions runOptions = TaskRunOptions.OnSuccess, bool taskIsTopOfChain = false)
        {
            var nextTask = base.Then<T>(continuation, runOptions, taskIsTopOfChain);
            var nextTaskBase = ((TaskBase)(object)nextTask);
            // if the current task has a fault handler that matches this signature, attach it to the chain we're appending
            if (finallyHandler != null)
            {
                TaskBase endOfChainTask = (TaskBase)nextTaskBase.GetEndOfChain();
                while (endOfChainTask != this && endOfChainTask != null)
                {
                    if (endOfChainTask is TaskBase<TResult>)
                        ((TaskBase<TResult>)endOfChainTask).finallyHandler += finallyHandler;
                    endOfChainTask = endOfChainTask.DependsOn;
                }
            }
            return nextTask;
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
            CatchInternal(handler);
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
            return Then(new FuncTask<TResult, TResult>(Token, continuation) { Affinity = affinity, Name = "Finally" }, TaskRunOptions.OnAlways);
        }

        /// <summary>
        /// Run a callback at the end of the task execution, on a separate thread, regardless of execution state
        /// </summary>
        public ITask Finally(Action<bool, Exception, TResult> continuation, TaskAffinity affinity = TaskAffinity.Concurrent)
        {
            Guard.ArgumentNotNull(continuation, "continuation");
            return Then(new ActionTask<TResult>(Token, (s, ex, res) =>
                {
                    continuation(s, ex, res);
                    if (!s)
                        throw ex;
                })
                { Affinity = affinity, Name = "Finally" }, TaskRunOptions.OnAlways)
                .CatchInternal(_ => true);
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

        public new virtual TResult RunSynchronously()
        {
            RaiseOnStart();
            Token.ThrowIfCancellationRequested();
            var previousIsSuccessful = previousSuccess.HasValue ? previousSuccess.Value : (DependsOn?.Successful ?? true);
            TResult ret = default(TResult);
            try
            {
                ret = RunWithReturn(previousIsSuccessful);
            }
            finally
            {
                RaiseOnEnd(ret);
            }
            return ret;
        }


        protected override void RaiseOnStart()
        {
            UpdateProgress(0, 100);
            OnStart?.Invoke(this);
            RaiseOnStartInternal();
        }

        protected virtual void RaiseOnEnd(TResult data)
        {
            this.result = data;
            hasRun = true;
            OnEnd?.Invoke(this, result, !taskFailed, exception);
            RaiseOnEndInternal();
            SetupContinuations();
            UpdateProgress(100, 100);
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
        public TResult Result { get { return result; } }
    }

    public abstract class TaskBase<T, TResult> : TaskBase<TResult>
    {
        private readonly Func<T> getPreviousResult;

        public TaskBase(CancellationToken token, Func<T> getPreviousResult = null)
            : base(token)
        {
            Task = new Task<TResult>(RunSynchronously, Token, TaskCreationOptions.None);
            this.getPreviousResult = getPreviousResult;
        }

        public override TResult RunSynchronously()
        {
            RaiseOnStart();
            Token.ThrowIfCancellationRequested();
            var previousIsSuccessful = previousSuccess.HasValue ? previousSuccess.Value : (DependsOn?.Successful ?? true);

            // if this task depends on another task and the dependent task was successful, use the value of that other task as input to this task
            // otherwise if there's a method to retrieve the value, call that
            // otherwise use the PreviousResult property
            T prevResult = PreviousResult;
            if (previousIsSuccessful && DependsOn != null && DependsOn is ITask<T>)
                prevResult = ((ITask<T>)DependsOn).Result;
            else if (getPreviousResult != null)
                prevResult = getPreviousResult();

            TResult ret = default(TResult);
            try
            {
                ret = RunWithData(previousIsSuccessful, prevResult);
            }
            finally
            {
                RaiseOnEnd(ret);
            }
            return ret;
        }

        protected virtual TResult RunWithData(bool success, T previousResult)
        {
            base.Run(success);
            return default(TResult);
        }

        public T PreviousResult { get; set; } = default(T);
    }

    public abstract class DataTaskBase<TData, TResult> : TaskBase<TResult>, ITask<TData, TResult>
    {
        public DataTaskBase(CancellationToken token)
            : base(token)
        {}

        public event Action<TData> OnData;
        protected void RaiseOnData(TData data)
        {
            OnData?.Invoke(data);
        }
    }

    public abstract class DataTaskBase<T, TData, TResult> : TaskBase<T, TResult>, ITask<TData, TResult>
    {
        public DataTaskBase(CancellationToken token)
            : base(token)
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
