using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace GitHub.Unity
{
    static class TaskExtensions
    {
        /// <summary>
        /// Runs the <paramref name="next"/> task when the previous one (<paramref name="task"/>) is done, on whatever
        /// scheduler the <paramref name="next"/> task is configured to run on.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="task"></param>
        /// <param name="next"></param>
        /// <returns></returns>
        public static T Then<T>(this ITask task, T next)
            where T : ITask
        {
            next.ContinueWith(task);
            return next;
        }

        /// <summary>
        /// Fires off an action taking as input a bool for whether the previous task was successful
        /// and the data returned by that task. The action runs on the UI thread.
        /// </summary>
        /// <param name="task"></param>
        /// <param name="next"></param>
        /// <returns><paramref name="task"/></returns>
        public static ITask<T> Finally<T>(this FuncTask<T> task, Action<bool, T> next)
        {
            return task.ContinueWithUI(next);
        }

        /// <summary>
        /// Fires off an action taking as input a bool for whether the previous task was successful
        /// and the data returned by that task. The action runs on the UI thread.
        /// </summary>
        /// <param name="task"></param>
        /// <param name="next"></param>
        /// <returns><paramref name="task"/></returns>
        public static ITask<List<T>> Finally<T>(this FuncListTask<T> task, Action<bool, List<T>> next)
        {
            return task.ContinueWithUI(next);
        }

        /// <summary>
        /// Fires off an action taking as input a bool for whether the previous task was successful.
        /// The action runs on the UI thread.
        /// </summary>
        /// <param name="task"></param>
        /// <param name="next"></param>
        /// <returns><paramref name="task"/></returns>
        public static ITask Finally(this ITask task, Action<bool> next)
        {
            return task.ContinueWithUI(next);
        }

        public static async Task<T> Catch<T>(this Task<T> source, Func<Exception, T> handler = null)
        {
            try
            {
                return await source;
            }
            catch (Exception ex)
            {
                if (handler != null)
                    return handler(ex);
                return default(T);
            }
        }

        public static async Task Catch(this Task source, Action<Exception> handler = null)
        {
            try
            {
                await source;
            }
            catch (Exception ex)
            {
                if (handler != null)
                    handler(ex);
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "task")]
        public static void Forget(this Task task)
        {
        }

        public static async Task<ITask<T>> Catch<T>(this ITask<T> source, Func<ITask<T>, Exception, ITask<T>> handler = null)
        {
            try
            {
                await source.Task;
            }
            catch (Exception ex)
            {
                if (handler != null)
                    return handler(source, ex);
            }
            return source;
        }

        public static async Task<ITask> Catch(this ITask source, Action<ITask, Exception> handler = null)
        {
            try
            {
                await source.Task;
            }
            catch (Exception ex)
            {
                if (handler != null)
                    handler(source, ex);
            }
            return source;
        }

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
}