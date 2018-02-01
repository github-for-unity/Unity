using System;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace GitHub.Unity
{
    static class TaskHelpers
    {
        public static Task<T> GetCompletedTask<T>(T result)
        {
            return TaskEx.FromResult(result);
        }

        public static Task<T> ToTask<T>(this Exception exception)
        {
          TaskCompletionSource<T> completionSource = new TaskCompletionSource<T>();
          completionSource.TrySetException(exception);
          return completionSource.Task;
        }
    }

    [Serializable]
    public class NotReadyException : Exception
    {
        public NotReadyException() : base()
        { }
        public NotReadyException(string message) : base(message)
        { }
        public NotReadyException(string message, Exception innerException) : base(message, innerException)
        { }
        protected NotReadyException(SerializationInfo info, StreamingContext context) : base(info, context)
        { }
    }
}