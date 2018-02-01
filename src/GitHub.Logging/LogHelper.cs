using System;

namespace GitHub.Logging
{
    public static class LogHelper
    {
        private static readonly LogAdapterBase nullLogAdapter = new NullLogAdapter();

        private static bool tracingEnabled;
        public static bool TracingEnabled
        {
            get
            {
                return tracingEnabled;
            }
            set
            {
                if (tracingEnabled != value)
                {
                    tracingEnabled = value;
                    Instance?.Info("Trace Logging " + (value ? "Enabled" : "Disabled"));
                }
            }
        }

        private static LogAdapterBase logAdapter = nullLogAdapter;

        public static LogAdapterBase LogAdapter
        {
            get { return logAdapter; }
            set { logAdapter = value ?? nullLogAdapter; }
        }

        private static ILogging instance;

        public static ILogging Instance
        {
            get {
                if (instance == null)
                {
                    instance = GetLogger();
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
            return new LogFacade($"<{context ?? "Global"}>");
        }

        public static void Info(string s)
        {
            Instance.Info(s);
        }

        public static void Debug(string s)
        {
            Instance.Debug(s);
        }

        public static void Trace(string s)
        {
            Instance.Trace(s);
        }

        public static void Warning(string s)
        {
            Instance.Warning(s);
        }

        public static void Error(string s)
        {
            Instance.Error(s);
        }

        public static void Error(Exception exception)
        {
            Instance.Error(exception);
        }

        public static void Info(string format, params object[] objects)
        {
            Instance.Info(format, objects);
        }

        public static void Debug(string format, params object[] objects)
        {
            Instance.Debug(format, objects);
        }

        public static void Trace(string format, params object[] objects)
        {
            Instance.Trace(format, objects);
        }

        public static void Warning(string format, params object[] objects)
        {
            Instance.Warning(format, objects);
        }

        public static void Error(string format, params object[] objects)
        {
            Instance.Error(format, objects);
        }

        public static void Info(Exception ex, string s)
        {
            Instance.Info(ex, s);
        }

        public static void Debug(Exception ex, string s)
        {
            Instance.Debug(ex, s);
        }

        public static void Trace(Exception ex, string s)
        {
            Instance.Trace(ex, s);
        }

        public static void Warning(Exception ex, string s)
        {
            Instance.Warning(ex, s);
        }

        public static void Error(Exception ex, string s)
        {
            Instance.Error(ex, s);
        }
    }
}