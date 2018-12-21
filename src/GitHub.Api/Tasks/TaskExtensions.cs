using GitHub.Logging;
using System;
using System.Threading.Tasks;

namespace GitHub.Unity
{
    public static class TaskExtensions
    {
        public static async Task StartAwait(this ITask source, Action<Exception> handler = null)
        {
            try
            {
                await source.StartAsAsync();
            }
            catch (Exception ex)
            {
                LogHelper.GetLogger().Error(ex);
                if (handler == null)
                    throw;
                handler(ex);
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
                LogHelper.GetLogger().Error(ex);
                if (handler == null)
                    throw;
                return handler(ex);
            }
        }

        public static ITask Then(this ITask task, Action continuation, TaskAffinity affinity = TaskAffinity.Concurrent, TaskRunOptions runOptions = TaskRunOptions.OnSuccess)
        {
            Guard.ArgumentNotNull(continuation, "continuation");
            return task.Then(new ActionTask(task.Token, _ => continuation()) { Affinity = affinity, Name = "Then" }, runOptions);
        }

        public static ITask Then(this ITask task, Action<bool> continuation, TaskAffinity affinity = TaskAffinity.Concurrent, TaskRunOptions runOptions = TaskRunOptions.OnSuccess)
        {
            Guard.ArgumentNotNull(continuation, "continuation");
            return task.Then(new ActionTask(task.Token, continuation) { Affinity = affinity, Name = "Then" }, runOptions);
        }

        public static ITask Then<T>(this ITask<T> task, Action<bool, T> continuation, TaskAffinity affinity = TaskAffinity.Concurrent, TaskRunOptions runOptions = TaskRunOptions.OnSuccess)
        {
            Guard.ArgumentNotNull(continuation, "continuation");
            return task.Then(new ActionTask<T>(task.Token, continuation) { Affinity = affinity, Name = $"Then<{typeof(T)}>" }, runOptions);
        }

        public static ITask<T> Then<T>(this ITask task, Func<bool, T> continuation, TaskAffinity affinity = TaskAffinity.Concurrent, TaskRunOptions runOptions = TaskRunOptions.OnSuccess)
        {
            Guard.ArgumentNotNull(continuation, "continuation");
            return task.Then(new FuncTask<T>(task.Token, continuation) { Affinity = affinity, Name = $"Then<{typeof(T)}>" }, runOptions);
        }

        public static ITask<TRet> Then<T, TRet>(this ITask<T> task, Func<bool, T, TRet> continuation, TaskAffinity affinity = TaskAffinity.Concurrent, TaskRunOptions runOptions = TaskRunOptions.OnSuccess)
        {
            Guard.ArgumentNotNull(continuation, "continuation");
            return task.Then(new FuncTask<T, TRet>(task.Token, continuation) { Affinity = affinity, Name = $"Then<{typeof(T)}, {typeof(TRet)}>" }, runOptions);
        }

        public static ITask<T> Then<T>(this ITask task, Task<T> continuation, TaskAffinity affinity = TaskAffinity.Concurrent, TaskRunOptions runOptions = TaskRunOptions.OnSuccess)
        {
            var cont = new TPLTask<T>(continuation) { Affinity = affinity, Name = $"ThenAsync<{typeof(T)}>" };
            return task.Then(cont, runOptions);
        }

        public static ITask ThenInUI(this ITask task, Action continuation, TaskRunOptions runOptions = TaskRunOptions.OnSuccess)
        {
            return task.Then(continuation, TaskAffinity.UI, runOptions);
        }

        public static ITask ThenInUI(this ITask task, Action<bool> continuation, TaskRunOptions runOptions = TaskRunOptions.OnSuccess)
        {
            return task.Then(continuation, TaskAffinity.UI, runOptions);
        }

        public static ITask ThenInUI<T>(this ITask<T> task, Action<bool, T> continuation, TaskRunOptions runOptions = TaskRunOptions.OnSuccess)
        {
            return task.Then(continuation, TaskAffinity.UI, runOptions);
        }

        public static ITask<TRet> ThenInUI<T, TRet>(this ITask<T> task, Func<bool, T, TRet> continuation, TaskRunOptions runOptions = TaskRunOptions.OnSuccess)
        {
            return task.Then(continuation, TaskAffinity.UI, runOptions);
        }

        public static ITask FinallyInUI<T>(this T task, Action<bool, Exception> continuation)
            where T : ITask
        {
            return task.Finally(continuation, TaskAffinity.UI);
        }

        public static ITask FinallyInUI<T>(this ITask<T> task, Action<bool, Exception, T> continuation)
        {
            return task.Finally(continuation, TaskAffinity.UI);
        }

        public static Task<T> StartAsAsync<T>(this ITask<T> task)
        {
            var tcs = new TaskCompletionSource<T>();
            task.Finally((success, r) =>
            {
                tcs.TrySetResult(r);
            });
            task.Catch(e =>
            {
                tcs.TrySetException(e);
            });
            task.Start();
            return tcs.Task;
        }

        public static Task<bool> StartAsAsync(this ITask task)
        {
            var tcs = new TaskCompletionSource<bool>();
            task.Finally(success =>
            {
                tcs.TrySetResult(success);
            });
            task.Catch(e =>
            {
                tcs.TrySetException(e);
            });
            task.Start();
            return tcs.Task;
        }
    }
}
