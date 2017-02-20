using System;

namespace GitHub.Unity
{
    interface ITaskResultDispatcher
    {
        ITaskResultDispatcher<T> GetDispatcher<T>(Action<T> onSuccess, Action onFailure = null);
        void ReportFailure(FailureSeverity severity, string title, string error);
    }

    interface ITaskResultDispatcher<T>
    {
        void ReportSuccess(T data);
        void ReportFailure(FailureSeverity severity, string title, string error);
        void ReportFailure();
    }
}