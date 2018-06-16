#if !NET35
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace GitHub.Unity
{
    public static class TaskEx
    {
        public static Task<TResult> FromResult<TResult>(TResult result)
        {
            return Task.FromResult(result);
        }

        public static Task<Task> WhenAny(System.Collections.Generic.IEnumerable<Task> tasks)
        {
            return Task.WhenAny(tasks);
        }

        public static Task<Task> WhenAny(params Task[] tasks)
        {
            return Task.WhenAny(tasks);
        }

        public static Task<Task<TResult>> WhenAny<TResult>(IEnumerable<Task<TResult>> tasks)
        {
            return Task.WhenAny(tasks);
        }

        public static Task<Task<TResult>> WhenAny<TResult>(params Task<TResult>[] tasks)        
        {
            return Task.WhenAny(tasks);
        }

        public static Task Delay(int millisecondsDelay)
        {
            return Task.Delay(millisecondsDelay);
        }

        public static Task Delay(int millisecondsDelay, CancellationToken cancellationToken)
        {
            return Task.Delay(millisecondsDelay, cancellationToken);
        }

        public static Task Delay(TimeSpan delay)
        {
            return Task.Delay(delay);
        }

        public static Task Delay(TimeSpan delay, CancellationToken cancellationToken)
        {
            return Task.Delay(delay, cancellationToken);
        }
    }
}
#endif
