using System;
using System.Runtime.Serialization;

namespace GitHub.Unity
{
    [Serializable]
    class ProcessException : OperationCanceledException
    {
        public int ErrorCode { get; }
        public string[] EnvironmentVariables { get; set; }

        public ProcessException(int errorCode, string message) : base(message)
        {
            ErrorCode = errorCode;
        }
        public ProcessException(int errorCode, string message, Exception innerException) : base(message, innerException)
        {
            ErrorCode = errorCode;
        }
        public ProcessException(string message, Exception innerException) : base(message, innerException)
        { }
        protected ProcessException(SerializationInfo info, StreamingContext context) : base(info, context)
        { }
    }
}
