using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace GitHub.Unity
{
    class SimpleTask : BaseTask
    {
        private readonly Action action;
        private readonly TaskScheduler scheduler;

        public SimpleTask(Action action)
            : this(action, ThreadingHelper.TaskScheduler)
        {}

        public SimpleTask(Action action, TaskScheduler scheduler)
        {
            this.action = action;
            this.scheduler = scheduler;
        }

        public SimpleTask(Func<Task> runAction)
            : base(() => new Task<bool>(() =>
            {
                runAction().Wait();
                return true;
            }))
        {}

        public override void Run(CancellationToken cancellationToken)
        {
            if (action == null)
            {
                base.Run(cancellationToken);
                return;
            }

            if (cancellationToken.IsCancellationRequested)
                return;

            Task.Factory.StartNew(action, cancellationToken, TaskCreationOptions.None, scheduler).Wait();
            Progress = 1.0f;

            OnCompleted();

            RaiseOnEnd();
        }

        public override TaskQueueSetting Queued { get { return TaskQueueSetting.Queue; } }
    }

    class BaseTask : ITask, IDisposable
    {
        private readonly Func<Task<bool>> runAction;
        public event Action<ITask> OnBegin;
        public event Action<ITask> OnEnd;

        public BaseTask()
        {
            Logger = Logging.GetLogger(GetType());
        }

        public BaseTask(Func<Task<bool>> runAction)
            : this()
        {
            this.runAction = runAction;
        }

        public virtual void Abort()
        {}

        public virtual void Disconnect()
        {}

        public virtual void Reconnect()
        {}

        public virtual void Run(CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
                return;

            if (runAction != null)
            {
                var t = runAction();
                t.Start(ThreadingHelper.TaskScheduler);
                t.Wait(cancellationToken);
            }

            Progress = 1.0f;

            OnCompleted();

            RaiseOnEnd();
        }

        public virtual Task<bool> RunAsync(CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
                TaskEx.FromResult(false);

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

        public void RaiseOnBegin()
        {
            OnBegin?.Invoke(this);
        }

        public void RaiseOnEnd()
        {
            OnEnd?.Invoke(this);
            Done = true;
        }

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
        public virtual float Progress { get; protected set; }
        public object Result { get; protected set; }
    }
}
