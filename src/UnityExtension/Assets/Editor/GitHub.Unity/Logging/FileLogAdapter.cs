using System;
using System.IO;
using System.Linq;
using System.Threading;

namespace GitHub.Unity
{
    class FileLogAdapter : ILogging
    {
        private static readonly object lk = new object();
        private readonly string contextPrefix;
        private readonly string filePath;

        public FileLogAdapter(string path, string context)
        {
            if (String.IsNullOrEmpty(context))
            {
                contextPrefix = string.Empty;
            }
            else
            {
                contextPrefix = string.Format("<{0}> ", context);
            }

            filePath = path;
        }

        public void Info(string message)
        {
            WriteLine("INFO " + message);
        }

        public void Info(string format, params object[] objects)
        {
            Info(String.Format(format, objects));
        }

        public void Info(Exception ex, string message)
        {
            var exceptionMessage = GetExceptionMessage(ex);
            Info(string.Concat(message, Environment.NewLine, exceptionMessage));
        }

        public void Info(Exception ex)
        {
            Info(ex, string.Empty);
        }

        public void Info(Exception ex, string format, params object[] objects)
        {
            Info(ex, String.Format(format, objects));
        }

        public void Debug(string message)
        {
#if DEBUG
            WriteLine("DEBUG " + message);
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
            Debug(string.Concat(message, Environment.NewLine, exceptionMessage));
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

        public void Warning(string message)
        {
            WriteLine("WARN " + message);
        }

        public void Warning(string format, params object[] objects)
        {
            Warning(String.Format(format, objects));
        }

        public void Warning(Exception ex, string message)
        {
            var exceptionMessage = GetExceptionMessage(ex);
            Warning(string.Concat(message, Environment.NewLine, exceptionMessage));
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
            WriteLine("ERROR " + message);
        }

        public void Error(string format, params object[] objects)
        {
            Error(String.Format(format, objects));
        }

        public void Error(Exception ex, string message)
        {
            var exceptionMessage = GetExceptionMessage(ex);
            Error(string.Concat(message, Environment.NewLine, exceptionMessage));
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

        private void Write(string message)
        {
            lock(lk)
            {
                try
                {
                    File.AppendAllText(filePath, message);
                }
                catch
                {}
            }
        }

        private void WriteLine(string message)
        {
            message = String.Concat(Prefix, message, Environment.NewLine);
            Write(message);
        }

        private string Prefix
        {
            get
            {
                var time = DateTime.Now.ToString("HH:mm:ss tt");
                var threadId = Thread.CurrentThread.ManagedThreadId;
                return string.Format("{0} [{1,2}] {2,-35}", time, threadId, contextPrefix);
            }
        }
    }
}
