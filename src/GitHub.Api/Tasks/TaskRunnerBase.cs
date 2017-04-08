using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace GitHub.Unity
{
    enum WaitMode
    {
        Background,
        Modal,
        Blocking
    };

    class TaskRunnerBase : ITaskRunner
    {
        private const string TaskThreadExceptionRestartError = "GitHub task thread restarting after encountering an exception: {0}";
        private const int FailureDelayDefault = 1;
        private const int FailureDelayLong = 5000;
        private const int NoTasksSleep = 15;
        private const string TaskCacheParseError = "GitHub: Failed to parse task cache";

        protected readonly object tasksLock = new object();

        protected ConcurrentQueue<ITask> Tasks { get; private set; }

        protected ITask activeTask;

        protected readonly IMainThreadSynchronizationContext context;
        protected readonly CancellationToken cancellationToken;

        protected Task MainLoop { get; private set; }

        protected bool running;

        private Exception lastException;
        protected ConcurrentQueue<Action> ScheduledCalls { get; private set; }
        private bool readyForMoreCalls = true;
        private int callerFlag = 0;
        private double lastMainThreadCall = 0;
        protected ILogging Logger { get; private set; }
        protected static TaskRunnerBase Instance { get; private set; }
        protected static string CacheFilePath { get; set; }

        public TaskRunnerBase(IMainThreadSynchronizationContext context, CancellationToken cancellationToken)
        {
            Tasks = new ConcurrentQueue<ITask>();
            ScheduledCalls = new ConcurrentQueue<Action>();
            Logger = Logging.GetLogger(GetType());
            this.context = context;
            this.cancellationToken = cancellationToken;

            Instance = this;

            //if (File.Exists(CacheFilePath))
            //{
            //    ReadCache();
            //    File.Delete(CacheFilePath);

            //    OnSessionRestarted();
            //}
        }

        public void AddTask(ITask task)
        {
            lock (tasksLock)
            {
                if ((task.Queued == TaskQueueSetting.NoQueue && Tasks.Count > 0) ||
                (task.Queued == TaskQueueSetting.QueueSingle &&
                ((activeTask != null && activeTask.GetType() == task.GetType()) ||
                    Tasks.Any(t => t.GetType() == task.GetType()))))
                {
                    return;
                }

                Logger.Trace("Adding Task: \"{0}\" Label: \"{1}\" ActiveTask: \"{2}\"", task.GetType().Name, task.Label, activeTask);
                Tasks.Enqueue(task);
            }

            //WriteCache();

        }

        public void Run()
        {
            MainLoop = Task.Factory.StartNew(Start, cancellationToken, TaskCreationOptions.None, ThreadingHelper.TaskScheduler);
        }

        private void Start()
        {
            SynchronizationContext.SetSynchronizationContext(context as SynchronizationContext);
            while (true)
            {
                try
                {
                    RunLoop();

                    break;
                }
                // Aborted by domain unload or explicitly via the editor quit handler. Button down the hatches.
                catch (ThreadAbortException)
                {
                    running = false;

                    // Disconnect or abort the active task
                    if (activeTask != null && !activeTask.Done)
                    {
                        if (activeTask.Cached)
                        {
                            try
                            {
                                activeTask.Disconnect();
                            }
                            finally
                            {
                                activeTask = null;
                            }
                        }
                        else
                        {
                            try
                            {
                                activeTask.Abort();
                            }
                            finally
                            {
                                activeTask = null;
                            }
                        }
                    }

                    break;
                }
                // Something broke internally - reboot
                catch (Exception e)
                {
                    running = false;
                    var repeat = lastException != null && e.TargetSite.Equals(lastException.TargetSite);
                    lastException = e;

                    if (!repeat)
                    {
                        Logger.Error(TaskThreadExceptionRestartError, e);
                        Thread.Sleep(FailureDelayDefault);
                    }
                    else
                    {
                        Thread.Sleep(FailureDelayLong);
                    }
                }
            }
        }

        private void RunLoop()
        {
            running = true;

            while (running)
            {
                // Clear any completed task
                if (activeTask != null && activeTask.Done)
                {
                    activeTask = null;
                }

                var runningNewTask = activeTask == null;
                // Grab a new task
                if (runningNewTask)
                {
                    lock (tasksLock)
                    {
                        if (Tasks.Count > 0)
                        {
                            if (!Tasks.TryDequeue(out activeTask))
                            {
                                Logger.Error("Could not dequeue task");
                            }
                            Logger.Trace("Dequeued new task {0}", activeTask.Label);
                            //activeTask.OnBegin = task => ScheduleMainThread(WriteCache);
                            if (activeTask.Blocking)
                            {
                                activeTask.OnEnd += t => {
                                    Logger.Trace("Task {0} ended", activeTask.Label);
                                    activeTask = null;
                                    OnWaitingModalTaskEnd(t);
                                };
                            }
                            else
                            {
                                activeTask.OnEnd += t => {
                                    Logger.Trace("Task {0} ended", activeTask.Label);
                                    activeTask = null;
                                    OnWaitingBackgroundTaskEnd(t);
                                };
                            }
                        }
                    }
                }

                // Run and monitor active task
                if (activeTask != null && runningNewTask)
                {
                    ScheduleMainThreadInternal(() =>
                    {
                        if (activeTask != null)
                        {
                            WaitForTask(activeTask, activeTask.Blocking ? WaitMode.Modal : WaitMode.Background);
                        }
                    });

                    Task.Factory.StartNew(() =>
                    {
                        try
                        {
                            activeTask.Run(cancellationToken);
                        }
                        catch (Exception ex)
                        {
                            Logger.Error(ex);
                            activeTask.RaiseOnEnd();
                            activeTask = null;
                        }
                    }, cancellationToken, TaskCreationOptions.None, ThreadingHelper.TaskScheduler);
                    //WriteCache();
                }

                PumpMainThread();
                Thread.Sleep(NoTasksSleep);
            }
        }

        protected virtual void WaitForTask(ITask task, WaitMode mode = WaitMode.Background)
        {
        }

        protected virtual void OnWaitingBackgroundTaskEnd(ITask task)
        {
        }

        protected virtual void OnWaitingModalTaskEnd(ITask task)
        {
        }

        public static void ScheduleMainThread(Action action)
        {
            Instance.ScheduleMainThreadInternal(action);
        }

        protected void ScheduleMainThreadInternal(Action action)
        {
            ScheduledCalls.Enqueue(action);
            var ellapsed = TimeSpan.FromTicks(DateTime.Now.Ticks).TotalMilliseconds;
            //logger.Trace("QueuedAction ReadyForMore:{0} Delta:{1}ms LastCall:{2}ms ThisCall:{3}ms", readyForMoreCalls, ellapsed - lastMainThreadCall, lastMainThreadCall, ellapsed);
            PumpMainThread();
        }

        private void PumpMainThread()
        {
            var ellapsed = TimeSpan.FromTicks(DateTime.Now.Ticks).TotalMilliseconds;

            //logger.Debug("Pumping {0} {1} {2}", readyForMoreCalls, scheduledCalls.Count, (int)(ellapsed - lastMainThreadCall));

            if (readyForMoreCalls && ScheduledCalls.Count > 0 &&
                (lastMainThreadCall == 0 || 33 < ellapsed - lastMainThreadCall))
            {
                if (Interlocked.CompareExchange(ref callerFlag, 1, 0) == 0)
                {
                    lastMainThreadCall = ellapsed;
                    readyForMoreCalls = false;
                    //logger.Trace("Scheduling a thing");
                    context.Schedule(() =>
                    {
                        //logger.Trace("Executing actions");
                        readyForMoreCalls = true;
                        Action act = null;
                        while (ScheduledCalls.TryDequeue(out act))
                        {
                            act?.Invoke();
                        }
                    });
                    callerFlag = 0;
                }
            }
        }

        public static void Add(ITask task)
        {
            Instance.AddTask(task);
        }

        public static void ReportSuccess(Action callback)
        {
            ScheduleMainThread(callback);
        }

        private bool ReadCache()
        {
            var text = File.ReadAllText(CacheFilePath);

            object parseResult;
            IList<object> cache;

            // Parse root list with at least one item (active task) or fail
            if (!SimpleJson.TryDeserializeObject(text, out parseResult) || (cache = parseResult as IList<object>) == null || cache.Count < 1)
            {
                Logger.Error(TaskCacheParseError);
                return false;
            }

            // Parse active task
            var taskData = cache[0] as IDictionary<string, object>;
            var cachedActiveTask = taskData != null ? ParseTask(taskData) : null;

            // Parse tasks list or fail
            var cachedTasks = new ConcurrentQueue<ITask>();
            for (var index = 1; index < cache.Count; ++index)
            {
                taskData = cache[index] as IDictionary<string, object>;

                if (taskData == null)
                {
                    Logger.Error(TaskCacheParseError);
                    return false;
                }

                cachedTasks.Enqueue(ParseTask(taskData));
            }

            // Apply everything only after fully successful parse
            activeTask = cachedActiveTask;
            Tasks = cachedTasks;

            return true;
        }

        private ITask ParseTask(IDictionary<string, object> data)
        {
            //CachedTask type;

            //try
            //{
            //    type = (CachedTask)Enum.Parse(typeof(CachedTask), (string)data[TypeKey]);
            //}
            //catch (Exception)
            //{
            //    return null;
            //}

            //try
            //{
            //    switch (type)
            //    {
            //        case CachedTask.ProcessTask:
            //            return ProcessTask.Parse(
            //                EntryPoint.Environment, EntryPoint.ProcessManager, EntryPoint.TaskResultDispatcher,
            //                data);
            //        default:
            //            logger.Error(TaskParseUnhandledTypeError, type);
            //            return null;
            //    }
            //}
            //catch (Exception)
            //{
            //    return null;
            //}

            return null;
        }
    }
}