using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace GitHub.Unity
{
    interface ITask
    {
        void Run(CancellationToken cancellationToken);
        Task<bool> RunAsync(CancellationToken cancellationToken);
        void Abort();
        void Disconnect();
        void Reconnect();
        void WriteCache(TextWriter cache);
        void RaiseOnBegin();
        void RaiseOnEnd();
        bool Blocking { get; }
        float Progress { get; }
        bool Done { get; }
        TaskQueueSetting Queued { get; }
        bool Critical { get; }
        bool Cached { get; }
        string Label { get; }
        object Result { get; }
        event Action<ITask> OnBegin;
        event Action<ITask> OnEnd;
    }

    interface ITask<T> : ITask
    {
        T TaskResult { get; }
    }
}