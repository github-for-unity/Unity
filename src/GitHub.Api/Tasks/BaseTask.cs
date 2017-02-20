using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace GitHub.Unity
{
    class BaseTask : ITask, IDisposable
    {
        private readonly Func<Task<bool>> runAction;

        public BaseTask()
        {
            Logger = Logging.GetLogger(GetType());
        }

        public BaseTask(Func<Task<bool>> runAction)
        {
            this.runAction = runAction;
        }

        public virtual void Abort()
        {}

        public virtual void Disconnect()
        {}

        public virtual void Reconnect()
        {}

        public virtual void Run(CancellationToken cancel)
        {
            if (runAction != null)
                runAction();
        }

        public virtual Task<bool> RunAsync(CancellationToken cancellationToken)
        {
            if (runAction != null)
            {
                return runAction();
            }
            return TaskEx.FromResult(true);
        }

        public virtual void WriteCache(TextWriter cache)
        {}

        protected virtual void OnCompleted()
        {}

        public virtual void Dispose(bool disposing)
        {}

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected ILogging Logger { get; private set; }

        public virtual bool Blocking { get; set; }
        public virtual bool Cached { get; set; }
        public virtual bool Critical { get; set; }
        public virtual bool Done { get; protected set; }
        public virtual string Label { get; set; }
        public virtual TaskQueueSetting Queued { get; set; }
        public virtual Action<ITask> OnBegin { get; set; }

        public Action<ITask> OnEnd { get; set; }

        public virtual float Progress { get; protected set; }
    }
}
