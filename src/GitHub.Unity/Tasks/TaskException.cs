using System;
using System.Runtime.Serialization;

namespace GitHub.Unity
{
    [Serializable]
    public class TaskException : Exception
    {
        public TaskException()
        {
        }

        public TaskException(string message) : base(message)
        {
        }

        public TaskException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected TaskException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}