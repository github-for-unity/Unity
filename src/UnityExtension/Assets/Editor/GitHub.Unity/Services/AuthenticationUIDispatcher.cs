using System;

namespace GitHub.Unity
{
    class AuthenticationUIDispatcher : BaseUIDispatcher
    {
        protected override void Run(Action<bool> onClose)
        {
            Tasks.ScheduleMainThread(() => AuthenticationWindow.Open(onClose));
        }
    }
}