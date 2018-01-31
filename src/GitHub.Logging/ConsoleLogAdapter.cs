using System;
using System.Threading;

namespace GitHub.Logging
{
    class ConsoleLogAdapter : LogAdapterBase
    {
        private string GetMessage(string level, string context, string message)
        {
            var time = DateTime.Now.ToString("HH:mm:ss.fff tt");
            var threadId = Thread.CurrentThread.ManagedThreadId;
            return string.Format("{0} {1} [{2,2}] {3} {4}", time, level, threadId, context, message);
        }

        public override void Info(string context, string message)
        {
            WriteLine("INFO", context, message);
        }

        public override void Debug(string context, string message)
        {
            WriteLine("DEBUG", context, message);
        }

        public override void Trace(string context, string message)
        {
            WriteLine("TRACE", context, message);
        }

        public override void Warning(string context, string message)
        {
            WriteLine("WARN", context, message);
        }

        public override void Error(string context, string message)
        {
            WriteLine("ERROR", context, message);
        }

        private void WriteLine(string level, string context, string message)
        {
            Console.WriteLine(GetMessage(level, context, message));
        }
    }
}