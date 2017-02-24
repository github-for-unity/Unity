#define ENABLE_TRACE
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

        public abstract void Info(string message);

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

        public abstract void Debug(string message);

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

        public abstract void Trace(string message);

        public void Trace(string format, params object[] objects)
        {
#if ENABLE_TRACE
            Trace(String.Format(format, objects));
#endif
        }

        public void Trace(Exception ex, string message)
        {
#if ENABLE_TRACE
            var exceptionMessage = GetExceptionMessage(ex);
            Trace(String.Concat(message, Environment.NewLine, (string)exceptionMessage));
#endif
        }

        public void Trace(Exception ex)
        {
#if ENABLE_TRACE
            Trace(ex, string.Empty);
#endif
        }

        public void Trace(Exception ex, string format, params object[] objects)
        {
#if ENABLE_TRACE
            Trace(ex, String.Format(format, objects));
#endif
        }

        public abstract void Warning(string message);

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

        public abstract void Error(string message);

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