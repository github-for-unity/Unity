using GitHub.Api;
using System;
using System.IO;
using System.Linq;
using UnityEngine;

namespace GitHub.Unity
{
    class FileLogAdapter : ILogging
    {
        private static readonly object lk = new object();
        private readonly string contextPrefix;
        private readonly string filePath;

        private string Prefix
        {
            get
            {
                var time = DateTime.Now.ToString("HH:mm:ss tt");
                var threadId = System.Threading.Thread.CurrentThread.ManagedThreadId;
                return string.Format("{0} [{1,2}] {2,-35}", time, threadId, contextPrefix);
            }
        }

        public FileLogAdapter(string path, string context)
        {
            contextPrefix = string.Empty;
            if (context != null)
            {
                contextPrefix = string.Format("<{0}> ", context);
            }

            filePath = path;
        }

        private void Write(string message)
        {
            lock (lk)
            {
                try
                {
                    File.AppendAllText(filePath, message);
                }
                catch { }
            }
        }

        private void WriteLine(string message)
        {
            message = Prefix + message + Environment.NewLine;
            Write(message);
            ;
        }

        private void WriteLine(string format, params object[] objects)
        {
            WriteLine(String.Format(format, objects));
        }

        public void Info(string message)
        {
            WriteLine("INFO " + message);
        }

        public void Info(string format, params object[] objects)
        {
            WriteLine("INFO " + format, objects);
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
            WriteLine("DEBUG " + format, objects);
#endif
        }

        public void Debug(Exception ex)
        {
#if DEBUG
            var message = "DEBUG " + ex.Message + Environment.NewLine + ex.StackTrace;
            var caller = Environment.StackTrace;
            var stack = caller.Split(new string[] { Environment.NewLine }, StringSplitOptions.None);
            if (stack.Length > 2)
                message = message + Environment.NewLine + String.Join(Environment.NewLine, stack.Skip(2).ToArray());
            WriteLine(message);
#endif
        }

        public void Warning(string message)
        {
            WriteLine("WARN " + message);
        }

        public void Warning(string format, params object[] objects)
        {
            WriteLine("WARN " + format, objects);
        }

        public void Error(string message)
        {
            WriteLine("ERROR " + message);
        }

        public void Error(string format, params object[] objects)
        {
            WriteLine("ERROR " + format, objects);
        }
    }
}