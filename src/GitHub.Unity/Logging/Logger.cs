using System;

namespace GitHub.Unity.Logging
{
    public static class Logger
    {
        public static ILogger Instance { get; private set; }

        public static Func<string, ILogger> LoggerFactory
        {
            get { return _loggerFactory; }
            set
            {
                _loggerFactory = value;
                Instance = _loggerFactory(null);
            }
        }

        private static Func<string, ILogger> _loggerFactory = s => new UnityLogAdapter(s);

        static Logger()
        {
            Instance = _loggerFactory(null);
        }

        public static ILogger GetLogger<T>()
        {
            return GetLogger(typeof(T));
        }

        public static ILogger GetLogger(Type type)
        {
            return GetLogger(type.Name);
        }

        public static ILogger GetLogger(string context = null)
        {
            return _loggerFactory(context);
        }

        public static void Log(string s)
        {
            Instance.Log(s);
        }

        public static void LogWarning(string s)
        {
            Instance.LogWarning(s);
        }

        public static void LogError(string s)
        {
            Instance.LogError(s);
        }

        public static void LogFormat(string format, params object[] objects)
        {
            Instance.LogFormat(format, objects);
        }

        public static void LogWarningFormat(string format, params object[] objects)
        {
            Instance.LogWarningFormat(format, objects);
        }

        public static void LogErrorFormat(string format, params object[] objects)
        {
            Instance.LogErrorFormat(format, objects);
        }
    }
}