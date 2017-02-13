using System;

namespace GitHub.Unity
{
    interface ITaskResultDispatcher
    {
        void ReportSuccess(Action callback);
        void ReportFailure(FailureSeverity severity, ITask task, string error);
        void ReportFailure(Action callback);
    }
}