using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace GitHub.Unity
{
    class TaskSchedulerExcludingThread : TaskScheduler
    {
        private static ParameterizedThreadStart longRunningThreadWork = new ParameterizedThreadStart(LongRunningThreadWork);
        private static WaitCallback taskExecuteWaitCallback = new WaitCallback(TaskExecuteWaitCallback);
        private static MethodInfo executeEntryMethod;

        static TaskSchedulerExcludingThread()
        {
            executeEntryMethod = typeof(Task).GetMethod("ExecuteEntry", BindingFlags.NonPublic | BindingFlags.Instance);
        }

        public TaskSchedulerExcludingThread(int threadToExclude)
        {
            ThreadToExclude = threadToExclude;
        }

        private static void LongRunningThreadWork(object obj)
        {
            ExecuteEntry(obj as Task, true);
        }

        private static bool ExecuteEntry(Task task, bool flag)
        {
            return (bool)executeEntryMethod.Invoke(task, new object[] { flag });
        }

        protected override void QueueTask(Task task)
        {
            if ((task.CreationOptions & TaskCreationOptions.LongRunning) != TaskCreationOptions.None)
                new Thread(longRunningThreadWork)
                {
                    IsBackground = true
                }.Start(task);
            else
                ThreadPool.QueueUserWorkItem(taskExecuteWaitCallback, (object)task);
        }

        private static void TaskExecuteWaitCallback(object obj)
        {
            ExecuteEntry(obj as Task, true);
        }

        protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
        {
            try
            {
                if (Thread.CurrentThread.ManagedThreadId == ThreadToExclude)
                    return false;
                return ExecuteEntry(task, true);
            }
            finally
            {
                if (taskWasPreviouslyQueued)
                    NotifyWorkItemProgress();
            }
        }

        protected override bool TryDequeue(Task task)
        {
            return false;
        }

        protected override IEnumerable<Task> GetScheduledTasks()
        {
            yield return (Task)null;
        }

        private void NotifyWorkItemProgress()
        {
        }

        public int ThreadToExclude { get; set; }
    }


    /// <summary>Provides concurrent and exclusive task schedulers that coordinate.</summary>
    [DebuggerDisplay("ConcurrentTasksWaiting={ConcurrentTaskCount}, ExclusiveTasksWaiting={ExclusiveTaskCount}")]
    [DebuggerTypeProxy(typeof(ConcurrentExclusiveInterleaveDebugView))]
    public sealed class ConcurrentExclusiveInterleave
    {
        private readonly CancellationToken token;
        /// <summary>Synchronizes all activity in this type and its generated schedulers.</summary>
        private readonly object internalLock;
        /// <summary>The scheduler used to queue and execute "reader" tasks that may run concurrently with other readers.</summary>
        private readonly ConcurrentExclusiveTaskScheduler concurrentTaskScheduler;
        /// <summary>Whether the exclusive processing of a task should include all of its children as well.</summary>
        private readonly bool exclusiveProcessingIncludesChildren;
        /// <summary>The scheduler used to queue and execute "writer" tasks that must run exclusively while no other tasks for this interleave are running.</summary>
        private readonly ConcurrentExclusiveTaskScheduler exclusiveTaskScheduler;
        private readonly TaskSchedulerExcludingThread interleaveTaskScheduler;
        /// <summary>The parallel options used by the asynchronous task and parallel loops.</summary>
        private ParallelOptions parallelOptions;
        /// <summary>Whether this interleave has queued its processing task.</summary>
        private Task taskExecuting;

        /// <summary>Initializes the ConcurrentExclusiveInterleave.</summary>
        /// <param name="token"></param>
        public ConcurrentExclusiveInterleave(CancellationToken token)
            : this(false)
        {
            this.token = token;
        }

        /// <summary>Initializes the ConcurrentExclusiveInterleave.</summary>
        /// <param name="targetScheduler">The target scheduler on which this interleave should execute.</param>
        /// <param name="exclusiveProcessingIncludesChildren">Whether the exclusive processing of a task should include all of its children as well.</param>
        public ConcurrentExclusiveInterleave(bool exclusiveProcessingIncludesChildren)
        {
            interleaveTaskScheduler = new TaskSchedulerExcludingThread(Thread.CurrentThread.ManagedThreadId);
            parallelOptions = new ParallelOptions { TaskScheduler = interleaveTaskScheduler };

            // Create the state for this interleave
            internalLock = new object();
            this.exclusiveProcessingIncludesChildren = exclusiveProcessingIncludesChildren;
            concurrentTaskScheduler = new ConcurrentExclusiveTaskScheduler(this, new Queue<Task>(), interleaveTaskScheduler.MaximumConcurrencyLevel);
            exclusiveTaskScheduler = new ConcurrentExclusiveTaskScheduler(this, new Queue<Task>(), 1);
        }

        public async Task Wait()
        {
            if (taskExecuting != null)
                await taskExecuting;
        }

        /// <summary>Notifies the interleave that new work has arrived to be processed.</summary>
        /// <remarks>Must only be called while holding the lock.</remarks>
        internal void NotifyOfNewWork()
        {
            // If a task is already running, bail.
            if (taskExecuting != null) return;

            if (token.IsCancellationRequested) return;

            // Otherwise, run the processor. Store the task and then start it to ensure that 
            // the assignment happens before the body of the task runs.
            taskExecuting = new Task(ConcurrentExclusiveInterleaveProcessor, token, TaskCreationOptions.None);
            taskExecuting.Start(parallelOptions.TaskScheduler);
        }

        /// <summary>The body of the async processor to be run in a Task.  Only one should be running at a time.</summary>
        /// <remarks>This has been separated out into its own method to improve the Parallel Tasks window experience.</remarks>
        private void ConcurrentExclusiveInterleaveProcessor()
        {
            if (token.IsCancellationRequested) return;
            interleaveTaskScheduler.ThreadToExclude = Thread.CurrentThread.ManagedThreadId;

            // Run while there are more tasks to be processed.  We assume that the first time through,
            // there are tasks.  If they aren't, worst case is we try to process and find none.
            var runTasks = true;
            var cleanupOnExit = true;
            while (runTasks)
            {
                try
                {
                    // Process all waiting exclusive tasks
                    foreach (var task in GetExclusiveTasks())
                    {
                        if (token.IsCancellationRequested) return;

                        exclusiveTaskScheduler.ExecuteTask(task);
                        // Just because we executed the task doesn't mean it's "complete",
                        // if it has child tasks that have not yet completed
                        // and will complete later asynchronously.  To account for this, 
                        // if a task isn't yet completed, leave the interleave processor 
                        // but leave it still in a running state.  When the task completes,
                        // we'll come back in and keep going.  Note that the children
                        // must not be scheduled to this interleave, or this will deadlock.
                        if (exclusiveProcessingIncludesChildren && !task.IsCompleted)
                        {
                            cleanupOnExit = false;
                            task.ContinueWith(_ => ConcurrentExclusiveInterleaveProcessor(), parallelOptions.TaskScheduler);
                            return;
                        }
                    }

                    if (token.IsCancellationRequested) return;

                    // Process all waiting concurrent tasks *until* any exclusive tasks show up, in which
                    // case we want to switch over to processing those (by looping around again).
                    Parallel.ForEach(GetConcurrentTasksUntilExclusiveExists(), parallelOptions, ExecuteConcurrentTask);
                }
                finally
                {
                    if (cleanupOnExit)
                    {
                        lock (internalLock)
                        {
                            // If there are no tasks remaining, we're done. If there are, loop around and go again.
                            if (concurrentTaskScheduler.Tasks.Count == 0 && exclusiveTaskScheduler.Tasks.Count == 0)
                            {
                                taskExecuting = null;
                                runTasks = false;
                            }
                        }
                    }
                }
            }
        }

        /// <summary>Runs a concurrent task.</summary>
        /// <param name="task">The task to execute.</param>
        /// <remarks>This has been separated out into its own method to improve the Parallel Tasks window experience.</remarks>
        private void ExecuteConcurrentTask(Task task)
        {
            concurrentTaskScheduler.ExecuteTask(task);
        }

        /// <summary>
        /// Gets an enumerable that yields waiting concurrent tasks one at a time until
        /// either there are no more concurrent tasks or there are any exclusive tasks.
        /// </summary>
        private IEnumerable<Task> GetConcurrentTasksUntilExclusiveExists()
        {
            while (true)
            {
                if (token.IsCancellationRequested) yield break;

                Task foundTask = null;
                lock (internalLock)
                {
                    if (exclusiveTaskScheduler.Tasks.Count == 0 && concurrentTaskScheduler.Tasks.Count > 0)
                    {
                        foundTask = concurrentTaskScheduler.Tasks.Dequeue();
                    }
                }
                if (token.IsCancellationRequested) yield break;
                if (foundTask != null) yield return foundTask;
                else yield break;
            }
        }

        /// <summary>
        /// Gets an enumerable that yields all of the exclusive tasks one at a time.
        /// </summary>
        private IEnumerable<Task> GetExclusiveTasks()
        {
            while (true)
            {
                if (token.IsCancellationRequested) yield break;

                Task foundTask = null;
                lock (internalLock)
                {
                    if (exclusiveTaskScheduler.Tasks.Count > 0) foundTask = exclusiveTaskScheduler.Tasks.Dequeue();
                }
                if (token.IsCancellationRequested) yield break;
                if (foundTask != null) yield return foundTask;
                else yield break;
            }
        }

        /// <summary>
        /// Gets a TaskScheduler that can be used to schedule tasks to this interleave
        /// that may run concurrently with other tasks on this interleave.
        /// </summary>
        public TaskScheduler ConcurrentTaskScheduler
        {
            get { return concurrentTaskScheduler; }
        }

        /// <summary>
        /// Gets a TaskScheduler that can be used to schedule tasks to this interleave
        /// that must run exclusively with regards to other tasks on this interleave.
        /// </summary>
        public TaskScheduler ExclusiveTaskScheduler
        {
            get { return exclusiveTaskScheduler; }
        }

        /// <summary>Gets the number of tasks waiting to run exclusively.</summary>
        private int ExclusiveTaskCount
        {
            get
            {
                lock (internalLock) return exclusiveTaskScheduler.Tasks.Count;
            }
        }

        /// <summary>Gets the number of tasks waiting to run concurrently.</summary>
        private int ConcurrentTaskCount
        {
            get
            {
                lock (internalLock) return concurrentTaskScheduler.Tasks.Count;
            }
        }

        /// <summary>Provides a debug view for ConcurrentExclusiveInterleave.</summary>
        internal class ConcurrentExclusiveInterleaveDebugView
        {
            /// <summary>The interleave being debugged.</summary>
            private readonly ConcurrentExclusiveInterleave interleave;

            /// <summary>Initializes the debug view.</summary>
            /// <param name="interleave">The interleave being debugged.</param>
            public ConcurrentExclusiveInterleaveDebugView(ConcurrentExclusiveInterleave interleave)
            {
                Guard.ArgumentNotNull(interleave, "interleave");

                this.interleave = interleave;
            }

            public IEnumerable<Task> ExclusiveTasksWaiting
            {
                get { return interleave.exclusiveTaskScheduler.Tasks; }
            }

            /// <summary>Gets the number of tasks waiting to run concurrently.</summary>
            public IEnumerable<Task> ConcurrentTasksWaiting
            {
                get { return interleave.concurrentTaskScheduler.Tasks; }
            }

            /// <summary>Gets a description of the processing task for debugging purposes.</summary>
            public Task InterleaveTask
            {
                get { return interleave.taskExecuting; }
            }
        }

        /// <summary>
        /// A scheduler shim used to queue tasks to the interleave and execute those tasks on request of the interleave.
        /// </summary>
        private class ConcurrentExclusiveTaskScheduler : TaskScheduler
        {
            /// <summary>The parent interleave.</summary>
            private readonly ConcurrentExclusiveInterleave interleave;
            /// <summary>The maximum concurrency level for the scheduler.</summary>
            private readonly int maximumConcurrencyLevel;
            /// <summary>Whether a Task is currently being processed on this thread.</summary>
            private readonly ThreadLocal<bool> processingTaskOnCurrentThread = new ThreadLocal<bool>();

            /// <summary>Initializes the scheduler.</summary>
            /// <param name="interleave">The parent interleave.</param>
            /// <param name="tasks">The queue to store queued tasks into.</param>
            internal ConcurrentExclusiveTaskScheduler(ConcurrentExclusiveInterleave interleave, Queue<Task> tasks,
                int maximumConcurrencyLevel)
            {
                Guard.ArgumentNotNull(interleave, "interleave");
                Guard.ArgumentNotNull(tasks, "tasks");

                this.interleave = interleave;
                this.maximumConcurrencyLevel = maximumConcurrencyLevel;
                Tasks = tasks;
            }

            /// <summary>Queues a task to the scheduler.</summary>
            /// <param name="task">The task to be queued.</param>
            protected override void QueueTask(Task task)
            {
                lock (interleave.internalLock)
                {
                    Tasks.Enqueue(task);
                    interleave.NotifyOfNewWork();
                }
            }

            /// <summary>Tries to execute the task synchronously on this scheduler.</summary>
            /// <param name="task">The task to execute.</param>
            /// <param name="taskWasPreviouslyQueued">Whether the task was previously queued to the scheduler.</param>
            /// <returns>true if the task could be executed; otherwise, false.</returns>
            protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
            {
                if (processingTaskOnCurrentThread.Value)
                {
                    var t = new Task<bool>(state => TryExecuteTask((Task)state), task);
                    t.RunSynchronously(interleave.parallelOptions.TaskScheduler);
                    return t.Result;
                }

                return false;
            }

            /// <summary>Gets for debugging purposes the tasks scheduled to this scheduler.</summary>
            /// <returns>An enumerable of the tasks queued.</returns>
            protected override IEnumerable<Task> GetScheduledTasks()
            {
                return Tasks;
            }

            /// <summary>Executes a task on this scheduler.</summary>
            /// <param name="task">The task to be executed.</param>
            internal void ExecuteTask(Task task)
            {
                var isProcessingTaskOnCurrentThread = this.processingTaskOnCurrentThread.Value;
                if (!isProcessingTaskOnCurrentThread) this.processingTaskOnCurrentThread.Value = true;
                //try
                //{
                    TryExecuteTask(task);
                //}
                //catch(Exception ex)
                //{
                //    Logging.Error(ex);
                //    throw;
                //}

                if (!isProcessingTaskOnCurrentThread) this.processingTaskOnCurrentThread.Value = false;
            }

            /// <summary>Gets the maximum concurrency level this scheduler is able to support.</summary>
            public override int MaximumConcurrencyLevel
            {
                get { return maximumConcurrencyLevel; }
            }

            /// <summary>Gets the queue of tasks for this scheduler.</summary>
            internal Queue<Task> Tasks { get; }
        }
    }
}
