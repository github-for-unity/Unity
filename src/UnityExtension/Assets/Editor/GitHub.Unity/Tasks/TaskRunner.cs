using System;
using System.IO;
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

        public TaskRunner(IMainThreadSynchronizationContext context, CancellationToken cancellationToken)
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
