using System;

namespace GitHub.Unity.Logging
{
    static class Logger
    {
        private static Func<string, ILogger> loggerFactory = s => new UnityLogAdapter(s);

        public static Func<string, ILogger> LoggerFactory
        {
            get { return loggerFactory; }
            set
            {
                loggerFactory = value;
                Instance = loggerFactory(null);
            }
        }

        private static ILogger instance;

        private static ILogger Instance
        {
            get { return instance; }
            set
            {
                if (instance == null)
                {
                    instance = loggerFactory(null);
                }
                instance = value;
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
            return loggerFactory(context);
        }

        public static void Info(string s)
        {
            Instance.Info(s);
        }

        public static void Debug(string s)
        {
            Instance.Debug(s);
        }

        public static void Warning(string s)
        {
            Instance.Warning(s);
        }

        public static void Error(string s)
        {
            Instance.Error(s);
        }

        public static void Debug(string format, params object[] objects)
        {
            Instance.Debug(format, objects);
        }

        public static void Warning(string format, params object[] objects)
        {
            Instance.Warning(format, objects);
        }

        public static void Error(string format, params object[] objects)
        {
            Instance.Error(format, objects);
        }
    }
}