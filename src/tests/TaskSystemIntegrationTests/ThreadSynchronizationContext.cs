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
            SetSynchronizationContext(this);
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
                    catch {}
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
                LogHelper.GetLogger<ThreadSynchronizationContext>().Trace($"Running {data.Id} on main thread");
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
}