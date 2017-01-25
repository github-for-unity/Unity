using System;

namespace GitHub.Unity.Logging
{
    static class Logger
    {
        private static Func<string, ILogger> _loggerFactory = s => new UnityLogAdapter(s);

        public static Func<string, ILogger> LoggerFactory
        {
            get { return _loggerFactory; }
            set
            {
                _loggerFactory = value;
                Instance = _loggerFactory(null);
            }
        }

        private static ILogger _instance;

        private static ILogger Instance
        {
            get { return _instance; }
            set
            {
                if (_instance == null)
                {
                    _instance = _loggerFactory(null);
                }
                _instance = value;
            }
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
            Instance.Debug(s);
        }

        public static void LogWarning(string s)
        {
            Instance.Warning(s);
        }

        public static void LogError(string s)
        {
            Instance.Error(s);
        }

        public static void LogFormat(string format, params object[] objects)
        {
            Instance.Debug(format, objects);
        }

        public static void LogWarningFormat(string format, params object[] objects)
        {
            Instance.Warning(format, objects);
        }

        public static void LogErrorFormat(string format, params object[] objects)
        {
            Instance.Error(format, objects);
        }
    }
}