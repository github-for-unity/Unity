using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace GitHub.Unity
{
    class TaskQueue : TaskBase
    {
        private TaskCompletionSource<bool> aggregateTask = new TaskCompletionSource<bool>();
        private readonly List<ITask> queuedTasks = new List<ITask>();
        private volatile bool isSuccessful = true;
        private volatile Exception exception;
        private int finishedTaskCount;

        public TaskQueue() : base()
        {
            Initialize(aggregateTask.Task);
        }

        public ITask Queue(ITask task)
        {
            task.OnEnd += TaskFinished;
            queuedTasks.Add(task);
            return this;
        }

        public override ITask Start()
        {
            foreach (var task in queuedTasks)
                task.Start();
            return base.Start();
        }

        private void TaskFinished(ITask task, bool success, Exception ex)
        {
            var count = Interlocked.Increment(ref finishedTaskCount);
            isSuccessful &= success;
            if (!success)
                exception = ex;
            if (count == queuedTasks.Count)
            {
                if (isSuccessful)
                {
                    aggregateTask.TrySetResult(true);
                }
                else
                {
                    aggregateTask.TrySetException(ex);
                }
            }
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

        public ActionTask(Task task)
            : base(task)
        {
            Name = "ActionTask(Task)";
        }

        public override void Run(bool success)
        {
            base.Run(success);

            RaiseOnStart();
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
                Errors = ex.Message;
                if (!RaiseFaultHandlers(ex))
                    throw;
            }
            finally
            {
                RaiseOnEnd();
            }
        }
    }

    class ActionTask<T> : TaskBase
    {
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
            Task = new Task(() => Run(DependsOn?.Successful ?? true,
                // if this task depends on another task and the dependent task was successful, use the value of that other task as input to this task
                // otherwise if there's a method to retrieve the value, call that
                // otherwise use the PreviousResult property
                (DependsOn?.Successful ?? false) ? ((ITask<T>)DependsOn).Result : getPreviousResult != null ? getPreviousResult() : PreviousResult),
                Token, TaskCreationOptions.None);
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
            Task = new Task(() => Run(DependsOn?.Successful ?? true,
                // if this task depends on another task and the dependent task was successful, use the value of that other task as input to this task
                // otherwise if there's a method to retrieve the value, call that
                // otherwise use the PreviousResult property
                (DependsOn?.Successful ?? false) ? ((ITask<T>)DependsOn).Result : getPreviousResult != null ? getPreviousResult() : PreviousResult),
                Token, TaskCreationOptions.None);
            Name = $"ActionTask<Exception, {typeof(T)}>";
        }

        public ActionTask(Task task)
            : base(task)
        {
            Name = $"ActionTask<{typeof(T)}>(Task)";
        }

        protected virtual void Run(bool success, T previousResult)
        {
            base.Run(success);

            RaiseOnStart();

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
                Errors = ex.Message;
                if (!RaiseFaultHandlers(ex))
                    throw;
            }
            finally
            {
                RaiseOnEnd();
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

        public FuncTask(Task<T> task)
            : base(task)
        {
            Name = $"FuncTask<{typeof(T)}>(Task)";
        }

        public override T RunWithReturn(bool success)
        {
            T result = base.RunWithReturn(success);

            RaiseOnStart();

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
                Errors = ex.Message;
                if (!RaiseFaultHandlers(ex))
                    throw;
            }
            finally
            {
                RaiseOnEnd(result);
            }

            return result;
        }
    }

    class FuncTask<T, TResult> : TaskBase<T, TResult>
    {
        protected Func<bool, T, TResult> Callback { get; }
        protected Func<bool, Exception, T, TResult> CallbackWithException { get; }

        public FuncTask(CancellationToken token, Func<bool, T, TResult> action)
            : base(token)
        {
            Guard.ArgumentNotNull(action, "action");
            this.Callback = action;
            Name = $"FuncTask<{typeof(T)}, {typeof(TResult)}>";
        }

        public FuncTask(CancellationToken token, Func<bool, Exception, T, TResult> action)
            : base(token)
        {
            Guard.ArgumentNotNull(action, "action");
            this.CallbackWithException = action;
            Name = $"FuncTask<{typeof(T)}, Exception, {typeof(TResult)}>";
        }


        public FuncTask(Task<TResult> task)
            : base(task)
        {
            Name = $"FuncTask<{typeof(T)}, {typeof(TResult)}>(Task)";
        }

        protected override TResult RunWithData(bool success, T previousResult)
        {
            var result = base.RunWithData(success, previousResult);

            RaiseOnStart();

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
                Errors = ex.Message;
                if (!RaiseFaultHandlers(ex))
                    throw;
            }
            finally
            {
                RaiseOnEnd(result);
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

        public FuncListTask(Task<List<T>> task)
            : base(task)
        { }

        public override List<T> RunWithReturn(bool success)
        {
            var result = base.RunWithReturn(success);

            RaiseOnStart();

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
            catch (AggregateException ex)
            {
                var e = ex.GetBaseException();
                Errors = e.Message;
                if (!RaiseFaultHandlers(e))
                    throw e;
            }
            catch (Exception ex)
            {
                Errors = ex.Message;
                if (!RaiseFaultHandlers(ex))
                    throw;
            }
            finally
            {
                if (result == null)
                    result = new List<T>();

                RaiseOnEnd(result);
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

        public FuncListTask(Task<List<TResult>> task)
            : base(task)
        { }

        protected override List<TResult> RunWithData(bool success, T previousResult)
        {
            var result = base.RunWithData(success, previousResult);

            RaiseOnStart();

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
                Errors = ex.Message;
                if (!RaiseFaultHandlers(ex))
                    throw;
            }
            finally
            {
                RaiseOnEnd(result);
            }

            return result;
        }
    }

}