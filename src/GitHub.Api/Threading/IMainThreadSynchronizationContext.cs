using System;

namespace GitHub.Unity
{
    interface IMainThreadSynchronizationContext
    {
        void Schedule(Action action);
    }
}