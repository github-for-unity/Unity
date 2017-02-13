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
            return string.Format("{0} {1} {2,-35} [{3,2}] {4}{5}", time, level, ContextPrefix, threadId, message, Environment.NewLine);
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
