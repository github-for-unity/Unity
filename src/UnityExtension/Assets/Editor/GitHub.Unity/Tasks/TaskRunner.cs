using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using UnityEditor;

/*

    Scenarios:
    Quitting mid-operation both a Blocking and a Critical task.
    Implement escalating re-start delay on the main queue pumpt.
    Detect bad state quit - prompt to resume or halt operations.
    Detect git lock, offer to run cleanup or halt operations.
    Implement ability to resume after halted operations.
    Implement ability to skip a long fetch operation, so we can keep working.

*/

namespace GitHub.Unity
{
    class TaskQueueScheduler : ITaskQueueScheduler
    {
        public void Queue(ITask task)
        {
            TaskRunner.Add(task);
        }
    }
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

        protected readonly MainThreadSynchronizationContextBase context;
        protected readonly CancellationToken cancellationToken;

        protected Thread Thread { get; private set; }

        protected bool running;

        private Exception lastException;
        protected ConcurrentQueue<Action> ScheduledCalls { get; private set; }
        private bool readyForMoreCalls = true;
        private int callerFlag = 0;
        private double lastMainThreadCall = 0;
        protected ILogging Logger { get; private set; }
        protected static TaskRunnerBase Instance { get; private set; }
        protected static string CacheFilePath { get; set; }

