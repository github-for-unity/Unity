using System;
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
    enum TaskQueueSetting
    {
        NoQueue,
        Queue,
        QueueSingle
    }

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

    enum CachedTask
    {
        TestTask,
        ProcessTask
    };

    enum FailureSeverity
    {
        Moderate,
        Critical
    };

    class Tasks
    {
        internal const string TypeKey = "type", ProcessKey = "process";

        private const int NoTasksSleep = 100;
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
        private Queue<ITask> tasks;
        private object tasksLock = new object();
        private Thread thread;

        private Tasks()
        {
            editorApplicationQuit = (UnityAction)Delegate.Combine(editorApplicationQuit, new UnityAction(OnQuit));
            CacheFilePath = Path.Combine(Application.dataPath, Path.Combine("..", Path.Combine("Temp", CacheFileName)));
            EditorApplication.playmodeStateChanged += () => {
                if (EditorApplication.isPlayingOrWillChangePlaymode && !EditorApplication.isPlaying)
                {
                    OnPlaymodeEnter();
                }
            };

            tasks = new Queue<ITask>();
            if (File.Exists(CacheFilePath))
            {
                ReadCache();
                File.Delete(CacheFilePath);

                OnSessionRestarted();
            }
        }

        // "Everything is broken - let's rebuild from the ashes (read: cache)"
        public static void Initialize()
        {
            Instance = new Tasks();
        }

        public static void Run()
        {
            Instance.thread = new Thread(Instance.Start);
            Instance.thread.Start();
        }

        public static void Add(ITask task)
        {
            lock(Instance.tasksLock)
            {
                if ((task.Queued == TaskQueueSetting.NoQueue && Instance.tasks.Count > 0) ||
                    (task.Queued == TaskQueueSetting.QueueSingle && Instance.tasks.Any(t => t.GetType() == task.GetType())))
                {
                    return;
                }

                Instance.tasks.Enqueue(task);
            }

            Instance.WriteCache();
        }

        public static void ScheduleMainThread(Action action)
        {
            EditorApplication.delayCall += () => action();
        }

        public static void ReportFailure(FailureSeverity severity, ITask task, string error)
        {
            if (severity == FailureSeverity.Moderate)
            {
                Debug.LogErrorFormat(TaskFailureMessage, task.Label, error);
            }
            else
            {
                ScheduleMainThread(
                    () => EditorUtility.DisplayDialog(TaskFailureTitle, String.Format(TaskFailureMessage, task.Label, error), TaskFailureOK));
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
            while (true)
            {
                try
                {
                    RunInternal();

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
                        Debug.LogErrorFormat(TaskThreadExceptionRestartError, e);
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

        private void RunInternal()
        {
            running = true;

            while (running)
            {
                // Clear any completed task
                if (activeTask != null && activeTask.Done)
                {
                    activeTask = null;
                }

                // Grab a new task
                if (activeTask == null)
                {
                    lock(tasksLock)
                    {
                        if (tasks.Count > 0)
                        {
                            activeTask = tasks.Dequeue();
                            activeTask.OnBegin = task => ScheduleMainThread(WriteCache);
                        }
                    }
                }

                // Run and monitor active task
                if (activeTask != null)
                {
                    ScheduleMainThread(() => {
                        if (activeTask != null)
                        {
                            WaitForTask(activeTask, activeTask.Blocking ? WaitMode.Modal : WaitMode.Background);
                        }
                    });

                    activeTask.Run();
                    WriteCache();
                }
                else
                // Wait for something to do
                {
                    Thread.Sleep(NoTasksSleep);
                }
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
                Debug.LogErrorFormat(TaskCacheWriteExceptionError, e);
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
                Debug.LogError(TaskCacheParseError);
                return false;
            }

            // Parse active task
            var taskData = cache[0] as IDictionary<string, object>;
            var cachedActiveTask = taskData != null ? ParseTask(taskData) : null;

            // Parse tasks list or fail
            var cachedTasks = new Queue<ITask>(cache.Count - 1);
            for (var index = 1; index < cache.Count; ++index)
            {
                taskData = cache[index] as IDictionary<string, object>;

                if (taskData == null)
                {
                    Debug.LogError(TaskCacheParseError);
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
                        Debug.LogErrorFormat(TaskParseUnhandledTypeError, type);
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

            // Unintrusive background process
            if (mode == WaitMode.Background)
            {
                task.OnEnd = OnWaitingBackgroundTaskEnd;

                DisplayBackgroundProgressBar(task.Label, task.Progress);

                if (!task.Done)
                {
                    ScheduleMainThread(() => WaitForTask(task, mode));
                }
            }
            // Obstruct editor interface, while offering cancel button
            else if (mode == WaitMode.Modal)
            {
                task.OnEnd = OnWaitingModalTaskEnd;

                if (!EditorUtility.DisplayCancelableProgressBar(TaskProgressTitle, task.Label, task.Progress) && !task.Done)
                {
                    ScheduleMainThread(() => WaitForTask(task, mode));
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
            ScheduleMainThread(() => ClearBackgroundProgressBar());
        }

        private void OnWaitingModalTaskEnd(ITask task)
        {
            ScheduleMainThread(() => EditorUtility.ClearProgressBar());
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
