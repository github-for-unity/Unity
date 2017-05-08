using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace GitHub.Unity
{
    class ActionTask : TaskBase
    {
        protected Action Callback { get; }
        protected Action CallbackIfDependentFailed { get; }

        public ActionTask(CancellationToken token, Action action)
            : base(token)
        {
            Guard.ArgumentNotNull(action, "action");

            this.Callback = action;
        }

        public ActionTask(CancellationToken token, Action action, ITask dependsOn)
            : this(token, action)
        {
        }

        public ActionTask(CancellationToken token, Action action, Action actionIfDependentFailed, ITask dependsOn)
            : this(token, action, dependsOn)
        {
            Guard.ArgumentNotNull(actionIfDependentFailed, "actionIfDependentFailed");

            this.CallbackIfDependentFailed = actionIfDependentFailed;
        }

        protected override void Run()
        {
            Logger.Debug(String.Format("Executing id:{0}", Task.Id));

            RaiseOnStart();

            if (DependsOn != null && !DependsOn.Successful)
            {
                CallbackIfDependentFailed?.Invoke();
            }
            else
            {
                Callback?.Invoke();
            }
            RaiseOnEnd();
        }
    }

    class ActionTask<T> : TaskBase
    {
        protected Action<T> Callback { get; }
        protected Action CallbackIfDependentFailed { get; }

        public ActionTask(CancellationToken token, Action<T> action)
            : base(token)
        {
            Guard.ArgumentNotNull(token, "token");
            Guard.ArgumentNotNull(action, "action");

            this.Callback = action;
            Task = new Task(o => RunWithData(((Lazy<T>)o).Value), new Lazy<T>(() => ((ITask<T>)DependsOn).Result), Token, TaskCreationOptions.None);
        }

        public ActionTask(CancellationToken token, Action<T> action, ITask<T> dependsOn)
            : base(token, dependsOn)
        {
            Guard.ArgumentNotNull(token, "token");
            Guard.ArgumentNotNull(action, "action");
            Guard.ArgumentNotNull(dependsOn, "dependsOn");

            this.Callback = action;
            Task = new Task(o => RunWithData(((Lazy<T>)o).Value), new Lazy<T>(() => ((ITask<T>)DependsOn).Result), Token, TaskCreationOptions.None);
        }

        public ActionTask(CancellationToken token, Action<T> action, Action actionIfDependentFailed, ITask<T> dependsOn)
            : base(token, dependsOn)
        {
            Guard.ArgumentNotNull(token, "token");
            Guard.ArgumentNotNull(action, "action");
            Guard.ArgumentNotNull(actionIfDependentFailed, "actionIfDependentFailed");
            Guard.ArgumentNotNull(dependsOn, "dependsOn");

            this.Callback = action;
            this.CallbackIfDependentFailed = actionIfDependentFailed;
            Task = new Task(o => RunWithData(((Lazy<T>)o).Value), new Lazy<T>(() => ((ITask<T>)DependsOn).Result), Token, TaskCreationOptions.None);
        }

        protected virtual void RunWithData(T previousResult)
        {
            Logger.Debug(String.Format("Executing id:{0}", Task.Id));

            RaiseOnStart();

            if (DependsOn != null && !DependsOn.Successful)
            {
                CallbackIfDependentFailed?.Invoke();
            }
            else
            {
                Callback?.Invoke(previousResult);
            }
            RaiseOnEnd();
        }
    }

    class FuncTask<T> : TaskBase<T>
    {
        protected Func<T> Callback { get; }
        protected Func<T> CallbackIfDependentFailed { get; }

        public FuncTask(CancellationToken token, Func<T> action) : base(token)
        {
            Guard.ArgumentNotNull(token, "token");
            Guard.ArgumentNotNull(action, "action");

            this.Callback = action;
        }

        public FuncTask(CancellationToken token, Func<T> action, ITask dependsOn)
            : base(token, dependsOn)
        {
            Guard.ArgumentNotNull(token, "token");
            Guard.ArgumentNotNull(action, "action");
            Guard.ArgumentNotNull(dependsOn, "dependsOn");

            this.Callback = action;
        }

        public FuncTask(CancellationToken token, Func<T> action, Func<T> actionIfDependentFailed, ITask dependsOn)
            : base(token, dependsOn)
        {
            Guard.ArgumentNotNull(token, "token");
            Guard.ArgumentNotNull(action, "action");
            Guard.ArgumentNotNull(actionIfDependentFailed, "actionIfDependentFailed");
            Guard.ArgumentNotNull(dependsOn, "dependsOn");

            this.Callback = action;
            this.CallbackIfDependentFailed = actionIfDependentFailed;
        }

        protected override T RunWithReturn()
        {
            Logger.Debug(String.Format("Executing id:{0}", Task.Id));

            RaiseOnStart();

            T result = default(T);
            if (DependsOn != null && !DependsOn.Successful)
            {
                result = CallbackIfDependentFailed();
            }
            else
            {
                result = Callback();
            }
            RaiseOnEnd();
            return result;
        }
    }

    class FuncTask<TDependentResult, T> : TaskBase<TDependentResult, T>
    {
        protected Func<TDependentResult, T> Callback { get; }
        protected Func<T> CallbackIfDependentFailed { get; }

        public FuncTask(CancellationToken token, Func<TDependentResult, T> action, ITask<TDependentResult> dependsOn)
            : base(token, dependsOn)
        {
            Guard.ArgumentNotNull(token, "token");
            Guard.ArgumentNotNull(action, "action");
            Guard.ArgumentNotNull(dependsOn, "dependsOn");

            this.Callback = action;
        }

        public FuncTask(CancellationToken token, Func<TDependentResult, T> action, Func<T> actionIfDependentFailed, ITask<TDependentResult> dependsOn)
            : base(token, dependsOn)
        {
            Guard.ArgumentNotNull(token, "token");
            Guard.ArgumentNotNull(action, "action");
            Guard.ArgumentNotNull(actionIfDependentFailed, "actionIfDependentFailed");
            Guard.ArgumentNotNull(dependsOn, "dependsOn");

            this.Callback = action;
            this.CallbackIfDependentFailed = actionIfDependentFailed;
        }

        protected override T RunWithData(TDependentResult previousResult)
        {
            Logger.Debug(String.Format("Executing id:{0}", Task.Id));

            RaiseOnStart();

            T result = default(T);
            if (DependsOn != null && !DependsOn.Successful)
            {
                if (CallbackIfDependentFailed != null)
                    result = CallbackIfDependentFailed();
            }
            else
            {
                result = Callback(previousResult);
            }
            RaiseOnEnd();
            return result;
        }
    }

    class FuncListTask<T> : ListTaskBase<List<T>, T>
    {
        protected Func<List<T>> Callback { get; }
        protected Func<List<T>> CallbackIfDependentFailed { get; }

        public FuncListTask(CancellationToken token, Func<List<T>> action)
            : base(token)
        {
            Guard.ArgumentNotNull(token, "token");
            Guard.ArgumentNotNull(action, "action");

            this.Callback = action;
        }

        public FuncListTask(CancellationToken token, Func<List<T>> action, ITask dependsOn)
            : base(token, dependsOn)
        {
            Guard.ArgumentNotNull(token, "token");
            Guard.ArgumentNotNull(action, "action");
            Guard.ArgumentNotNull(dependsOn, "dependsOn");

            this.Callback = action;
        }

        public FuncListTask(CancellationToken token, Func<List<T>> action, Func<List<T>> actionIfDependentFailed, ITask dependsOn)
            : base(token, dependsOn)
        {
            Guard.ArgumentNotNull(token, "token");
            Guard.ArgumentNotNull(action, "action");
            Guard.ArgumentNotNull(actionIfDependentFailed, "actionIfDependentFailed");
            Guard.ArgumentNotNull(dependsOn, "dependsOn");

            this.Callback = action;
            this.CallbackIfDependentFailed = actionIfDependentFailed;
        }

        protected override List<T> RunWithReturn()
        {
            Logger.Debug(String.Format("Executing id:{0}", Task.Id));

            RaiseOnStart();

            List<T> result = null;
            if (DependsOn != null && !DependsOn.Successful)
            {
                result = CallbackIfDependentFailed();
            }
            else
            {
                result = Callback();
            }
            RaiseOnEnd();
            if (result == null)
                result = new List<T>();
            return result;
        }
    }

    class FuncListTask<TDependentResult, T, TData> : ListTaskBase<TDependentResult, T, TData>
    {
        protected Func<TDependentResult, T> Callback { get; }
        protected Func<TDependentResult, T> CallbackIfDependentFailed { get; }

        public FuncListTask(CancellationToken token, Func<TDependentResult, T> action, ITask<TDependentResult> dependsOn)
            : base(token, dependsOn)
        {
            Guard.ArgumentNotNull(token, "token");
            Guard.ArgumentNotNull(action, "action");
            Guard.ArgumentNotNull(dependsOn, "dependsOn");

            this.Callback = action;
        }

        public FuncListTask(CancellationToken token, Func<TDependentResult, T> action, Func<TDependentResult, T> actionIfDependentFailed, ITask<TDependentResult> dependsOn)
            : base(token, dependsOn)
        {
            Guard.ArgumentNotNull(token, "token");
            Guard.ArgumentNotNull(action, "action");
            Guard.ArgumentNotNull(actionIfDependentFailed, "actionIfDependentFailed");
            Guard.ArgumentNotNull(dependsOn, "dependsOn");

            this.Callback = action;
            this.CallbackIfDependentFailed = actionIfDependentFailed;
        }

        protected override T RunWithData(TDependentResult previousResult)
        {
            Logger.Debug(String.Format("Executing id:{0}", Task.Id));

            RaiseOnStart();

            T result = default(T);
            if (DependsOn != null && !DependsOn.Successful)
            {
                if (CallbackIfDependentFailed != null)
                    result = CallbackIfDependentFailed(previousResult);
            }
            else
            {
                result = Callback(previousResult);
            }
            RaiseOnEnd();
            return result;
        }
    }

}