using System;

namespace GitHub.Unity
{
    interface ITaskResultDispatcher
    {
        void ReportSuccess(Action callback);
        void ReportFailure(FailureSeverity severity, string title, string error);
        void ReportFailure(Action callback);
    }
}