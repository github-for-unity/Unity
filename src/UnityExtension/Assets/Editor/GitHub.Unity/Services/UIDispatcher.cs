using System;
using System.Threading.Tasks;

namespace GitHub.Unity
{
    class AuthenticationUIDispatcher : BaseUIDispatcher
    {
        protected override void Run(Action onClose)
        {
            Tasks.ScheduleMainThread(() => AuthenticationWindow.Open(onClose));
        }
    }
}