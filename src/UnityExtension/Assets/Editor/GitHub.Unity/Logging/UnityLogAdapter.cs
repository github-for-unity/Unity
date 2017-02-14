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


        public override void Info(string message)
        {
            UnityEngine.Debug.Log(GetMessage(message));
        }

        public override void Debug(string message)
        {
            UnityEngine.Debug.Log(GetMessage(message));
        }

        public override void Trace(string message)
        {
            UnityEngine.Debug.Log(GetMessage(message));
        }

        public override void Warning(string message)
        {
            UnityEngine.Debug.LogWarning(GetMessage(message));
        }

        public override void Error(string message)
        {
            UnityEngine.Debug.LogError(GetMessage(message));
        }
    }
}