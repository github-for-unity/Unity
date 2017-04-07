using System;
using System.Threading;
using System.Threading.Tasks;

namespace GitHub.Unity
{
    static class TaskRunnerExtensions
    {
        public static Task<bool> Run(this ProcessTask task, string arguments)
        {
            task.SetArguments(arguments);
            return task.RunAsync(CancellationToken.None);
        }
    }

    static class AsyncExtensions
    {
        public static void Forget(this Task task)
        {
        }
    }

    static class TaskExtensions
    {
        //http://stackoverflow.com/a/29491927
        public static Action<T> Debounce<T>(this Action<T> func, int milliseconds = 300)
        {
            var last = 0;
            return arg =>
            {
                var current = Interlocked.Increment(ref last);
                TaskEx.Delay(milliseconds).ContinueWith(task =>
                {
                    if (current == last) func(arg);
                    task.Dispose();
                });
            };
        }

        public static Action Debounce(this Action func, int milliseconds = 300)
        {
            var last = 0;
            return () =>
            {
                var current = Interlocked.Increment(ref last);
                TaskEx.Delay(milliseconds).ContinueWith(task =>
                {
                    if (current == last) func();
                    task.Dispose();
                });
            };
        }
    }

    static class TaskExt
    {
        public static async Task<TEventArgs> FromEvent<TEventHandler, TEventArgs>(
            Func<Action<TEventArgs>, Action, Action<Exception>, TEventHandler> getHandler,
            Action<TEventHandler> subscribe,
            Action<TEventHandler> unsubscribe,
            Action<Action<TEventArgs>, Action, Action<Exception>> initiate,
            CancellationToken token = default(CancellationToken))
            where TEventHandler : class
        {
            var tcs = new TaskCompletionSource<TEventArgs>();

            Action<TEventArgs> complete = args => tcs.TrySetResult(args);
            Action cancel = () => tcs.TrySetCanceled();
            Action<Exception> reject = ex => tcs.TrySetException(ex);

            TEventHandler handler = getHandler(complete, cancel, reject);

            subscribe(handler);
            try
            {
                using (token.Register(() => tcs.TrySetCanceled(),
                    useSynchronizationContext: false))
                {
                    initiate(complete, cancel, reject);
                    return await tcs.Task;
                }
            }
            finally
            {
                unsubscribe(handler);
            }
        }
    }
}