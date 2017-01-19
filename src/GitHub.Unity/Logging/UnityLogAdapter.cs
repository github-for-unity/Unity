using System;

namespace GitHub.Unity.Logging
{
    public class UnityLogAdapter : ILogger
    {
        private readonly string _logPrefix;

        public UnityLogAdapter(string context)
        {
            _logPrefix = string.IsNullOrEmpty(context) 
                ? "GitHub: " 
                : string.Format("GitHub:{0}: ", context);
        }

        public UnityLogAdapter() : this(string.Empty)
        {
        }

        public void Log(string message)
        {
            UnityEngine.Debug.Log(_logPrefix + message);
        }

        public void LogWarning(string message)
        {
            UnityEngine.Debug.LogWarning(_logPrefix + message);
        }

        public void LogError(string message)
        {
            UnityEngine.Debug.LogError(_logPrefix + message);
        }

        public void LogFormat(string format, params object[] objects)
        {
            UnityEngine.Debug.LogFormat(_logPrefix + format, objects);
        }

        public void LogWarningFormat(string format, params object[] objects)
        {
            UnityEngine.Debug.LogWarningFormat(_logPrefix + format, objects);
        }

        public void LogErrorFormat(string format, params object[] objects)
        {
            UnityEngine.Debug.LogErrorFormat(_logPrefix + format, objects);
        }
    }
}