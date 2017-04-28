using System;
using System.Threading;

namespace GitHub.Unity
{
    class UnityLogAdapter : LogAdapterBase
    {
        public UnityLogAdapter(string context) : base(context)
        {}

        private string GetMessage(string message)
        {
            var time = DateTime.Now.ToString("HH:mm:ss tt");
            var threadId = Thread.CurrentThread.ManagedThreadId;
            return string.Format("{0} [{1,2}] {2} {3}", time, threadId, ContextPrefix, message);
        }


        protected override void OnInfo(string message)
        {
            UnityEngine.Debug.Log(GetMessage(message));
        }

        protected override void OnDebug(string message)
        {
            UnityEngine.Debug.Log(GetMessage(message));
        }

        protected override void OnTrace(string message)
        {
            UnityEngine.Debug.Log(GetMessage(message));
        }

        protected override void OnWarning(string message)
        {
            UnityEngine.Debug.LogWarning(GetMessage(message));
        }

        protected override void OnError(string message)
        {
            UnityEngine.Debug.LogError(GetMessage(message));
        }
    }
}