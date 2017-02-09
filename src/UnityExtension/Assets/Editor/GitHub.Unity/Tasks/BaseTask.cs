using System;
using System.IO;

namespace GitHub.Unity
{
    abstract class BaseTask : ITask, IDisposable
    {
        public virtual bool Blocking { get; protected set; }
        public virtual bool Cached { get; protected set; }
        public virtual bool Critical { get; protected set; }
        public virtual bool Done { get; protected set; }
        public virtual string Label { get; protected set; }
        public virtual Action<ITask> OnBegin { get; set; }
        public virtual Action<ITask> OnEnd { get; set; }
        public virtual float Progress { get; protected set; }
        public virtual TaskQueueSetting Queued { get; protected set; }

        public virtual void Abort()
        {}

        public virtual void Disconnect()
        {}

        public virtual void Reconnect()
        {}

        public virtual void Run()
        {}

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