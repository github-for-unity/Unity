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
            Logger.Log(_logPrefix + message);
        }

        public void LogWarning(string message)
        {
            Logger.LogWarning(_logPrefix + message);
        }

        public void LogError(string message)
        {
            Logger.LogError(_logPrefix + message);
        }

        public void LogFormat(string format, params object[] objects)
        {
            Logger.LogFormat(_logPrefix + format, objects);
        }

        public void LogWarningFormat(string format, params object[] objects)
        {
            Logger.LogWarningFormat(_logPrefix + format, objects);
        }

        public void LogErrorFormat(string format, params object[] objects)
        {
            Logger.LogErrorFormat(_logPrefix + format, objects);
        }
    }
}