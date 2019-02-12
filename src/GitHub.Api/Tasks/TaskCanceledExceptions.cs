using System;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace GitHub.Unity
{
    [Serializable]
    class DependentTaskFailedException : TaskCanceledException
    {
        protected DependentTaskFailedException() : base()
        { }
        protected DependentTaskFailedException(string message) : base(message)
        { }
        protected DependentTaskFailedException(string message, Exception innerException) : base(message, innerException)
        { }
        protected DependentTaskFailedException(SerializationInfo info, StreamingContext context) : base(info, context)
        { }

        public DependentTaskFailedException(ITask task, Exception ex) : this(ex.InnerException != null ? ex.InnerException.Message : ex.Message, ex.InnerException ?? ex)
        {}
    }
}
