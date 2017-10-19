using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace GitHub.Unity
{
    static class ThreadingHelper
    {
        public static TaskScheduler MainThreadScheduler { get; set; }

        public static int MainThread { get; set; }
        static bool InMainThread { get { return MainThread == 0 || Thread.CurrentThread.ManagedThreadId == MainThread; } }

        public static void SetUIThread()
        {
            MainThread = Thread.CurrentThread.ManagedThreadId;
        }

        public static bool InUIThread => InMainThread || Guard.InUnitTestRunner;

        /// <summary>
        /// Switch to the UI thread
        /// Auto-disables switching when running in unit test mode
        /// </summary>
        /// <returns></returns>
        public static IAwaitable SwitchToMainThreadAsync()
        {
            return Guard.InUnitTestRunner ?
                new AwaitableWrapper() :
                new AwaitableWrapper(MainThreadScheduler);
        }


        /// <summary>
        /// Switch to a thread pool background thread if the current thread isn't one, otherwise does nothing
        /// Auto-disables switching when running in unit test mode
        /// </summary>
        /// <param name="scheduler"></param>
        /// <returns></returns>
        public static IAwaitable SwitchToThreadAsync(TaskScheduler scheduler = null)
        {
            return Guard.InUnitTestRunner ?
                new AwaitableWrapper() :
                new AwaitableWrapper(scheduler ?? TaskManager.Instance.ConcurrentScheduler);
        }

        class AwaitableWrapper : IAwaitable
        {
            Func<IAwaiter> getAwaiter;

            public AwaitableWrapper()
            {
                getAwaiter = () => new AwaiterWrapper();
            }

            public AwaitableWrapper(TaskScheduler scheduler)
            {
                getAwaiter = () => new AwaiterWrapper(new TaskSchedulerAwaiter(scheduler));
            }

            public IAwaiter GetAwaiter() => getAwaiter();
        }

        class AwaiterWrapper : IAwaiter
        {
            Func<bool> isCompleted;
            Action<Action> onCompleted;
            Action getResult;

            public AwaiterWrapper()
            {
                isCompleted = () => true;
                onCompleted = c => c();
                getResult = () => { };
            }

            public AwaiterWrapper(TaskSchedulerAwaiter awaiter)
            {
                isCompleted = () => awaiter.IsCompleted;
                onCompleted = c => awaiter.OnCompleted(c);
                getResult = () => awaiter.GetResult();
            }

            public bool IsCompleted => isCompleted();

            public void OnCompleted(Action continuation) => onCompleted(continuation);

            public void GetResult() => getResult();
        }

        public struct TaskSchedulerAwaiter : INotifyCompletion
        {
            private readonly TaskScheduler scheduler;

            public bool IsCompleted
            {
                get
                {
                    return (this.scheduler == TaskManager.Instance.UIScheduler && InUIThread) || (this.scheduler != TaskManager.Instance.UIScheduler && !InUIThread);
                }
            }

            public TaskSchedulerAwaiter(TaskScheduler scheduler)
            {
                this.scheduler = scheduler;
            }

            public void OnCompleted(Action action)
            {
                Task.Factory.StartNew(action, TaskManager.Instance.Token, TaskCreationOptions.None, this.scheduler);
            }

            public void GetResult()
            {
            }
        }

    }
}