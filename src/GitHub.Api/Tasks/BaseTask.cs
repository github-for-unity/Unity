using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace GitHub.Unity
{
    abstract class BaseTask : ITask, IDisposable
    {
        protected BaseTask()
        {
            Logger = Logging.GetLogger(GetType());
        }

        protected ILogging Logger { get; private set; }

        public virtual bool Blocking { get; protected set; }
        public virtual bool Cached { get; protected set; }
        public virtual bool Critical { get; protected set; }
        public virtual bool Done { get; protected set; }
        public virtual string Label { get; protected set; }
        public virtual Action<ITask> OnBegin { get; set; }

        Action<ITask> onEnd;
        public Action<ITask> OnEnd
        {
            get
            {
                return onEnd;
            }
            set
            {
                onEnd = value;
            }
        }

        public virtual float Progress { get; protected set; }
        public virtual TaskQueueSetting Queued { get; protected set; }

        public virtual void Abort()
        {}

        public virtual void Disconnect()
        {}

        public virtual void Reconnect()
        {}

        public virtual void Run(CancellationToken cancel)
        {}

        public virtual Task<bool> RunAsync(CancellationToken cancellationToken)
        {
            return TaskEx.FromResult(true);
        }

        public virtual void WriteCache(TextWriter cache)
        {}

        bool disposed = false;
        public virtual void Dispose(bool disposing)
        {
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}