using System;
using System.Threading;
using UnityEditor;

namespace GitHub.Unity
{
    class MainThreadSynchronizationContext : SynchronizationContext, IMainThreadSynchronizationContext
    {
        public void Schedule(Action action)
        {
            Guard.ArgumentNotNull(action, "action");
            Post(_ => action.SafeInvoke(), null);
        }

        public override void Post(SendOrPostCallback d, object state)
        {
            if (d == null)
                return;

            EditorApplication.delayCall += () => d(state);
        }
    }
}