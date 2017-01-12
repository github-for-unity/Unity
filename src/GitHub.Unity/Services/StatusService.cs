using System;

namespace GitHub.Unity
{
    class StatusService
    {
        event Action<GitStatus> statusUpdated;

        public static StatusService Instance { get; private set; }

        public static void Initialize()
        {
            Instance = new StatusService();
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
            statusUpdated?.Invoke(status);
        }
    }
}
