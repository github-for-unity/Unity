using GitHub.Logging;
using System;
using System.Threading;

namespace GitHub.Unity
{
    class UnityLogAdapter : LogAdapterBase
    {
        private string GetMessage(string context, string message)
        {
            var time = DateTime.Now.ToString("HH:mm:ss tt");
            var threadId = Thread.CurrentThread.ManagedThreadId;
            return string.Format("{0} [{1,2}] {2} {3}", time, threadId, context, message);
        }

        public override void Info(string context, string message)
        {
            UnityEngine.Debug.Log(GetMessage(context, message));
        }

        public override void Debug(string context, string message)
        {
            UnityEngine.Debug.Log(GetMessage(context, message));
        }

        public override void Trace(string context, string message)
        {
            UnityEngine.Debug.Log(GetMessage(context, message));
        }

        public override void Warning(string context, string message)
        {
            UnityEngine.Debug.LogWarning(GetMessage(context, message));
        }

        public override void Error(string context, string message)
        {
            UnityEngine.Debug.LogError(GetMessage(context, message));
        }
    }
}