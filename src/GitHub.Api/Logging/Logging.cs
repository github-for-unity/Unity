using System;

namespace GitHub.Unity
{
    public static class Logging
    {
        private static Func<string, ILogging> loggerFactory;

        public static Func<string, ILogging> LoggerFactory
        {
            get { return loggerFactory; }
            set
            {
                loggerFactory = value;
                Instance = loggerFactory(null);
            }
        }

        private static ILogging instance;

        private static ILogging Instance
        {
            get {
                if (instance == null)
                {
                    instance = loggerFactory(null);
                }
                return instance;
            }
            set { instance = value; }
        }

        public static ILogging GetLogger<T>()
        {
            return GetLogger(typeof(T));
        }

        public static ILogging GetLogger(Type type)
        {
            return GetLogger(type.Name);
        }

        public static ILogging GetLogger(string context = null)
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