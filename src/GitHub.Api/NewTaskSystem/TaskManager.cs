using System;
using System.Threading;
using System.Threading.Tasks;

namespace GitHub.Unity
{
    class TaskManager : ITaskManager
    {
        private static readonly ILogging logger = Logging.GetLogger<ProcessManager>();

        private readonly TaskScheduler uiScheduler;
        private readonly CancellationTokenSource cts;
        private readonly ConcurrentExclusiveInterleave manager;
        public TaskScheduler UIScheduler { get { return uiScheduler; } }
        public TaskScheduler ConcurrentScheduler { get { return manager.ConcurrentTaskScheduler; } }
        public TaskScheduler ExclusiveScheduler { get { return manager.ExclusiveTaskScheduler; } }
        public CancellationToken Token { get { return cts.Token; } }

        private static ITaskManager instance;
        public static ITaskManager Instance => instance;

        public TaskManager(TaskScheduler uiScheduler)
        {
            cts = new CancellationTokenSource();
            this.manager = new ConcurrentExclusiveInterleave(cts.Token);
            this.uiScheduler = uiScheduler;
            instance = this;
        }

        public void Stop()
        {
            cts.Cancel();
            manager.Wait();
        }

        public static TaskScheduler GetScheduler(TaskAffinity affinity)
        {
            switch (affinity)
            {
                case TaskAffinity.Exclusive:
                    return Instance.ExclusiveScheduler;
                case TaskAffinity.UI:
                    return Instance.UIScheduler;
                case TaskAffinity.Concurrent:
                default:
                    return Instance.ConcurrentScheduler;
            }
        }


        public void Schedule(params ITask[] tasks)
        {
            Guard.ArgumentNotNull(tasks, "tasks");

            var enumerator = tasks.GetEnumerator();
            bool isLast = !enumerator.MoveNext();
            do
            {
                var task = enumerator.Current as ITask;
                isLast = !enumerator.MoveNext();
                Schedule(task, isLast);
            } while (!isLast);
        }

        public T Schedule<T>(T task)
            where T : ITask
        {
            return Schedule(task, true);
        }

        private T Schedule<T>(T task, bool setupFaultHandler)
            where T : ITask
        {
            switch (task.Affinity)
            {
                case TaskAffinity.Exclusive:
                    return ScheduleExclusive(task, setupFaultHandler);
                case TaskAffinity.UI:
                    return ScheduleUI(task, setupFaultHandler);
                case TaskAffinity.Concurrent:
                default:
                    return ScheduleConcurrent(task, setupFaultHandler);
            }
        }

        public ITask Schedule(Action action)
        {
            return Schedule(new ActionTask(cts.Token, action));
        }

        public ITask ScheduleUI(Action action)
        {
            return ScheduleUI(new ActionTask(cts.Token, action));
        }

        public T ScheduleUI<T>(T task)
            where T : ITask
        {
            return ScheduleUI(task, true);
        }

        private T ScheduleUI<T>(T task, bool setupFaultHandler)
            where T : ITask
        {
            if (setupFaultHandler)
            {
                task.Task.ContinueWith(tt =>
                {
                    logger.Error(tt.Exception.InnerException, String.Format("Exception on ui thread: {0} {1}", tt.Id, task.Name));
                },
                    cts.Token,
                    TaskContinuationOptions.OnlyOnFaulted, uiScheduler
                );
            }
            logger.Trace(String.Format("Schedule {0} {1}", "UI", task.Task.Id));
            return (T)task.Start(uiScheduler);
        }

        public T ScheduleExclusive<T>(T task)
            where T : ITask
        {
            return ScheduleExclusive(task, true);
        }

        private T ScheduleExclusive<T>(T task, bool setupFaultHandler)
            where T : ITask
        {
            if (setupFaultHandler)
            {
                task.Task.ContinueWith(tt =>
                {
                    logger.Error(tt.Exception.InnerException, String.Format("Exception on exclusive thread: {0} {1}", tt.Id, task.Name));
                },
                    cts.Token,
                    TaskContinuationOptions.OnlyOnFaulted, uiScheduler
                );
            }
            logger.Trace(String.Format("Schedule {0} {1}", "Exclusive", task.Task.Id));
            return (T)task.Start(manager.ExclusiveTaskScheduler);
        }

        public T ScheduleConcurrent<T>(T task)
            where T : ITask
        {
            return ScheduleConcurrent(task, true);
        }

        private T ScheduleConcurrent<T>(T task, bool setupFaultHandler)
            where T : ITask
        {
            if (setupFaultHandler)
            {
                task.Task.ContinueWith(tt =>
                {
                    logger.Error(tt.Exception.InnerException, String.Format("Exception on concurrent thread: {0} {1}", tt.Id, task.Name));
                },
                    cts.Token,
                    TaskContinuationOptions.OnlyOnFaulted, uiScheduler
                );
            }
            logger.Trace(String.Format("Schedule {0} {1}", "Concurrent", task.Task.Id));
            return (T)task.Start(manager.ConcurrentTaskScheduler);
        }
    }
}