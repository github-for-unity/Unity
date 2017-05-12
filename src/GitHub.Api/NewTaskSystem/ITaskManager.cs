using System;
using System.Threading;
using System.Threading.Tasks;

namespace GitHub.Unity
{
    interface ITaskManager
    {
        TaskScheduler ConcurrentScheduler { get; }
        TaskScheduler ExclusiveScheduler { get; }
        TaskScheduler UIScheduler { get; }
        CancellationToken Token { get; }

        void Schedule(params ITask[] tasks);
        T Schedule<T>(T task) where T : ITask;
        ITask Schedule(Action action);
        T ScheduleConcurrent<T>(T task) where T : ITask;
        T ScheduleExclusive<T>(T task) where T : ITask;
        ITask ScheduleUI(Action action);
        T ScheduleUI<T>(T task) where T : ITask;
        void Stop();
    }
}