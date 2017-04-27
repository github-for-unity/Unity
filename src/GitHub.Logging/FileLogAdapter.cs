using System;
using System.IO;
using System.Threading;

namespace GitHub.Unity
{
    class FileLogAdapter : LogAdapterBase
    {
        private static readonly object lk = new object();
        private readonly string filePath;

        public FileLogAdapter(string path, string context) : base(context)
        {
            filePath = path;
        }

        private string GetMessage(string level, string message)
        {
            var time = DateTime.Now.ToString("HH:mm:ss tt");
            var threadId = Thread.CurrentThread.ManagedThreadId;
            return string.Format("{0} {1} [{2,2}] {3,-35} {4}{5}", time, level, threadId, ContextPrefix, message, Environment.NewLine);
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
            Write(GetMessage(level, message));
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
    }
}
