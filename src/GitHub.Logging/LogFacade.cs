using System;

namespace GitHub.Logging
{
    class LogFacade : ILogging
    {
        private readonly string context;

        public LogFacade(string context)
        {
            this.context = context;
        }

        public void Info(string message)
        {
            LogHelper.LogAdapter.Info(context, message);
        }

        public void Debug(string message)
        {
#if DEBUG
            LogHelper.LogAdapter.Debug(context, message);
#endif
        }

        public void Trace(string message)
        {
            if (!LogHelper.TracingEnabled) return;
            LogHelper.LogAdapter.Trace(context, message);
        }

        public void Info(string format, params object[] objects)
        {
            Info(String.Format(format, objects));
        }

        public void Info(Exception ex, string message)
        {
            Info(String.Concat(message, Environment.NewLine, ex.GetExceptionMessage()));
        }

        public void Info(Exception ex)
        {
            Info(ex, string.Empty);
        }

        public void Info(Exception ex, string format, params object[] objects)
        {
            Info(ex, String.Format(format, objects));
        }

        public void Debug(string format, params object[] objects)
        {
#if DEBUG
            Debug(String.Format(format, objects));
#endif
        }

        public void Debug(Exception ex, string message)
        {
#if DEBUG
            Debug(String.Concat(message, Environment.NewLine, ex.GetExceptionMessage()));
#endif
        }

        public void Debug(Exception ex)
        {
#if DEBUG
            Debug(ex, string.Empty);
#endif
        }

        public void Debug(Exception ex, string format, params object[] objects)
        {
#if DEBUG
            Debug(ex, String.Format(format, objects));
#endif
        }

        public void Trace(string format, params object[] objects)
        {
            if (!LogHelper.TracingEnabled) return;

            Trace(String.Format(format, objects));
        }

        public void Trace(Exception ex, string message)
        {
            if (!LogHelper.TracingEnabled) return;
            Trace(String.IsNullOrEmpty(message) ? ex.GetExceptionMessage() : String.Concat(message, Environment.NewLine, ex.GetExceptionMessage()));
        }

        public void Trace(Exception ex)
        {
            if (!LogHelper.TracingEnabled) return;

            Trace(ex, string.Empty);
        }

        public void Trace(Exception ex, string format, params object[] objects)
        {
            if (!LogHelper.TracingEnabled) return;

            Trace(ex, String.Format(format, objects));
        }

        public void Warning(string message)
        {
            LogHelper.LogAdapter.Warning(context, message);
        }

        public void Warning(string format, params object[] objects)
        {
            Warning(String.Format(format, objects));
        }

        public void Warning(Exception ex, string message)
        {
            Warning(String.Concat(message, Environment.NewLine, ex.GetExceptionMessage()));
        }

        public void Warning(Exception ex)
        {
            Warning(ex, string.Empty);
        }

        public void Warning(Exception ex, string format, params object[] objects)
        {
            Warning(ex, String.Format(format, objects));
        }

        public void Error(string message)
        {
            LogHelper.LogAdapter.Error(context, message);
        }

        public void Error(string format, params object[] objects)
        {
            Error(String.Format(format, objects));
        }

        public void Error(Exception ex, string message)
        {
            Error(String.Concat(message, Environment.NewLine, ex.GetExceptionMessage()));
        }

        public void Error(Exception ex)
        {
            Error(ex, string.Empty);
        }

        public void Error(Exception ex, string format, params object[] objects)
        {
            Error(ex, String.Format(format, objects));
        }
    }
}
