using System;
using System.IO;

namespace GitHub.Unity
{
    interface ITask
    {
        void Run();
        void Abort();
        void Disconnect();
        void Reconnect();
        void WriteCache(TextWriter cache);
        bool Blocking { get; }
        float Progress { get; }
        bool Done { get; }
        TaskQueueSetting Queued { get; }
        bool Critical { get; }
        bool Cached { get; }
        Action<ITask> OnBegin { set; }
        Action<ITask> OnEnd { set; }
        string Label { get; }
    };
}