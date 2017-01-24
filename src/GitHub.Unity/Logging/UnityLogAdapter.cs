using System;
using UnityEngine;

namespace GitHub.Unity.Logging
{
    public class UnityLogAdapter : ILogger
    {
        private readonly string _contextPrefix;

        private string Prefix
        {
            get
            {
                var time = DateTime.Now.ToString("HH:mm:ss tt");
                var threadId = System.Threading.Thread.CurrentThread.ManagedThreadId;
                return string.Format("{0} [{1}] <{2}> ", time, threadId, _contextPrefix);
            }
        }

        static UnityLogAdapter()
        {
            Application.SetStackTraceLogType(LogType.Log, StackTraceLogType.Full);
            Application.SetStackTraceLogType(LogType.Warning, StackTraceLogType.Full);
            Application.SetStackTraceLogType(LogType.Error, StackTraceLogType.Full);
        }

        public UnityLogAdapter(string context)
        {
            _contextPrefix = string.IsNullOrEmpty(context) 
                ? "GitHub" 
                : string.Format("GitHub:{0}", context);
        }

        public UnityLogAdapter() : this(string.Empty)
        {
        }

        public void Info(string message)
        {
            UnityEngine.Debug.Log(Prefix + message);
        }

        public void Info(string format, params object[] objects)
        {
            UnityEngine.Debug.LogFormat(Prefix + format, objects);
        }

        public void Debug(string message)
        {
#if DEBUG
            UnityEngine.Debug.Log(Prefix + message);
#endif
        }

        public void Debug(string format, params object[] objects)
        {
#if DEBUG
            UnityEngine.Debug.LogFormat(Prefix + format, objects);
#endif
        }

        public void Warning(string message)
        {
            UnityEngine.Debug.LogWarning(Prefix + message);
        }

        public void Warning(string format, params object[] objects)
        {
            UnityEngine.Debug.LogWarningFormat(Prefix + format, objects);
        }

        public void Error(string message)
        {
            UnityEngine.Debug.LogError(Prefix + message);
        }

        public void Error(string format, params object[] objects)
        {
            UnityEngine.Debug.LogErrorFormat(Prefix + format, objects);
        }
    }
}