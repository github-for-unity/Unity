using GitHub.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace GitHub.Unity
{
    class ThreadSynchronizationContext : SynchronizationContext
    {
        private readonly CancellationToken token;
        private readonly ConcurrentQueue<PostData> queue = new ConcurrentQueue<PostData>();
        private readonly ConcurrentQueue<PostData> priorityQueue = new ConcurrentQueue<PostData>();
        private readonly JobSignal jobSignal = new JobSignal();
        private long jobId;
        private readonly Task task;
        private int threadId;

        public ThreadSynchronizationContext(CancellationToken token)
        {
            this.token = token;
            task = new Task(Start, token, TaskCreationOptions.LongRunning);
            task.Start();
        }

        public override void Post(SendOrPostCallback d, object state)
        {
            queue.Enqueue(new PostData { Callback = d, State = state });
        }

        public override void Send(SendOrPostCallback d, object state)
        {
            if (Thread.CurrentThread.ManagedThreadId == threadId)
            {
                d(state);
            }
            else
            {
                var id = Interlocked.Increment(ref jobId);
                priorityQueue.Enqueue(new PostData { Id = id, Callback = d, State = state });
                Wait(id);
            }
        }

        private void Wait(long id)
        {
            jobSignal.Wait(id, token);
        }

        private void Start()
        {
            threadId = Thread.CurrentThread.ManagedThreadId;
            var lastTime = DateTime.Now.Ticks;
            var wait = new ManualResetEventSlim(false);
            var ticksPerFrame = TimeSpan.TicksPerMillisecond * 10;
            var count = 0;
            var secondStart = DateTime.Now.Ticks;
            while (!token.IsCancellationRequested)
            {
                var current = DateTime.Now.Ticks;
                count++;
                if (current - secondStart > TimeSpan.TicksPerMillisecond * 1000)
                {
                    //Console.WriteLine(String.Format("FPS {0}", count));
                    count = 0;
                    secondStart = current;
                }
                Pump();
                lastTime = DateTime.Now.Ticks;
                long waitTime = (current + ticksPerFrame - lastTime) / TimeSpan.TicksPerMillisecond;
                if (waitTime > 0 && waitTime < int.MaxValue)
                {
                    try
                    {
                        wait.Wait((int)waitTime, token);
                    }
                    catch { }
                }
            }
        }

        public void Pump()
        {
            PostData data;
            if (priorityQueue.TryDequeue(out data))
            {
                data.Run();
            }
            if (queue.TryDequeue(out data))
            {
                //LogHelper.GetLogger<ThreadSynchronizationContext>().Trace($"Running {data.Id} on main thread");
                data.Run();
            }
        }
        struct PostData
        {
            public long Id;
            public SendOrPostCallback Callback;
            public object State;
            public void Run()
            {
                Callback(State);
            }
        }

        class JobSignal : ManualResetEventSlim
        {
            private readonly HashSet<long> signaledIds = new HashSet<long>();

            public void Set(long id)
            {
                try
                {
                    signaledIds.Add(id);
                }
                catch { } // it's already on the list
                Set();
                Reset();
            }

            public bool Wait(long id, CancellationToken token)
            {
                bool signaled = false;
                do
                {

                    signaled = signaledIds.Contains(id);
                    if (signaled)
                        break;
                    Wait(token);
                }
                while (!token.IsCancellationRequested && !signaled);
                return signaled;
            }
        }
    }

    /// <summary>Provides a task scheduler that targets a specific SynchronizationContext.</summary>
    public sealed class SynchronizationContextTaskScheduler : TaskScheduler
    {
        /// <summary>The queue of tasks to execute, maintained for debugging purposes.</summary>
        private readonly ConcurrentQueue<Task> _tasks;
        /// <summary>The target context under which to execute the queued tasks.</summary>
        private readonly SynchronizationContext _context;

        /// <summary>Initializes an instance of the SynchronizationContextTaskScheduler class.</summary>
        public SynchronizationContextTaskScheduler() :
            this(SynchronizationContext.Current)
        {
        }

        /// <summary>
        /// Initializes an instance of the SynchronizationContextTaskScheduler class
        /// with the specified SynchronizationContext.
        /// </summary>
        /// <param name="context">The SynchronizationContext under which to execute tasks.</param>
        public SynchronizationContextTaskScheduler(SynchronizationContext context)
        {
            if (context == null) throw new ArgumentNullException("context");
            _context = context;
            _tasks = new ConcurrentQueue<Task>();
        }

        /// <summary>Queues a task to the scheduler for execution on the I/O ThreadPool.</summary>
        /// <param name="task">The Task to queue.</param>
        protected override void QueueTask(Task task)
        {
            _tasks.Enqueue(task);
            _context.Post(delegate
            {
                Task nextTask;
                if (_tasks.TryDequeue(out nextTask)) TryExecuteTask(nextTask);
            }, null);
        }

        /// <summary>Tries to execute a task on the current thread.</summary>
        /// <param name="task">The task to be executed.</param>
        /// <param name="taskWasPreviouslyQueued">Ignored.</param>
        /// <returns>Whether the task could be executed.</returns>
        protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
        {
            return _context == SynchronizationContext.Current && TryExecuteTask(task);
        }

        /// <summary>Gets an enumerable of tasks queued to the scheduler.</summary>
        /// <returns>An enumerable of tasks queued to the scheduler.</returns>
        protected override IEnumerable<Task> GetScheduledTasks()
        {
            return _tasks.ToArray();
        }

        /// <summary>Gets the maximum concurrency level supported by this scheduler.</summary>
        public override int MaximumConcurrencyLevel { get { return 1; } }
    }
}