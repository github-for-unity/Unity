using System;
using System.Threading;
using System.Threading.Tasks;

namespace GitHub.Unity
{
    static class TaskExtensions
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "task")]
        public static void Forget(this Task task)
        {
        }

        public static async Task SafeAwait(this Task source, Action<Exception> handler = null)
        {
            try
            {
                await source;
            }
            catch (Exception ex)
            {
                if (handler != null)
                    handler(ex);
                else
                {
                    if (Guard.InUnitTestRunner)
                        throw;
                    Logging.GetLogger().Error(ex);
                }
            }
        }

        public static async Task<T> SafeAwait<T>(this Task<T> source, Func<Exception, T> handler = null)
        {
            try
            {
                return await source;
            }
            catch (Exception ex)
            {
                if (handler != null)
                    return handler(ex);
                if (Guard.InUnitTestRunner)
                    throw;
                Logging.GetLogger().Error(ex);
                return default(T);
            }
        }

        public static async Task StartAwait(this ITask source, Action<Exception> handler = null)
        {
            try
            {
                await source.StartAsAsync();
            }
            catch (Exception ex)
            {
                if (handler != null)
                    handler(ex);
                else
                {
                    if (Guard.InUnitTestRunner)
                        throw;
                    Logging.GetLogger().Error(ex);
                }
            }
        }

        public static async Task<T> StartAwait<T>(this ITask<T> source, Func<Exception, T> handler = null)
        {
            try
            {
                return await source.StartAsAsync();
            }
            catch (Exception ex)
            {
                if (handler != null)
                    return handler(ex);
                if (Guard.InUnitTestRunner)
                    throw;
                Logging.GetLogger().Error(ex);
                return default(T);
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "task")]
        public static void Forget(this ITask task)
        {
            task.Task.Forget();
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

        public static ITask Then(this ITask task, Action<bool> continuation, bool always = false)
        {
            Guard.ArgumentNotNull(continuation, "continuation");
            return new ActionTask(task.Token, continuation, task, always) { Name = "Then" };
        }

        public static ITask Then(this ITask task, Action<bool> continuation, TaskAffinity affinity, bool always = false)
        {
            Guard.ArgumentNotNull(continuation, "continuation");
            return new ActionTask(task.Token, continuation, task, always) { Affinity = affinity, Name = "Then" };
        }

        public static ITask Then<T>(this ITask<T> task, Action<bool, T> continuation, TaskAffinity affinity = TaskAffinity.Concurrent, bool always = false)
        {
            Guard.ArgumentNotNull(continuation, "continuation");
            return new ActionTask<T>(task.Token, continuation, task, always) { Affinity = affinity, Name = $"Then<{typeof(T)}>" };
        }

        public static ITask<T> Then<T>(this ITask task, Func<bool, T> continuation, TaskAffinity affinity = TaskAffinity.Concurrent, bool always = false)
        {
            Guard.ArgumentNotNull(continuation, "continuation");
            return new FuncTask<T>(task.Token, continuation, task) { Affinity = affinity, Name = $"Then<{typeof(T)}>" };
        }

        public static ITask<TRet> Then<T, TRet>(this ITask<T> task, Func<bool, T, TRet> continuation, TaskAffinity affinity = TaskAffinity.Concurrent, bool always = false)
        {
            Guard.ArgumentNotNull(continuation, "continuation");
            return new FuncTask<T, TRet>(task.Token, continuation, task) { Affinity = affinity, Name = $"Then<{typeof(T)}, {typeof(TRet)}>" };
        }

        public static ITask ThenInUI(this ITask task, Action<bool> continuation, bool always = false)
        {
            return task.Then(continuation, TaskAffinity.UI, always);
        }

        public static ITask ThenInUI<T>(this ITask<T> task, Action<bool, T> continuation, bool always = false)
        {
            return task.Then(continuation, TaskAffinity.UI, always);
        }

        public static ITask<T> ThenInUI<T>(this ITask task, Func<bool, T> continuation, bool always = false)
        {
            return task.Then(continuation, TaskAffinity.UI, always);
        }

        public static ITask<TRet> ThenInUI<T, TRet>(this ITask<T> task, Func<bool, T, TRet> continuation, bool always = false)
        {
            return task.Then(continuation, TaskAffinity.UI, always);
        }

        public static T FinallyInUI<T>(this T task, Action<bool, Exception> continuation)
            where T : ITask
        {
            return (T)task.Finally(continuation, TaskAffinity.UI);
        }

        public static ITask FinallyInUI<T>(this ITask<T> task, Action<bool, Exception, T> continuation)
        {
            return task.Finally(continuation, TaskAffinity.UI);
        }

        public static ITask<T> FinallyInUI<T>(this ITask<T> task, Func<bool, Exception, T, T> continuation)
        {
            return task.Finally(continuation, TaskAffinity.UI);
        }

        public static ITask<T> Then<T>(this ITask task, Task<T> continuation, TaskAffinity affinity = TaskAffinity.Concurrent, bool always = false)
        {
            var cont = new FuncTask<T>(continuation) { Affinity = affinity, Name = $"ThenAsync<{typeof(T)}>" };
            return task.Then(cont, always);
        }

        public static ITask<T> Then<T>(this ITask task, Func<Task<T>> continuation, TaskAffinity affinity = TaskAffinity.Concurrent, bool always = false)
        {
            return task.Then(continuation(), affinity, always);
        }

        public static Task<T> StartAsAsync<T>(this ITask<T> task)
        {
            var tcs = new TaskCompletionSource<T>();
            task.Finally((s, e, d) =>
            {
                if (!s)
                {
                    if (e == null) // this should never happen
                        e = new InvalidOperationException("Task failed but there's no exception?");
                    tcs.SetException(e);
                }
                else
                    tcs.SetResult(d);
            });
            task.Start();
            return tcs.Task;
        }

        public static Task<bool> StartAsAsync(this ITask task)
        {
            var tcs = new TaskCompletionSource<bool>();
            task.Finally((s, e) =>
            {
                if (!s)
                {
                    if (e == null) // this should never happen
                        e = new InvalidOperationException("Task failed but there's no exception?");
                    tcs.SetException(e);
                }
                else
                    tcs.SetResult(s);
            });
            task.Start();
            return tcs.Task;
        }
    }
}