using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace GitHub.Unity
{
    class ActionTask : TaskBase
    {
        protected Action<bool> Callback { get; }

        public ActionTask(CancellationToken token, Action<bool> action)
            : base(token)
        {
            Guard.ArgumentNotNull(action, "action");
            this.Callback = action;
        }

        public ActionTask(CancellationToken token, Action<bool> action, ITask dependsOn)
            : base(token, dependsOn)
        {
            Guard.ArgumentNotNull(action, "action");
            this.Callback = action;
        }

        public ActionTask(Task task)
            : base(task)
        {

        }

        protected override void Run(bool success)
        {
            Logger.Debug(String.Format("Executing id:{0}", Task.Id));

            RaiseOnStart();
            Callback?.Invoke(success);
            RaiseOnEnd();

            if (!success)
                throw DependsOn.Task.Exception.InnerException;
        }
    }

    class ActionTask<T> : TaskBase
    {
        protected Action<bool, T> Callback { get; }

        public ActionTask(CancellationToken token, Action<bool, T> action, ITask<T> dependsOn)
            : base(token, dependsOn)
        {
            Guard.ArgumentNotNull(action, "action");
            this.Callback = action;
            Task = new Task(() => Run(DependsOn.Successful, DependsOn.Successful ? ((ITask<T>)DependsOn).Result : default(T)),
                Token, TaskCreationOptions.None);
        }

        protected override void Run(bool success)
        {
            throw new NotImplementedException();
        }

        protected virtual void Run(bool success, T previousResult)
        {
            Logger.Debug(String.Format("Executing id:{0}", Task.Id));

            RaiseOnStart();
            Callback?.Invoke(success, previousResult);
            RaiseOnEnd();

            if (!success)
                throw DependsOn.Task.Exception.InnerException;
        }
    }

    class FuncTask<T> : TaskBase<T>
    {
        protected Func<bool, T> Callback { get; }

        public FuncTask(CancellationToken token, Func<bool, T> action)
            : base(token)
        {
            Guard.ArgumentNotNull(action, "action");
            this.Callback = action;
        }

        public FuncTask(CancellationToken token, Func<bool, T> action, ITask dependsOn)
            : base(token, dependsOn)
        {
            Guard.ArgumentNotNull(action, "action");
            this.Callback = action;
        }

        protected override T RunWithReturn(bool success)
        {
            Logger.Debug(String.Format("Executing id:{0}", Task.Id));

            RaiseOnStart();

            T result = default(T);
            result = Callback(success);
            RaiseOnEnd();

            if (!success)
                throw DependsOn.Task.Exception.InnerException;

            return result;
        }
    }

    class FuncTask<T, TResult> : TaskBase<T, TResult>
    {
        protected Func<bool, T, TResult> Callback { get; }

        public FuncTask(CancellationToken token, Func<bool, T, TResult> action, ITask<T> dependsOn)
            : base(token, dependsOn)
        {
            Guard.ArgumentNotNull(action, "action");
            this.Callback = action;
        }

        protected override TResult RunWithData(bool success, T previousResult)
        {
            Logger.Debug(String.Format("Executing id:{0}", Task.Id));

            RaiseOnStart();
            var result = Callback(success, previousResult);
            RaiseOnEnd();

            if (!success)
                throw DependsOn.Task.Exception.InnerException;

            return result;
        }
    }

    class FuncListTask<T> : ListTaskBase<List<T>, T>
    {
        protected Func<bool, List<T>> Callback { get; }

        public FuncListTask(CancellationToken token, Func<bool, List<T>> action)
            : base(token)
        {
            Guard.ArgumentNotNull(action, "action");
            this.Callback = action;
        }

        public FuncListTask(CancellationToken token, Func<bool, List<T>> action, ITask dependsOn)
            : base(token, dependsOn)
        {
            Guard.ArgumentNotNull(action, "action");
            this.Callback = action;
        }

        protected override List<T> RunWithReturn(bool success)
        {
            Logger.Debug(String.Format("Executing id:{0}", Task.Id));

            RaiseOnStart();

            List<T> result = null;
            result = Callback(success);
            RaiseOnEnd();
            if (result == null)
                result = new List<T>();

            if (!success)
                throw DependsOn.Task.Exception.InnerException;

            return result;
        }
    }

    class FuncListTask<TDependentResult, TResult, TData> : ListTaskBase<TDependentResult, TResult, TData>
    {
        protected Func<bool, TDependentResult, TResult> Callback { get; }

        public FuncListTask(CancellationToken token, Func<bool, TDependentResult, TResult> action, ITask<TDependentResult> dependsOn)
            : base(token, dependsOn)
        {
            Guard.ArgumentNotNull(action, "action");
            this.Callback = action;
        }

        protected override TResult RunWithData(bool success, TDependentResult previousResult)
        {
            Logger.Debug(String.Format("Executing id:{0}", Task.Id));

            RaiseOnStart();
            TResult result = default(TResult);
            result = Callback(success, previousResult);
            RaiseOnEnd();

            if (!success)
                throw DependsOn.Task.Exception.InnerException;

            return result;
        }
    }

}