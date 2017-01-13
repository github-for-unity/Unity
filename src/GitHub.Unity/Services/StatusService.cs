using System;

namespace GitHub.Unity
{
    class StatusService
    {
        event Action<GitStatus> statusUpdated;

        private static StatusService instance;
        public static StatusService Instance
        {
            get
            {
                if (instance == null)
                    instance = new StatusService();
                return instance;
            }
            set { instance = value; }
        }

        public static void Shutdown()
        {
            Instance = null;
        }

        public void Run()
        {
            GitStatusTask.Schedule(InternalInvoke);
        }

        public void RegisterCallback(Action<GitStatus> callback)
        {
            statusUpdated += callback;
        }

        public void UnregisterCallback(Action<GitStatus> callback)
        {
            statusUpdated -= callback;
        }

        private void InternalInvoke(GitStatus status)
        {
            statusUpdated.Invoke(status);
        }
    }
}
