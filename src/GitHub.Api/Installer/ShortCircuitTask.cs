using System;
using System.Threading;

namespace GitHub.Unity
{
    class ShortCircuitTask<TResult> : TaskBase<TResult, TResult> where TResult : class 
    {
        private readonly Func<TResult> action;

        public ShortCircuitTask(CancellationToken token, TaskBase<TResult> funcTask) : base(token)
        {
            action = () => funcTask.Start().Result;
        }

        public ShortCircuitTask(CancellationToken token, Func<TResult> action) : base(token)
        {
            this.action = action;
        }

        protected override TResult RunWithData(bool success, TResult previousResult)
        {
            base.RunWithData(success, previousResult);

            if (success && previousResult != null)
            {
                return previousResult;
            }

            return action();
        }
    }
}