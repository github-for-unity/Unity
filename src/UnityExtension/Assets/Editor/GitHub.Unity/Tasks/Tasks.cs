using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;

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
    class Tasks
    {
        private static readonly ILogging logger = Logging.GetLogger<Tasks>();

        internal const string TypeKey = "type", ProcessKey = "process";

        private const int NoTasksSleep = 15;
        private const int BlockingTaskWaitSleep = 10;
        private const int FailureDelayDefault = 1;
        private const int FailureDelayLong = 5000;
        private const string CacheFileName = "GitHubCache";
        private const string QuitActionFieldName = "editorApplicationQuit";
        private const string TaskThreadExceptionRestartError = "GitHub task thread restarting after encountering an exception: {0}";
        private const string TaskCacheWriteExceptionError = "GitHub: Exception when writing task cache: {0}";
        private const string TaskCacheParseError = "GitHub: Failed to parse task cache";
        private const string TaskParseUnhandledTypeError = "GitHub: Trying to parse unhandled cached task: {0}";
        private const string TaskFailureTitle = "GitHub";
        private const string TaskFailureMessage = "{0} failed:\n{1}";
        private const string TaskFailureOK = "OK";
        private const string TaskProgressTitle = "GitHub";
        private const string TaskBlockingTitle = "Critical GitHub task";
        private const string TaskBlockingDescription = "A critical GitHub task ({0}) has yet to complete. What would you like to do?";
        private const string TaskBlockingComplete = "Complete";
        private const string TaskBlockingInterrupt = "Interrupt";
        private const BindingFlags kQuitActionBindingFlags = BindingFlags.NonPublic | BindingFlags.Static;

        private static FieldInfo quitActionField;
        private static ProgressBarDisplayMethod displayBackgroundProgressBar;
        private static Action clearBackgroundProgressBar;
        private ITask activeTask;
        private Exception lastException;

        private bool running = false;
        private ConcurrentQueue<ITask> tasks;
        private object tasksLock = new object();
        private Thread thread;
        private readonly MainThreadSynchronizationContext context;

        private Tasks(MainThreadSynchronizationContext context)
        {
            this.context = context;

            editorApplicationQuit = (UnityAction)Delegate.Combine(editorApplicationQuit, new UnityAction(OnQuit));
            CacheFilePath = Path.Combine(Application.dataPath, Path.Combine("..", Path.Combine("Temp", CacheFileName)));
            EditorApplication.playmodeStateChanged += () =>
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode && !EditorApplication.isPlaying)
                {
                    OnPlaymodeEnter();
                }
            };

            tasks = new ConcurrentQueue<ITask>();
            //if (File.Exists(CacheFilePath))
            //{
            //    ReadCache();
            //    File.Delete(CacheFilePath);

            //    OnSessionRestarted();
            //}
        }

        // "Everything is broken - let's rebuild from the ashes (read: cache)"
        public static void Initialize(MainThreadSynchronizationContext context)
        {
            Instance = new Tasks(context);
        }

        public static void Run()
        {
            Instance.RunInternal();
        }

        private void RunInternal()
        {
            thread = new Thread(Start);
            thread.Start();
        }

        public static void Add(ITask task)
        {
            Instance.AddTask(task);
        }

        private void AddTask(ITask task)
        {
            logger.Debug("Adding Task: \"{0}\" Label: \"{1}\"", task.GetType().Name, task.Label);
            lock (tasksLock)
            {
                if ((task.Queued == TaskQueueSetting.NoQueue && tasks.Count > 0) ||
                    (task.Queued == TaskQueueSetting.QueueSingle && tasks.Any(t => t.GetType() == task.GetType())))
                {
                    return;
                }

                tasks.Enqueue(task);
            }

            //WriteCache();
        }

        private ConcurrentQueue<Action> scheduledCalls = new ConcurrentQueue<Action>();
        private double lastMainThreadCall = 0;
        private bool readyForMoreCalls = true;
        public static void ScheduleMainThread(Action action)
        {
            Instance.ScheduleMainThreadInternal(action);
            
        }

        private void ScheduleMainThreadInternal(Action action)
        {
            scheduledCalls.Enqueue(action);
            var ellapsed = TimeSpan.FromTicks(DateTime.Now.Ticks).TotalMilliseconds;
            logger.Trace("QueuedAction ReadyForMore:{0} Delta:{1}ms LastCall:{2}ms ThisCall:{3}ms", readyForMoreCalls, ellapsed - lastMainThreadCall, lastMainThreadCall, ellapsed);
            PumpMainThread();
        }

        private int callerFlag = 0;
        private void PumpMainThread()
        {
            var ellapsed = TimeSpan.FromTicks(DateTime.Now.Ticks).TotalMilliseconds;

            //logger.Debug("Pumping {0} {1} {2}", readyForMoreCalls, scheduledCalls.Count, (int)(ellapsed - lastMainThreadCall));

            if (readyForMoreCalls && scheduledCalls.Count > 0 &&
               (lastMainThreadCall == 0 || 33 < ellapsed - lastMainThreadCall))
            {
                if (Interlocked.CompareExchange(ref callerFlag, 1, 0) == 0)
                {
                    lastMainThreadCall = ellapsed;
                    readyForMoreCalls = false;
                    logger.Debug("Scheduling a thing");
                    context.Schedule(() =>
                    {
                        logger.Debug("Executing actions");
                        readyForMoreCalls = true;
                        Action act = null;
                        while (scheduledCalls.TryDequeue(out act))
                        {
                            act();
                        }
                    });
                    callerFlag = 0;
                }
            }
        }

        public static void ReportFailure(FailureSeverity severity, ITask task, string error)
        {
            if (severity == FailureSeverity.Moderate)
            {
                logger.Error("Failure: \"{0}\" Reason:\"{1}\"", task.Label, error);
            }
            else
            {
                ScheduleMainThread(() => EditorUtility.DisplayDialog(TaskFailureTitle, String.Format(TaskFailureMessage, task.Label, error), TaskFailureOK));
            }
        }

        private static void SecureQuitActionField()
        {
            if (quitActionField == null)
            {
                quitActionField = typeof(EditorApplication).GetField(QuitActionFieldName, kQuitActionBindingFlags);

                if (quitActionField == null)
                {
                    throw new TaskException("Unable to reflect EditorApplication." + QuitActionFieldName);
                }
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
                        logger.Error(TaskThreadExceptionRestartError, e);
                        Thread.Sleep(FailureDelayDefault);
                    }
                    else
                    {
                        Thread.Sleep(FailureDelayLong);
                    }
                }
            }
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

        private void OnQuit()
        {
            // Stop the queue
            running = false;

            if (activeTask != null && activeTask.Critical)
            {
                WaitForTask(activeTask, WaitMode.Blocking);
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
                    lock(tasksLock)
                    {
                        if (tasks.Count > 0)
                        {
                            if (!tasks.TryDequeue(out activeTask))
                            {
                                logger.Debug("Could not dequeue task");
                            }
                            //activeTask.OnBegin = task => ScheduleMainThread(WriteCache);
                            if (activeTask.Blocking)
                                activeTask.OnEnd = OnWaitingModalTaskEnd;
                            else
                                activeTask.OnEnd = OnWaitingBackgroundTaskEnd;
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

                    ThreadPool.QueueUserWorkItem(_ =>
                    {
                        try
                        {
                            activeTask.Run();
                        }
                        catch (Exception ex)
                        {
                            logger.Debug(ex);
                            activeTask.OnEnd.SafeInvoke(activeTask);
                            activeTask = null;
                        }
                    });
                    //WriteCache();
                }

                PumpMainThread();
                Thread.Sleep(NoTasksSleep);
            }

            thread.Abort();
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
                    foreach (var task in tasks)
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
                logger.Error(TaskCacheWriteExceptionError, e);
            }
        }

        private bool ReadCache()
        {
            var text = File.ReadAllText(CacheFilePath);

            object parseResult;
            IList<object> cache;

            // Parse root list with at least one item (active task) or fail
            if (!SimpleJson.TryDeserializeObject(text, out parseResult) || (cache = parseResult as IList<object>) == null || cache.Count < 1)
            {
                logger.Error(TaskCacheParseError);
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
                    logger.Error(TaskCacheParseError);
                    return false;
                }

                cachedTasks.Enqueue(ParseTask(taskData));
            }

            // Apply everything only after fully successful parse
            activeTask = cachedActiveTask;
            tasks = cachedTasks;

            return true;
        }

        private ITask ParseTask(IDictionary<string, object> data)
        {
            CachedTask type;

            try
            {
                type = (CachedTask)Enum.Parse(typeof(CachedTask), (string)data[TypeKey]);
            }
            catch (Exception)
            {
                return null;
            }

            try
            {
                switch (type)
                {
                    case CachedTask.TestTask:
                        return TestTask.Parse(data);
                    case CachedTask.ProcessTask:
                        return ProcessTask.Parse(data);
                    default:
                        logger.Error(TaskParseUnhandledTypeError, type);
                        return null;
                }
            }
            catch (Exception)
            {
                return null;
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

            logger.Trace("WaitForTask: \"{0}\"", task.Label);

            // Unintrusive background process
            if (mode == WaitMode.Background)
            {
                DisplayBackgroundProgressBar(task.Label, task.Progress);

                if (!task.Done)
                {
                    ScheduleMainThreadInternal(() => WaitForTask(task, mode));
                }
            }
            // Obstruct editor interface, while offering cancel button
            else if (mode == WaitMode.Modal)
            {
                if (!EditorUtility.DisplayCancelableProgressBar(TaskProgressTitle, task.Label, task.Progress) && !task.Done)
                {
                    ScheduleMainThreadInternal(() => WaitForTask(task, mode));
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

        private void OnWaitingBackgroundTaskEnd(ITask task)
        {
            ScheduleMainThreadInternal(() => ClearBackgroundProgressBar());
        }

        private void OnWaitingModalTaskEnd(ITask task)
        {
            ScheduleMainThreadInternal(() => EditorUtility.ClearProgressBar());
        }

        private static Tasks Instance { get; set; }
        private static string CacheFilePath { get; set; }

        private static UnityAction editorApplicationQuit
        {
            get
            {
                SecureQuitActionField();
                return (UnityAction)quitActionField.GetValue(null);
            }
            set
            {
                SecureQuitActionField();
                quitActionField.SetValue(null, value);
            }
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
