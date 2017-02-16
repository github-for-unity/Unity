using System;
using System.Threading;

namespace GitHub.Unity
{
    class ConsoleLogAdapter : LogAdapterBase
    {
        public ConsoleLogAdapter(string context) : base(context)
        {}

        private string GetMessage(string level, string message)
        {
            var time = DateTime.Now.ToString("HH:mm:ss tt");
            var threadId = Thread.CurrentThread.ManagedThreadId;
            return string.Format("{0} {1} [{2,2}] {3} {4}", time, level, threadId, ContextPrefix, message);
        }

        public override void Info(string message)
        {
            WriteLine("INFO", message);
        }

        public override void Debug(string message)
        {
#if DEBUG
            WriteLine("DEBUG", message);
#endif
        }

        public override void Trace(string message)
        {
#if DEBUG
            WriteLine("TRACE", message);
#endif
        }

        public override void Warning(string message)
        {
            WriteLine("WARN", message);
        }

        public override void Error(string message)
        {
            WriteLine("ERROR", message);
        }

        private void WriteLine(string level, string message)
        {
            Console.WriteLine(GetMessage(level, message));
        }
    }
}