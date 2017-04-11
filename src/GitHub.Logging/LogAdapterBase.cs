using System;
using System.Linq;

namespace GitHub.Unity
{
    abstract class LogAdapterBase : ILogging
    {
        protected string ContextPrefix { get; }

        protected LogAdapterBase(string context)
        {
            if (String.IsNullOrEmpty(context))
            {
                ContextPrefix = string.Empty;
            }
            else
            {
                ContextPrefix = string.Format("<{0}>", context);
            }
        }

        protected abstract void OnInfo(string message);

        public void Info(string message)
        {
            OnInfo(message);
        }

        public void Info(string format, params object[] objects)
        {
            Info(String.Format(format, objects));
        }

        public void Info(Exception ex, string message)
        {
            var exceptionMessage = GetExceptionMessage(ex);
            Info(String.Concat(message, Environment.NewLine, (string)exceptionMessage));
        }

        public void Info(Exception ex)
        {
            Info(ex, string.Empty);
        }

        public void Info(Exception ex, string format, params object[] objects)
        {
            Info(ex, String.Format(format, objects));
        }

        protected abstract void OnDebug(string message);

        public void Debug(string message)
        {
#if DEBUG
            OnDebug(message);
#endif
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
            var exceptionMessage = GetExceptionMessage(ex);
            Debug(String.Concat(message, Environment.NewLine, (string)exceptionMessage));
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

        protected abstract void OnTrace(string message);

        public void Trace(string message)
        {
            if (!Logging.TracingEnabled) return;

            OnTrace(message);
        }

        public void Trace(string format, params object[] objects)
        {
            if (!Logging.TracingEnabled) return;

            Trace(String.Format(format, objects));
        }

        public void Trace(Exception ex, string message)
        {
            if (!Logging.TracingEnabled) return;

            var exceptionMessage = GetExceptionMessage(ex);
            Trace(String.Concat(message, Environment.NewLine, (string)exceptionMessage));
        }

        public void Trace(Exception ex)
        {
            if (!Logging.TracingEnabled) return;

            Trace(ex, string.Empty);
        }

        public void Trace(Exception ex, string format, params object[] objects)
        {
            if (!Logging.TracingEnabled) return;

            Trace(ex, String.Format(format, objects));
        }

        protected abstract void OnWarning(string message);

        public void Warning(string message)
        {
            OnWarning(message);
        }

        public void Warning(string format, params object[] objects)
        {
            Warning(String.Format(format, objects));
        }

        public void Warning(Exception ex, string message)
        {
            var exceptionMessage = GetExceptionMessage(ex);
            Warning(String.Concat(message, Environment.NewLine, (string)exceptionMessage));
        }

        public void Warning(Exception ex)
        {
            Warning(ex, string.Empty);
        }

        public void Warning(Exception ex, string format, params object[] objects)
        {
            Warning(ex, String.Format(format, objects));
        }

        protected abstract void OnError(string message);

        public void Error(string message)
        {
            OnError(message);
        }

        public void Error(string format, params object[] objects)
        {
            Error(String.Format(format, objects));
        }

        public void Error(Exception ex, string message)
        {
            var exceptionMessage = GetExceptionMessage(ex);
            Error(String.Concat(message, Environment.NewLine, (string)exceptionMessage));
        }

        public void Error(Exception ex)
        {
            Error(ex, string.Empty);
        }

        public void Error(Exception ex, string format, params object[] objects)
        {
            Error(ex, String.Format(format, objects));
        }

        private static string GetExceptionMessage(Exception ex)
        {
            var message = ex.Message + Environment.NewLine + ex.StackTrace;
            var caller = Environment.StackTrace;
            var stack = caller.Split(new string[] { Environment.NewLine }, StringSplitOptions.None);
            if (stack.Length > 2)
            {
                message = message + Environment.NewLine + String.Join(Environment.NewLine, stack.Skip(2).ToArray());
            }
            return message;
        }
    }
}