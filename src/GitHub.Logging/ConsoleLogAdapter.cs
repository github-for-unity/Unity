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
            var time = DateTime.Now.ToString("HH:mm:ss.fff tt");
            var threadId = Thread.CurrentThread.ManagedThreadId;
            return string.Format("{0} {1} [{2,2}] {3} {4}", time, level, threadId, ContextPrefix, message);
        }

        protected override void OnInfo(string message)
        {
            WriteLine("INFO", message);
        }

        protected override void OnDebug(string message)
        {
            WriteLine("DEBUG", message);
        }

        protected override void OnTrace(string message)
        {
            WriteLine("TRACE", message);
        }

        protected override void OnWarning(string message)
        {
            WriteLine("WARN", message);
        }

        protected override void OnError(string message)
        {
            WriteLine("ERROR", message);
        }

        private void WriteLine(string level, string message)
        {
            Console.WriteLine(GetMessage(level, message));
        }
    }
}