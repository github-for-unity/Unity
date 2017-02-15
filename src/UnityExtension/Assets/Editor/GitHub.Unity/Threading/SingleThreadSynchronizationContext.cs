using GitHub.Api;
using System;
using System.Threading;
using UnityEditor;

namespace GitHub.Unity
{
    class MainThreadSynchronizationContext : SynchronizationContext
    {
        private static readonly ILogging logger = Logging.GetLogger<MainThreadSynchronizationContext>();

        public override void Post(SendOrPostCallback d, object state)
        {
            if (d == null)
                return;

            EditorApplication.delayCall += () => d(state);
        }

        public virtual void Schedule(Action action)
        {
            //logger.Debug("Scheduling action");

            Guard.ArgumentNotNull(action, "action");
            Post(_ => action.SafeInvoke(), null);
        }
    }
}