        public TaskRunnerBase(MainThreadSynchronizationContextBase context, CancellationToken cancellationToken)
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
            Thread = new Thread(Start);
            Thread.Start();
        }

        private void Start()
        {
            SynchronizationContext.SetSynchronizationContext(context);
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
                                    activeTask = null;
                                    OnWaitingModalTaskEnd(t);
                                };
                            }
                            else
                            {
                                activeTask.OnEnd += t => {
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
                            //WaitForTask(activeTask, activeTask.Blocking ? WaitMode.Modal : WaitMode.Background);
                        }
                    });

                    ThreadPool.QueueUserWorkItem(_ =>
                    {
                        try
                        {
                            activeTask.Run(cancellationToken);
                        }
                        catch (Exception ex)
                        {
                            Logger.Error(ex);
                            activeTask.OnEnd.SafeInvoke(activeTask);
                            activeTask = null;
                        }
                    });
                    //WriteCache();
                }

                PumpMainThread();
                Thread.Sleep(TaskRunner.NoTasksSleep);
            }

            Thread.Abort();
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
                            act.SafeInvoke();
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
            TaskRunner.ScheduleMainThread(callback);
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

    class TaskRunner : TaskRunnerBase
    {
        internal const string TypeKey = "type";

        private const int BlockingTaskWaitSleep = 10;
        private const string CacheFileName = "GitHubCache";
        private const string TaskCacheWriteExceptionError = "GitHub: Exception when writing task cache: {0}";
        private const string TaskParseUnhandledTypeError = "GitHub: Trying to parse unhandled cached task: {0}";
        private const string TaskFailureTitle = "GitHub";
        private const string TaskFailureMessage = "{0} failed:\n{1}";
        private const string TaskFailureOK = "OK";
        private const string TaskProgressTitle = "GitHub";
        private const string TaskBlockingTitle = "Critical GitHub task";
        private const string TaskBlockingDescription = "A critical GitHub task ({0}) has yet to complete. What would you like to do?";
        private const string TaskBlockingComplete = "Complete";
        private const string TaskBlockingInterrupt = "Interrupt";

        private static ProgressBarDisplayMethod displayBackgroundProgressBar;
        private static Action clearBackgroundProgressBar;

        public TaskRunner(MainThreadSynchronizationContextBase context, CancellationToken cancellationToken)
            : base(context, cancellationToken)
        {
        }

        public void Shutdown()
        {
            // Stop the queue
            running = false;

            if (activeTask != null && activeTask.Critical)
            {
                WaitForTask(activeTask, WaitMode.Blocking);
            }
        }

        public static void ReportFailure(FailureSeverity severity, string title, string error)
        {
            if (severity == FailureSeverity.Moderate)
            {
                Logging.Error("Failure: \"{0}\" Reason:\"{1}\"", title, error);
            }
            else
            {
                ScheduleMainThread(() => EditorUtility.DisplayDialog(TaskFailureTitle, String.Format(TaskFailureMessage, title, error), TaskFailureOK));
            }
        }

        private static void DisplayBackgroundProgressBar(string description, float progress)
        {
            if (displayBackgroundProgressBar == null)
            {
                var type = typeof(EditorWindow).Assembly.GetType("UnityEditor.AsyncProgressBar");
                displayBackgroundProgressBar =
                    (ProgressBarDisplayMethod)
                        Delegate.CreateDelegate(typeof(ProgressBarDisplayMethod),
                            type.GetMethod("Display", new Type[] { typeof(string), typeof(float) }));
            }
            displayBackgroundProgressBar(description, progress);
        }

        private static void ClearBackgroundProgressBar()
        {
            if (clearBackgroundProgressBar == null)
            {
                var type = typeof(EditorWindow).Assembly.GetType("UnityEditor.AsyncProgressBar");
                clearBackgroundProgressBar = (Action)Delegate.CreateDelegate(typeof(Action), type.GetMethod("Clear", new Type[] { }));
            }
            clearBackgroundProgressBar();
        }

        // About to enter playmode
        private void OnPlaymodeEnter()
        {
            if (activeTask != null)
            {
                ClearBackgroundProgressBar();
                EditorUtility.ClearProgressBar();
            }
        }

        // A recompile or playmode enter/exit cause the script environment to reload while we had tasks at hand
        private void OnSessionRestarted()
        {
            ClearBackgroundProgressBar();
            EditorUtility.ClearProgressBar();
            if (activeTask != null)
            {
                activeTask.Reconnect();
            }
        }

        private void WriteCache()
        {
            try
            {
                var cache = File.CreateText(CacheFilePath);
                cache.Write("[");

                // Cache the active task
                if (activeTask != null && !activeTask.Done && activeTask.Cached)
                {
                    activeTask.WriteCache(cache);
                }
                else
                {
                    cache.Write("false");
                }

                // Cache the queue
                lock(tasksLock)
                {
                    foreach (var task in Tasks)
                    {
                        if (!task.Cached)
                        {
                            continue;
                        }

                        cache.Write(",\n");
                        task.WriteCache(cache);
                    }
                }

                cache.Write("]");
                cache.Close();
            }
            catch (Exception e)
            {
                Logger.Error(TaskCacheWriteExceptionError, e);
            }
        }

        /// <summary>
        /// Update progress bars to match progress of given task
        /// </summary>
        private void WaitForTask(ITask task, WaitMode mode = WaitMode.Background)
        {
            if (activeTask != task)
            {
                return;
            }

            //logger.Trace("WaitForTask: \"{0}\"", task.Label);

            // Unintrusive background process
            if (mode == WaitMode.Background)
            {
                DisplayBackgroundProgressBar(task.Label, task.Progress);

                //if (!task.Done)
                //{
                //    ScheduleMainThreadInternal(() => WaitForTask(task, mode));
                //}
            }
            // Obstruct editor interface, while offering cancel button
            else if (mode == WaitMode.Modal)
            {
                if (!EditorUtility.DisplayCancelableProgressBar(TaskProgressTitle, task.Label, task.Progress) && !task.Done)
                {
                    //ScheduleMainThreadInternal(() => WaitForTask(task, mode));
                }
                else if (!task.Done)
                {
                    task.Abort();
                }
            }
            // Offer to interrupt task via dialog box, else block main thread until completion
            else
            {
                if (EditorUtility.DisplayDialog(TaskBlockingTitle, String.Format(TaskBlockingDescription, task.Label), TaskBlockingComplete,
                    TaskBlockingInterrupt))
                {
                    do
                    {
                        EditorUtility.DisplayProgressBar(TaskProgressTitle, task.Label, task.Progress);
                        Thread.Sleep(BlockingTaskWaitSleep);
                    } while (!task.Done);

                    EditorUtility.ClearProgressBar();
                }
                else
                {
                    task.Abort();
                }
            }
        }

        protected override void OnWaitingBackgroundTaskEnd(ITask task)
        {
            ScheduleMainThreadInternal(() => ClearBackgroundProgressBar());
        }

        protected override void OnWaitingModalTaskEnd(ITask task)
        {
            ScheduleMainThreadInternal(() => EditorUtility.ClearProgressBar());
        }

        private enum WaitMode
        {
            Background,
            Modal,
            Blocking
        };

        private delegate void ProgressBarDisplayMethod(string text, float progress);
    }
}
