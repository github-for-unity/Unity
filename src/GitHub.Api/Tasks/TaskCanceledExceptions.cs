using System;
using System.Threading.Tasks;

namespace GitHub.Unity
{
    [Serializable]
    class DependentTaskFailedException : TaskCanceledException
    {
        public DependentTaskFailedException(ITask task, Exception ex) : base(ex.InnerException != null ? ex.InnerException.Message : ex.Message, ex.InnerException ?? ex)
        {}
    }

    [Serializable]
    class ProcessException : TaskCanceledException
    {
        public ProcessException(ITask process) : base(process.Errors)
        { }
    }
}