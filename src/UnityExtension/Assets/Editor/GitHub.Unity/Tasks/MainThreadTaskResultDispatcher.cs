using System;

namespace GitHub.Unity
{
    class MainThreadTaskResultDispatcher : ITaskResultDispatcher
    {
        public ITaskResultDispatcher<T> GetDispatcher<T>(Action<T> onSuccess, Action onFailure = null)
        {
            return new MainThreadTaskResultDispatcher<T>(onSuccess, onFailure);
        }

        public void ReportFailure(FailureSeverity severity, string title, string error)
        {
            TaskRunner.ReportFailure(severity, title, error);
        }

    }

    class MainThreadTaskResultDispatcher<T> : ITaskResultDispatcher<T>
    {
        private readonly Action<T> successCallback;
        private readonly Action failureCallback;

        public MainThreadTaskResultDispatcher(Action<T> successCallback, Action failureCallback = null)
        {
            this.successCallback = successCallback;
            this.failureCallback = failureCallback;
        }

        public void ReportFailure()
        {
            TaskRunner.ScheduleMainThread(failureCallback);
        }

        public void ReportFailure(FailureSeverity severity, string title, string error)
        {
            TaskRunner.ReportFailure(severity, title, error);
        }

        public void ReportSuccess(T data)
        {
            TaskRunner.ScheduleMainThread(() => successCallback.SafeInvoke(data));
        }
    }
}