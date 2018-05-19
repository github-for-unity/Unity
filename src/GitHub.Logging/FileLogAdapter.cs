using System;
using System.IO;
using System.Threading;

namespace GitHub.Logging
{
    public class FileLogAdapter : LogAdapterBase
    {
        private static readonly object lk = new object();
        private readonly string filePath;

        public FileLogAdapter(string path)
        {
            filePath = path;
        }

        private string GetMessage(string level, string context, string message)
        {
            var time = DateTime.Now.ToString("yyMMdd-HH:mm:ss.fff");
            var threadId = Thread.CurrentThread.ManagedThreadId;
            return string.Format("{0} {1,5} [{2,2}] {3,-35} {4}{5}", time, level, threadId, context, message, Environment.NewLine);
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
            Write(GetMessage(level, context, message));
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
