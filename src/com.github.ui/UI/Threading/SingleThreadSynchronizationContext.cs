using System;
using System.Threading;
using Unity.VersionControl.Git;
using UnityEditor;

namespace GitHub.Unity
{
    public class MainThreadSynchronizationContext : SynchronizationContext, IMainThreadSynchronizationContext
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
