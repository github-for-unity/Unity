using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace GitHub.Unity
{
    class TaskQueue : TPLTask
    {
        private TaskCompletionSource<bool> aggregateTask = new TaskCompletionSource<bool>();
        private readonly List<ITask> queuedTasks = new List<ITask>();
        private int finishedTaskCount;

        public TaskQueue() : base()
        {
            Initialize(aggregateTask.Task);
        }

        public ITask Queue(ITask task)
        {
            // if this task fails, both OnEnd and Catch will be called
            // if a task before this one on the chain fails, only Catch will be called
            // so avoid calling TaskFinished twice by ignoring failed OnEnd calls
            task.OnEnd += InvokeFinishOnlyOnSuccess;
            task.Catch(e => TaskFinished(false, e));
            queuedTasks.Add(task);
            return this;
        }

        public override void RunSynchronously()
        {
            if (queuedTasks.Any())
            {
                foreach (var task in queuedTasks)
                    task.Start();
            }
            else
            {
                aggregateTask.TrySetResult(true);
            }

            base.RunSynchronously();
        }

        protected override void Schedule()
        {
            if (queuedTasks.Any())
            {
                foreach (var task in queuedTasks)
                    task.Start();
            }
            else
            {
                aggregateTask.TrySetResult(true);
            }

            base.Schedule();
        }

        private void InvokeFinishOnlyOnSuccess(ITask task, bool success, Exception ex)
        {
            if (success)
                TaskFinished(true, null);
        }

        private void TaskFinished(bool success, Exception ex)
        {
            var count = Interlocked.Increment(ref finishedTaskCount);
            if (count == queuedTasks.Count)
            {
                var exceptions = queuedTasks.Where(x => !x.Successful).Select(x => x.Exception).ToArray();
                var isSuccessful = exceptions.Length == 0;

                if (isSuccessful)
                {
                    aggregateTask.TrySetResult(true);
                }
                else
                {
                    aggregateTask.TrySetException(new AggregateException(exceptions));
                }
            }
        }
    }

    class TaskQueue<TTaskResult, TResult> : TPLTask<List<TResult>>
    {
        private TaskCompletionSource<List<TResult>> aggregateTask = new TaskCompletionSource<List<TResult>>();
        private readonly List<ITask<TTaskResult>> queuedTasks = new List<ITask<TTaskResult>>();
        private int finishedTaskCount;
        private Func<ITask<TTaskResult>, TResult> resultConverter;

        /// <summary>
        /// If <typeparamref name="TTaskResult"/> is not assignable to <typeparamref name="TResult"/>, you must pass a
        /// method to convert between the two. Implicit conversions don't count (so even though NPath has an implicit
        /// conversion to string, you still need to pass in a converter)
        /// </summary>
        /// <param name="resultConverter"></param>
        public TaskQueue(Func<ITask<TTaskResult>, TResult> resultConverter = null) : base()
        {
            // this excludes implicit operators - that requires using reflection to figure out if
            // the types are convertible, and I'd rather not do that
            if (resultConverter == null && !typeof(TResult).IsAssignableFrom(typeof(TTaskResult)))
            {
                throw new ArgumentNullException(nameof(resultConverter),
                    String.Format(CultureInfo.InvariantCulture, "Cannot cast {0} to {1} and no {2} method was passed in to do the conversion", typeof(TTaskResult), typeof(TResult), nameof(resultConverter)));
            }
            this.resultConverter = resultConverter;
            Initialize(aggregateTask.Task);
        }

        /// <summary>
        /// Queues an ITask for running, and when the task is done, <paramref name="theResultConverter"/> is called
        /// to convert the result of the task to something else
        /// </summary>
        /// <param name="task"></param>
        /// 
        /// <returns></returns>
        public ITask<TTaskResult> Queue(ITask<TTaskResult> task)
        {
            // if this task fails, both OnEnd and Catch will be called
            // if a task before this one on the chain fails, only Catch will be called
            // so avoid calling TaskFinished twice by ignoring failed OnEnd calls
            task.OnEnd += InvokeFinishOnlyOnSuccess;
            task.Catch(e => TaskFinished(default(TTaskResult), false, e));
            queuedTasks.Add(task);
            return task;
        }

        public override List<TResult> RunSynchronously()
        {
            if (queuedTasks.Any())
            {
                foreach (var task in queuedTasks)
                    task.Start();
            }
            else
            {
                aggregateTask.TrySetResult(new List<TResult>());
            }

            return base.RunSynchronously();
        }

        protected override void Schedule()
        {
            if (queuedTasks.Any())
            {
                foreach (var task in queuedTasks)
                    task.Start();
            }
            else
            {
                aggregateTask.TrySetResult(new List<TResult>());
            }

            base.Schedule();
        }

        private void InvokeFinishOnlyOnSuccess(ITask<TTaskResult> task, TTaskResult result, bool success, Exception ex)
        {
            if (success)
                TaskFinished(result, true, null);
        }

        private void TaskFinished(TTaskResult result, bool success, Exception ex)
        {
            var count = Interlocked.Increment(ref finishedTaskCount);
            if (count == queuedTasks.Count)
            {
                var exceptions = queuedTasks.Where(x => !x.Successful).Select(x => x.Exception).ToArray();
                var isSuccessful = exceptions.Length == 0;

                if (isSuccessful)
                {
                    List<TResult> results;
                    if (resultConverter != null)
                        results = queuedTasks.Select(x => resultConverter(x)).ToList();
                    else
                        results = queuedTasks.Select(x => (TResult)(object)x.Result).ToList();
                    aggregateTask.TrySetResult(results);
                }
                else
                {
                    aggregateTask.TrySetException(new AggregateException(exceptions));
                }
            }
        }
    }

    class TPLTask : TaskBase
    {
        private Task task;

        protected TPLTask() : base()
        {}

        public TPLTask(Task task)
            : base()
        {
            Initialize(task);
        }

        protected void Initialize(Task theTask)
        {
            this.task = theTask;
            Task = new Task(RunSynchronously, Token, TaskCreationOptions.None);
        }

        protected override void Run(bool success)
        {
            base.Run(success);

            Token.ThrowIfCancellationRequested();
            try
            {
                if (task.Status == TaskStatus.Created && !task.IsCompleted &&
                    ((task.CreationOptions & (TaskCreationOptions)512) == TaskCreationOptions.None))
                {
                    var scheduler = TaskManager.GetScheduler(Affinity);
                    Token.ThrowIfCancellationRequested();
                    task.RunSynchronously(scheduler);
                }
                else
                    task.Wait();
            }
            catch (Exception ex)
            {
                if (!RaiseFaultHandlers(ex))
                    throw exception;
                Token.ThrowIfCancellationRequested();
            }
        }
    }

    class TPLTask<T> : TaskBase<T>
    {
        private Task<T> task;

        protected TPLTask() : base()
        { }

        public TPLTask(Task<T> task)
            : base()
        {
            Initialize(task);
        }

        protected void Initialize(Task<T> theTask)
        {
            this.task = theTask;
            Task = new Task<T>(RunSynchronously, Token, TaskCreationOptions.None);
        }

        protected override T RunWithReturn(bool success)
        {
            var ret = base.RunWithReturn(success);

            Token.ThrowIfCancellationRequested();
            try
            {
                if (task.Status == TaskStatus.Created && !task.IsCompleted &&
                    ((task.CreationOptions & (TaskCreationOptions)512) == TaskCreationOptions.None))
                {
                    var scheduler = TaskManager.GetScheduler(Affinity);
                    Token.ThrowIfCancellationRequested();
                    task.RunSynchronously(scheduler);
                }
                ret = task.Result;
            }
            catch (Exception ex)
            {
                if (!RaiseFaultHandlers(ex))
                    throw exception;
                Token.ThrowIfCancellationRequested();
            }
            return ret;
        }
    }

    class ActionTask : TaskBase
    {
        protected Action<bool> Callback { get; }
        protected Action<bool, Exception> CallbackWithException { get; }

        public ActionTask(CancellationToken token, Action action)
            : base(token)
        {
            Guard.ArgumentNotNull(action, "action");
            this.Callback = _ => action();
            Name = "ActionTask";
        }

        public ActionTask(CancellationToken token, Action<bool> action)
            : base(token)
        {
            Guard.ArgumentNotNull(action, "action");
            this.Callback = action;
            Name = "ActionTask";
        }

        public ActionTask(CancellationToken token, Action<bool, Exception> action)
            : base(token)
        {
            Guard.ArgumentNotNull(action, "action");
            this.CallbackWithException = action;
            Name = "ActionTask<Exception>";
        }

        protected override void Run(bool success)
        {
            base.Run(success);
            try
            {
                Callback?.Invoke(success);
                if (CallbackWithException != null)
                {
                    var thrown = GetThrownException();
                    CallbackWithException?.Invoke(success, thrown);
                }
            }
            catch (Exception ex)
            {
                if (!RaiseFaultHandlers(ex))
                    throw exception;
            }
        }
    }

    class ActionTask<T> : TaskBase
    {
        private readonly Func<T> getPreviousResult;

        protected Action<bool, T> Callback { get; }
        protected Action<bool, Exception, T> CallbackWithException { get; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="token"></param>
        /// <param name="action"></param>
        /// <param name="getPreviousResult">Method to call that returns the value that this task is going to work with. You can also use the PreviousResult property to set this value</param>
        public ActionTask(CancellationToken token, Action<bool, T> action, Func<T> getPreviousResult = null)
            : base(token)
        {
            Guard.ArgumentNotNull(action, "action");
            this.Callback = action;
            this.getPreviousResult = getPreviousResult;
            Task = new Task(RunSynchronously, Token, TaskCreationOptions.None);
            Name = $"ActionTask<{typeof(T)}>";
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="token"></param>
        /// <param name="action"></param>
        /// <param name="getPreviousResult">Method to call that returns the value that this task is going to work with. You can also use the PreviousResult property to set this value</param>
        public ActionTask(CancellationToken token, Action<bool, Exception, T> action, Func<T> getPreviousResult = null)
            : base(token)
        {
            Guard.ArgumentNotNull(action, "action");
            this.CallbackWithException = action;
            this.getPreviousResult = getPreviousResult;
            Task = new Task(RunSynchronously, Token, TaskCreationOptions.None);
            Name = $"ActionTask<Exception, {typeof(T)}>";
        }

        public override void RunSynchronously()
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

            try
            {
                Run(previousIsSuccessful, prevResult);
            }
            finally
            {
                RaiseOnEnd();
            }
        }

        protected virtual void Run(bool success, T previousResult)
        {
            base.Run(success);
            try
            {
                Callback?.Invoke(success, previousResult);
                if (CallbackWithException != null)
                {
                    var thrown = GetThrownException();
                    CallbackWithException?.Invoke(success, thrown, previousResult);
                }
            }
            catch (Exception ex)
            {
                if (!RaiseFaultHandlers(ex))
                    throw exception;
            }
        }

        public T PreviousResult { get; set; } = default(T);
    }

    class FuncTask<T> : TaskBase<T>
    {
        protected Func<bool, T> Callback { get; }
        protected Func<bool, Exception, T> CallbackWithException { get; }

        public FuncTask(CancellationToken token, Func<T> action)
            : base(token)
        {
            Guard.ArgumentNotNull(action, "action");
            this.Callback = _ => action();
            Name = $"FuncTask<{typeof(T)}>";
        }

        public FuncTask(CancellationToken token, Func<bool, T> action)
            : base(token)
        {
            Guard.ArgumentNotNull(action, "action");
            this.Callback = action;
            Name = $"FuncTask<{typeof(T)}>";
        }

        public FuncTask(CancellationToken token, Func<bool, Exception, T> action)
            : base(token)
        {
            Guard.ArgumentNotNull(action, "action");
            this.CallbackWithException = action;
            Name = $"FuncTask<Exception, {typeof(T)}>";
        }

        protected override T RunWithReturn(bool success)
        {
            T result = base.RunWithReturn(success);
            try
            {
                if (Callback != null)
                {
                    result = Callback(success);
                }
                else if (CallbackWithException != null)
                {
                    var thrown = GetThrownException();
                    result = CallbackWithException(success, thrown);
                }
            }
            catch (Exception ex)
            {
                if (!RaiseFaultHandlers(ex))
                    throw exception;
            }
            return result;
        }
    }

    class FuncTask<T, TResult> : TaskBase<T, TResult>
    {
        protected Func<bool, T, TResult> Callback { get; }
        protected Func<bool, Exception, T, TResult> CallbackWithException { get; }

        public FuncTask(CancellationToken token, Func<bool, T, TResult> action, Func<T> getPreviousResult = null)
            : base(token, getPreviousResult)
        {
            Guard.ArgumentNotNull(action, "action");
            this.Callback = action;
            Name = $"FuncTask<{typeof(T)}, {typeof(TResult)}>";
        }

        public FuncTask(CancellationToken token, Func<bool, Exception, T, TResult> action, Func<T> getPreviousResult = null)
            : base(token, getPreviousResult)
        {
            Guard.ArgumentNotNull(action, "action");
            this.CallbackWithException = action;
            Name = $"FuncTask<{typeof(T)}, Exception, {typeof(TResult)}>";
        }

        protected override TResult RunWithData(bool success, T previousResult)
        {
            var result = base.RunWithData(success, previousResult);
            try
            {
                if (Callback != null)
                {
                    result = Callback(success, previousResult);
                }
                else if (CallbackWithException != null)
                {
                    var thrown = GetThrownException();
                    result = CallbackWithException(success, thrown, previousResult);
                }
            }
            catch (Exception ex)
            {
                if (!RaiseFaultHandlers(ex))
                    throw exception;
            }
            return result;
        }
    }

    class FuncListTask<T> : DataTaskBase<T, List<T>>
    {
        protected Func<bool, List<T>> Callback { get; }
        protected Func<bool, FuncListTask<T>, List<T>> CallbackWithSelf { get; }
        protected Func<bool, Exception, List<T>> CallbackWithException { get; }

        public FuncListTask(CancellationToken token, Func<bool, List<T>> action)
            : base(token)
        {
            Guard.ArgumentNotNull(action, "action");
            this.Callback = action;
        }

        public FuncListTask(CancellationToken token, Func<bool, Exception, List<T>> action)
            : base(token)
        {
            Guard.ArgumentNotNull(action, "action");
            this.CallbackWithException = action;
        }

        public FuncListTask(CancellationToken token, Func<bool, FuncListTask<T>, List<T>> action)
            : base(token)
        {
            Guard.ArgumentNotNull(action, "action");
            this.CallbackWithSelf = action;
        }

        protected override List<T> RunWithReturn(bool success)
        {
            var result = base.RunWithReturn(success);
            try
            {
                if (Callback != null)
                {
                    result = Callback(success);
                }
                else if (CallbackWithSelf != null)
                {
                    result = CallbackWithSelf(success, this);
                }
                else if (CallbackWithException != null)
                {
                    var thrown = GetThrownException();
                    result = CallbackWithException(success, thrown);
                }
            }
            catch (Exception ex)
            {
                if (!RaiseFaultHandlers(ex))
                    throw exception;
            }
            finally
            {
                if (result == null)
                    result = new List<T>();
            }
            return result;
        }
    }

    class FuncListTask<T, TData, TResult> : DataTaskBase<T, TData, List<TResult>>
    {
        protected Func<bool, T, List<TResult>> Callback { get; }
        protected Func<bool, Exception, T, List<TResult>> CallbackWithException { get; }

        public FuncListTask(CancellationToken token, Func<bool, T, List<TResult>> action)
            : base(token)
        {
            Guard.ArgumentNotNull(action, "action");
            this.Callback = action;
        }

        public FuncListTask(CancellationToken token, Func<bool, Exception, T, List<TResult>> action)
            : base(token)
        {
            Guard.ArgumentNotNull(action, "action");
            this.CallbackWithException = action;
        }

        protected override List<TResult> RunWithData(bool success, T previousResult)
        {
            var result = base.RunWithData(success, previousResult);
            try
            {
                if (Callback != null)
                {
                    result = Callback(success, previousResult);
                }
                else if (CallbackWithException != null)
                {
                    var thrown = GetThrownException();
                    result = CallbackWithException(success, thrown, previousResult);
                }
            }
            catch (Exception ex)
            {
                if (!RaiseFaultHandlers(ex))
                    throw exception;
            }
            return result;
        }
    }
}
