using System;
using System.Threading;
using System.Threading.Tasks;

namespace GitHub.Unity
{
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