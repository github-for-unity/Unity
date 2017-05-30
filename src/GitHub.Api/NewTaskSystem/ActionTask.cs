using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace GitHub.Unity
{
    class ActionTask : TaskBase
    {
        protected Action<bool> Callback { get; }
        protected Action<bool, Exception> CallbackWithException { get; }

        public ActionTask(CancellationToken token, Action<bool> action, ITask dependsOn = null, bool always = false)
            : base(token, dependsOn, always)
        {
            Guard.ArgumentNotNull(action, "action");
            this.Callback = action;
            Name = "ActionTask";
        }

        public ActionTask(CancellationToken token, Action<bool, Exception> action, ITask dependsOn = null, bool always = false)
            : base(token, dependsOn, always)
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

        protected override void Run(bool success)
        {
            base.Run(success);

            RaiseOnStart();
            Exception exception = null;
            try
            {
                Callback?.Invoke(success);
                if (CallbackWithException != null)
                {
                    Exception thrown = GetThrownException();
                    thrown = thrown != null ? thrown.InnerException : thrown;
                    CallbackWithException?.Invoke(success, thrown);
                }
            }
            catch (Exception ex)
            {
                Errors = ex.Message;
                exception = ex;
            }
            RaiseOnEnd();

            if (exception != null)
                throw exception;
        }
    }

    class ActionTask<T> : TaskBase
    {
        protected Action<bool, T> Callback { get; }
        protected Action<bool, Exception, T> CallbackWithException { get; }

        public ActionTask(CancellationToken token, Action<bool, T> action, ITask<T> dependsOn, bool always = false)
            : base(token, dependsOn, always)
        {
            Guard.ArgumentNotNull(action, "action");
            this.Callback = action;
            Task = new Task(() => Run(DependsOn.Successful, DependsOn.Successful ? ((ITask<T>)DependsOn).Result : default(T)),
                Token, TaskCreationOptions.None);
            Name = $"ActionTask<{typeof(T)}>";
        }

        public ActionTask(CancellationToken token, Action<bool, Exception, T> action, ITask<T> dependsOn, bool always = false)
            : base(token, dependsOn, always)
        {
            Guard.ArgumentNotNull(action, "action");
            this.CallbackWithException = action;
            Task = new Task(() => Run(DependsOn.Successful, DependsOn.Successful ? ((ITask<T>)DependsOn).Result : default(T)),
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
            Exception exception = null;
            try
            {
                Callback?.Invoke(success, previousResult);
                if (CallbackWithException != null)
                {
                    Exception thrown = GetThrownException();
                    thrown = thrown != null ? thrown.InnerException : thrown;
                    CallbackWithException?.Invoke(success, thrown, previousResult);
                }
            }
            catch (Exception ex)
            {
                Errors = ex.Message;
                exception = ex;
            }
            RaiseOnEnd();

            if (exception != null)
                throw exception;
        }
    }

    class FuncTask<T> : TaskBase<T>
    {
        protected Func<bool, T> Callback { get; }
        protected Func<bool, Exception, T> CallbackWithException { get; }

        public FuncTask(CancellationToken token, Func<bool, T> action, ITask dependsOn = null, bool always = false)
            : base(token, dependsOn, always)
        {
            Guard.ArgumentNotNull(action, "action");
            this.Callback = action;
            Name = $"FuncTask<{typeof(T)}>";
        }

        public FuncTask(CancellationToken token, Func<bool, Exception, T> action, ITask dependsOn = null, bool always = false)
            : base(token, dependsOn, always)
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

        protected override T RunWithReturn(bool success)
        {
            T result = base.RunWithReturn(success);

            RaiseOnStart();
            Exception exception = null;
            try
            {
                if (Callback != null)
                {
                    result = Callback(success);
                }
                else if (CallbackWithException != null)
                {
                    Exception thrown = GetThrownException();
                    thrown = thrown != null ? thrown.InnerException : thrown;
                    result = CallbackWithException(success, thrown);
                }
            }
            catch (Exception ex)
            {
                Errors = ex.Message;
                exception = ex;
            }
            RaiseOnEnd();

            if (exception != null)
                throw exception;

            return result;
        }
    }

    class FuncTask<T, TResult> : TaskBase<T, TResult>
    {
        protected Func<bool, T, TResult> Callback { get; }
        protected Func<bool, Exception, T, TResult> CallbackWithException { get; }

        public FuncTask(CancellationToken token, Func<bool, T, TResult> action, ITask<T> dependsOn = null, bool always = false)
            : base(token, dependsOn, always)
        {
            Guard.ArgumentNotNull(action, "action");
            this.Callback = action;
            Name = $"FuncTask<{typeof(T)}, {typeof(TResult)}>";
        }

        public FuncTask(CancellationToken token, Func<bool, Exception, T, TResult> action, ITask<T> dependsOn = null, bool always = false)
            : base(token, dependsOn, always)
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
            Exception exception = null;
            try
            {
                if (Callback != null)
                {
                    result = Callback(success, previousResult);
                }
                else if (CallbackWithException != null)
                {
                    Exception thrown = GetThrownException();
                    thrown = thrown != null ? thrown.InnerException : thrown;
                    result = CallbackWithException(success, thrown, previousResult);
                }
            }
            catch (Exception ex)
            {
                Errors = ex.Message;
                exception = ex;
            }
            RaiseOnEnd();

            if (exception != null)
                throw exception;

            return result;
        }
    }

    class FuncListTask<T> : DataTaskBase<T, List<T>>
    {
        protected Func<bool, List<T>> Callback { get; }
        protected Func<bool, Exception, List<T>> CallbackWithException { get; }

        public FuncListTask(CancellationToken token, Func<bool, List<T>> action, ITask dependsOn = null, bool always = false)
            : base(token, dependsOn, always)
        {
            Guard.ArgumentNotNull(action, "action");
            this.Callback = action;
        }

        public FuncListTask(CancellationToken token, Func<bool, Exception, List<T>> action, ITask dependsOn = null, bool always = false)
            : base(token, dependsOn, always)
        {
            Guard.ArgumentNotNull(action, "action");
            this.CallbackWithException = action;
        }

        public FuncListTask(Task<List<T>> task)
            : base(task)
        { }

        protected override List<T> RunWithReturn(bool success)
        {
            var result = base.RunWithReturn(success);

            RaiseOnStart();
            Exception exception = null;
            try
            {
                if (Callback != null)
                {
                    result = Callback(success);
                }
                else if (CallbackWithException != null)
                {
                    Exception thrown = GetThrownException();
                    thrown = thrown != null ? thrown.InnerException : thrown;
                    result = CallbackWithException(success, thrown);
                }
            }
            catch (Exception ex)
            {
                Errors = ex.Message;
                exception = ex;
            }
            RaiseOnEnd();

            if (exception != null)
                throw exception;

            if (result == null)
                result = new List<T>();

            return result;
        }
    }

    class FuncListTask<T, TData, TResult> : DataTaskBase<T, TData, List<TResult>>
    {
        protected Func<bool, T, List<TResult>> Callback { get; }
        protected Func<bool, Exception, T, List<TResult>> CallbackWithException { get; }

        public FuncListTask(CancellationToken token, Func<bool, T, List<TResult>> action, ITask<T> dependsOn = null, bool always = false)
            : base(token, dependsOn, always)
        {
            Guard.ArgumentNotNull(action, "action");
            this.Callback = action;
        }

        public FuncListTask(CancellationToken token, Func<bool, Exception, T, List<TResult>> action, ITask<T> dependsOn = null, bool always = false)
            : base(token, dependsOn, always)
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
            Exception exception = null;
            try
            {
                if (Callback != null)
                {
                    result = Callback(success, previousResult);
                }
                else if (CallbackWithException != null)
                {
                    Exception thrown = GetThrownException();
                    thrown = thrown != null ? thrown.InnerException : thrown;
                    result = CallbackWithException(success, thrown, previousResult);
                }
            }
            catch (Exception ex)
            {
                Errors = ex.Message;
                exception = ex;
            }
            RaiseOnEnd();

            if (exception != null)
                throw exception;

            return result;
        }
    }

}