using System;

namespace GitHub.Unity
{
    class TaskResultDispatcher : ITaskResultDispatcher
    {
        private static ITaskResultDispatcher instance;

        public static ITaskResultDispatcher Default
        {
            get
            {
                if (instance == null)
                    instance = new TaskResultDispatcher();
                return instance;
            }
            set
            {
                instance = value;
            }
        }

        public ITaskResultDispatcher<T> GetDispatcher<T>(Action<T> onSuccess, Action onFailure = null)
        {
            return new TaskResultDispatcher<T>(onSuccess, onFailure);
        }

        public void ReportFailure(FailureSeverity severity, string title, string error)
        {
        }
    }

    class TaskResultDispatcher<T> : ITaskResultDispatcher<T>
    {
        private readonly Action<T> successCallback;
        private readonly Action failureCallback;

        public TaskResultDispatcher(Action<T> successCallback, Action failureCallback = null)
        {
            this.successCallback = successCallback;
            this.failureCallback = failureCallback;
        }

        public void ReportSuccess(T data)
        {
            successCallback?.Invoke(data);
        }

        public void ReportFailure(FailureSeverity severity, string title, string error)
        {
        }

        public void ReportFailure()
        {
            failureCallback?.Invoke();
        }
    }
}